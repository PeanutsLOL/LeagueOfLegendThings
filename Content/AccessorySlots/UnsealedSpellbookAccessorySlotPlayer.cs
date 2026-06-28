using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LeagueOfLegendThings.Content.Buffs.SummonersRift;

/// <summary>
/// Huge thanks to abluescarab's tModLoader-WingSlot
/// for providing a reference implementation of mod accessory slots and player integration.
/// Link: https://github.com/abluescarab/tModLoader-WingSlot
/// </summary>

namespace LeagueOfLegendThings.Content.AccessorySlots
{
    // ModAccessorySlotPlayer implementation to integrate the Unsealed Spellbook slot
    // with the mod accessory slot lifecycle (save/load/sync/update).
    public class UnsealedSpellbookAccessorySlotPlayer : ModPlayer
    {
        public override void UpdateEquips()
        {
            base.UpdateEquips();

            var spellbookPlayer = Player.GetModPlayer<UnsealedSpellbookPlayer>();
            if (spellbookPlayer == null) return;

            var func = spellbookPlayer.UnsealedSpellbookFunctionalItem;
            if (func != null && !func.IsAir)
            {
                try
                {
                    Player.ApplyEquipFunctional(func, true);
                    ItemLoader.UpdateAccessory(func, Player, false);
                }
                catch{}
            }

            var vanity = spellbookPlayer.UnsealedSpellbookVanityItem;
            if (vanity != null && !vanity.IsAir)
            {
                try
                {
                    Player.ApplyEquipVanity(vanity);
                }
                catch{}
            }
        }

        public override void SaveData(TagCompound tag)
        {
            // No custom persistent data currently; leave hooks for future slot persistence.
            // No logging in production build
        }

        public override void LoadData(TagCompound tag)
        {
            // No logging in production build
        }

        public override void CopyClientState(ModPlayer target)
        {
            base.CopyClientState(target);
            // No logging in production build
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            base.SendClientChanges(clientPlayer);
            // No logging in production build
        }
    }
}
