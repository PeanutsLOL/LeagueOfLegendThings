using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    public class GuardianPlayer : ModPlayer
    {
        private const int GuardianCooldownTicks = 25 * 60;
        private const int GuardianBuffDuration = 5 * 60;   // 5秒防御+移速
        private const int GuardianDefenseBonus = 30;
        private const float GuardianMoveSpeedBonus = 0.30f;
        private const float LowHpThreshold = 0.30f;         // 30% HP 以下自动触发

        private int guardianCooldown = 0;
        private bool guardianTriggeredForCurrentShield = false;
        private bool guardianPlayedCooldownEnd = false;
        private bool _lowHpTriggered; // 每次冷却内低血量只触发一次

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.GuardianSelected)
                return;

            bool hasIceShield = Player.HasBuff(BuffID.IceBarrier);

            // 冷却递减
            if (guardianCooldown > 0)
            {
                guardianCooldown--;
                if (guardianCooldown <= 0 && !guardianPlayedCooldownEnd)
                {
                    var sfx2 = new SoundStyle("LeagueOfLegendThings/Content/SFX/Guardian_SFX_2")
                    {
                        Volume = 0.8f,
                        PitchVariance = 0.2f
                    };
                    SoundEngine.PlaySound(sfx2, Player.position);
                    guardianPlayedCooldownEnd = true;
                    _lowHpTriggered = false;
                }
            }

            // 触发条件1: Ice Barrier 激活
            if (hasIceShield && !guardianTriggeredForCurrentShield && guardianCooldown <= 0)
            {
                guardianTriggeredForCurrentShield = true;
                TriggerGuardian();
            }
            if (!hasIceShield)
                guardianTriggeredForCurrentShield = false;

            // 触发条件2: HP 低于 30%（每冷却内一次）
            if (!_lowHpTriggered && guardianCooldown <= 0
                && (float)Player.statLife / Player.statLifeMax2 < LowHpThreshold)
            {
                _lowHpTriggered = true;
                TriggerGuardian();
            }

            // 持续的防御+移速 buff
            if (guardianCooldown > GuardianCooldownTicks - GuardianBuffDuration)
            {
                Player.statDefense += GuardianDefenseBonus;
                Player.moveSpeed += GuardianMoveSpeedBonus;
            }
        }

        private void TriggerGuardian()
        {
            guardianCooldown = GuardianCooldownTicks;
            guardianPlayedCooldownEnd = false;

            var sfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Guardian_SFX_4")
            {
                Volume = 0.8f,
                PitchVariance = 0.2f
            };
            SoundEngine.PlaySound(sfx, Player.position);

            // 治疗
            Player.statLife += 250;
            Player.HealEffect(250, true);

            // 视觉特效
            for (int i = 0; i < 12; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(40f, 40f);
                Dust d = Dust.NewDustPerfect(Player.Center + offset, DustID.BlueTorch,
                    offset.SafeNormalize(Vector2.UnitY) * 3f, 100, default, 1.8f);
                d.noGravity = true;
            }
        }
    }
}
