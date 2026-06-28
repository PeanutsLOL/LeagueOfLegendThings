using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    // Domination small runes: Eyeball Collection, Ravenous/Ingenious/Treasure/Relentless Hunter
    public class DominationSmallRunesPlayer : ModPlayer
    {
        private const int EyeballMaxStacks = 10;
        private const int HunterMaxStacks = 5;

        private const float EyeballDamagePerStack = 0.008f; // 0.8% per stack
        private const float RavenousLifeStealPerStack = 0.0005f; // 0.05% per stack
        private const float IngeniousPotionReductionPerStack = 0.03f; // 3% per stack
        private const float RelentlessMoveSpeedPerStack = 0.03f; // 3% per stack

        private int eyeballStacks;
        private int hunterStacks;
        private int outOfCombatTimer;
        private float pendingPotionReduction;

        public override void SaveData(TagCompound tag)
        {
            tag["eyeballStacks"] = eyeballStacks;
            tag["hunterStacks"] = hunterStacks;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("eyeballStacks"))
                eyeballStacks = tag.GetInt("eyeballStacks");
            if (tag.ContainsKey("hunterStacks"))
                hunterStacks = tag.GetInt("hunterStacks");
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, damageDone);
        }

        public override void PostUpdateMiscEffects()
        {
            if (outOfCombatTimer > 0)
            {
                outOfCombatTimer--;
            }

            var save = ModContent.GetInstance<RuneSaveSystem>();

            if (save.EyeballCollectionSelected && eyeballStacks > 0)
            {
                Player.GetDamage(DamageClass.Generic) += EyeballDamagePerStack * eyeballStacks;
            }

            if (save.RelentlessHunterSelected && hunterStacks > 0 && outOfCombatTimer <= 0)
            {
                Player.moveSpeed += RelentlessMoveSpeedPerStack * hunterStacks;
            }
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.IngeniousHunterSelected)
                return;

            if (hunterStacks <= 0)
                return;

            if (item.potion && item.healLife > 0)
            {
                float reduction = hunterStacks * IngeniousPotionReductionPerStack;
                pendingPotionReduction = MathHelper.Clamp(reduction, 0f, 0.6f);
            }
        }

        public override void PostUpdate()
        {
            if (pendingPotionReduction > 0f && Player.potionDelay > 0)
            {
                Player.potionDelay = (int)(Player.potionDelay * (1f - pendingPotionReduction));
                pendingPotionReduction = 0f;
            }
        }

        public override void UpdateDead()
        {
            outOfCombatTimer = 0;
            pendingPotionReduction = 0f;
        }

        private void HandleHit(NPC target, int damageDone)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!(save.EyeballCollectionSelected || HasHunterRuneSelected(save)))
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (damageDone > 0)
            {
                outOfCombatTimer = 5 * 60;
            }

            HandleRavenousHeal(save, damageDone);
            HandleKillStacks(save, target);
        }

        private void HandleRavenousHeal(RuneSaveSystem save, int damageDone)
        {
            if (!save.RavenousHunterSelected || hunterStacks <= 0)
                return;

            if (damageDone <= 0)
                return;

            float stolen = damageDone * (RavenousLifeStealPerStack * hunterStacks);
            Player.GetModPlayer<Mayhem.LeechPoolPlayer>().Fill(stolen);
        }

        private void HandleKillStacks(RuneSaveSystem save, NPC target)
        {
            if (target.life > 0)
                return;

            if (save.EyeballCollectionSelected)
            {
                if (target.lifeMax < 300)
                    return;
                int add = target.boss ? 2 : 1;
                eyeballStacks = System.Math.Min(eyeballStacks + add, EyeballMaxStacks);
            }

            if (HasHunterRuneSelected(save) && target.boss)
            {
                hunterStacks = System.Math.Min(hunterStacks + 1, HunterMaxStacks);
            }

            if (save.TreasureHunterSelected && hunterStacks > 0)
            {
                int extraCopper = (int)(target.value * 0.05f * hunterStacks);
                extraCopper = System.Math.Min(extraCopper, 10000); // cap at 1 gold
                if (extraCopper > 0)
                {
                    SpawnCoins(extraCopper, Player.GetSource_OnHit(target));
                }
            }
        }

        private void SpawnCoins(int totalCopper, Terraria.DataStructures.IEntitySource source)
        {
            if (totalCopper <= 0)
                return;

            int platinum = totalCopper / 1000000;
            totalCopper %= 1000000;
            int gold = totalCopper / 10000;
            totalCopper %= 10000;
            int silver = totalCopper / 100;
            int copper = totalCopper % 100;

            if (platinum > 0)
                Player.QuickSpawnItem(source, ItemID.PlatinumCoin, platinum);
            if (gold > 0)
                Player.QuickSpawnItem(source, ItemID.GoldCoin, gold);
            if (silver > 0)
                Player.QuickSpawnItem(source, ItemID.SilverCoin, silver);
            if (copper > 0)
                Player.QuickSpawnItem(source, ItemID.CopperCoin, copper);
        }

        private static bool HasHunterRuneSelected(RuneSaveSystem save)
        {
            return save.RavenousHunterSelected || save.IngeniousHunterSelected ||
                   save.TreasureHunterSelected || save.RelentlessHunterSelected;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            // Relentless Hunter: 5s 无伤才生效
            outOfCombatTimer = 5 * 60;
        }
    }
}
