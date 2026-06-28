using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Buffs;
using LeagueOfLegendThings.Content.Buffs.SummonersRift;
using LeagueOfLegendThings.Content.Systems;
using Microsoft.Xna.Framework;

/// <summary>
/// Huge thanks to abluescarab's tModLoader-WingSlot
/// for providing a reference implementation of mod accessory slots and player integration.
/// Link: https://github.com/abluescarab/tModLoader-WingSlot
/// </summary>

namespace LeagueOfLegendThings.Content.AccessorySlots
{
    public class UnsealedSpellbookAccessorySlot : ModAccessorySlot
    {
        public override bool IsEnabled()
        {
            Player p = Player;
            bool enabled = false;
            if (p != null)
            {
                var runeSave = ModContent.GetInstance<RuneSaveSystem>();
                enabled = runeSave.UnsealedSpellbookSelected || p.GetModPlayer<UnsealedSpellbookPlayer>().HasUnsealedSpellbook;
            }
            return enabled;
        }

        public override bool CanAcceptItem(Item checkItem, AccessorySlotType context)
        {
            return base.CanAcceptItem(checkItem, context);
        }

        public override void ApplyEquipEffects()
        {
            // 调用原生行为：把功能性物品应用到玩家（刷新功能效果并处理显示）
            if (FunctionalItem != null && !FunctionalItem.IsAir)
            {
                // 把物品保存到玩家上，实际的效果在 Player.UpdateEquips() 阶段应用，
                // 以确保与其它饰品的处理时序一致。
                var spellbookPlayer = Player.GetModPlayer<UnsealedSpellbookPlayer>();
                spellbookPlayer.UnsealedSpellbookFunctionalItem = FunctionalItem.Clone();
                try
                {
                    Player.ApplyEquipFunctional(FunctionalItem, true);
                    ItemLoader.UpdateAccessory(FunctionalItem, Player, false);
                }
                catch{}
            }
        
            // 应用外观物品（仅视觉）
            if (VanityItem != null && !VanityItem.IsAir)
            {
                var spellbookPlayer = Player.GetModPlayer<UnsealedSpellbookPlayer>();
                spellbookPlayer.UnsealedSpellbookVanityItem = VanityItem.Clone();
                try
                {
                    Player.ApplyEquipVanity(VanityItem);
                }
                catch{}
            }
        }

        public override void BackgroundDrawColor(AccessorySlotType context, ref Color color)
        {
            color = new Color(135, 206, 250);
            base.BackgroundDrawColor(context, ref color);
        }
    }
}
