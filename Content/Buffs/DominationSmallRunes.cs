using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Domination small runes: Eyeball Collection, Ravenous/Ingenious/Treasure/Relentless Hunter
    public class DominationSmallRunesPlayer : ModPlayer
    {
        private const int EyeballMaxStacks = 10;
        private const int HunterMaxStacks = 5;

        private const float EyeballDamagePerStack = 0.008f; // 0.8% per stack
        private const float RavenousLifeStealPerStack = 0.0005f; // 0.05% per stack
        private const float IngeniousPotionReductionPerStack = 0.03f; // 3% per stack
        private const float RelentlessMoveSpeedPerStack = 0.03f; // 3% per stack

        private int eyeballStacks;
        private int hunterStacks;
        private int outOfCombatTimer;
        private float healBuffer;
        private float pendingPotionReduction;

        public override void SaveData(TagCompound tag)
        {
            tag["eyeballStacks"] = eyeballStacks;
            tag["hunterStacks"] = hunterStacks;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("eyeballStacks"))
                eyeballStacks = tag.GetInt("eyeballStacks");
            if (tag.ContainsKey("hunterStacks"))
                hunterStacks = tag.GetInt("hunterStacks");
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleHit(target, damageDone);
        }

        public override void PostUpdateMiscEffects()
        {
            if (outOfCombatTimer > 0)
            {
                outOfCombatTimer--;
            }

            var save = ModContent.GetInstance<RuneSaveSystem>();

            if (save.EyeballCollectionSelected && eyeballStacks > 0)
            {
                Player.GetDamage(DamageClass.Generic) += EyeballDamagePerStack * eyeballStacks;
            }

            if (save.RelentlessHunterSelected && hunterStacks > 0 && outOfCombatTimer <= 0)
            {
                Player.moveSpeed += RelentlessMoveSpeedPerStack * hunterStacks;
            }
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.IngeniousHunterSelected)
                return;

            if (hunterStacks <= 0)
                return;

            if (item.potion && item.healLife > 0)
            {
                float reduction = hunterStacks * IngeniousPotionReductionPerStack;
                pendingPotionReduction = MathHelper.Clamp(reduction, 0f, 0.6f);
            }
        }

        public override void PostUpdate()
        {
            if (pendingPotionReduction > 0f && Player.potionDelay > 0)
            {
                Player.potionDelay = (int)(Player.potionDelay * (1f - pendingPotionReduction));
                pendingPotionReduction = 0f;
            }
        }

        public override void UpdateDead()
        {
            outOfCombatTimer = 0;
            healBuffer = 0f;
            pendingPotionReduction = 0f;
        }

        private void HandleHit(NPC target, int damageDone)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!(save.EyeballCollectionSelected || HasHunterRuneSelected(save)))
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (damageDone > 0)
            {
                outOfCombatTimer = 5 * 60;
            }

            HandleRavenousHeal(save, damageDone);
            HandleKillStacks(save, target);
        }

        private void HandleRavenousHeal(RuneSaveSystem save, int damageDone)
        {
            if (!save.RavenousHunterSelected || hunterStacks <= 0)
                return;

            if (damageDone <= 0)
                return;

            float healAmount = damageDone * (RavenousLifeStealPerStack * hunterStacks);
            healBuffer += healAmount;
            if (healBuffer >= 1f)
            {
                int healInt = (int)healBuffer;
                healBuffer -= healInt;
                int heal = System.Math.Min(healInt, Player.statLifeMax2 - Player.statLife);
                if (heal > 0)
                {
                    Player.statLife += heal;
                    Player.HealEffect(heal, broadcast: true);
                }
            }
        }

        private void HandleKillStacks(RuneSaveSystem save, NPC target)
        {
            if (target.life > 0)
                return;

            if (save.EyeballCollectionSelected)
            {
                if (target.lifeMax < 300)
                    return;
                int add = target.boss ? 2 : 1;
                eyeballStacks = System.Math.Min(eyeballStacks + add, EyeballMaxStacks);
            }

            if (HasHunterRuneSelected(save) && target.boss)
            {
                hunterStacks = System.Math.Min(hunterStacks + 1, HunterMaxStacks);
            }

            if (save.TreasureHunterSelected && hunterStacks > 0)
            {
                int extraCopper = (int)(target.value * 0.05f * hunterStacks);
                extraCopper = System.Math.Min(extraCopper, 100000); // cap at 1 gold
                if (extraCopper > 0)
                {
                    Player.QuickSpawnItem(Player.GetSource_OnHit(target), ItemID.CopperCoin, extraCopper);
                }
            }
        }

        private static bool HasHunterRuneSelected(RuneSaveSystem save)
        {
            return save.RavenousHunterSelected || save.IngeniousHunterSelected ||
                   save.TreasureHunterSelected || save.RelentlessHunterSelected;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            // Relentless Hunter: 5s 无伤才生效
            outOfCombatTimer = 5 * 60;
        }
    }
}
