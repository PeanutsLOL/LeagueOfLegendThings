using System.IO;
using LeagueOfLegendThings.Content.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    // First Strike
    public class FirstStrikePlayer : ModPlayer
    {
        public bool HasFirstStrike;
        private int trackingTimer;
        private int trackedTargetId = -1;
        private int sumDamage;

        private int cooldownTimer;
        private bool tookDamageDuringCooldown;
        private int lastRecordedLife;

        public override void ResetEffects()
        {
            HasFirstStrike = false;
        }

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            HasFirstStrike = save.FirstStrikeSelected;
        }

        public override void UpdateDead()
        {
            ResetTracking();
            cooldownTimer = 0;
            tookDamageDuringCooldown = false;
            lastRecordedLife = 0;
        }

        public override void PostUpdate()
        {
            // init last life
            if (lastRecordedLife == 0)
                lastRecordedLife = Player.statLife;

            // detect player damage to possibly restart cooldown
            if (cooldownTimer > 0 && Player.statLife < lastRecordedLife)
            {
                // re-enter cooldown
                cooldownTimer = 420;
                tookDamageDuringCooldown = true;
                var sfxReady = new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX")
                {
                    Volume = 0.75f,
                    PitchVariance = 0.1f
                };
                SoundEngine.PlaySound(sfxReady, Player.Center);
            }

            lastRecordedLife = Player.statLife;

            if (trackingTimer > 0)
            {
                trackingTimer--;
                if (trackingTimer <= 0)
                {
                    TryFireFirstStrike();
                }
            }

            if (cooldownTimer > 0)
            {
                cooldownTimer--;
                if (cooldownTimer <= 0)
                {
                    if (!tookDamageDuringCooldown)
                    {
                        var sfxReady = new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX")
                        {
                            Volume = 0.75f,
                            PitchVariance = 0.1f
                        };
                        SoundEngine.PlaySound(sfxReady, Player.Center);
                    }

                    tookDamageDuringCooldown = false;
                }
            }
        }

        private void TryFireFirstStrike()
        {
            // spawn projectile to target and start cooldown
            if (trackedTargetId < 0 || trackedTargetId >= Main.maxNPCs)
            {
                ResetTracking();
                return;
            }

            NPC target = Main.npc[trackedTargetId];
            if (!target.active)
            {
                ResetTracking();
                return;
            }

            if (Main.myPlayer == Player.whoAmI)
            {
                int proj = Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<FirstStrikeProj>(),
                    0,
                    0f,
                    Player.whoAmI
                );

                if (proj >= 0 && proj < Main.maxProjectiles)
                {
                    Projectile p = Main.projectile[proj];
                    // pass targetId and sumDamage
                    p.ai[1] = trackedTargetId + 1; // positive for npc
                    p.localAI[0] = sumDamage;
                    p.netUpdate = true;
                }

                var sfxFlyPeriod = new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX_3")
                {
                    Volume = 0.75f,
                    PitchVariance = 0.1f
                };
                SoundEngine.PlaySound(sfxFlyPeriod, Player.Center);
            }

            // start cooldown
            cooldownTimer = 420;
            tookDamageDuringCooldown = false;
            ResetTracking();
        }

        private void ResetTracking()
        {
            trackedTargetId = -1;
            sumDamage = 0;
            trackingTimer = 0;
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!HasFirstStrike || !target.boss)
                return;

            if (cooldownTimer > 0)
                return;

            if (trackingTimer > 0)
            {
                if (target.whoAmI == trackedTargetId)
                {
                    sumDamage += damageDone;
                }
            }
            else
            {
                trackedTargetId = target.whoAmI;
                sumDamage = damageDone;
                trackingTimer = 180; // 3s
                SoundEngine.PlaySound(new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX_4"), Player.Center);
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!HasFirstStrike || !target.boss)
                return;

            if (cooldownTimer > 0)
                return;

            if (trackingTimer > 0)
            {
                if (target.whoAmI == trackedTargetId)
                {
                    sumDamage += damageDone;
                }
            }
            else
            {
                trackedTargetId = target.whoAmI;
                sumDamage = damageDone;
                trackingTimer = 180;
                var sfxTriggered = new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX_4")
                {
                    Volume = 0.75f,
                    PitchVariance = 0.1f
                };
                SoundEngine.PlaySound(sfxTriggered, Player.Center);
            }
        }
    }

    public class FirstStrikeProj : ModProjectile
    {
        private int TargetId => (int)Projectile.ai[1]; // stored as npcId+1

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.NanoBullet;

        public override void SetStaticDefaults()
        {
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.rotation += MathHelper.PiOver2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 600;

            Entity target = GetTarget();
            if (target == null)
            {
                // fly forward slowly and die
                Projectile.velocity *= 0.98f;
                return;
            }

            Vector2 toTarget = target.Center - Projectile.Center;
            float dist = toTarget.Length();

            for (int i = 0; i < 4; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFlare, Projectile.velocity.X * 0.3f, Projectile.velocity.Y * 0.3f, 100, default, 1.2f);
                if (dust >= 0 && dust < Main.maxDust)
                {
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.2f;
                }
            }

            if (dist < 10f)
            {
                if (target is NPC npc && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int sum = (int)Projectile.localAI[0];
                    float classBonus = RuneDamageHelper.GetHighestClassBonus(owner);
                    int weaponDmg = RuneDamageHelper.GetHeldWeaponDamage(owner);
                    int dmg = (int)(sum * 0.15f * (1f + classBonus) + npc.defense * 5 + weaponDmg * 1.5f);
                    if (dmg < 1) dmg = 1;
                    npc.SimpleStrikeNPC(dmg, owner.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                }

                if (target is NPC npc2)
                {
                    var sfxHit = new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX_2")
                    {
                        Volume = 0.75f,
                        PitchVariance = 0.1f
                    };
                    SoundEngine.PlaySound(sfxHit, npc2.Center);
                }

                Projectile.Kill();
                return;
            }

            float speed = 120f;
            Vector2 desired = toTarget / dist * speed;
            float inertia = 2f;
            Projectile.velocity = (Projectile.velocity * (inertia - 1f) + desired) / inertia;
            // rotate sprite to face movement, plus 90deg offset
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        private Entity GetTarget()
        {
            if (TargetId <= 0)
                return null;

            int npcId = TargetId - 1;
            if (npcId >= 0 && npcId < Main.maxNPCs)
            {
                NPC npc = Main.npc[npcId];
                if (npc.active)
                    return npc;
            }

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int sum = (int)Projectile.localAI[0];
                int dmg = (int)(sum * 0.15f + target.defense * 5);
                if (dmg < 1) dmg = 1;
                target.SimpleStrikeNPC(dmg, Projectile.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
            }

            SoundEngine.PlaySound(new SoundStyle("LeagueOfLegendThings/Content/SFX/First_Strike_SFX_2"), target.Center);
            Projectile.Kill();
        }
    }
}