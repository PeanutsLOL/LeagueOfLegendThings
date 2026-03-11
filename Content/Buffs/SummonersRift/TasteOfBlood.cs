using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Taste of Blood
    public class TasteOfBloodPlayer : ModPlayer
    {
        private const int ProcCooldown = 20 * 60;
        private const int HealAmount = 16;

        private int cooldownTimer;

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleTasteOfBlood(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleTasteOfBlood(target);
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

        private void HandleTasteOfBlood(NPC target)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.TasteOfBloodSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (cooldownTimer > 0)
                return;

            if (Player.statLife >= Player.statLifeMax2)
                return;

            int heal = System.Math.Min(HealAmount, Player.statLifeMax2 - Player.statLife);
            if (heal <= 0)
                return;

            Player.statLife += heal;
            Player.HealEffect(heal, broadcast: true);

            cooldownTimer = ProcCooldown;
        }
    }
}
