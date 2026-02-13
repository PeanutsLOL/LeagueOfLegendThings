using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace LeagueOfLegendThings.Content.Config
{
    public class RuneConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        public bool EnableLethalTempoRune { get; set; } = true;

        // 可以在此继续添加其他符文的开关
        // [DefaultValue(true)]
        // public bool EnableXxxRune { get; set; } = true;
    }
}
