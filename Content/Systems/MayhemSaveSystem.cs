using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace LeagueOfLegendThings.Content.Systems
{
    /// <summary>
    /// 海克斯大乱斗存档系统
    /// 负责保存世界层级的增幅器解锁状态，便于跨游戏会话持久化
    /// </summary>
    public class MayhemSaveSystem : ModSystem
    {
        /// <summary>白银增幅器是否已解锁（默认可用）</summary>
        public bool SilverTierUnlocked;

        /// <summary>黄金增幅器是否已解锁（进入困难模式时解锁）</summary>
        public bool GoldTierUnlocked;

        /// <summary>棱彩增幅器是否已解锁（击败全部机械 Boss 时解锁）</summary>
        public bool PrismaticTierUnlocked;

        public override void OnWorldLoad()
        {
            ResetFlags();
        }

        public override void OnWorldUnload()
        {
            ResetFlags();
        }

        private void ResetFlags()
        {
            SilverTierUnlocked = true; // 默认可用
            GoldTierUnlocked = false;
            PrismaticTierUnlocked = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag[nameof(SilverTierUnlocked)] = SilverTierUnlocked;
            tag[nameof(GoldTierUnlocked)] = GoldTierUnlocked;
            tag[nameof(PrismaticTierUnlocked)] = PrismaticTierUnlocked;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            SilverTierUnlocked = tag.GetBool(nameof(SilverTierUnlocked));
            GoldTierUnlocked = tag.GetBool(nameof(GoldTierUnlocked));
            PrismaticTierUnlocked = tag.GetBool(nameof(PrismaticTierUnlocked));
        }
    }
}
