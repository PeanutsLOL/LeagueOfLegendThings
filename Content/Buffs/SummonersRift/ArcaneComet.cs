using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;
using LeagueOfLegendThings.Content.Projectiles;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class ArcaneCometPlayer : ModPlayer
    {
        public int cometCooldownTimer = 0;
        public int readyCometIndex = -1; // 记录当前环绕的彗星索引

        private Vector2 lastHitPosition;

        public override void PostUpdate()
        {
            if (cometCooldownTimer > 0)
            {
                cometCooldownTimer--;
            }
            else if (readyCometIndex == -1) // CD好了且没有彗星
            {
                var runeSave = ModContent.GetInstance<RuneSaveSystem>();
                if (runeSave.ArcaneCometSelected)
                {
                    // 生成环绕彗星（预备状态），伤害先传0，发射时再更新
                    var sfx0 = new SoundStyle("LeagueOfLegendThings/Content/Projectiles/Arcane_Comet_SFX_0")
                    {
                        Volume = 0.75f,
                        PitchVariance = 0.2f
                    };
                    Terraria.Audio.SoundEngine.PlaySound(sfx0, Player.position);
                    readyCometIndex = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<ArcaneCometProj>(), 0, 0f, Player.whoAmI, 0); // ai[0]=0 表示预备态
                }
            }
        }

        private void HandleArcaneComet(NPC target)
        {
            // 触发条件：有预备好的彗星 + 攻击动画结束 + 目标合法
            if (readyCometIndex == -1)
                return;

            if (Player.itemAnimation != 0)
                return;

            if (target == null || !target.active || target.friendly || target.lifeMax <= 5)
                return;

            // 修正：只允许锁定非友方且生命值大于5的目标
            if (target.friendly || target.lifeMax <= 5)
                return;

            lastHitPosition = target.Center;

            Projectile proj = Main.projectile[readyCometIndex];
            if (!proj.active || proj.type != ModContent.ProjectileType<ArcaneCometProj>())
            {
                readyCometIndex = -1; // 如果弹幕意外消失，重置索引
                return;
            }

            // 计算实时伤害
            float manaRatio = Player.statManaMax2 > 0 ? (float)Player.statMana / Player.statManaMax2 : 0f;
            int finalDamage = (int)(30 + 270 * manaRatio);

            // 转换为发射态
            proj.damage = (int)Player.GetTotalDamage(DamageClass.Magic).ApplyTo(finalDamage);
            proj.ai[0] = 1; // ai[0]=1 表示发射态
            proj.ai[1] = lastHitPosition.X;
            proj.localAI[0] = lastHitPosition.Y;
            proj.localAI[1] = 0f;
            proj.Center = Player.Center + new Vector2(0f, -60f);
            proj.velocity = Vector2.Zero;
            proj.netUpdate = true;

            var sfx1 = new SoundStyle("LeagueOfLegendThings/Content/Projectiles/Arcane_Comet_SFX_1")
            {
                Volume = 0.75f,
                PitchVariance = 0.2f
            };
            Terraria.Audio.SoundEngine.PlaySound(sfx1, Player.position);

            // 进入冷却
            readyCometIndex = -1;
            int maxCD = 1200;
            int minCD = 480;
            float reductionFactor = MathHelper.Clamp(Player.statManaMax2 / 220f, 0f, 1f);
            cometCooldownTimer = (int)MathHelper.Lerp(maxCD, minCD, reductionFactor);
        }
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (item.DamageType == DamageClass.Magic)
                HandleArcaneComet(target);
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.DamageType == DamageClass.Magic)
                HandleArcaneComet(target);
        }
    }
}