using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ID;
using Terraria.DataStructures;

/// <summary>
/// Huge thanks to abluescarab's tModLoader-WingSlot
/// for providing a reference implementation of mod accessory slots and player integration.
/// Link: https://github.com/abluescarab/tModLoader-WingSlot
/// </summary>

namespace LeagueOfLegendThings.Content.Buffs
{
    // UnsealedSpellbook
    public class UnsealedSpellbookPlayer : ModPlayer
    {
        public bool HasUnsealedSpellbook;
        public Item UnsealedSpellbookFunctionalItem;
        public Item UnsealedSpellbookVanityItem;

        public override void ResetEffects()
        {
            HasUnsealedSpellbook = false;
            if (UnsealedSpellbookFunctionalItem == null)
                UnsealedSpellbookFunctionalItem = new Item();
            if (UnsealedSpellbookVanityItem == null)
                UnsealedSpellbookVanityItem = new Item();
            UnsealedSpellbookFunctionalItem.SetDefaults(ItemID.None, true);
            UnsealedSpellbookVanityItem.SetDefaults(ItemID.None, true);
        }

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.UnsealedSpellbookSelected)
            {
                HasUnsealedSpellbook = true;
            }
        }

        public override void UpdateEquips()
        {
            // 应用由插槽保存下来的物品效果（在 ModAccessorySlot 中把物品写入到这里）
            var funcItem = UnsealedSpellbookFunctionalItem;
            if (funcItem != null && !funcItem.IsAir)
            {
                try
                {
                    Player.ApplyEquipFunctional(funcItem, true);
                    ItemLoader.UpdateAccessory(funcItem, Player, false);
                }
                catch{}
            }

            var vanityItem = UnsealedSpellbookVanityItem;
            if (vanityItem != null && !vanityItem.IsAir)
            {
                try
                {
                    Player.ApplyEquipVanity(vanityItem);
                }
                catch{}
            }
        }
    }
}