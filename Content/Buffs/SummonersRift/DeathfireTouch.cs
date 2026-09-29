using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    /// <summary>
    /// 冥火之触 (Deathfire Touch) — 巫术系基石
    /// 魔法伤害命中时灼烧目标，3秒内每秒造成伤害。
    /// 连续灼烧3秒后伤害提升75%。
    /// 每目标独立冷却，命中刷新灼烧。
    /// </summary>
    public class DeathfireTouchPlayer : ModPlayer
    {
        private const int BurnDuration = 3 * 60;
        private const float BaseDmgPerSec = 120f;
        private const float ClassBonusScaling = 0.80f; // 最高职业加成系数
        private const float EnhanceMultiplier = 1.75f;
        private const int EnhanceThreshold = 3 * 60;

        private readonly Dictionary<int, int> _burnTimer = new();    // npcIndex → 剩余灼烧帧
        private readonly Dictionary<int, int> _burnElapsed = new();  // npcIndex → 本段灼烧已持续帧

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (item.DamageType == DamageClass.Magic)
                ApplyBurn(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.DamageType == DamageClass.Magic)
                ApplyBurn(target);
        }

        private void ApplyBurn(NPC target)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().DeathfireTouchSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            // 首次施加音效
            bool fresh = !_burnTimer.ContainsKey(target.whoAmI) || _burnTimer[target.whoAmI] <= 0;
            if (fresh)
            {
                var sfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Brand_Select_SFX")
                {
                    Volume = 0.7f,
                    PitchVariance = 0.1f
                };
                SoundEngine.PlaySound(sfx, target.Center);
            }

            _burnTimer[target.whoAmI] = BurnDuration;
            if (!_burnElapsed.ContainsKey(target.whoAmI))
                _burnElapsed[target.whoAmI] = 0;
        }

        public override void PostUpdateMiscEffects()
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().DeathfireTouchSelected)
                return;

            var expired = new List<int>();
            foreach (var kvp in _burnTimer)
            {
                int idx = kvp.Key;
                if (idx < 0 || idx >= Main.maxNPCs) { expired.Add(idx); continue; }
                NPC npc = Main.npc[idx];
                if (!npc.active || npc.life <= 0) { expired.Add(idx); continue; }

                _burnTimer[idx]--;
                _burnElapsed[idx]++;

                // 每秒跳一次伤害（每 60 帧），首帧不跳
                if (_burnTimer[idx] % 60 == 0 && _burnTimer[idx] < BurnDuration - 1)
                {
                    float classBonus = RuneDamageHelper.GetHighestClassBonus(Player);
                    float dmg = BaseDmgPerSec + (BaseDmgPerSec * classBonus * ClassBonusScaling);

                    // 连续灼烧 3 秒后强化
                    bool enhanced = _burnElapsed[idx] >= EnhanceThreshold;
                    if (enhanced) dmg *= EnhanceMultiplier;

                    int final = System.Math.Max(1, (int)dmg);
                    npc.SimpleStrikeNPC(final, Player.direction, crit: false,
                        knockBack: 0f, damageType: DamageClass.Magic);

                    // ═══ 伤害跳转：燃烧爆裂 ═══
                    int dustType = enhanced ? DustID.Shadowflame : DustID.BlueFlare;
                    int dustTypeSpark = enhanced ? DustID.PurpleCrystalShard : DustID.BlueTorch;

                    // 火花：无重力，从小到大向外散射
                    for (int i = 0; i < 10; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 10f + Main.rand.NextFloat(-0.2f, 0.2f);
                        float speed = Main.rand.NextFloat(1.5f, 4f);
                        Vector2 vel = new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * speed;
                        Dust d = Dust.NewDustPerfect(npc.Center, dustTypeSpark,
                            vel, 100, default, Main.rand.NextFloat(0.4f, 1.2f));
                        d.noGravity = true;
                        d.fadeIn = 0.5f;
                    }

                    // 余烬：受重力，大颗粒弹出后下落，模拟燃烧碎片
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-1f, 1f); // 大致向上
                        float speed = Main.rand.NextFloat(1f, 3.5f);
                        Vector2 vel = new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * speed;
                        Dust d = Dust.NewDustPerfect(npc.Center, dustType,
                            vel, 100, default, Main.rand.NextFloat(1.3f, 2.2f));
                        d.noGravity = false;  // 受重力下落
                        d.fadeIn = 0.3f;
                    }

                    // 强化：额外紫色环爆
                    if (enhanced)
                    {
                        RuneVisualHelper.SpawnRingBurst(npc.Center, DustID.Shadowflame, 10, 28f, 3.5f, 1.6f);
                    }
                }

                // ═══ 每 3 帧：持续冒烟粒子（从大到小上升，带随机左右偏转）═══
                if (_burnTimer[idx] % 3 == 0)
                {
                    int wispType = _burnElapsed[idx] >= EnhanceThreshold ? DustID.PurpleCrystalShard : DustID.BlueFlare;
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 pos = npc.position + new Vector2(Main.rand.NextFloat(0, npc.width), npc.height * 0.3f + Main.rand.NextFloat(0, npc.height * 0.4f));
                        float velX = Main.rand.NextFloat(-0.8f, 0.8f);
                        float velY = Main.rand.NextFloat(-1.8f, -0.4f);
                        float startScale = Main.rand.NextFloat(0.7f, 1.3f);
                        Dust d = Dust.NewDustPerfect(pos, wispType,
                            new Vector2(velX, velY), 80, default, startScale);
                        d.noGravity = true;
                        d.fadeIn = 0.5f;
                    }
                }

                if (_burnTimer[idx] <= 0)
                    expired.Add(idx);
            }

            foreach (var id in expired)
            {
                _burnTimer.Remove(id);
                _burnElapsed.Remove(id);
            }
        }

        public override void UpdateDead()
        {
            _burnTimer.Clear();
            _burnElapsed.Clear();
        }

        /// <summary>用于 UI 查询目标灼烧剩余时间</summary>
        public int GetBurnRemaining(int npcIndex)
            => _burnTimer.TryGetValue(npcIndex, out int v) ? v : 0;

        /// <summary>用于 UI 查询灼烧是否处于强化阶段</summary>
        public bool IsBurnEnhanced(int npcIndex)
            => _burnElapsed.TryGetValue(npcIndex, out int v) && v >= EnhanceThreshold;
    }
}
