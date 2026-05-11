using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Buffs.Mayhem;
using LeagueOfLegendThings.Content.Config;
using LeagueOfLegendThings.Content.UI;

namespace LeagueOfLegendThings.Content.Items.Mayhem
{
    /// <summary>
    /// 统计铁砧 — 在军火商处以 10 铂金币购买
    /// 打开碎片选择界面（3 选 1），根据层级随机属性
    /// 棱彩轮次有概率出现 Shardholder Value Shard
    /// </summary>
    public class StatBonusAnvilItem : ModItem
    {
        public override string Texture => "LeagueOfLegendThings/Content/Icon/Stat_Bonus_item";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(10, 0, 0, 0); // 10 铂金
            Item.rare = ItemRarityID.Expert;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.consumable = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var mp = Main.LocalPlayer.GetModPlayer<MayhemPlayer>();
            int count = mp.TotalShardsTaken;

            string line = count >= StatShardSystem.SHARDHOLDER_GUARANTEED_AT
                ? $"Forged: {count} times (Shardholder guaranteed!)"
                : $"Forged: {count} times (Shardholder at {StatShardSystem.SHARDHOLDER_GUARANTEED_AT})";
            tooltips.Add(new TooltipLine(Mod, "ForgeCount", line)
            {
                OverrideColor = count >= StatShardSystem.SHARDHOLDER_GUARANTEED_AT
                    ? new Microsoft.Xna.Framework.Color(255, 215, 0)
                    : new Microsoft.Xna.Framework.Color(180, 180, 200)
            });

            // 按住 Shift 显示当前累计属性
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
            {
                if (count == 0)
                {
                    tooltips.Add(new TooltipLine(Mod, "NoStats", "No stat shards forged yet.")
                        { OverrideColor = new Microsoft.Xna.Framework.Color(150, 150, 150) });
                }
                else
                {
                    var stats = mp.GetShardStatSummary();
                    bool hasSH = mp.HasShardholder;

                    tooltips.Add(new TooltipLine(Mod, "StatsHeader", hasSH
                        ? $"--- Shard Stats (Shardholder ×{mp.ShardholderMultiplier:F1}) ---"
                        : "--- Shard Stats ---")
                        { OverrideColor = hasSH
                            ? new Microsoft.Xna.Framework.Color(255, 215, 0)
                            : new Microsoft.Xna.Framework.Color(200, 200, 200) });

                    foreach (var s in stats)
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        tooltips.Add(new TooltipLine(Mod, "Stat_" + s, "  " + s)
                            { OverrideColor = new Microsoft.Xna.Framework.Color(180, 220, 180) });
                    }

                    tooltips.Add(new TooltipLine(Mod, "StatsHint", "[Shift for details]")
                        { OverrideColor = new Microsoft.Xna.Framework.Color(120, 120, 120) });
                }
            }
            else if (count > 0)
            {
                tooltips.Add(new TooltipLine(Mod, "ShiftHint", "Hold Shift to view forged stats")
                    { OverrideColor = new Microsoft.Xna.Framework.Color(130, 130, 130) });
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune)
                return false;

            var mp = player.GetModPlayer<MayhemPlayer>();
            return mp.CanUseStatAnvil();
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return true;

            // 打开碎片选择 UI
            var mp = player.GetModPlayer<MayhemPlayer>();
            mp.OpenStatAnvilUI();

            // 消耗物品
            return true;
        }
    }

    /// <summary>
    /// 将 StatBonusAnvilItem 添加到军火商商店（仅 Mayhem 模式）
    /// </summary>
    public class MayhemGlobalNPC : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType != NPCID.ArmsDealer) return;
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;

            shop.Add(ModContent.ItemType<StatBonusAnvilItem>());
        }
    }
}
