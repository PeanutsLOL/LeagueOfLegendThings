using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    public class LegendBloodline : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Icon/Legend_Bloodline";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var bloodlinePlayer = player.GetModPlayer<LegendBloodlinePlayer>();
            bloodlinePlayer.HasLegendBloodline = true;
        }
    }

    public class LegendBloodlinePlayer : ModPlayer
    {
        // 是否激活传说：血统效果（不使用 buff 栏，自实现逻辑）
        public bool HasLegendBloodline;

        private const float LifestealPerStack = 0.003f; // 0.3%
        private const float MaxStacks = 15f;
        private const int MaxLifeBonus = 85;

        public override void ResetEffects()
        {
            HasLegendBloodline = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // 检查符文是否选择，直接设置标记（不使用 buff）
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.LegendBloodlineSelected)
            {
                HasLegendBloodline = true;

                float stacks = CalculateStacks();
                if (stacks >= MaxStacks)
                {
                    Player.statLifeMax2 += MaxLifeBonus;
                }
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (item.DamageType == DamageClass.Melee ||
                item.DamageType == DamageClass.Ranged ||
                item.DamageType == DamageClass.Magic)
            {
                TryLifesteal(damageDone);
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.minion)
                return;

            if (proj.DamageType == DamageClass.Melee ||
                proj.DamageType == DamageClass.Ranged ||
                proj.DamageType == DamageClass.Magic)
            {
                TryLifesteal(damageDone);
            }
        }

        private void TryLifesteal(int damageDone)
        {
            if (!HasLegendBloodline)
                return;

            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.LegendBloodlineSelected)
                return;

            float stacks = CalculateStacks();
            if (stacks <= 0f)
                return;

            float lifestealRate = stacks * LifestealPerStack;
            float stolen = damageDone * lifestealRate;
            Player.GetModPlayer<Mayhem.LeechPoolPlayer>().TryConsume(stolen);
        }

        private float CalculateStacks()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            return LegendSeries.CalculateStacks(save, MaxStacks);
        }

        public override void UpdateDead()
        {
            HasLegendBloodline = false;
        }

    }
}
