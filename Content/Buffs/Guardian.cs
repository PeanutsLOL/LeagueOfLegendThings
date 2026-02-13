using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ID;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class GuardianPlayer : ModPlayer
    {
        private const int GuardianCooldownTicks = 50 * 60; // 50 seconds

        private int guardianCooldown = 0;
        private bool guardianTriggeredForCurrentShield = false;
        private bool guardianPlayedCooldownEnd = false;

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.GuardianSelected)
                return;

            bool hasIceShield = Player.HasBuff(BuffID.IceBarrier);

            // If shield is present and not yet triggered for this shielding, and not on cooldown
            if (hasIceShield && !guardianTriggeredForCurrentShield && guardianCooldown <= 0)
            {
                guardianTriggeredForCurrentShield = true;
                guardianCooldown = GuardianCooldownTicks;
                guardianPlayedCooldownEnd = false;

                // Play SFX_4 and heal 120
                var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Guardian_SFX_4")
                {
                    Volume = 0.8f,
                    PitchVariance = 0.2f
                };
                SoundEngine.PlaySound(sfx, Player.position);

                Player.statLife += 120;
                Player.HealEffect(120, true);
            }

            // If shield is absent, allow future triggers
            if (!hasIceShield)
            {
                guardianTriggeredForCurrentShield = false;
            }

            // Cooldown handling and play end SFX once when cooldown reaches 0
            if (guardianCooldown > 0)
            {
                guardianCooldown--;
                if (guardianCooldown <= 0 && !guardianPlayedCooldownEnd)
                {
                    var sfx2 = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Guardian_SFX_2")
                    {
                        Volume = 0.8f,
                        PitchVariance = 0.2f
                    };
                    SoundEngine.PlaySound(sfx2, Player.position);
                    guardianPlayedCooldownEnd = true;
                }
            }
        }
    }
}
