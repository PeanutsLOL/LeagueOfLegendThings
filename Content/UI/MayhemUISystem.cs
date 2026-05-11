using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace LeagueOfLegendThings.Content.UI
{
    /// <summary>
    /// Mayhem 增幅器选择 UI 的 ModSystem
    /// 负责 UI 生命周期管理、渲染层注入
    /// </summary>
    public class MayhemUISystem : ModSystem
    {
        private UserInterface _interface;
        internal MayhemAugmentUIState MayhemUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                MayhemUI = new MayhemAugmentUIState();
                MayhemUI.Activate();
                _interface = new UserInterface();
                _interface.SetState(MayhemUI);

                // 将 UI 引用注册到选择中继系统
                ModContent.GetInstance<MayhemSelectionSystem>().UIState = MayhemUI;
            }
        }

        public override void Unload()
        {
            _interface = null;
            MayhemUI = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _interface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (_interface == null) return;

            // 在 "Vanilla: Mouse Text" 之前插入，确保 UI 能接收鼠标输入
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "LeagueOfLegendThings: Mayhem Selection UI",
                    delegate
                    {
                        _interface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }
    }
}
