using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Hail of Blades
    public class HailOfBladesPlayer : ModPlayer
    {
        private const int MaxStrikes = 3;
        private const int ProcCooldown = 10 * 60;
        private const float AttackSpeedBonus = 1.6f; // +160%

        private int remainingStrikes;
        private int cooldownTimer;

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHailOfBlades(target, isValidSource: true);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner != Player.whoAmI)
                return;

            if (proj.minion || proj.DamageType == DamageClass.Summon)
                return;

            HandleHailOfBlades(target, isValidSource: true);
        }

        public override void PostUpdateMiscEffects()
        {
            bool wasOnCooldown = cooldownTimer > 0;
            if (cooldownTimer > 0)
            {
                cooldownTimer--;
            }

            if (wasOnCooldown && cooldownTimer == 0)
            {
                var readySfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Hail_of_Blades_SFX")
                {
                    Volume = 0.5f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(readySfx, Player.Center);
            }

            if (remainingStrikes > 0)
            {
                Player.GetAttackSpeed(Player.HeldItem.DamageType) += AttackSpeedBonus;
            }
        }

        public override void UpdateDead()
        {
            remainingStrikes = 0;
            cooldownTimer = 0;
        }

        private void HandleHailOfBlades(NPC target, bool isValidSource)
        {
            if (!isValidSource)
                return;

            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.HailOfBladesSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (cooldownTimer > 0 && remainingStrikes <= 0)
                return;

            if (remainingStrikes <= 0)
            {
                remainingStrikes = MaxStrikes;
                var procSfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Hail_of_Blades_SFX_2")
                {
                    Volume = 0.5f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(procSfx, Player.Center);
            }

            ConsumeStrike();
        }

        private void ConsumeStrike()
        {
            if (remainingStrikes <= 0)
                return;

            remainingStrikes--;

            if (remainingStrikes <= 0)
            {
                cooldownTimer = ProcCooldown;
            }
        }
    }
}
