using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Resolve small runes
    public class ResolveSmallRunesPlayer : ModPlayer
    {
        private const float DemolishBossRange = 600f;
        private const int DemolishCooldown = 35 * 60;
        private const int DemolishStackInterval = 30; // 0.5s
        private const int DemolishDecayInterval = 60; // 1s
        private const int DemolishMaxStacks = 6;
        private const int FontOfLifeCooldown = 10 * 60;
        private const int FontOfLifeHeal = 4;
        private const int ConditioningTime = 8 * 60 * 60;
        private const int ConditioningDefense = 6;
        private const int SecondWindDuration = 5 * 60;
        private const int BonePlatingDuration = 5 * 60;
        private const int BonePlatingCharges = 3;
        private const int BonePlatingReduction = 6;
        private const int OvergrowthInterval = 60 * 60;
        private const int OvergrowthMax = 50;
        private const float RevitalizeBonus = 0.10f;
        private const int UnflinchingDuration = 2 * 60;
        private const float UnflinchingMoveSpeed = 0.10f;
        private const float UnflinchingKnockback = 0.2f;

        private int demolishStacks;
        private int demolishStackTimer;
        private int demolishDecayTimer;
        private bool demolishInRange;
        private int demolishCooldownTimer;
        private int fontTimer;
        private int conditioningTimer;
        private bool conditioningActive;
        private int secondWindTimer;
        private int bonePlatingTimer;
        private int bonePlatingCharges;
        private int overgrowthTimer;
        private int overgrowthBonusLife;
        private int unflinchingTimer;

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();

            UpdateDemolish(save);
            if (fontTimer > 0) fontTimer--;
                        if (demolishCooldownTimer > 0) demolishCooldownTimer--;
            if (secondWindTimer > 0) secondWindTimer--;
            if (bonePlatingTimer > 0) bonePlatingTimer--;
            if (unflinchingTimer > 0) unflinchingTimer--;

            if (save.ConditioningSelected && !conditioningActive)
            {
                conditioningTimer++;
                if (conditioningTimer >= ConditioningTime)
                {
                    conditioningActive = true;
                }
            }

            if (save.ConditioningSelected && conditioningActive)
            {
                Player.statDefense += ConditioningDefense;
            }

            if (save.OvergrowthSelected)
            {
                overgrowthTimer++;
                if (overgrowthTimer >= OvergrowthInterval)
                {
                    overgrowthTimer = 0;
                    overgrowthBonusLife = System.Math.Min(overgrowthBonusLife + 5, OvergrowthMax);
                }
                Player.statLifeMax2 += overgrowthBonusLife;
            }

            if (save.UnflinchingSelected && unflinchingTimer > 0)
            {
                Player.moveSpeed += UnflinchingMoveSpeed;
                Player.noKnockback = Player.noKnockback || UnflinchingKnockback > 0f;
            }

            if (save.SecondWindSelected && secondWindTimer > 0)
            {
                if (Main.GameUpdateCount % 60 == 0)
                {
                    int heal = System.Math.Min(1, Player.statLifeMax2 - Player.statLife);
                    if (heal > 0)
                    {
                        Player.statLife += heal;
                        Player.HealEffect(heal, broadcast: true);
                    }
                }
            }
        }

        public override void UpdateDead()
        {
            demolishStacks = 0;
            demolishStackTimer = 0;
            demolishDecayTimer = 0;
            demolishInRange = false;
            demolishCooldownTimer = 0;
            fontTimer = 0;
            conditioningTimer = 0;
            conditioningActive = false;
            secondWindTimer = 0;
            bonePlatingTimer = 0;
            bonePlatingCharges = 0;
            overgrowthTimer = 0;
            overgrowthBonusLife = 0;
            unflinchingTimer = 0;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target);
        }

        private void HandleHit(NPC target)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!(save.DemolishSelected || save.FontOfLifeSelected))
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (save.DemolishSelected && demolishStacks >= DemolishMaxStacks)
            {
                int bonusDamage = 100 + (int)(target.lifeMax * 0.35f);
                target.SimpleStrikeNPC(bonusDamage, Player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Demolish_SFX_3")
                {
                    Volume = 0.75f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(sfx, Player.Center);

                demolishStacks = 0;
                demolishStackTimer = 0;
                demolishDecayTimer = 0;
                demolishCooldownTimer = DemolishCooldown;
            }

            if (save.FontOfLifeSelected && fontTimer <= 0 && Player.statLife < Player.statLifeMax2)
            {
                int heal = System.Math.Min(FontOfLifeHeal, Player.statLifeMax2 - Player.statLife);
                if (heal > 0)
                {
                    Player.statLife += heal;
                    Player.HealEffect(heal, broadcast: true);
                }
                fontTimer = FontOfLifeCooldown;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.BonePlatingSelected)
                return;

            if (bonePlatingTimer > 0 && bonePlatingCharges > 0)
            {
                modifiers.FinalDamage -= BonePlatingReduction;
                bonePlatingCharges--;
                if (bonePlatingCharges <= 0)
                {
                    bonePlatingTimer = 0;
                }
            }
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.SecondWindSelected)
            {
                secondWindTimer = SecondWindDuration;
            }

            if (save.BonePlatingSelected)
            {
                bonePlatingTimer = BonePlatingDuration;
                bonePlatingCharges = BonePlatingCharges;
            }

            if (save.UnflinchingSelected)
            {
                unflinchingTimer = UnflinchingDuration;
            }
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().RevitalizeSelected)
                return;

            healValue = (int)(healValue * (1f + RevitalizeBonus));
        }

        public override void PostUpdate()
        {
            if (ModContent.GetInstance<RuneSaveSystem>().ShieldBashSelected)
            {
                if (Player.statDefense >= 50)
                {
                    Player.GetDamage(DamageClass.Generic) += 0.05f;
                }
            }
        }

        private void UpdateDemolish(RuneSaveSystem save)
        {
            if (!save.DemolishSelected)
                return;

            if (demolishCooldownTimer > 0)
                return;

            NPC boss = FindNearestBoss(DemolishBossRange);
            bool inRange = boss != null;

            if (inRange && !demolishInRange)
            {
                var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Demolish_SFX_0")
                {
                    Volume = 0.75f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(sfx, Player.Center);
            }

            demolishInRange = inRange;

            if (inRange)
            {
                if (demolishStacks < DemolishMaxStacks)
                {
                    demolishStackTimer++;
                    if (demolishStackTimer >= DemolishStackInterval)
                    {
                        demolishStackTimer = 0;
                        demolishStacks++;
                        if (demolishStacks < DemolishMaxStacks)
                        {
                            var sfx1 = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Demolish_SFX_1")
                            {
                                Volume = 0.75f,
                                PitchVariance = 0f
                            };
                            SoundEngine.PlaySound(sfx1, Player.Center);
                        }
                        else
                        {
                            var sfx2 = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Demolish_SFX_2")
                            {
                                Volume = 0.75f,
                                PitchVariance = 0f
                            };
                            SoundEngine.PlaySound(sfx2, Player.Center);
                        }
                    }
                }
                demolishDecayTimer = 0;
            }
            else
            {
                if (demolishStacks > 0)
                {
                    demolishDecayTimer++;
                    if (demolishDecayTimer >= DemolishDecayInterval)
                    {
                        demolishDecayTimer = 0;
                        demolishStacks--;
                        var sfx4 = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Demolish_SFX_4")
                        {
                            Volume = 0.75f,
                            PitchVariance = 0f
                        };
                        SoundEngine.PlaySound(sfx4, Player.Center);

                        if (demolishStacks <= 0)
                        {
                            var sfx5 = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Demolish_SFX_5")
                            {
                                Volume = 0.75f,
                                PitchVariance = 0f
                            };
                            SoundEngine.PlaySound(sfx5, Player.Center);
                        }
                    }
                }
                demolishStackTimer = 0;
            }
        }

        private NPC FindNearestBoss(float range)
        {
            NPC nearest = null;
            float best = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.boss)
                    continue;
                float dist = Vector2.Distance(Player.Center, npc.Center);
                if (dist < best)
                {
                    best = dist;
                    nearest = npc;
                }
            }
            return nearest;
        }
    }
}
