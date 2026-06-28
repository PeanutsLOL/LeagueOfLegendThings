using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.Projectiles
{
    public class LethalTempoNoteProjectile : ModProjectile
    {
        public override string Texture => "LeagueOfLegendThings/Content/Projectiles/LethalTempoNoteProjectile";
        // ai[0]：0 = 向上弧，1 = 向下弧
        // ai[1]：飞行基准方向角（弧度），在 OnSpawn 中设置，用于对称弧线
        // localAI[0]：阶段，0 = 抛物线，1 = 追踪
        // localAI[1]：抛物线计时器

        private static float ForwardSpeed = 5f;        // 抛物线前向速度
        private static float ArcLift = 7.0f;          // 抛物线初始垂直速度幅度
        private static float Gravity = 0.14f;         // 抛物线重力（越小曲线越长）
        private static int ArcTimeLimit = 70;         // 抛物线最长持续帧数
        private static int ArcMinTime = 15;           // 至少跑这么多帧再允许进入追踪
        private static float HomingRange = 1920f;      // 追踪范围
        private static float HomingSpeedStart = 10f;  // 追踪初速度（开始较慢）
        private static float HomingSpeedMax = 66f;    // 追踪最大速度（后半段很快）
        private static float HomingAccel = 2.0f;      // 追踪加速度（每帧加速）
        private static float HomingInertia = 6f;     // 追踪转向惯性（减小以便快速贴合）

        public override void SetDefaults()
        {
            // 贴图 32×32，碰撞盒匹配
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.scale = 1.0f;

            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.tileCollide = false; // 穿过物块
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.light = 0.5f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1; // 自定义 AI

            // 保证命中后立即消失，不重复触发
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // 飞行基准方向：从玩家指向目标
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            // 垂直方向：dir 逆时针旋转 90° = (-dir.y, dir.x)
            Vector2 perp = new Vector2(-dir.Y, dir.X);

            // 弧线方向：ai[0]=0 沿 +perp（上弧），ai[0]=1 沿 -perp（下弧）
            float arcSign = Projectile.ai[0] == 0f ? 1f : -1f;

            // 初速度：前向分量 + 垂直弧线分量（相对飞行方向对称）
            Projectile.velocity = dir * ForwardSpeed + perp * ArcLift * arcSign;

            // 存储飞行方向角，供后续弧线阶段使用
            Projectile.ai[1] = dir.ToRotation();

            Projectile.localAI[0] = 0f; // 阶段：抛物线
            Projectile.localAI[1] = 0f; // 计时（抛物线阶段计时）
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                RunArcPhase();
            }
            else
            {
                RunHomingPhase();
            }

            // 贴图箭头朝上：旋转使箭头沿速度方向
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.rand.NextBool(2))
            {
                Vector2 tailPos = Projectile.position - Projectile.velocity * 0.6f;

                // 主尾焰
                Dust star = Dust.NewDustDirect(
                    tailPos,
                    Projectile.width,
                    Projectile.height,
                    DustID.Enchanted_Gold,
                    Projectile.velocity.X * 0.15f,
                    Projectile.velocity.Y * 0.15f,
                    100,
                    default,
                    1.8f
                );
                star.noGravity = true;
                star.velocity *= 0.35f;
                star.fadeIn = 1.3f;

                // 辅助光晕：金色火焰
                Dust glow = Dust.NewDustDirect(
                    tailPos,
                    Projectile.width,
                    Projectile.height,
                    DustID.GoldFlame,
                    Projectile.velocity.X * 0.1f,
                    Projectile.velocity.Y * 0.1f,
                    120,
                    default,
                    1.0f
                );
                glow.noGravity = true;
                glow.velocity *= 0.3f;
            }
        }

        private void RunArcPhase()
        {
            Projectile.localAI[1]++;

            // 从存储的角度恢复飞行方向与垂直方向
            float baseAngle = Projectile.ai[1];
            Vector2 dir = new Vector2((float)Math.Cos(baseAngle), (float)Math.Sin(baseAngle));
            Vector2 perp = new Vector2(-dir.Y, dir.X); // 逆时针 90°

            // 弧线符号：L(ai[0]=0) 沿 +perp，R(ai[0]=1) 沿 -perp
            float arcSign = Projectile.ai[0] == 0f ? 1f : -1f;

            // 沿飞行方向的速度阻尼
            float forwardSpeed = Vector2.Dot(Projectile.velocity, dir);
            forwardSpeed *= 0.92f;
            // 垂直分量：减速（拉回飞行轴线），实现镜像对称的弧线
            float perpSpeed = Vector2.Dot(Projectile.velocity, perp);
            perpSpeed -= Gravity * arcSign; // 向飞行轴线拉回

            Projectile.velocity = dir * forwardSpeed + perp * perpSpeed;

            // 顶点判断：垂直分量过零即到弧顶
            float newPerpSpeed = Vector2.Dot(Projectile.velocity, perp);
            bool passedApex = (arcSign > 0f && newPerpSpeed <= 0f)  // +perp 抛 → perp 降为 ≤0
                           || (arcSign < 0f && newPerpSpeed >= 0f); // -perp 抛 → perp 升为 ≥0

            bool timeout = Projectile.localAI[1] >= ArcTimeLimit;
            if ((passedApex && Projectile.localAI[1] >= ArcMinTime) || timeout)
            {
                Projectile.localAI[0] = 1f;
                Projectile.localAI[1] = 0f; // 切换追踪后计时重新开始，用于加速
            }
        }

        private void RunHomingPhase()
        {
            NPC target = FindTarget();
            if (target == null) return;

            // 根据追踪时间逐帧提速，直到封顶
            float desiredSpeed = HomingSpeedStart + Projectile.localAI[1] * HomingAccel;
            if (desiredSpeed > HomingSpeedMax)
                desiredSpeed = HomingSpeedMax;
            Projectile.localAI[1]++;

            Vector2 desiredVel = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * desiredSpeed;
            Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + desiredVel) / HomingInertia;

            // 保持上限
            float speed = Projectile.velocity.Length();
            if (speed > desiredSpeed)
            {
                Projectile.velocity *= desiredSpeed / speed;
            }
        }

        private NPC FindTarget()
        {
            NPC best = null;
            float bestDist = HomingRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy()) continue;

                float dist = Vector2.Distance(Projectile.Center, npc.Center);
                if (dist < bestDist && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height))
                {
                    bestDist = dist;
                    best = npc;
                }
            }

            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                tex,
                drawPos,
                null,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.GoldFlame,
                    0f,
                    0f,
                    100,
                    default,
                    1.5f
                );
                dust.noGravity = true;
                dust.velocity *= 1.4f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item92, Projectile.position);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    target.position,
                    target.width,
                    target.height,
                    DustID.Electric,
                    0f,
                    0f,
                    100,
                    default,
                    0.7f
                );
                dust.noGravity = false;
                dust.velocity *= 0.5f;
            }

            // 命中即移除（黄色火花）
            Dust dustOnHit = Dust.NewDustDirect(
                target.position,
                target.width,
                target.height,
                DustID.GoldFlame,
                0f,
                0f,
                100,
                default,
                2.0f
            );
            dustOnHit.noGravity = true;
            dustOnHit.color = Color.Yellow;
            dustOnHit.scale = 1.4f;
            dustOnHit.fadeIn = 1.1f;

            Projectile.Kill();
        }
    }
}