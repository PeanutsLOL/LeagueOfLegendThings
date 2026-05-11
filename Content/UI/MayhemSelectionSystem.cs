using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.UI
{
    /// <summary>
    /// Mayhem 增幅器选择中继系统
    /// 在 MayhemPlayer 和 MayhemAugmentUIState 之间传递选择状态
    /// </summary>
    public class MayhemSelectionSystem : ModSystem
    {
        /// <summary>等待玩家选择的层级（"Silver"/"Gold"/"Prismatic"），空字符串表示无待选项</summary>
        public string PendingTier { get; set; } = "";

        /// <summary>玩家选中的增幅器名称</summary>
        public string SelectedAugment { get; set; } = "";

        /// <summary>本次完成选择的层级</summary>
        public string CompletedTier { get; set; } = "";

        /// <summary>选择是否已完成</summary>
        public bool SelectionComplete { get; set; }

        /// <summary>UI 实例引用，由 MayhemUISystem 设置</summary>
        internal MayhemAugmentUIState UIState { get; set; }

        /// <summary>
        /// 请求打开增幅器选择界面
        /// </summary>
        public void RequestSelection(string tier)
        {
            PendingTier = tier;
            SelectedAugment = "";
            SelectionComplete = false;
            UIState?.OpenForTier(tier);
        }

        /// <summary>
        /// 玩家完成选择（由 UI 回调）
        /// </summary>
        public void CompleteSelection(string augment, string tier)
        {
            SelectedAugment = augment;
            CompletedTier = tier;
            PendingTier = "";
            SelectionComplete = true;
        }

        /// <summary>
        /// 重置选择状态
        /// </summary>
        public void Reset()
        {
            PendingTier = "";
            SelectedAugment = "";
            CompletedTier = "";
            SelectionComplete = false;
            UIState?.Close();
        }
    }
}
