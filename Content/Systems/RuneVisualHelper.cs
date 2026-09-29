using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace LeagueOfLegendThings.Content.Systems
{
    /// <summary>
    /// 符文视觉特效辅助 — 提供常用的粒子爆发模式
    /// 全部使用原版 DustID，无需额外贴图
    /// </summary>
    public static class RuneVisualHelper
    {
        /// <summary>目标位置环形爆发粒子</summary>
        public static void SpawnRingBurst(Vector2 center, int dustType, int count = 12, float radius = 30f, float speed = 3f, float scale = 1.2f, bool noGravity = true)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Vector2 offset = new Vector2(radius, 0).RotatedBy(angle);
                Dust d = Dust.NewDustPerfect(center + offset * 0.3f, dustType,
                    offset.SafeNormalize(Vector2.UnitY) * speed, 100, default, scale);
                d.noGravity = noGravity;
                d.fadeIn = 0.5f;
            }
        }

        /// <summary>从目标中心上升的粒子</summary>
        public static void SpawnRisingParticles(Vector2 center, int dustType, int count = 8, float spread = 20f, float riseSpeed = -2f, float scale = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(spread, spread * 0.5f);
                Dust d = Dust.NewDustPerfect(center + offset, dustType,
                    new Vector2(Main.rand.NextFloat(-1f, 1f), riseSpeed + Main.rand.NextFloat(-1f, 0f)),
                    100, default, scale);
                d.noGravity = true;
                d.fadeIn = 0.8f;
            }
        }

        /// <summary>地面冲击波（从中心向两侧展开）</summary>
        public static void SpawnShockwave(Vector2 center, int dustType = DustID.SolarFlare, int count = 16, float radius = 40f, float speed = 4f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Vector2 dir = new Vector2(1, 0).RotatedBy(angle);
                // 椭圆：水平方向更宽
                Vector2 offset = new Vector2(dir.X * radius * 1.6f, dir.Y * radius * 0.6f);
                Dust d = Dust.NewDustPerfect(center, dustType,
                    dir * speed, 100, default, 1.5f);
                d.noGravity = true;
                d.fadeIn = 1f;
            }
            // 内圈小粒子
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8;
                Vector2 dir = new Vector2(1, 0).RotatedBy(angle);
                Dust d = Dust.NewDustPerfect(center, DustID.GoldCoin,
                    dir * (speed * 0.5f), 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        /// <summary>金色冲击爆发（用于强攻、先攻等）</summary>
        public static void SpawnGoldenBurst(Vector2 center, int count = 10, float scale = 1.5f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Vector2 dir = new Vector2(1, 0).RotatedBy(angle);
                Dust d = Dust.NewDustPerfect(center, DustID.YellowTorch,
                    dir * 3f, 100, default, scale);
                d.noGravity = true;
            }
            // 中心白光
            for (int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(2f, 2f), 100, default, scale * 1.3f);
                d.noGravity = true;
            }
        }

        /// <summary>暗影爆发（黑暗收割）</summary>
        public static void SpawnShadowBurst(Vector2 center, int count = 14)
        {
            SpawnRingBurst(center, DustID.Shadowflame, count, 28f, 3.5f, 1.6f);
            SpawnRisingParticles(center, DustID.PurpleCrystalShard, 6, 16f, -3f, 1.3f);
            // 灵魂粒子：白色向内收缩
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.TwoPi * i / 4;
                Dust d = Dust.NewDustPerfect(center, DustID.WhiteTorch,
                    new Vector2(1, 0).RotatedBy(angle) * 1.5f, 100, default, 2f);
                d.noGravity = true;
                d.fadeIn = 1.5f;
            }
        }

        /// <summary>治疗粒子（不灭之握、守护者）</summary>
        public static void SpawnHealBurst(Vector2 center, int count = 10, float scale = 1.4f)
        {
            SpawnRisingParticles(center, DustID.HealingPlus, count, 18f, -2.5f, scale);
            SpawnRingBurst(center, DustID.GreenTorch, 8, 20f, 2f, 1f);
        }

        /// <summary>闪电粒子（电刑辅助）</summary>
        public static void SpawnElectricSpark(Vector2 start, Vector2 end, int segmentCount = 6)
        {
            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                Vector2 pos = Vector2.Lerp(start, end, t);
                Dust d = Dust.NewDustPerfect(pos, DustID.Electric,
                    Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, 1.8f);
                d.noGravity = true;
            }
        }
    }
}
