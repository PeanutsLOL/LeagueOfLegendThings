using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Cheap Shot
    public class CheapShotPlayer : ModPlayer
    {
        private const int ProcCooldown = 4 * 60;
        private const int BonusDamage = 45;

        private int cooldownTimer;

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleCheapShot(target, DamageClass.Melee);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            DamageClass damageType = proj.DamageType;
            HandleCheapShot(target, damageType);
        }

        public override void PostUpdateMiscEffects()
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer--;
            }
        }

        public override void UpdateDead()
        {
            cooldownTimer = 0;
        }

        private void HandleCheapShot(NPC target, DamageClass damageType)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().SecondaryPath.Equals("Domination") &&
                !ModContent.GetInstance<RuneSaveSystem>().PrimaryPath.Equals("Domination"))
                return;

            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.PrimaryRow1.Equals("Cheap Shot") &&
                !(runeSave.SecondaryPick1 == "Cheap Shot" || runeSave.SecondaryPick2 == "Cheap Shot"))
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (cooldownTimer > 0)
                return;

            if (!HasCrowdControl(target))
                return;

            target.SimpleStrikeNPC(BonusDamage, Player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);

            cooldownTimer = ProcCooldown;
        }

        private bool HasCrowdControl(NPC target)
        {
            // 常见控制类 Debuff 判定
            if (target.HasBuff(BuffID.Frostburn) ||
                   target.HasBuff(BuffID.Frostburn2) ||
                   target.HasBuff(BuffID.Chilled) ||
                   target.HasBuff(BuffID.Frozen) ||
                   target.HasBuff(BuffID.Stoned) ||
                   target.HasBuff(BuffID.Confused) ||
                   target.HasBuff(BuffID.Slow) ||
                   target.HasBuff(BuffID.Webbed) ||
                   target.HasBuff(BuffID.CursedInferno) ||
                   target.HasBuff(BuffID.Ichor) ||
                   target.HasBuff(BuffID.ShadowFlame))
            {
                return true;
            }

            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 灾厄控制类 Debuff（可按需补充）
                string[] calamityDebuffs =
                {
                    "BrimstoneFlames",
                    "MarkedforDeath",
                    "FrozenLungs",
                    "GlacialState",
                    "Petrified",
                    "GalvanicCorrosion",
                    "CrushDepth",
                    "Eutrophication",
                    "Vaporfied",
                    "NeuralPlague",
                    "Silenced",
                    "Stunned"
                };

                foreach (string debuffName in calamityDebuffs)
                {
                    if (calamity.TryFind(debuffName, out ModBuff modBuff))
                    {
                        if (target.HasBuff(modBuff.Type))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
