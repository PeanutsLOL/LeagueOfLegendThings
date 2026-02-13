using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    // Sorcery small runes
    public class SorcerySmallRunesPlayer : ModPlayer
    {
        private const float AxiomMagicDamage = 0.04f;
        private const float TranscendenceDamage = 0.04f;
        private const float CelerityMoveSpeed = 0.04f;
        private const float AbsoluteFocusDamage = 0.05f;
        private const float WaterwalkingMoveSpeed = 0.10f;
        private const int NimbusDuration = 2 * 60;
        private const float NimbusMoveSpeed = 0.20f;
        private const int ManaflowCooldown = 20 * 60;
        private const int ManaflowMaxStacks = 6;
        private const int ScorchCooldown = 8 * 60;
        private const int ScorchDamage = 10;
        private const int GatheringStormInterval = 60 * 60 * 60;
        private const float GatheringStormDamagePerStack = 0.01f;
        private const float TranscendencePotionReduction = 0.05f;

        private int manaflowStacks;
        private int manaflowTimer;
        private int nimbusTimer;
        private int scorchTimer;
        private int gatheringStormTimer;
        private int gatheringStormStacks;
        private float pendingPotionReduction;

        public override void SaveData(TagCompound tag)
        {
            tag["gatheringStormStacks"] = gatheringStormStacks;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("gatheringStormStacks"))
                gatheringStormStacks = tag.GetInt("gatheringStormStacks");
        }

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();

            if (manaflowTimer > 0)
                manaflowTimer--;
            if (nimbusTimer > 0)
                nimbusTimer--;
            if (scorchTimer > 0)
                scorchTimer--;

            if (save.AxiomArcanistSelected)
            {
                Player.GetDamage(DamageClass.Magic) += AxiomMagicDamage;
            }
            if (save.CeleritySelected)
            {
                Player.moveSpeed += 0.25f;
            }
            if (save.AbsoluteFocusSelected && Player.statLife >= Player.statLifeMax2 * 0.7f)
            {
                Player.GetDamage(DamageClass.Magic) += 0.10f;
            }
            if (save.WaterwalkingSelected && Player.wet)
            {
                Player.moveSpeed += WaterwalkingMoveSpeed;
            }
            if (save.NimbusCloakSelected && nimbusTimer > 0)
            {
                Player.moveSpeed += NimbusMoveSpeed;
            }
            if (save.ManaflowBandSelected && manaflowStacks > 0)
            {
                Player.statManaMax2 += manaflowStacks * 5;
            }
            if (save.GatheringStormSelected)
            {
                gatheringStormTimer++;
                if (gatheringStormTimer >= GatheringStormInterval)
                {
                    gatheringStormTimer = 0;
                    gatheringStormStacks++;
                }
                if (gatheringStormStacks > 0)
                {
                    Player.GetDamage(DamageClass.Generic) += GatheringStormDamagePerStack * gatheringStormStacks;
                }
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
            manaflowTimer = 0;
            nimbusTimer = 0;
            scorchTimer = 0;
            gatheringStormTimer = 0;
            pendingPotionReduction = 0f;
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();

            if (item.potion && item.healLife > 0)
            {
                if (save.NimbusCloakSelected)
                {
                    nimbusTimer = NimbusDuration;
                }
                if (save.TranscendenceSelected)
                {
                    pendingPotionReduction = TranscendencePotionReduction;
                }
            }
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
            if (!(save.ManaflowBandSelected || save.ScorchSelected))
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (save.ManaflowBandSelected && manaflowTimer <= 0 && manaflowStacks < ManaflowMaxStacks)
            {
                manaflowStacks++;
                manaflowTimer = ManaflowCooldown;
                Player.statMana = System.Math.Min(Player.statMana + 20, Player.statManaMax2);
            }

            if (save.ScorchSelected && scorchTimer <= 0)
            {
                target.SimpleStrikeNPC(ScorchDamage, Player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                scorchTimer = ScorchCooldown;
            }
        }
    }
}
