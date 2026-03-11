using Terraria;
using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.Items.Weapons.PreHM.TrueIceBowAshe
{
    public class TrueIceBowAsheGlobalProjectile : GlobalProjectile
    {
        public bool IsFromTrueIceBow;
        public bool NoDropVelocityInitialized;
        public float LockedVelocityY;
        
        public override bool InstancePerEntity => true;

        public override void AI(Projectile projectile)
        {
            if (!IsFromTrueIceBow)
                return;

            if (!NoDropVelocityInitialized)
            {
                LockedVelocityY = projectile.velocity.Y;
                NoDropVelocityInitialized = true;
            }

            projectile.velocity.Y = LockedVelocityY;
        }
    }
}
