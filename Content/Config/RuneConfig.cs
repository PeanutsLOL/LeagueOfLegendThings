using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace LeagueOfLegendThings.Content.Config
{
    public class RuneConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        [DefaultValue(false)]
        public bool EnableHardMode { get; set; }

        [DefaultValue(false)]
        public bool EnableAramMayhemRune { get; set; }

        [DefaultValue(false)]
        public bool LeechPoolBarLocked { get; set; }

        [DefaultValue(0f)]
        public float LeechPoolBarX { get; set; }

        [DefaultValue(0f)]
        public float LeechPoolBarY { get; set; }
    }
}
