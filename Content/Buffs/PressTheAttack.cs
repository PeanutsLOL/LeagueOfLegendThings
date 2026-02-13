using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Press the Attack
    public class PressTheAttackPlayer : ModPlayer
    {
        private const int ProcDamageBuffDuration = 6 * 60;
        private const int ProcCooldown = 5 * 60;

        private int hitCount;
        private int accumulatedDamage;
        private int buffTimer;
        private int cooldownTimer;

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandlePressTheAttack(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandlePressTheAttack(target, damageDone);
        }

        public override void PostUpdateMiscEffects()
        {
            bool hadBuff = buffTimer > 0;

            if (buffTimer > 0)
            {
                buffTimer--;
                Player.GetDamage(DamageClass.Generic) += 0.08f;
            }

            if (cooldownTimer > 0)
            {
                cooldownTimer--;
            }

            if (buffTimer <= 0 && hadBuff)
            {
                cooldownTimer = ProcCooldown;
                hitCount = 0;
                accumulatedDamage = 0;
            }
        }

        public override void UpdateDead()
        {
            hitCount = 0;
            accumulatedDamage = 0;
            buffTimer = 0;
            cooldownTimer = 0;
        }

        private void HandlePressTheAttack(NPC target, int damageDone)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.PressTheAttackSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (buffTimer > 0)
            {
                buffTimer = ProcDamageBuffDuration;
                return;
            }

            if (cooldownTimer > 0)
                return;

            hitCount++;
            accumulatedDamage += Math.Max(0, damageDone);

            if (hitCount >= 3)
            {
                int denom = Player.statLifeMax2 + Player.statManaMax2;
                float scale = denom > 0 ? (accumulatedDamage * 0.33f) * (denom / 700f) : 0f;
                int bonusDamage = Math.Max(1, (int)MathF.Round(40f + scale));

                target.SimpleStrikeNPC(bonusDamage, Player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                CombatText.NewText(Player.Hitbox, Color.Aqua, $"Dealt {bonusDamage} DMG");
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: target.whoAmI);
                }

                var sfx = new SoundStyle(Main.rand.NextBool()
                    ? "LeagueOfLegendThings/Content/Buffs/Press_the_Attack_SFX"
                    : "LeagueOfLegendThings/Content/Buffs/Press_the_Attack_SFX_2")
                {
                    Volume = 0.75f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(sfx, Player.Center);

                buffTimer = ProcDamageBuffDuration;
                hitCount = 0;
                accumulatedDamage = 0;
            }
        }
    }
}
