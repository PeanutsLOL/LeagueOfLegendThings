using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    // Domination small runes:
    //   Row2: Sixth Sense (免死), Grisly Mementos (击杀叠伤), Deep Ward (Boss无伤闪避)
    //   Row3: Treasure/Relentless/Ultimate Hunter (猎人系叠层)
    public class DominationSmallRunesPlayer : ModPlayer
    {
        private const int GrislyMaxStacks = 10;
        private const int HunterMaxStacks = 5;

        private const float GrislyDamagePerStack = 0.008f; // 0.8% per stack
        private const float RelentlessMoveSpeedPerStack = 0.03f; // 3% per stack
        private const float UltimateHunterCDRPerStack = 0.04f; // 4% per stack

        private int grislyStacks;
        private int hunterStacks;
        private int outOfCombatTimer;

        // ── Sixth Sense (第六感): 每60秒免疫一次致命伤害 ──
        private const int SixthSenseCooldown = 60 * 60;
        private int sixthSenseCD;

        // ── Deep Ward (深入守卫): Boss战中无伤20秒 → 获得5秒闪避 ──
        private const int DeepWardNoHitRequired = 20 * 60;
        private const int DeepWardDodgeDuration = 5 * 60;
        private int deepWardNoHitTimer;
        private int deepWardDodgeTimer;
        private bool deepWardDodgeActive;

        public override void SaveData(TagCompound tag)
        {
            tag["grislyStacks"] = grislyStacks;
            tag["hunterStacks"] = hunterStacks;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("grislyStacks"))
                grislyStacks = tag.GetInt("grislyStacks");
            if (tag.ContainsKey("hunterStacks"))
                hunterStacks = tag.GetInt("hunterStacks");
        }

        // ── 叠层触发 ──

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, damageDone);
        }

        private void HandleHit(NPC target, int damageDone)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!(save.GrislyMementosSelected || HasHunterRuneSelected(save)))
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (damageDone > 0)
                outOfCombatTimer = 5 * 60;

            HandleKillStacks(save, target);
        }

        private void HandleKillStacks(RuneSaveSystem save, NPC target)
        {
            if (target.life > 0)
                return;

            if (save.GrislyMementosSelected)
            {
                if (target.lifeMax < 300)
                    return;
                int add = target.boss ? 2 : 1;
                grislyStacks = System.Math.Min(grislyStacks + add, GrislyMaxStacks);
            }

            if (HasHunterRuneSelected(save) && target.boss)
            {
                hunterStacks = System.Math.Min(hunterStacks + 1, HunterMaxStacks);
            }

            if (save.TreasureHunterSelected && hunterStacks > 0)
            {
                int extraCopper = (int)(target.value * 0.05f * hunterStacks);
                extraCopper = System.Math.Min(extraCopper, 10000);
                if (extraCopper > 0)
                    SpawnCoins(extraCopper, Player.GetSource_OnHit(target));
            }
        }

        // ── 每帧更新 ──

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();

            // 冷却 & 计时
            if (outOfCombatTimer > 0) outOfCombatTimer--;
            if (sixthSenseCD > 0) sixthSenseCD--;

            // Grisly Mementos: 击杀叠伤
            if (save.GrislyMementosSelected && grislyStacks > 0)
                Player.GetDamage(DamageClass.Generic) += GrislyDamagePerStack * grislyStacks;

            // Relentless Hunter: 脱战移速
            if (save.RelentlessHunterSelected && hunterStacks > 0 && outOfCombatTimer <= 0)
                Player.moveSpeed += RelentlessMoveSpeedPerStack * hunterStacks;

            // Deep Ward: Boss战中累计无伤时间
            if (save.DeepWardSelected)
                UpdateDeepWard();
        }

        private void UpdateDeepWard()
        {
            // 检查附近是否有活跃 Boss
            bool bossNearby = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss
                    && Vector2.Distance(Player.Center, Main.npc[i].Center) < 2000f)
                {
                    bossNearby = true;
                    break;
                }
            }

            if (deepWardDodgeActive)
            {
                deepWardDodgeTimer--;
                if (deepWardDodgeTimer <= 0)
                {
                    deepWardDodgeActive = false;
                    deepWardNoHitTimer = 0;
                }
                return;
            }

            if (bossNearby)
            {
                deepWardNoHitTimer++;
                if (deepWardNoHitTimer >= DeepWardNoHitRequired)
                {
                    // 获得闪避
                    deepWardDodgeActive = true;
                    deepWardDodgeTimer = DeepWardDodgeDuration;
                    deepWardNoHitTimer = 0;

                    // 视觉效果 + 音效
                    SpawnDeepWardReadyFX();
                }
            }
            else
            {
                deepWardNoHitTimer = 0;
            }
        }

        // ── 免死 & 闪避 ──

        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();

            // Sixth Sense: 致命伤免疫（60s CD）
            if (save.SixthSenseSelected && sixthSenseCD <= 0 && info.Damage >= Player.statLife)
            {
                sixthSenseCD = SixthSenseCooldown;
                SpawnCheatDeathFX();
                return true;
            }

            // Deep Ward: 闪避 buff 消耗
            if (save.DeepWardSelected && deepWardDodgeActive)
            {
                deepWardDodgeActive = false;
                deepWardDodgeTimer = 0;
                deepWardNoHitTimer = 0;
                SpawnDodgeFX();
                return true;
            }

            return false;
        }

        // ── 受伤重置 ──

        public override void OnHurt(Player.HurtInfo info)
        {
            outOfCombatTimer = 5 * 60;
            deepWardNoHitTimer = 0;
        }

        public override void UpdateDead()
        {
            outOfCombatTimer = 0;
            deepWardNoHitTimer = 0;
            deepWardDodgeActive = false;
            deepWardDodgeTimer = 0;
        }

        // ── 粒子特效 ──

        private void SpawnCheatDeathFX()
        {
            // 金色/白色保护光环
            RuneVisualHelper.SpawnRingBurst(Player.Center, DustID.GoldCoin, 14, 40f, 3f, 1.5f);
            RuneVisualHelper.SpawnRingBurst(Player.Center, DustID.WhiteTorch, 8, 25f, 1.5f, 2f);
            var sfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Hold_ping_SFX")
            {
                Volume = 0.85f,
                PitchVariance = 0.1f
            };
            SoundEngine.PlaySound(sfx, Player.Center);
        }

        private void SpawnDodgeFX()
        {
            // 蓝色闪避粒子
            RuneVisualHelper.SpawnRingBurst(Player.Center, DustID.BlueFlare, 10, 30f, 3.5f, 1.3f);
            var sfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Enemy_Missing_ping_SFX")
            {
                Volume = 0.75f,
                PitchVariance = 0.2f
            };
            SoundEngine.PlaySound(sfx, Player.Center);
        }

        private void SpawnDeepWardReadyFX()
        {
            // 闪避就绪提示：蓝色上升粒子
            RuneVisualHelper.SpawnRisingParticles(Player.Center, DustID.BlueFlare, 6, 20f, -3f, 1.5f);
            var sfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX")
            {
                Volume = 0.5f,
                PitchVariance = 0.2f
            };
            SoundEngine.PlaySound(sfx, Player.Center);
        }

        // ── 辅助 ──

        private void SpawnCoins(int totalCopper, Terraria.DataStructures.IEntitySource source)
        {
            if (totalCopper <= 0) return;

            int platinum = totalCopper / 1000000;
            totalCopper %= 1000000;
            int gold = totalCopper / 10000;
            totalCopper %= 10000;
            int silver = totalCopper / 100;
            int copper = totalCopper % 100;

            if (platinum > 0) Player.QuickSpawnItem(source, ItemID.PlatinumCoin, platinum);
            if (gold > 0) Player.QuickSpawnItem(source, ItemID.GoldCoin, gold);
            if (silver > 0) Player.QuickSpawnItem(source, ItemID.SilverCoin, silver);
            if (copper > 0) Player.QuickSpawnItem(source, ItemID.CopperCoin, copper);
        }

        /// <summary>Row3 三大猎人符文（含终极猎人）是否选中</summary>
        private static bool HasHunterRuneSelected(RuneSaveSystem save)
        {
            return save.TreasureHunterSelected || save.RelentlessHunterSelected
                || save.UltimateHunterSelected;
        }

        // 公开属性供 UI / 其他 mod 查询
        public int GetGrislyStacks() => grislyStacks;
        public int GetHunterStacks() => hunterStacks;
        public bool IsDeepWardDodgeActive() => deepWardDodgeActive;
        public int GetDeepWardProgress() => deepWardNoHitTimer; // 0..DeepWardNoHitRequired
        public int GetSixthSenseCooldown() => sixthSenseCD;
    }
}
