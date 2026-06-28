using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Buffs.Mayhem;

namespace LeagueOfLegendThings.Content.Commands
{
    /// <summary>
    /// 重置所有属性锻造器数据 — /resetshards
    /// 用于修复因 bug 导致的不正常属性累积
    /// </summary>
    public class ResetShardsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "resetshards";

        public override string Usage => "/resetshards";

        public override string Description => Language.GetTextValue("Mods.LeagueOfLegendThings.StatShards.Command.ResetDescription");

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var mp = caller.Player.GetModPlayer<MayhemPlayer>();
            mp.ResetAllShards();

            Main.NewText(Language.GetTextValue("Mods.LeagueOfLegendThings.StatShards.Command.ResetDone"), 255, 215, 0);
        }
    }
}
