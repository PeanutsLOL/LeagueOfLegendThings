using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    // Glacial Augment — 攻击时对Boss造成额外伤害+强力减速
    public class GlacialAugmentPlayer : ModPlayer
    {
        public bool HasGlacialAugment;
        private int _gaCooldown;
        private const int GaCooldownTicks = 8 * 60;
        private const int GaSlowDuration = 3 * 60;
        private const float GaSlowAmount = 0.50f;
        private const int GaBonusDamage = 45;

        public override void ResetEffects()
        {
            HasGlacialAugment = false;
        }

        public override void PostUpdateMiscEffects()
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            HasGlacialAugment = runeSave.GlacialAugmentSelected;
            if (_gaCooldown > 0) _gaCooldown--;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (HasGlacialAugment && target.boss)
                ApplyGlacialAugment(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (HasGlacialAugment && target.boss)
                ApplyGlacialAugment(target);
        }

        private void ApplyGlacialAugment(NPC target)
        {
            if (_gaCooldown > 0) return;
            _gaCooldown = GaCooldownTicks;

            target.SimpleStrikeNPC(GaBonusDamage, Player.direction, crit: false, knockBack: 0f, DamageClass.Generic);
            target.AddBuff(BuffID.Slow, GaSlowDuration);
            // 自定义减速通过减慢 NPC 速度实现
            if (target.velocity.Length() > 0.5f)
                target.velocity *= (1f - GaSlowAmount);

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustDirect(target.position, target.width, target.height,
                    DustID.Ice, 0f, -3f, 100, default, 1.5f);
                dust.noGravity = true;
                dust.velocity *= 0.8f;
            }
        }

        public override void PostUpdate()
        {
            if (!HasGlacialAugment) return;
            if (Main.rand.NextBool(20))
            {
                Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height,
                    DustID.Ice, 0f, 0f, 100, default, 1.5f);
                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }
        }

        public override void UpdateDead()
        {
            HasGlacialAugment = false;
            _gaCooldown = 0;
        }
    }
}