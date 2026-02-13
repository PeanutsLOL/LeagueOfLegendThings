using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Sudden Impact
    public class SuddenImpactPlayer : ModPlayer
    {
        private const int ReadyDuration = 4 * 60;
        private const int ProcCooldown = 10 * 60;
        private const float TeleportDistance = 120f;
        private const float DashSpeedThreshold = 8f;
        private const float VelocityBurstDelta = 6f;

        private int readyTimer;
        private int cooldownTimer;
        private Vector2 lastCenter;
        private Vector2 lastVelocity;
        private bool initialized;

        public override void PreUpdateMovement()
        {
            if (!initialized)
            {
                lastCenter = Player.Center;
                lastVelocity = Player.velocity;
                initialized = true;
            }
        }

        public override void PostUpdateMiscEffects()
        {
            if (cooldownTimer > 0)
            {
                cooldownTimer--;
            }

            if (readyTimer > 0)
            {
                readyTimer--;
            }

            if (ShouldTriggerReady())
            {
                readyTimer = ReadyDuration;
            }

            lastCenter = Player.Center;
            lastVelocity = Player.velocity;
        }

        public override void UpdateDead()
        {
            readyTimer = 0;
            cooldownTimer = 0;
            initialized = false;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleSuddenImpact(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleSuddenImpact(target);
        }

        private void HandleSuddenImpact(NPC target)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().SuddenImpactSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (readyTimer <= 0)
                return;

            int bonusDamage = GetBonusDamage();
            target.SimpleStrikeNPC(bonusDamage, Player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
            CombatText.NewText(Player.Hitbox, Color.OrangeRed, $"Dealt {bonusDamage} DMG");

            readyTimer = 0;
            cooldownTimer = ProcCooldown;
        }

        private bool ShouldTriggerReady()
        {
            if (readyTimer > 0 || cooldownTimer > 0)
                return false;

            if (!ModContent.GetInstance<RuneSaveSystem>().SuddenImpactSelected)
                return false;

            float distance = Vector2.Distance(Player.Center, lastCenter);
            bool teleported = distance >= TeleportDistance;

            bool dashed = Player.dash > 0 || Player.dashDelay < 0;

            float speed = Player.velocity.Length();
            float lastSpeed = lastVelocity.Length();
            bool burst = speed >= DashSpeedThreshold && (speed - lastSpeed) >= VelocityBurstDelta;

            return teleported || dashed || burst;
        }

        private static int GetBonusDamage()
        {
            if (NPC.downedMoonlord)
                return 240;
            if (Main.hardMode)
                return 140;
            return 40;
        }
    }
}
