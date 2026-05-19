using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using LeagueOfLegendThings.Content.Buffs.Mayhem;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.UI
{
    /// <summary>
    /// ARAM Mayhem 增幅器选择界面 — 3 张卡片，图标在上、名称和描述在下
    /// </summary>
    public class MayhemAugmentUIState : UIState
    {
        private DraggableUIPanel _panel;
        private UIText _title;
        private UIText _tierLabel;
        private AugmentCard[] _cards = new AugmentCard[3];
        private bool[] _cardRerolled = new bool[3];

        private string _tier = "";
        private string[] _currentOptions = new string[3];
        private HashSet<string> _usedAugments = new HashSet<string>();

        public bool SelectionMade { get; private set; }
        public string ChosenAugment { get; private set; }

        // ============ 初始化 ============

        public void OpenForTier(string tier)
        {
            _tier = tier;
            SelectionMade = false;
            ChosenAugment = "";
            _usedAugments.Clear();
            for (int i = 0; i < 3; i++) _cardRerolled[i] = false;

            RollFreshOptions();

            if (_panel == null) BuildPanel();
            else RefreshCards();

            if (_panel.Parent == null)
                Append(_panel);
        }

        public void Close()
        {
            _panel?.Remove();
        }

        public bool IsOpen => _panel?.Parent != null;

        // ============ 面板构建 ============

        private void BuildPanel()
        {
            int cardW = 190;
            int cardH = 400;
            int spacing = 24;
            int padX = 30;
            int padTop = 70;
            int padBottom = 14;
            int panelW = padX * 2 + 3 * cardW + 2 * spacing;
            int panelH = padTop + cardH + padBottom;

            _panel = new DraggableUIPanel();
            _panel.SetPadding(14);
            _panel.BackgroundColor = new Color(16, 16, 28) * 0.96f;
            _panel.BorderColor = new Color(180, 160, 80);
            _panel.Width.Set(panelW, 0f);
            _panel.Height.Set(panelH, 0f);
            _panel.Left.Set(Main.screenWidth / 2f - panelW / 2f, 0f);
            _panel.Top.Set(Main.screenHeight / 2f - panelH / 2f, 0f);

            // 标题
            _title = new UIText(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemAugments.UI.ChooseTitle", "Choose Your Augment"), 1.35f);
            _title.HAlign = 0.5f;
            _title.Top.Set(10, 0f);
            _panel.Append(_title);

            // 层级标签
            _tierLabel = new UIText("", 0.95f);
            _tierLabel.HAlign = 0.5f;
            _tierLabel.Top.Set(38, 0f);
            _panel.Append(_tierLabel);

            // 卡片
            int startX = padX;
            int cardTop = padTop;
            for (int i = 0; i < 3; i++)
            {
                var card = new AugmentCard(i, OnSelectCard, OnRerollCard);
                _cards[i] = card;
                card.Width.Set(cardW, 0f);
                card.Height.Set(cardH, 0f);
                card.Left.Set(startX + i * (cardW + spacing), 0f);
                card.Top.Set(cardTop, 0f);
                _panel.Append(card);
            }

            RefreshCards();
        }

        private void RefreshCards()
        {
            _tierLabel?.SetText(GetTierDisplayName(_tier));
            if (_tierLabel != null)
                _tierLabel.TextColor = TierColor(_tier);

            for (int i = 0; i < 3; i++)
                _cards[i]?.SetAugment(_currentOptions[i], _tier, _cardRerolled[i]);
        }

        // ============ 随机抽取 ============

        private void RollFreshOptions()
        {
            string[] pool = PoolForTier(_tier);
            if (pool == null || pool.Length < 3) return;

            var shuffled = new List<string>(pool);
            int n = shuffled.Count;
            while (n > 1) { n--; int k = Main.rand.Next(n + 1); var t = shuffled[k]; shuffled[k] = shuffled[n]; shuffled[n] = t; }

            for (int i = 0; i < 3; i++)
            {
                _currentOptions[i] = shuffled[i];
                _usedAugments.Add(shuffled[i]);
            }
        }

        // ============ 回调 ============

        private void OnSelectCard(int cardIndex)
        {
            if (SelectionMade) return;
            string augment = _currentOptions[cardIndex];
            if (string.IsNullOrEmpty(augment)) return;
            SelectionMade = true;
            ChosenAugment = augment;
            ModContent.GetInstance<MayhemSelectionSystem>().CompleteSelection(augment, _tier);
            Close();
        }

        private void OnRerollCard(int cardIndex)
        {
            if (_cardRerolled[cardIndex]) return;
            string[] pool = PoolForTier(_tier);
            if (pool == null) return;

            var available = new List<string>();
            foreach (var a in pool)
                if (!_usedAugments.Contains(a)) available.Add(a);

            if (available.Count == 0) return;

            string roll = available[Main.rand.Next(available.Count)];
            _currentOptions[cardIndex] = roll;
            _usedAugments.Add(roll);
            _cardRerolled[cardIndex] = true;
            RefreshCards();
        }

        // ============ 辅助 ============

        private static string[] PoolForTier(string tier) => tier switch
        {
            "Silver" => SilverAugments.AllAugments,
            "Gold" => GoldAugments.AllAugments,
            "Prismatic" => PrismaticAugments.AllAugments,
            _ => null
        };

        private static string GetTierDisplayName(string tier) => tier switch
        {
            "Silver" => Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemAugments.UI.SilverLabel", "Silver Augment — Pre-Hardmode"),
            "Gold" => Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemAugments.UI.GoldLabel", "Gold Augment — Hardmode"),
            "Prismatic" => Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemAugments.UI.PrismaticLabel", "Prismatic Augment — All Mech Bosses"),
            _ => ""
        };

        private static Color TierColor(string tier) => tier switch
        {
            "Silver" => new Color(180, 180, 200),
            "Gold" => new Color(230, 190, 60),
            "Prismatic" => new Color(210, 80, 230),
            _ => Color.White
        };

        /// <summary>内部名 → 材质资源路径映射</summary>
        private static readonly Dictionary<string, string> IconPaths = new()
        {
            ["BluntForce"] = "LeagueOfLegendThings/Content/Icon/Blunt_Force_mayhem_augment",
            ["Deft"] = "LeagueOfLegendThings/Content/Icon/Deft_mayhem_augment",
            ["BuffBuddies"] = "LeagueOfLegendThings/Content/Icon/Buff_Buddies_mayhem_augment",
            ["Erosion"] = "LeagueOfLegendThings/Content/Icon/Erosion_mayhem_augment",
            ["Adamant"] = "LeagueOfLegendThings/Content/Icon/Adamant_mayhem_augment",
            ["CrackOpenThatEgg"] = "LeagueOfLegendThings/Content/Icon/Crack_Open_That_Egg_mayhem_augment",
            ["DiveBomber"] = "LeagueOfLegendThings/Content/Icon/Dive_Bomber_mayhem_augment",
            ["DontBlink"] = "LeagueOfLegendThings/Content/Icon/Don't_Blink_mayhem_augment",
            ["EscAPADe"] = "LeagueOfLegendThings/Content/Icon/EscAPADe_mayhem_augment",
            ["Flashbang"] = "LeagueOfLegendThings/Content/Icon/Flashbang_mayhem_augment",
            ["GuiltyPleasure"] = "LeagueOfLegendThings/Content/Icon/Guilty_Pleasure_mayhem_augment",
            ["AllForYou"] = "LeagueOfLegendThings/Content/Icon/All_For_You_mayhem_augment",
            ["CelestialBody"] = "LeagueOfLegendThings/Content/Icon/Celestial_Body_mayhem_augment",
            ["Cerberus"] = "LeagueOfLegendThings/Content/Icon/Cerberus_mayhem_augment",
            ["CriticalRhythm"] = "LeagueOfLegendThings/Content/Icon/Critical_Rhythm_mayhem_augment",
            ["DemonsDance"] = "LeagueOfLegendThings/Content/Icon/Demon's_Dance_mayhem_augment",
            ["DoubleTap"] = "LeagueOfLegendThings/Content/Icon/Double_Tap_mayhem_augment",
            ["EscapePlan"] = "LeagueOfLegendThings/Content/Icon/Escape_Plan_mayhem_augment",
            ["Firebrand"] = "LeagueOfLegendThings/Content/Icon/Firebrand_mayhem_augment",
            ["SoulSiphon"] = "LeagueOfLegendThings/Content/Icon/Soul_Siphon_mayhem_augment",
            ["GetExcited"] = "LeagueOfLegendThings/Content/Icon/Get_Excited_mayhem_augment",
            ["Goliath"] = "LeagueOfLegendThings/Content/Icon/Goliath_mayhem_augment",
            ["GiantSlayer"] = "LeagueOfLegendThings/Content/Icon/Giant_Slayer_mayhem_augment",
            ["GlassCannon"] = "LeagueOfLegendThings/Content/Icon/Glass_Cannon_mayhem_augment",
            ["DualWield"] = "LeagueOfLegendThings/Content/Icon/Dual_Wield_mayhem_augment",
            ["Earthwake"] = "LeagueOfLegendThings/Content/Icon/Earthwake_mayhem_augment",
            ["Eureka"] = "LeagueOfLegendThings/Content/Icon/Eureka_mayhem_augment",
            ["CircleOfDeath"] = "LeagueOfLegendThings/Content/Icon/Circle_of_Death_mayhem_augment",
            ["CantTouchThis"] = "LeagueOfLegendThings/Content/Icon/Can't_Touch_This_mayhem_augment",
            ["Dropkick"] = "LeagueOfLegendThings/Content/Icon/Dropkick_mayhem_augment",
        };

        // ============ 卡片类 ============

        private class AugmentCard : UIElement
        {
            private readonly int _index;
            private readonly Action<int> _onSelect;
            private readonly Action<int> _onReroll;

            private string _augmentName;
            private string _tier;
            private bool _rerolled;
            private Asset<Texture2D> _icon;

            public AugmentCard(int index, Action<int> onSelect, Action<int> onReroll)
            {
                _index = index; _onSelect = onSelect; _onReroll = onReroll;
            }

            public void SetAugment(string name, string tier, bool rerolled)
            {
                _augmentName = name; _tier = tier; _rerolled = rerolled;
                _icon = LoadIcon(name);
                BuildCard();
            }

            private static Asset<Texture2D> LoadIcon(string codeName)
            {
                if (IconPaths.TryGetValue(codeName ?? "", out string path))
                {
                    try { return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad); }
                    catch { }
                }
                return null;
            }

            private void BuildCard()
            {
                RemoveAllChildren();

                float w = Width.Pixels > 0 ? Width.Pixels : 190;
                float h = Height.Pixels > 0 ? Height.Pixels : 400;
                float pad = 10f;

                // 卡片背景
                var bg = new UIPanel();
                bg.Width.Set(w, 0f);
                bg.Height.Set(h, 0f);
                bg.SetPadding(0);
                bg.BackgroundColor = new Color(32, 32, 44) * 0.97f;
                bg.BorderColor = TierBorder(_tier);
                Append(bg);

                // 层级标签条（顶部细条）
                var tierStrip = new UIPanel();
                tierStrip.Width.Set(w - 2, 0f);
                tierStrip.Height.Set(4, 0f);
                tierStrip.Left.Set(1, 0f);
                tierStrip.Top.Set(1, 0f);
                tierStrip.SetPadding(0);
                tierStrip.BackgroundColor = TierAccent(_tier);
                tierStrip.BorderColor = Color.Transparent;
                bg.Append(tierStrip);

                // 图标区域
                float iconSize = w - pad * 4;
                float iconTop = pad * 2;

                // 图标背景框
                var iconFrame = new UIPanel();
                iconFrame.Width.Set(iconSize, 0f);
                iconFrame.Height.Set(iconSize, 0f);
                iconFrame.Left.Set(pad * 2, 0f);
                iconFrame.Top.Set(iconTop, 0f);
                iconFrame.SetPadding(0);
                iconFrame.BackgroundColor = new Color(20, 20, 30) * 0.8f;
                iconFrame.BorderColor = TierBorder(_tier) * 0.5f;
                bg.Append(iconFrame);

                // 图标图像 — 使用 NonPremultiplied 修复透明 PNG 边缘色溢
                if (_icon != null)
                {
                    var img = new NonPremultipliedUIImage(_icon);
                    img.Width.Set(iconSize - 6, 0f);
                    img.Height.Set(iconSize - 6, 0f);
                    img.Left.Set(3, 0f);
                    img.Top.Set(3, 0f);
                    iconFrame.Append(img);
                }
                else
                {
                    var fallback = new UIText(_augmentName ?? "???", 0.9f);
                    fallback.HAlign = 0.5f;
                    fallback.VAlign = 0.5f;
                    iconFrame.Append(fallback);
                }

                // 层级小标（图标右上角）
                var tierBadge = new UIPanel();
                tierBadge.Width.Set(56, 0f);
                tierBadge.Height.Set(20, 0f);
                tierBadge.Left.Set(iconSize - 48 + pad * 2, 0f);
                tierBadge.Top.Set(iconTop - 6, 0f);
                tierBadge.SetPadding(2);
                tierBadge.BackgroundColor = TierAccent(_tier) * 0.9f;
                tierBadge.BorderColor = Color.Transparent;
                var badgeText = new UIText(_tier ?? "", 0.65f);
                badgeText.HAlign = 0.5f;
                badgeText.VAlign = 0.5f;
                badgeText.TextColor = Color.White;
                tierBadge.Append(badgeText);
                bg.Append(tierBadge);

                // 名称
                float nameTop = iconTop + iconSize + pad;
                string locNameKey = $"Mods.LeagueOfLegendThings.MayhemAugments.{_tier}.{_augmentName}.DisplayName";
                var nameText = new UIText(Language.GetTextValue(locNameKey, _augmentName ?? ""), 1.0f);
                nameText.HAlign = 0.5f;
                nameText.Top.Set(nameTop, 0f);
                nameText.Width.Set(w - pad * 2, 0f);
                nameText.Left.Set(pad, 0f);
                nameText.TextColor = TierColor(_tier);
                bg.Append(nameText);

                // 描述
                string desc = GetDesc(_augmentName);
                float descTop = nameTop + 28;
                var descText = new UIText(desc, 0.78f);
                descText.Top.Set(descTop, 0f);
                descText.Left.Set(pad + 4, 0f);
                descText.Width.Set(w - pad * 2 - 8, 0f);
                descText.TextColor = new Color(180, 180, 195);
                descText.IsWrapped = true;
                bg.Append(descText);

                // 选择按钮
                float btnW = w - pad * 4;
                float btnH = 32;
                float btnX = pad * 2;
                float btnBottom = h - pad * 2;

                string selectText = Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemAugments.UI.Select", "Select");
                var selectBtn = new UITextPanel<string>(selectText, 0.85f);
                selectBtn.Width.Set(btnW, 0f);
                selectBtn.Height.Set(btnH, 0f);
                selectBtn.Left.Set(btnX, 0f);
                selectBtn.Top.Set(btnBottom - btnH - 4 - btnH, 0f);
                selectBtn.SetPadding(6);
                selectBtn.BackgroundColor = new Color(45, 110, 45);
                selectBtn.BorderColor = new Color(80, 180, 80);
                selectBtn.OnLeftClick += (_, _) => _onSelect(_index);
                bg.Append(selectBtn);

                // 刷新按钮
                string rerollText = _rerolled
                    ? Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemAugments.UI.Rerolled", "↻ Rerolled")
                    : Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemAugments.UI.Reroll", "↻ Reroll");
                var rerollBtn = new UITextPanel<string>(rerollText, 0.78f);
                rerollBtn.Width.Set(btnW, 0f);
                rerollBtn.Height.Set(btnH, 0f);
                rerollBtn.Left.Set(btnX, 0f);
                rerollBtn.Top.Set(btnBottom - btnH, 0f);
                rerollBtn.SetPadding(6);

                if (_rerolled)
                {
                    rerollBtn.BackgroundColor = new Color(40, 40, 40);
                    rerollBtn.BorderColor = new Color(60, 60, 60);
                    rerollBtn.TextColor = new Color(130, 130, 130);
                }
                else
                {
                    rerollBtn.BackgroundColor = new Color(55, 55, 90);
                    rerollBtn.BorderColor = new Color(100, 100, 160);
                    rerollBtn.OnLeftClick += (_, _) => _onReroll(_index);
                }
                bg.Append(rerollBtn);
            }

            // ============ 配色 ============

            private static Color TierBorder(string t) => t switch
            { "Silver" => new Color(140, 140, 175), "Gold" => new Color(210, 170, 45), "Prismatic" => new Color(185, 65, 210), _ => new Color(100, 100, 100) };

            private static Color TierAccent(string t) => t switch
            { "Silver" => new Color(110, 110, 150), "Gold" => new Color(190, 150, 35), "Prismatic" => new Color(160, 40, 185), _ => new Color(80, 80, 80) };

            private static Color TierColor(string t) => t switch
            { "Silver" => new Color(180, 180, 210), "Gold" => new Color(240, 195, 55), "Prismatic" => new Color(220, 90, 240), _ => Color.White };

            // ============ 描述 ============

            private string GetDesc(string name)
            {
                if (string.IsNullOrEmpty(name)) return name;
                string descKey = $"Mods.LeagueOfLegendThings.MayhemAugments.{_tier}.{name}.Description";
                string localized = Language.GetTextValue(descKey);
                // 如果本地化值等于 key（即未找到本地化），返回 internal name
                if (localized == descKey) return name;
                return localized;
            }
        }

        // ============ 可拖拽面板 ============

        private class DraggableUIPanel : UIPanel
        {
            private bool _dragging;
            private Vector2 _dragOffset;

            public override void LeftMouseDown(UIMouseEvent evt)
            {
                if (evt.Target == this)
                {
                    _dragging = true;
                    _dragOffset = new Vector2(Main.mouseX, Main.mouseY) - new Vector2(Left.Pixels, Top.Pixels);
                }
                base.LeftMouseDown(evt);
            }

            public override void LeftMouseUp(UIMouseEvent evt)
            {
                _dragging = false;
                base.LeftMouseUp(evt);
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (_dragging)
                {
                    Left.Pixels = Main.mouseX - _dragOffset.X;
                    Top.Pixels = Main.mouseY - _dragOffset.Y;
                    Left.Percent = 0f;
                    Top.Percent = 0f;
                }
                if (!Main.mouseLeft) _dragging = false;
            }
        }

        // ============ 修复透明PNG边缘溢色 ============

        /// <summary>
        /// 使用 BlendState.NonPremultiplied 绘制贴图，避免透明 PNG
        /// 在 UI 缩放时产生边缘色溢（fringing）问题
        /// </summary>
        private class NonPremultipliedUIImage : UIElement
        {
            private readonly Asset<Texture2D> _texture;

            public NonPremultipliedUIImage(Asset<Texture2D> texture)
            {
                _texture = texture;
                IgnoresMouseInteraction = true;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (_texture?.Value == null) return;
                var tex = _texture.Value;
                CalculatedStyle dim = GetDimensions();

                float scale = Math.Min(dim.Width / tex.Width, dim.Height / tex.Height);
                float drawW = tex.Width * scale;
                float drawH = tex.Height * scale;
                float x = dim.X + (dim.Width - drawW) / 2f;
                float y = dim.Y + (dim.Height - drawH) / 2f;

                // 保存并切换 blend state
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                    SamplerState.PointClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);

                spriteBatch.Draw(tex, new Rectangle((int)x, (int)y, (int)drawW, (int)drawH),
                    null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);

                // 恢复默认 blend state
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);
            }
        }
    }
}
