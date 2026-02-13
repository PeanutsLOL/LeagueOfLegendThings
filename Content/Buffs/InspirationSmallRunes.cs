using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Inspiration small runes
    public class InspirationSmallRunesPlayer : ModPlayer
    {
        private const int FlashtraptionOutOfCombat = 10 * 60;
        private const float FlashtraptionMoveSpeed = 0.08f;
        private const int MagicalFootwearTime = 8 * 60 * 60;
        private const float MagicalFootwearMoveSpeed = 0.10f;
        private const int CashBackCap = 5000; // 50 silver
        private const float CashBackPercent = 0.05f;
        private const int TripleTonicDuration = 30 * 60 * 60;
        private const float TripleTonicDamage = 0.14f;
        private const int TimeWarpMana = 20;
        private const int TimeWarpDuration = 2 * 60;
        private const float TimeWarpMoveSpeed = 0.10f;
        private const int TimeWarpHealDuration = 5 * 60;
        private const int TimeWarpHealTick = 30;
        private const int BiscuitInterval = 20 * 60;
        private const int BiscuitHeal = 10;
        private const int BiscuitMana = 10;
        private const float CosmicInsightReduction = 0.10f;
        private const float ApproachVelocityMoveSpeed = 0.20f;
        private const int JackWindow = 10 * 60;
        private const int JackActiveDuration = 5 * 60;
        private const int JackCooldown = 10 * 60;
        private const float JackDamage = 1.00f;

        private int outOfCombatTimer;
        private int magicalFootwearTimer;
        private bool magicalFootwearReady;
        private int tripleTonicTimer;
        private int timeWarpTimer;
        private int timeWarpHealTimer;
        private int timeWarpHealTickTimer;
        private int timeWarpHealRemaining;
        private int biscuitTimer;
        private int jackWindowTimer;
        private int jackActiveTimer;
        private int jackCooldownTimer;
        private bool jackMelee;
        private bool jackRanged;
        private bool jackMagic;
        private bool jackSummon;
        private float pendingPotionReduction;
        private bool initialized;

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();

            if (!initialized)
            {
                magicalFootwearTimer = MagicalFootwearTime;
                biscuitTimer = BiscuitInterval;
                initialized = true;
            }

            if (outOfCombatTimer > 0) outOfCombatTimer--;
            if (magicalFootwearTimer > 0) magicalFootwearTimer--;
            if (tripleTonicTimer > 0) tripleTonicTimer--;
            if (timeWarpTimer > 0) timeWarpTimer--;
            if (timeWarpHealTimer > 0) timeWarpHealTimer--;
            if (timeWarpHealTickTimer > 0) timeWarpHealTickTimer--;
            if (biscuitTimer > 0) biscuitTimer--;
            if (jackWindowTimer > 0) jackWindowTimer--;
            if (jackActiveTimer > 0) jackActiveTimer--;
            if (jackCooldownTimer > 0) jackCooldownTimer--;

            if (jackWindowTimer <= 0 && jackActiveTimer <= 0)
            {
                jackMelee = false;
                jackRanged = false;
                jackMagic = false;
                jackSummon = false;
            }

            if (save.HextechFlashtraptionSelected)
            {
                Player.buffImmune[BuffID.ChaosState] = true;
                if (Player.HeldItem != null && Player.HeldItem.type == ItemID.RodofDiscord)
                {
                    Player.HeldItem.mana = 150;
                }
                if (outOfCombatTimer <= 0)
                {
                    Player.moveSpeed += FlashtraptionMoveSpeed;
                }
            }
            else
            {
                if (Player.HeldItem != null && Player.HeldItem.type == ItemID.RodofDiscord)
                {
                    Player.HeldItem.mana = 100;
                }
            }

            if (save.MagicalFootwearSelected)
            {
                if (!magicalFootwearReady)
                {
                    if (magicalFootwearTimer <= 0)
                    {
                        magicalFootwearReady = true;
                    }
                }
                if (magicalFootwearReady)
                {
                    Player.moveSpeed += MagicalFootwearMoveSpeed;
                }
            }

            if (save.TripleTonicSelected && tripleTonicTimer > 0)
            {
                Player.GetDamage(DamageClass.Generic) += TripleTonicDamage;
            }

            if (save.TimeWarpTonicSelected && timeWarpTimer > 0)
            {
                Player.moveSpeed += TimeWarpMoveSpeed;
            }

            if (save.CosmicInsightSelected && pendingPotionReduction > 0f && Player.potionDelay > 0)
            {
                Player.potionDelay = (int)(Player.potionDelay * (1f - pendingPotionReduction));
                pendingPotionReduction = 0f;
            }

            if (save.ApproachVelocitySelected)
            {
                NPC target = FindNearestHostileWithDebuff(400f);
                if (target != null)
                {
                    Player.moveSpeed += ApproachVelocityMoveSpeed;
                }
            }

            if (save.JackOfAllTradesSelected && jackActiveTimer > 0)
            {
                Player.GetDamage(DamageClass.Generic) += JackDamage;
            }

            if (save.BiscuitDeliverySelected)
            {
                if (biscuitTimer <= 0)
                {
                    biscuitTimer = BiscuitInterval;
                    if (Player.statLife < Player.statLifeMax2)
                    {
                        int heal = System.Math.Min(BiscuitHeal, Player.statLifeMax2 - Player.statLife);
                        Player.statLife += heal;
                        Player.HealEffect(heal, broadcast: true);
                    }
                    if (Player.statMana < Player.statManaMax2)
                    {
                        Player.statMana = System.Math.Min(Player.statMana + BiscuitMana, Player.statManaMax2);
                        Player.ManaEffect(BiscuitMana);
                    }
                }
            }
            if (save.TimeWarpTonicSelected && timeWarpHealTimer > 0 && timeWarpHealRemaining > 0)
            {
                if (timeWarpHealTickTimer <= 0)
                {
                    timeWarpHealTickTimer = TimeWarpHealTick;
                    int heal = System.Math.Min(1, timeWarpHealRemaining);
                    if (heal > 0 && Player.statLife < Player.statLifeMax2)
                    {
                        Player.statLife += heal;
                        Player.HealEffect(heal, broadcast: true);
                        timeWarpHealRemaining -= heal;
                    }
                }
            }
        }

        public override void UpdateDead()
        {
            outOfCombatTimer = 0;
            magicalFootwearTimer = 0;
            magicalFootwearReady = false;
            tripleTonicTimer = 0;
            timeWarpTimer = 0;
            timeWarpHealTimer = 0;
            timeWarpHealTickTimer = 0;
            timeWarpHealRemaining = 0;
            biscuitTimer = 0;
            jackWindowTimer = 0;
            jackActiveTimer = 0;
            jackCooldownTimer = 0;
            jackMelee = false;
            jackRanged = false;
            jackMagic = false;
            jackSummon = false;
            pendingPotionReduction = 0f;
            initialized = false;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            outOfCombatTimer = FlashtraptionOutOfCombat;
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (item.potion && item.healLife > 0)
            {
                if (save.TripleTonicSelected)
                {
                    tripleTonicTimer = TripleTonicDuration;
                }
                if (save.TimeWarpTonicSelected)
                {
                    timeWarpTimer = TimeWarpDuration;
                    if (Player.statMana < Player.statManaMax2)
                    {
                        int addMana = System.Math.Min(TimeWarpMana, Player.statManaMax2 - Player.statMana);
                        Player.statMana += addMana;
                        Player.ManaEffect(addMana);
                    }
                    timeWarpHealTimer = TimeWarpHealDuration;
                    timeWarpHealRemaining += healValue;
                }
                if (save.CosmicInsightSelected)
                {
                    pendingPotionReduction = CosmicInsightReduction;
                }
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, item.DamageType);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, proj.DamageType);
        }

        private void HandleHit(NPC target, DamageClass damageType)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!(save.CashBackSelected || save.JackOfAllTradesSelected))
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (save.CashBackSelected && target.life <= 0)
            {
                int extraCopper = (int)(target.value * CashBackPercent);
                extraCopper = System.Math.Min(extraCopper, CashBackCap);
                if (extraCopper > 0)
                {
                    Player.QuickSpawnItem(Player.GetSource_OnHit(target), ItemID.CopperCoin, extraCopper);
                }
            }

            if (save.JackOfAllTradesSelected && jackCooldownTimer <= 0 && jackActiveTimer <= 0)
            {
                if (jackWindowTimer <= 0)
                {
                    jackWindowTimer = JackWindow;
                    jackMelee = false;
                    jackRanged = false;
                    jackMagic = false;
                    jackSummon = false;
                }

                if (damageType == DamageClass.Melee) jackMelee = true;
                if (damageType == DamageClass.Ranged) jackRanged = true;
                if (damageType == DamageClass.Magic) jackMagic = true;
                if (damageType == DamageClass.Summon) jackSummon = true;

                int count = 0;
                if (jackMelee) count++;
                if (jackRanged) count++;
                if (jackMagic) count++;
                if (jackSummon) count++;
                if (count >= 3)
                {
                    jackActiveTimer = JackActiveDuration;
                    jackCooldownTimer = JackCooldown;
                    jackWindowTimer = 0;
                }
            }
        }

        private NPC FindNearestHostileWithDebuff(float range)
        {
            NPC nearest = null;
            float best = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.lifeMax <= 5)
                    continue;
                if (!HasAnyDebuff(npc))
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

        private static bool HasAnyDebuff(NPC npc)
        {
            for (int i = 0; i < npc.buffType.Length; i++)
            {
                int type = npc.buffType[i];
                if (type <= 0)
                    continue;
                if (npc.buffTime[i] <= 0)
                    continue;
                if (Main.debuff[type])
                    return true;
            }
            return false;
        }
    }
}
