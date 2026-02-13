using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Phase Rush
    public class PhaseRushPlayer : ModPlayer
    {
        private const int ProcCooldown = 10 * 60;
        private const float MovementSpeedBonus = 1.0f; // +100%
        private int remainingDuration;
        private int cooldownTimer;
        private int hitCount;


        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandlePhaseRush(target, isValidSource: true);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner != Player.whoAmI)
                return;

            if (proj.minion || proj.DamageType == DamageClass.Summon)
                return;

            HandlePhaseRush(target, isValidSource: true);
        }

        public override void PostUpdateMiscEffects()
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer--;
            }

            if (remainingDuration > 0)
            {
                Player.moveSpeed += MovementSpeedBonus;
                remainingDuration--;
            }
        }

        public override void UpdateDead()
        {
            remainingDuration = 0;
            cooldownTimer = 0;
            hitCount = 0;
        }

        private void HandlePhaseRush(NPC target, bool isValidSource)
        {
            if (!isValidSource)
                return;

            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.PhaseRushSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (cooldownTimer > 0 && remainingDuration <= 0)
                return;

            // 只有在未激活时才累计命中数
            if (remainingDuration <= 0)
            {
                hitCount++;
                if (hitCount >= 3)
                {
                    remainingDuration = 4 * 60; // 4秒
                    hitCount = 0;
                    var procSfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Phase_Rush_SFX")
                    {
                        Volume = 0.5f,
                        PitchVariance = 0f
                    };
                    SoundEngine.PlaySound(procSfx, Player.Center);
                }
            }

            ConsumeRushTime();
        }

        private void ConsumeRushTime()
        {
            if (remainingDuration <= 0)
                return;

            remainingDuration--;

            if (remainingDuration <= 0)
            {
                cooldownTimer = ProcCooldown;
            }
        }
    }
}
