using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using LeagueOfLegendThings.Content.Buffs.Mayhem;

namespace LeagueOfLegendThings.Content.UI
{
    /// <summary>
    /// 统计铁砧 UI 的 ModSystem — 管理碎片选择界面的生命周期
    /// </summary>
    public class StatAnvilUISystem : ModSystem
    {
        private UserInterface _interface;
        internal StatAnvilUIState StatAnvilUI;

        /// <summary>临时存储的选项列表（等待玩家选择）</summary>
        internal List<StatShardSystem.Shard> PendingOptions;
        internal StatShardSystem.ShardTier PendingTier;

        /// <summary>选择是否已完成</summary>
        public bool SelectionComplete { get; private set; }
        /// <summary>选中的碎片</summary>
        public StatShardSystem.Shard SelectedShard { get; private set; }

        // 保存原始鼠标状态，在 PreUpdatePlayers 吞掉后在 UpdateUI 恢复给 UI
        private bool _savedMouseLeft, _savedMouseRight;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                StatAnvilUI = new StatAnvilUIState();
                StatAnvilUI.Activate();
                _interface = new UserInterface();
                _interface.SetState(StatAnvilUI);
            }
        }

        public override void Unload()
        {
            _interface = null;
            StatAnvilUI = null;
        }

        public override void PreUpdatePlayers()
        {
            if (StatAnvilUI is { IsVisible: true })
            {
                // 保存真实鼠标状态，供 UpdateUI 恢复
                _savedMouseLeft = Main.mouseLeft;
                _savedMouseRight = Main.mouseRight;
                // 立刻吞掉，阻止后续背包/玩家逻辑获取点击
                Main.mouseLeft = false;
                Main.mouseRight = false;
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (StatAnvilUI is { IsVisible: true })
            {
                // 临时恢复鼠标让 UI 能响应点击
                Main.mouseLeft = _savedMouseLeft;
                Main.mouseRight = _savedMouseRight;
            }

            _interface?.Update(gameTime);

            if (StatAnvilUI is { IsVisible: true })
            {
                // UI 处理完后再次吞掉
                Main.mouseLeft = false;
                Main.mouseRight = false;
                Main.LocalPlayer.mouseInterface = true;
            }

            // 检测玩家是否完成了选择（仅触发一次）
            if (StatAnvilUI != null && StatAnvilUI.SelectionMade)
            {
                SelectedShard = StatAnvilUI.ChosenShard;
                SelectionComplete = true;
                StatAnvilUI.SelectionMade = false; // 消费后立即清除，杜绝重复触发
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (_interface == null) return;
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text"));
            if (idx != -1)
            {
                layers.Insert(idx, new LegacyGameInterfaceLayer(
                    "LeagueOfLegendThings: Stat Anvil UI",
                    delegate { _interface.Draw(Main.spriteBatch, new GameTime()); return true; },
                    InterfaceScaleType.UI));
            }
        }

        /// <summary>打开碎片选择界面</summary>
        public void OpenSelection(List<StatShardSystem.Shard> options, StatShardSystem.ShardTier tier)
        {
            SelectionComplete = false;
            SelectedShard = null;
            PendingOptions = options;
            PendingTier = tier;
            StatAnvilUI?.Open(options, tier);
        }

        /// <summary>消耗选择结果（由 MayhemPlayer 调用）</summary>
        public StatShardSystem.Shard ConsumeSelection()
        {
            if (!SelectionComplete || SelectedShard == null) return null;
            var result = SelectedShard;
            SelectionComplete = false;
            SelectedShard = null;
            return result;
        }

        public void Reset()
        {
            SelectionComplete = false;
            SelectedShard = null;
            StatAnvilUI?.Close();
        }
    }
}
