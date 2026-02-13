using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.Audio;
using System;
using System.Collections.Generic;

namespace LeagueOfLegendThings.Content.Projectiles
{
    public class ArcaneCometProj : ModProjectile
    {
        public override string Texture => "LeagueOfLegendThings/Content/Projectiles/ArcaneCometProjectile";

        private const float SpinSpeed = 0f;
        private const float OrbitSpeed = 0.025f;
        private const float OrbitMajor = 75f;
        private const float OrbitMinor = 15f;
        private const float OrbitTiltDegrees = 15f;
        private const float LaunchSpeed = 22f;
        private const float Gravity = 0.35f;

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.scale = 0.5f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false; // 环绕时不碰撞，发射后再改
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 3600; // 发射态保留较长存活时间
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            // Continuous slow spin
            Projectile.rotation += SpinSpeed;

            // 状态 0：环绕玩家 (CD好了的提示)
            if (Projectile.ai[0] == 0)
            {
                if (!player.active || player.dead) { Projectile.Kill(); return; }

                // 只要在环绕态，就强制保持存活
                Projectile.timeLeft = 2;

                // 环绕逻辑
                float t = (float)(Main.GameUpdateCount * OrbitSpeed);
                float x = (float)Math.Cos(t) * OrbitMajor;
                float y = (float)Math.Sin(t) * OrbitMinor;

                float tilt = MathHelper.ToRadians(OrbitTiltDegrees);
                float cos = (float)Math.Cos(tilt);
                float sin = (float)Math.Sin(tilt);
                Vector2 offset = new Vector2(x * cos - y * sin - 15f, x * sin + y * cos);

                Projectile.Center = player.Center + offset;
                Projectile.localAI[0] = offset.Y < 0f ? 1f : 0f;

                // 简单的魔法粒子
                if (Main.rand.NextBool(1)) // 粒子密度提升，数值越小越密集
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position + new Vector2(20f, 8f), Projectile.width, Projectile.height, DustID.MagicMirror, 0, 0, 150, Color.Cyan, 1.2f);
                }
            }
            // 状态 1：发射至目标
            else
            {
                Projectile.timeLeft = 3600;
                Projectile.tileCollide = true;
                Vector2 targetPos = new Vector2(Projectile.ai[1], Projectile.localAI[0]);

                if (Projectile.localAI[1] == 0f)
                {
                    // 计算抛物线初速度，落点锁定为记录位置
                    Vector2 toTarget = targetPos - Projectile.Center;
                    float distance = toTarget.Length();
                    if (distance < 1f)
                        distance = 1f;

                    float t = MathHelper.Clamp(distance / LaunchSpeed, 12f, 45f);
                    Vector2 gravity = new Vector2(0f, Gravity);
                    Vector2 initialVel = (toTarget - 0.5f * gravity * t * t) / t;
                    Projectile.velocity = initialVel;
                    Projectile.localAI[1] = 1f;
                }

                // 简单重力形成抛物线
                Projectile.velocity.Y += Gravity;

                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                for (int i = 0; i < 30; i++)
                {   // 每帧生成30个
                    Vector2 dustOffset = new Vector2(Main.rand.NextFloat(-Projectile.width / 2f, Projectile.width / 2f), Main.rand.NextFloat(-Projectile.height / 2f, Projectile.height / 2f));
                    Dust.NewDust(Projectile.position + dustOffset, Projectile.width, Projectile.height, DustID.Flare_Blue, Projectile.velocity.X, Projectile.velocity.Y, Scale: 1.2f);
                }
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Projectile.ai[0] == 0)
            {
                // 只处理弹幕本身的层级
                if (Projectile.localAI[0] > 0f)
                {
                    overPlayers.Add(index); // 上半圈在角色层上方
                }
                else
                {
                    behindNPCsAndTiles.Add(index); // 下半圈在角色层下方
                }
                return;
            }
            behindProjectiles.Add(index);
        }

        public override void OnKill(int timeLeft)
        {
            // 仅在发射态 (ai[0] == 1) 产生爆炸效果和 AOE
            if (Projectile.ai[0] == 1)
            {
                Player player = Main.player[Projectile.owner];

                // 视觉特效：蓝白色爆炸粒子
                for (int i = 0; i < 30; i++) 
                {
                    Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, 0, 0, 100, default, 1.5f);
                    d.velocity *= 3f;
                    d.noGravity = true;
                }

                // 范围伤害判定 (半径 4 * 16px = 64px)
                float explosionRadius = 64f;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    // 确保 NPC 有效、不是友方、且在爆炸范围内
                    if (n.active && !n.friendly && !n.dontTakeDamage && Vector2.Distance(Projectile.Center, n.Center) < explosionRadius)
                    {
                        // 使用玩家的属性进行一次打击判定
                        // 注意：由于弹幕本身已经消失，这里手动给 NPC 一个击退和伤害
                        n.SimpleStrikeNPC(Projectile.damage, Projectile.direction, false, 5f, Projectile.DamageType, true, player.luck);
                        
                        // 如果你想让这次伤害也触发其他特效，可以使用 player.ApplyDamageToNPC
                    }
                }

                // 播放爆炸音效
                var sfx2 = new SoundStyle("LeagueOfLegendThings/Content/Projectiles/Arcane_Comet_SFX_2")
                {
                    Volume = 0.75f,
                    PitchVariance = 0.2f
                };
                Terraria.Audio.SoundEngine.PlaySound(sfx2, Projectile.position); 
            }
        }
    }
}