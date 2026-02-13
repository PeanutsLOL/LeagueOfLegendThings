using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using LeagueOfLegendThings;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Dark Harvest
    public class DarkHarvestPlayer : ModPlayer
    {
        private const int MinTargetLifeMax = 2000;
        private const int ProcCooldown = 35 * 60;

        private int soulCount;
        private int cooldownTimer;

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleDarkHarvest(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleDarkHarvest(target, damageDone);
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

        public void AddSoulStack()
        {
            soulCount++;
        }

        private void HandleDarkHarvest(NPC target, int damageDone)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.DarkHarvestSelected)
                return;

            if (target.friendly || target.lifeMax < MinTargetLifeMax)
                return;

            // 检查扣血前是否已低于50%：target.life是扣血后的值，加回damageDone还原
            int preHitLife = target.life + damageDone;
            if (preHitLife > target.lifeMax * 0.5f)
                return;

            if (cooldownTimer > 0)
                return;

            int heldItemDamage = Player.HeldItem?.damage ?? 0;
            float rawDamage = 30f + 11f * soulCount + heldItemDamage * 0.1f;
            int bonusDamage = Math.Max(1, (int)MathF.Round(rawDamage));

            target.SimpleStrikeNPC(bonusDamage, Player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
            CombatText.NewText(Player.Hitbox, Color.MediumPurple, $"Dealt {bonusDamage} DMG");

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                var procSfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Dark_Harvest_SFX_2")
                {
                    Volume = 0.8f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(procSfx, Player.Center);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                var modPacket = ModContent.GetInstance<global::LeagueOfLegendThings.LeagueOfLegendThings>().GetPacket();
                modPacket.Write((byte)LeaguePacketType.DarkHarvestProcSfx);
                modPacket.Write(Player.Center.X);
                modPacket.Write(Player.Center.Y);
                modPacket.Send();
            }

            cooldownTimer = ProcCooldown;

            // 生成原版 HallowBossLastingRainbow 射弹作为视觉特效，用 localAI[1]=777 标记以便染红
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int projId = Projectile.NewProjectile(
                    new EntitySource_OnHit(Player, target),
                    target.Center,
                    Vector2.Zero,
                    ProjectileID.HallowBossLastingRainbow,
                    0,
                    0f,
                    Player.whoAmI
                );
                if (projId >= 0 && projId < Main.maxProjectiles)
                {
                    Main.projectile[projId].friendly = false;
                    Main.projectile[projId].hostile = false;
                    Main.projectile[projId].ai[1] = 777f;  // 标记为黑暗收割特效
                }
            }
        }
    }
}
