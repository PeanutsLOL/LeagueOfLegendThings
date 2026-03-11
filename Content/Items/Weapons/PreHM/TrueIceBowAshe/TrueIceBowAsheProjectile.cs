using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.GameContent;
namespace LeagueOfLegendThings.Content.Items.Weapons.PreHM.TrueIceBowAshe
{
    public class TrueIceBowAsheProjectile : ModProjectile
    {
        public override string Texture => "LeagueOfLegendThings/Content/Items/Weapons/PreHM/TrueIceBowAshe/TrueIceBowProjectile";

        private const int TrailLength = 6;

        private bool lockedVelocityInitialized;
        private float lockedVelocityY;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {            
            Projectile.width = 32;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.light = 0.5f;
            Projectile.alpha = 0;
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            if (!lockedVelocityInitialized)
            {
                lockedVelocityY = Projectile.velocity.Y;
                lockedVelocityInitialized = true;
            }

            Projectile.velocity.Y = lockedVelocityY;

            if (Math.Abs(Projectile.velocity.X) > 0.01f || Math.Abs(Projectile.velocity.Y) > 0.01f)
            {
                Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
                Projectile.spriteDirection = Projectile.direction;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle sourceRect = texture.Frame();
            Vector2 origin = sourceRect.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldPos = Projectile.oldPos[i];
                if (oldPos == Vector2.Zero)
                    continue;

                float progress = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color trailColor = Projectile.GetAlpha(lightColor) * (0.45f * progress);
                trailColor *= 0.75f;

                Main.EntitySpriteDraw(
                    texture,
                    oldPos + Projectile.Size * 0.5f - Main.screenPosition,
                    sourceRect,
                    trailColor,
                    Projectile.oldRot[i],
                    origin,
                    Projectile.scale,
                    effects,
                    0
                );
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRect,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item50, Projectile.Center);
        }
    }
}