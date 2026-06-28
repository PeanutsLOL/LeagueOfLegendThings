using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    // Hail of Blades — 3次极速攻击，每击+15%伤害，计时过期防无限保持
    public class HailOfBladesPlayer : ModPlayer
    {
        private const int MaxStrikes = 3;
        private const int ProcCooldown = 7 * 60;
        private const float AttackSpeedBonus = 2.0f;    // +200%
        private const float DamagePerStrike = 0.15f;     // 每击额外+15%伤害
        private const int StrikeTimeout = 3 * 60;         // 3秒不用就过期
        private const int StrikeMinInterval = 15;         // 两次消耗至少隔15帧，防DoT秒吞

        private int remainingStrikes;
        private int cooldownTimer;
        private int _strikeTimer;        // 未命中计时（过期）
        private int _strikeConsumeDelay; // 消耗保护计数

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHailOfBlades(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner != Player.whoAmI)
                return;
            if (proj.minion || proj.DamageType == DamageClass.Summon)
                return;

            HandleHailOfBlades(target);
        }

        public override void PostUpdateMiscEffects()
        {
            // 冷却递减 & 就绪音效
            bool wasOnCooldown = cooldownTimer > 0;
            if (cooldownTimer > 0) cooldownTimer--;
            if (wasOnCooldown && cooldownTimer == 0)
            {
                var readySfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Hail_of_Blades_SFX")
                {
                    Volume = 0.5f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(readySfx, Player.Center);
            }

            // 保护计数递减
            if (_strikeConsumeDelay > 0) _strikeConsumeDelay--;

            // 攻速 buff 应用
            if (remainingStrikes > 0)
            {
                Player.GetAttackSpeed(Player.HeldItem.DamageType) += AttackSpeedBonus;
                Player.GetDamage(Player.HeldItem.DamageType) += DamagePerStrike;

                // 未命中计时：到时间就清空
                _strikeTimer++;
                if (_strikeTimer >= StrikeTimeout)
                {
                    remainingStrikes = 0;
                    cooldownTimer = ProcCooldown;
                }
            }
        }

        public override void UpdateDead()
        {
            remainingStrikes = 0;
            cooldownTimer = 0;
            _strikeTimer = 0;
            _strikeConsumeDelay = 0;
        }

        private void HandleHailOfBlades(NPC target)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.HailOfBladesSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (cooldownTimer > 0 && remainingStrikes <= 0)
                return;

            // 首次命中激活
            if (remainingStrikes <= 0)
            {
                remainingStrikes = MaxStrikes;
                _strikeTimer = 0;
                var procSfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Hail_of_Blades_SFX_2")
                {
                    Volume = 0.5f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(procSfx, Player.Center);
            }

            // 消耗保护：距上次消耗不足15帧则跳过（防DoT秒吞）
            if (_strikeConsumeDelay > 0)
                return;

            ConsumeStrike();
        }

        private void ConsumeStrike()
        {
            if (remainingStrikes <= 0)
                return;

            remainingStrikes--;
            _strikeTimer = 0;                // 命中就重置过期计时
            _strikeConsumeDelay = StrikeMinInterval; // 开始保护窗口

            if (remainingStrikes <= 0)
                cooldownTimer = ProcCooldown;
        }
    }
}
