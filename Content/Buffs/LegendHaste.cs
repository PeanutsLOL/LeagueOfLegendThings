using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class LegendHaste : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Buffs/Legend_Haste";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var hastePlayer = player.GetModPlayer<LegendHastePlayer>();
            hastePlayer.HasLegendHaste = true;
        }
    }

    public class LegendHastePlayer : ModPlayer
    {
        // 是否激活传说：极速效果
        public bool HasLegendHaste;

        // 每层血瓶CD缩减（2%）
        private const float CDReductionPerStack = 0.02f; // 2%
        private const float MaxStacks = 10f;

        public override void ResetEffects()
        {
            HasLegendHaste = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // 检查符文是否选择，直接设置标记（不使用 buff）
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.LegendHasteSelected)
            {
                HasLegendHaste = true;
            }
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            // 治疗药水使用后修改冷却时间
            if (HasLegendHaste && item.potion && item.healLife > 0)
            {
                float stacks = CalculateStacks();
                if (stacks > 0)
                {
                    // 计算 CD 缩减百分比
                    float cdReduction = stacks * CDReductionPerStack;
                    // 在下一帧修改 potionDelay
                    Player.GetModPlayer<LegendHastePlayer>().pendingCDReduction = cdReduction;
                }
            }
        }

        private float pendingCDReduction = 0f;

        public override void PostUpdate()
        {
            // 如果刚使用了药水且有待应用的 CD 缩减
            if (pendingCDReduction > 0 && Player.potionDelay > 0)
            {
                // 应用 CD 缩减
                Player.potionDelay = (int)(Player.potionDelay * (1f - pendingCDReduction));
                pendingCDReduction = 0f;
            }
        }

        private float CalculateStacks()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            return LegendSeries.CalculateStacks(save, MaxStacks);
        }

        public override void UpdateDead()
        {
            HasLegendHaste = false;
        }

    }
}
