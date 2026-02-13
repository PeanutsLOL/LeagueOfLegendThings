using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class LegendAlacrity : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Buffs/Legend_Alacrity";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var alacrityPlayer = player.GetModPlayer<LegendAlacrityPlayer>();
            alacrityPlayer.HasLegendAlacrity = true;
        }
    }

    public class LegendAlacrityPlayer : ModPlayer
    {
        // 是否激活传说：欢欣效果（不使用 buff 栏，自实现逻辑）
        public bool HasLegendAlacrity;

        // 每层攻速加成
        private const float AttackSpeedPerStack = 0.015f; // 1.5%
        private const float MaxStacks = 6f;

        public override void ResetEffects()
        {
            HasLegendAlacrity = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // 检查符文是否选择，直接设置标记（不使用 buff）
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.LegendAlacritySelected)
            {
                HasLegendAlacrity = true;

                // 计算当前层数并应用攻速加成
                float stacks = CalculateStacks();
                if (stacks > 0)
                {
                    float attackSpeedBonus = stacks * AttackSpeedPerStack;
                    Player.GetAttackSpeed(DamageClass.Generic) += attackSpeedBonus;
                }
            }
        }

        private float CalculateStacks()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            return LegendSeries.CalculateStacks(save, MaxStacks);
        }

        public override void UpdateDead()
        {
            HasLegendAlacrity = false;
        }

    }
}
