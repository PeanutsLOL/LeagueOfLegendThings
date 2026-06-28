using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using LeagueOfLegendThings.Content.Config;

namespace LeagueOfLegendThings.Content.UI
{
    public class LeechPoolUIState : UIState
    {
        public LeechPoolBar Bar;

        public override void OnInitialize()
        {
            Bar = new LeechPoolBar();
            Append(Bar);
        }
    }

    public class LeechPoolBar : UIElement
    {
        // 14×104 像素条，双边框结构：外黑2px + 内暗2px，填充区 6×96（3亮+3暗列）
        private const int BarW = 14, BarH = 104;
        private const int FillX = 4, FillY = 4, FillW = 6, FillH = 96;
        private const int TickInterval = 32; // 96/3
        private const int BorderW = 2; // 边框厚度
        private const int CornerR = 2; // 圆角半径

        private bool _dragging, _posInit;
        private Vector2 _dragOffset;
        private float _displayedPool;

        public LeechPoolBar()
        {
            Width.Set(BarW, 0f);
            Height.Set(BarH, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_posInit && Main.screenWidth > 0)
            {
                var cfg = ModContent.GetInstance<RuneConfig>();
                if (cfg.LeechPoolBarX > 0 || cfg.LeechPoolBarY > 0)
                {
                    // 从配置恢复位置
                    Left.Set(cfg.LeechPoolBarX, 0f);
                    Top.Set(cfg.LeechPoolBarY, 0f);
                }
                else
                {
                    // 默认：水平中心左偏 2%
                    Left.Set(Main.screenWidth * 0.48f - BarW / 2f, 0f);
                    Top.Set(Main.screenHeight * 0.35f, 0f);
                }
                Recalculate();
                _posInit = true;
            }

            var pp = Main.LocalPlayer?.GetModPlayer<Buffs.Mayhem.LeechPoolPlayer>();
            if (pp == null) return;
            float target = pp.PoolCurrent;
            _displayedPool += (target - _displayedPool) * 0.15f;
            if (System.Math.Abs(_displayedPool - target) < 0.1f) _displayedPool = target;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var dim = GetDimensions();
            var pp = Main.LocalPlayer?.GetModPlayer<Buffs.Mayhem.LeechPoolPlayer>();
            if (pp == null) return;

            float max = pp.PoolMax();
            float ratio = max > 0f ? System.Math.Clamp(_displayedPool / max, 0f, 1f) : 0f;

            int x = (int)dim.X, y = (int)dim.Y;
            var bar = new Rectangle(x, y, BarW, BarH);

            // ═══ 外发光（低血量灼烧感，跟随圆角轮廓）═══
            if (ratio < 0.25f)
            {
                Color glow = new Color(90, 8, 8);
                int gx = x - 1, gy = y - 1, gw = BarW + 2, gh = BarH + 2;
                int gr = CornerR + 1; // 外发光切角比外框多 1px
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(gx + gr, gy, gw - gr * 2, 1), glow);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(gx + gr, gy + gh - 1, gw - gr * 2, 1), glow);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(gx, gy + gr, 1, gh - gr * 2), glow);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(gx + gw - 1, gy + gr, 1, gh - gr * 2), glow);
            }

            // ═══ 外框纯黑（四角切角圆角）═══
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(x + CornerR, y, BarW - CornerR * 2, BorderW), Color.Black);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(x + CornerR, y + BarH - BorderW, BarW - CornerR * 2, BorderW), Color.Black);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(x, y + CornerR, BorderW, BarH - CornerR * 2), Color.Black);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(x + BarW - BorderW, y + CornerR, BorderW, BarH - CornerR * 2), Color.Black);

            // ═══ 内框深色（不切角，已被外框包住）═══
            int ix = x + BorderW, iy = y + BorderW, iw = BarW - BorderW * 2, ih = BarH - BorderW * 2;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(ix, iy, iw, BorderW), new Color(18, 18, 26));
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(ix, iy + ih - BorderW, iw, BorderW), new Color(18, 18, 26));
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(ix, iy, BorderW, ih), new Color(18, 18, 26));
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(ix + iw - BorderW, iy, BorderW, ih), new Color(18, 18, 26));

            // ═══ 填充区背景 ═══
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(x + FillX, y + FillY, FillW, FillH),
                new Color(10, 10, 16));

            // ═══ 填充色 — 逐行，亮列 3px + 暗列 3px ═══
            int filled = (int)(FillH * ratio);
            if (filled > 0)
            {
                Color mainColor = Color.Lerp(new Color(240, 55, 55), new Color(75, 235, 105), ratio);
                Color brightMix = ratio < 0.25f ? new Color(255, 210, 100) : Color.White;

                int fillTop = y + FillY + (FillH - filled);

                for (int row = 0; row < filled; row++)
                {
                    int py = fillTop + row;
                    Color rowColor;
                    if (row == 0)
                        rowColor = new Color(
                            System.Math.Min(255, mainColor.R + 35),
                            System.Math.Min(255, mainColor.G + 35),
                            System.Math.Min(255, mainColor.B + 35));
                    else if (row == filled - 1)
                        rowColor = new Color(
                            System.Math.Max(0, mainColor.R - 35),
                            System.Math.Max(0, mainColor.G - 35),
                            System.Math.Max(0, mainColor.B - 35));
                    else
                        rowColor = mainColor;

                    Color left = new Color(
                        (int)(rowColor.R * 0.7f + brightMix.R * 0.3f),
                        (int)(rowColor.G * 0.7f + brightMix.G * 0.3f),
                        (int)(rowColor.B * 0.7f + brightMix.B * 0.3f));
                    Color right = new Color(
                        (int)(rowColor.R * 0.6f),
                        (int)(rowColor.G * 0.6f),
                        (int)(rowColor.B * 0.6f));

                    // 亮列 3px 宽
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(x + FillX, py, FillW / 2, 1), left);
                    // 暗列 3px 宽
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(x + FillX + FillW / 2, py, FillW / 2, 1), right);
                }

                // ═══ 刻度线（每 1/3 高度，FillW 宽）═══
                for (int t = 1; t <= 2; t++)
                {
                    int tickY = fillTop + t * TickInterval;
                    if (tickY >= fillTop && tickY < y + FillY + FillH)
                    {
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                            new Rectangle(x + FillX, tickY - 1, FillW, 1),
                            new Color(255, 255, 255) * 0.18f);
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                            new Rectangle(x + FillX, tickY, FillW, 1),
                            new Color(0, 0, 0) * 0.55f);
                    }
                }
            }

            // ═══ 悬停提示 ═══
            if (bar.Contains(Main.MouseScreen.ToPoint()))
                DrawTooltip(spriteBatch, max);

            HandleDrag(bar);
        }

        private void DrawTooltip(SpriteBatch sb, float max)
        {
            string line1 = Language.GetTextValue("Mods.LeagueOfLegendThings.UI.LeechPool.Tooltip",
                $"{_displayedPool:F0}", $"{max:F0}");
            string line2 = Language.GetTextValue("Mods.LeagueOfLegendThings.UI.LeechPool.TooltipRegen",
                $"{max * 0.08f:F1}");

            var font = FontAssets.MouseText.Value;
            float tw1 = font.MeasureString(line1).X;
            float tw2 = font.MeasureString(line2).X;
            int tipW = (int)System.Math.Max(tw1, tw2) + 24;
            int tipH = 50;

            int tipX = (int)Main.MouseScreen.X + 18;
            int tipY = (int)Main.MouseScreen.Y - tipH / 2;
            if (tipX + tipW > Main.screenWidth) tipX = Main.screenWidth - tipW - 4;
            if (tipY < 4) tipY = 4;
            if (tipY + tipH > Main.screenHeight) tipY = Main.screenHeight - tipH - 4;

            // 双边框提示框
            var tip = new Rectangle(tipX, tipY, tipW, tipH);
            sb.Draw(TextureAssets.MagicPixel.Value, tip, Color.Black);
            var tipInner = new Rectangle(tipX + 1, tipY + 1, tipW - 2, tipH - 2);
            sb.Draw(TextureAssets.MagicPixel.Value, tipInner, new Color(14, 14, 24));
            var tipFill = new Rectangle(tipX + 2, tipY + 2, tipW - 4, tipH - 4);
            sb.Draw(TextureAssets.MagicPixel.Value, tipFill, new Color(24, 24, 38));

            Utils.DrawBorderString(sb, line1,
                new Vector2(tip.X + 12, tip.Y + 8), Color.White, 0.95f);
            Utils.DrawBorderString(sb, line2,
                new Vector2(tip.X + 12, tip.Y + 28), new Color(160, 195, 230), 0.85f);
        }

        private void HandleDrag(Rectangle barRect)
        {
            if (ModContent.GetInstance<RuneConfig>().LeechPoolBarLocked) return;
            if (!Main.mouseLeft) { _dragging = false; return; }
            if (!_dragging)
            {
                if (barRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    _dragging = true;
                    _dragOffset = Main.MouseScreen - new Vector2(barRect.X, barRect.Y);
                }
            }
            if (_dragging)
            {
                Main.LocalPlayer.mouseInterface = true; // 拖拽时阻断攻击，不拖不放
                Left.Set(Main.MouseScreen.X - _dragOffset.X, 0f);
                Top.Set(Main.MouseScreen.Y - _dragOffset.Y, 0f);
                Recalculate();

                var cfg = ModContent.GetInstance<RuneConfig>();
                cfg.LeechPoolBarX = Left.Pixels;
                cfg.LeechPoolBarY = Top.Pixels;
            }
        }
    }

    public class LeechPoolUISystem : ModSystem
    {
        internal LeechPoolUIState State;
        private UserInterface _interface;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                State = new LeechPoolUIState();
                State.Activate();
                _interface = new UserInterface();
                _interface.SetState(State);
            }
        }

        public override void Unload() { _interface = null; State = null; }

        public override void UpdateUI(GameTime gameTime)
        {
            if (State == null) return;

            var pp = Main.LocalPlayer?.GetModPlayer<Buffs.Mayhem.LeechPoolPlayer>();
            if (pp == null || pp.PoolMax() <= 0f)
            {
                if (State.Bar?.Parent != null) State.Bar.Remove();
                return;
            }

            if (State.Bar?.Parent == null)
                State.Append(State.Bar);

            _interface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (_interface == null) return;
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text"));
            if (idx == -1) idx = layers.Count;
            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "LeagueOfLegendThings: Leech Pool Bar",
                delegate
                {
                    var pp = Main.LocalPlayer?.GetModPlayer<Buffs.Mayhem.LeechPoolPlayer>();
                    if (pp != null && pp.PoolMax() > 0f)
                        _interface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
