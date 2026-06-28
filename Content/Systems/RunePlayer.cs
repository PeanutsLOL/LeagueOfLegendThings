using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LeagueOfLegendThings.Content.Config;

namespace LeagueOfLegendThings.Content.Systems
{
    /// <summary>
    /// 每个玩家的符文选择持久化。
    /// SaveData/LoadData 写入玩家文件，多人各自独立。
    /// 首次进入不设默认值，提示玩家手动选择。
    /// </summary>
    public class RunePlayer : ModPlayer
    {
        private bool _hintShown;

        public override void SaveData(TagCompound tag)
        {
            var rs = ModContent.GetInstance<RuneSaveSystem>();
            tag["PrimaryPath"] = rs.PrimaryPath;
            tag["SecondaryPath"] = rs.SecondaryPath;
            tag["PrimaryKeystone"] = rs.PrimaryKeystone;
            tag["PrimaryRow1"] = rs.PrimaryRow1;
            tag["PrimaryRow2"] = rs.PrimaryRow2;
            tag["PrimaryRow3"] = rs.PrimaryRow3;
            tag["SecondaryPick1"] = rs.SecondaryPick1;
            tag["SecondaryPick2"] = rs.SecondaryPick2;
            tag["SecondaryPick1Row"] = rs.SecondaryPick1Row;
            tag["SecondaryPick2Row"] = rs.SecondaryPick2Row;
        }

        public override void LoadData(TagCompound tag)
        {
            var rs = ModContent.GetInstance<RuneSaveSystem>();
            var cfg = ModContent.GetInstance<RuneConfig>();

            if (cfg.EnableAramMayhemRune)
            {
                rs.PrimaryPath = "";
                rs.SecondaryPath = "";
                rs.PrimaryKeystone = "";
                rs.PrimaryRow1 = "";
                rs.PrimaryRow2 = "";
                rs.PrimaryRow3 = "";
                rs.SecondaryPick1 = "";
                rs.SecondaryPick2 = "";
                rs.SecondaryPick1Row = -1;
                rs.SecondaryPick2Row = -1;
                _hintShown = false;
                return;
            }

            if (tag.ContainsKey("PrimaryPath"))
            {
                rs.PrimaryPath = tag.GetString("PrimaryPath");
                rs.SecondaryPath = tag.GetString("SecondaryPath");
                rs.PrimaryKeystone = tag.GetString("PrimaryKeystone");
                rs.PrimaryRow1 = tag.GetString("PrimaryRow1");
                rs.PrimaryRow2 = tag.GetString("PrimaryRow2");
                rs.PrimaryRow3 = tag.GetString("PrimaryRow3");
                rs.SecondaryPick1 = tag.GetString("SecondaryPick1");
                rs.SecondaryPick2 = tag.GetString("SecondaryPick2");
                rs.SecondaryPick1Row = tag.GetInt("SecondaryPick1Row");
                rs.SecondaryPick2Row = tag.GetInt("SecondaryPick2Row");
                _hintShown = false;
            }
            else
            {
                // 首次使用：不设默认，提示玩家手动选择
                rs.PrimaryPath = "";
                rs.SecondaryPath = "";
                rs.PrimaryKeystone = "";
                rs.PrimaryRow1 = "";
                rs.PrimaryRow2 = "";
                rs.PrimaryRow3 = "";
                rs.SecondaryPick1 = "";
                rs.SecondaryPick2 = "";
                rs.SecondaryPick1Row = -1;
                rs.SecondaryPick2Row = -1;
            }

            rs.SecondaryPick1 ??= "";
            rs.SecondaryPick2 ??= "";
        }

        public override void OnEnterWorld()
        {
            if (_hintShown) return;

            var rs = ModContent.GetInstance<RuneSaveSystem>();
            var cfg = ModContent.GetInstance<RuneConfig>();

            // Mayhem 模式下不提示符文
            if (cfg.EnableAramMayhemRune) return;

            // 未选择任何符文时提示
            if (string.IsNullOrEmpty(rs.PrimaryKeystone))
            {
                _hintShown = true;
                string hint = Language.GetTextValue("Mods.LeagueOfLegendThings.UI.Runes.HintNoRunes");
                Main.NewText(hint, 255, 215, 80);

                string hint2 = Language.GetTextValue("Mods.LeagueOfLegendThings.UI.Runes.HintMayhemToggle");
                Main.NewText(hint2, 180, 200, 230);
            }
        }
    }
}
