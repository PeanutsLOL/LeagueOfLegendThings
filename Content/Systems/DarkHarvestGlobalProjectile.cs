using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Buffs;
using LeagueOfLegendThings;

namespace LeagueOfLegendThings.Content.Systems
{
    /// <summary>
    /// 用于将黑暗收割生成的 HallowBossLastingRainbow 射弹染成红色并追踪飞向玩家
    /// 通过 Projectile.ai[1] == 777 标记识别
    /// localAI[0] 标记：1=已加魂，2=已播终音
    /// localAI[1] 标记：1=已施加追踪起跳速度
    /// </summary>
    public class DarkHarvestGlobalProjectile : GlobalProjectile
    {
        private const int ExpandDuration = (int)(1.75f * 60f); // 炸开停留帧数
        private const float HomingSpeed = 36f;       // 追踪最大速度
        private const float HomingAccel = 1.6f;      // 每帧加速
        private const float HomingInertia = 10f;     // 转向惯性
        private const float PickupDistance = 32f;    // 到达玩家判定距离
        private const int HomingDuration = 60;       // 1s内完成飞行

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ProjectileID.HallowBossLastingRainbow;
        }

        public override bool PreAI(Projectile projectile)
        {
            if (projectile.ai[1] != 777f)
                return true;

            // 完全接管AI
            Player owner = Main.player[projectile.owner];
            if (!owner.active || owner.dead)
            {
                projectile.Kill();
                return false;
            }
            float timer = projectile.ai[0];

            if (timer < ExpandDuration)
            {
                // 炸开阶段：原地停留，让原版AI播放动画
                projectile.ai[0]++;
                if (timer == 0f)
                {
                    projectile.velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), -5f);
                }
                // 向上漂浮一小段距离
                projectile.velocity = Vector2.Lerp(projectile.velocity, new Vector2(0f, -1.2f), 0.08f);
                projectile.scale = MathHelper.Lerp(projectile.scale, 1.9f, 0.15f);
                projectile.timeLeft = 600;

                // 散射红色粒子（更密集）
                if (timer % 1 == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 dustVel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(3f, 7f);
                        Dust d = Dust.NewDustDirect(projectile.Center - new Vector2(8, 8), 16, 16, DustID.RedTorch, dustVel.X, dustVel.Y, 120);
                        d.noGravity = true;
                        d.scale = 2.1f;
                    }
                }
            }
            else
            {
                // 追踪阶段：飞向玩家
                projectile.ai[0]++;
                float elapsed = timer - ExpandDuration;

                if (projectile.localAI[0] < 1f)
                {
                    projectile.localAI[0] = 1f;
                    owner.GetModPlayer<DarkHarvestPlayer>().AddSoulStack();

                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        var gainSfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Dark_Harvest_SFX")
                        {
                            Volume = 0.8f,
                            PitchVariance = 0f
                        };
                        SoundEngine.PlaySound(gainSfx, owner.Center);
                    }
                    else if (Main.netMode == NetmodeID.Server)
                    {
                        var modPacket = ModContent.GetInstance<global::LeagueOfLegendThings.LeagueOfLegendThings>().GetPacket();
                        modPacket.Write((byte)LeaguePacketType.DarkHarvestGainSfx);
                        modPacket.Write(owner.Center.X);
                        modPacket.Write(owner.Center.Y);
                        modPacket.Send();
                    }
                }

                projectile.scale = MathHelper.Lerp(projectile.scale, 1.5f, 0.08f);

                Vector2 toPlayer = owner.Center - projectile.Center;
                float distance = toPlayer.Length();

                if (distance < PickupDistance)
                {
                    if (projectile.localAI[0] < 2f)
                    {
                        projectile.localAI[0] = 2f;
                        if (Main.netMode == NetmodeID.SinglePlayer)
                        {
                            var finalSfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Dark_Harvest_SFX_4")
                            {
                                Volume = 0.8f,
                                PitchVariance = 0f
                            };
                            SoundEngine.PlaySound(finalSfx, owner.Center);
                        }
                        else if (Main.netMode == NetmodeID.Server)
                        {
                            var modPacket = ModContent.GetInstance<global::LeagueOfLegendThings.LeagueOfLegendThings>().GetPacket();
                            modPacket.Write((byte)LeaguePacketType.DarkHarvestFinalSfx);
                            modPacket.Write(owner.Center.X);
                            modPacket.Write(owner.Center.Y);
                            modPacket.Send();
                        }
                    }
                    projectile.Kill();
                    return false;
                }

                if (projectile.localAI[1] < 1f)
                {
                    // 进入追踪阶段的瞬时上抛速度，形成自然抛物线
                    projectile.velocity += new Vector2(0f, -12f);
                    projectile.localAI[1] = 1f;
                }

                toPlayer.Normalize();
                float targetSpeed = Math.Max(8f, distance / HomingDuration * 1.1f);
                float speed = Math.Min(targetSpeed + elapsed * HomingAccel * 0.1f, HomingSpeed);

                // 加速度式追踪：逐步将速度拉向目标方向
                Vector2 desiredVelocity = toPlayer * speed;
                float accelLerp = 1f / HomingInertia;
                projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, accelLerp);

                // 飞行拖尾粒子（每帧生成）
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustDirect(projectile.Center - new Vector2(6, 6), 20, 20, DustID.RedTorch, 0, 0, 150, Scale: 20f);
                    d.noGravity = true;
                    d.scale = 1.6f;
                    d.velocity = -projectile.velocity * 0.3f;
                }

                projectile.timeLeft = 600;
            }

            // 红色照明
            Lighting.AddLight(projectile.Center, 0.7f, 0.1f, 0.1f);

            // 阻止原版AI覆盖速度（否则会环绕目标）
            return false;
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            // 只处理被标记的黑暗收割射弹
            if (projectile.ai[1] != 777f)
                return true;

            // 用红色绘制原版贴图
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[projectile.type];
            int frameY = frameHeight * projectile.frame;
            Rectangle sourceRect = new Rectangle(0, frameY, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            Vector2 drawPos = projectile.Center - Main.screenPosition;

            float alpha = 1f - (projectile.alpha / 255f);

            // Additive 混合绘制红色发光
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            // 外层暗红发光（更大更浓）
            Main.spriteBatch.Draw(
                texture,
                drawPos,
                sourceRect,
                new Color(200, 30, 30) * 0.8f * alpha,
                projectile.rotation,
                origin,
                projectile.scale * 2.2f,
                SpriteEffects.None,
                0f
            );

            // 主体红色
            Main.spriteBatch.Draw(
                texture,
                drawPos,
                sourceRect,
                new Color(255, 50, 50) * 1f * alpha,
                projectile.rotation,
                origin,
                projectile.scale * 5f,
                SpriteEffects.None,
                0f
            );

            // 中心高亮
            Main.spriteBatch.Draw(
                texture,
                drawPos,
                sourceRect,
                new Color(255, 150, 120) * 0.8f * alpha,
                projectile.rotation,
                origin,
                projectile.scale * 0.9f,
                SpriteEffects.None,
                0f
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            return false; // 不绘制原版颜色
        }

        public override void PostAI(Projectile projectile)
        {
            if (projectile.ai[1] != 777f)
                return;

            // 额外红色照明
            Lighting.AddLight(projectile.Center, 0.7f, 0.1f, 0.1f);
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
        }
    }
}
