using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using LeagueOfLegendThings.Content.Buffs.Mayhem;

namespace LeagueOfLegendThings.Content.UI
{
    public class StatAnvilUIState : UIState
    {
        private UIPanel _header;          // 仅覆盖标题的窄面板
        private UIText _title;
        private ShardCard[] _cards = new ShardCard[3];
        private List<StatShardSystem.Shard> _options;
        private StatShardSystem.ShardTier _tier;
        private bool _shardholderWarning;
        private int _warningCard = -1;
        private int _openCooldown; // 打开后短暂禁用点击，防止误触

        public bool SelectionMade { get; set; }
        public StatShardSystem.Shard ChosenShard { get; private set; }

        // ============ 打开 / 关闭 ============

        private const float CARD_RATIO = 0.66f;

        public void Open(List<StatShardSystem.Shard> options, StatShardSystem.ShardTier tier)
        {
            _options = options;
            _tier = tier;
            SelectionMade = false;
            ChosenShard = null;
            _shardholderWarning = false;
            _warningCard = -1;
            _openCooldown = 20; // ~0.33 秒内忽略点击

            for (int i = 0; i < options.Count; i++)
                if (options[i].Id == StatShardSystem.SHARDHOLDER_ID)
                    _warningCard = i;

            // 每次打开重新布局，适配当前窗口分辨率
            LayoutAll();

            if (_header.Parent == null) Append(_header);
            for (int i = 0; i < 3; i++)
                if (_cards[i]?.Parent == null) Append(_cards[i]);

            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_openCooldown > 0) _openCooldown--;
        }

        public void Close()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            _header?.Remove();
            foreach (var c in _cards) c?.Remove();
        }

        // ============ 布局（每次 Open 执行，响应窗口大小变化）============

        private void LayoutAll()
        {
            float sw = Main.screenWidth;
            float sh = Main.screenHeight;

            // --- 标题栏 ---
            if (_header == null)
            {
                _header = new UIPanel();
                _header.SetPadding(10);
                _header.BackgroundColor = new Color(18, 18, 32) * 0.93f;
                _header.BorderColor = new Color(180, 160, 80);

                _title = new UIText(Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.Title", "Stat Bonus - Choose a Shard"), 1.15f);
                _title.HAlign = 0.5f; _title.VAlign = 0.3f;
                _header.Append(_title);

                var tierLabel = new UIText("", 0.8f);
                tierLabel.HAlign = 0.5f; tierLabel.VAlign = 0.85f;
                _header.Append(tierLabel);
            }

            _header.Width.Set(480, 0f);
            _header.Height.Set(56, 0f);
            _header.Left.Set((sw - 480) / 2f, 0f);
            _header.Top.Set(18, 0f);

            // 更新层级标签
            var tl = (UIText)_header.Children.ElementAtOrDefault(1);
            if (tl != null) { tl.TextColor = TierColor(_tier); tl.SetText(TierLabel(_tier)); }

            // --- 三张卡片 ---
            float colW = sw / 3f;
            float cardW = colW * CARD_RATIO;       // 列宽的 66%
            float cardH = sh * CARD_RATIO;  // 高度也按比例

            // 列中心: sw/6, sw/2, 5*sw/6
            float[] cx = { sw / 6f, sw / 2f, 5f * sw / 6f };
            float cardTop = sh * 0.14f;

            for (int i = 0; i < 3; i++)
            {
                if (_cards[i] == null)
                {
                    bool isSH = i < _options.Count && _options[i].Id == StatShardSystem.SHARDHOLDER_ID;
                    _cards[i] = new ShardCard(i, OnCardClick, isSH);
                }

                _cards[i].Width.Set(cardW, 0f);
                _cards[i].Height.Set(cardH, 0f);
                _cards[i].Left.Set(cx[i] - cardW / 2f, 0f);
                _cards[i].Top.Set(cardTop, 0f);
            }

            RefreshCards();
        }

        private void RefreshCards()
        {
            for (int i = 0; i < 3 && i < _options.Count; i++)
            {
                var shard = _options[i];
                bool isSH = shard.Id == StatShardSystem.SHARDHOLDER_ID;
                bool warned = isSH && _shardholderWarning;
                _cards[i]?.SetShard(shard, _tier, isSH, warned);
            }
        }

        // ============ 回调 ============

        private void OnCardClick(int cardIndex)
        {
            if (SelectionMade || cardIndex >= _options.Count) return;
            if (_openCooldown > 0) return; // 防误触冷却中

            var shard = _options[cardIndex];
            bool isSH = shard.Id == StatShardSystem.SHARDHOLDER_ID;

            if (isSH && !_shardholderWarning)
            {
                // 第一次点击 Shardholder → 弹出确认
                _shardholderWarning = true;
                float pct = shard.StatValue * 100f;
                Main.NewText(
                    Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.ShardholderWarning", pct),
                    255, 80, 80);
                RefreshCards();
                return;
            }

            // 确认选择
            SelectionMade = true;
            ChosenShard = shard;
            Close();
        }

        // ============ 配色 ============

        private static string TierLabel(StatShardSystem.ShardTier t) => t switch
        {
            StatShardSystem.ShardTier.Silver    => Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.SilverLabel", "Silver Shard (random value)"),
            StatShardSystem.ShardTier.Gold      => Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.GoldLabel", "Gold Shard (fixed value)"),
            StatShardSystem.ShardTier.Prismatic => Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.PrismaticLabel", "Prismatic Shard (powerful!)"),
            _ => ""
        };

        private static Color TierColor(StatShardSystem.ShardTier t) => t switch
        {
            StatShardSystem.ShardTier.Silver    => new Color(180, 180, 200),
            StatShardSystem.ShardTier.Gold      => new Color(230, 190, 60),
            StatShardSystem.ShardTier.Prismatic => new Color(210, 80, 230),
            _ => Color.White
        };

        private static Color TierBorder(StatShardSystem.ShardTier t) => t switch
        {
            StatShardSystem.ShardTier.Silver    => new Color(140, 140, 175),
            StatShardSystem.ShardTier.Gold      => new Color(210, 170, 45),
            StatShardSystem.ShardTier.Prismatic => new Color(185, 65, 210),
            _ => new Color(100, 100, 100)
        };

        // ============ 图标映射 ============

        /// <summary>根据碎片 ID 后缀返回对应的图标资源路径</summary>
        private static string GetIconPath(string shardId)
        {
            if (string.IsNullOrEmpty(shardId)) return null;
            string suffix = shardId.Contains('_') ? shardId.Substring(shardId.IndexOf('_') + 1) : shardId;

            return suffix switch
            {
                "Melee"       => "LeagueOfLegendThings/Content/Icon/Shard_AD",
                "Ranged"      => "LeagueOfLegendThings/Content/Icon/Shard_AD",
                "Might"       => "LeagueOfLegendThings/Content/Icon/Shard_AP",
                "Magic"       => "LeagueOfLegendThings/Content/Icon/Shard_AP",
                "Summon"      => "LeagueOfLegendThings/Content/Icon/Shard_AP",
                "Def"         => "LeagueOfLegendThings/Content/Icon/Shard_Armor",
                "Unbreak"     => "LeagueOfLegendThings/Content/Icon/Shard_Armor",
                "ArmPen"      => "LeagueOfLegendThings/Content/Icon/Shard_ArmorPen",
                "Mana"        => "LeagueOfLegendThings/Content/Icon/Shard_AP",
                "Life"        => "LeagueOfLegendThings/Content/Icon/Shard_Health",
                "LS"          => "LeagueOfLegendThings/Content/Icon/Shard_Omnivamp",
                "Heal"        => "LeagueOfLegendThings/Content/Icon/Shard_Heal",
                "AllDmg"      => "LeagueOfLegendThings/Content/Icon/Shard_AD",
                "AS"          => "LeagueOfLegendThings/Content/Icon/Shard_AS",
                "Crit"        => "LeagueOfLegendThings/Content/Icon/Shard_Crit",
                "Faith"       => "LeagueOfLegendThings/Content/Icon/Shard_CDR",
                "CritDmg"     => "LeagueOfLegendThings/Content/Icon/Shard_CritDmg",
                "Move"        => "LeagueOfLegendThings/Content/Icon/Shard_Move",
                "Precision"  => "LeagueOfLegendThings/Content/Icon/Shard_Crit",
                "Vitality"   => "LeagueOfLegendThings/Content/Icon/Shard_Health",
                "Shardholder" => "LeagueOfLegendThings/Content/Icon/Shard_CritDmg",
                _ => null
            };
        }

        private static string FormatShardValue(StatShardSystem.Shard shard)
        {
            if (shard == null) return "";
            float v = shard.StatValue;
            string suffix = shard.Id.Contains('_') ? shard.Id.Substring(shard.Id.IndexOf('_') + 1) : "";
            // Faith 显示为百分比（如 235%）
            if (suffix == "Faith")
                return $"{v * 100f:F0}%";
            // 百分比类
            if (suffix is "Melee" or "Ranged" or "Magic" or "Summon" or "AllDmg" or "Might"
                or "AS" or "Crit" or "Move" or "LS" or "Heal" or "CritDmg" or "Life")
                return $"{v:P0}";
            // 百分比棱彩
            if (shard.Id.StartsWith("Pris_") && suffix is "Life")
                return $"{v:P0}";
            // 绝对值类
            return $"{(int)v}";
        }

        private static Asset<Texture2D> LoadShardIcon(string shardId)
        {
            string path = GetIconPath(shardId);
            if (path == null) return null;
            try { return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad); }
            catch { return null; }
        }

        // ============ 卡片元素 ============

        private class ShardCard : UIElement
        {
            private readonly int _index;
            private readonly System.Action<int> _onClick;
            private bool _isShardholder;

            private StatShardSystem.Shard _shard;
            private StatShardSystem.ShardTier _tier;
            private bool _warned;
            private float _hoverScale;

            public ShardCard(int index, System.Action<int> onClick, bool isShardholder)
            {
                _index = index; _onClick = onClick; _isShardholder = isShardholder;
            }

            public void SetShard(StatShardSystem.Shard shard, StatShardSystem.ShardTier tier,
                bool isSH, bool warned)
            {
                _shard = shard; _tier = tier; _isShardholder = isSH; _warned = warned;
                Build();
            }

            private void Build()
            {
                RemoveAllChildren();

                float w = Width.Pixels > 0 ? Width.Pixels : 400;
                float h = Height.Pixels > 0 ? Height.Pixels : 300;

                // 卡片背景
                var bg = new UIPanel();
                bg.Width.Set(w, 0f); bg.Height.Set(h, 0f); bg.SetPadding(0);
                bg.BackgroundColor = new Color(28, 28, 42) * 0.96f;
                bg.BorderColor = _warned ? new Color(255, 60, 60) : TierBorder(_tier);
                Append(bg);

                // 层级色条 — 纯色矩形，圆角由 bg 面板边框提供
                int stripH = (int)(Main.screenHeight * 0.0042f);
                var strip = new FilledRect(TierColor(_tier));
                strip.Width.Set(w - 4, 0f); strip.Height.Set(stripH, 0f);
                strip.Left.Set(2, 0f); strip.Top.Set(2, 0f);
                bg.Append(strip);

                // Shardholder 提示横幅
                if (_isShardholder && _warned)
                {
                    var warnBar = new UIPanel();
                    warnBar.Width.Set(w - 12, 0f); warnBar.Height.Set(28, 0f);
                    warnBar.Left.Set(6, 0f); warnBar.Top.Set(10, 0f); warnBar.SetPadding(4);
                    warnBar.BackgroundColor = new Color(120, 25, 25) * 0.85f;
                    warnBar.BorderColor = new Color(255, 80, 80);
                    var wt = new UIText(Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.ClickAgain"), 0.75f);
                    wt.HAlign = 0.5f; wt.VAlign = 0.5f; wt.TextColor = new Color(255, 180, 180);
                    warnBar.Append(wt);
                    bg.Append(warnBar);
                }

                // 图标区
                float iconTop = (_isShardholder && _warned) ? 48 : 20;
                float iconSize = w * 0.55f;
                var iconFrame = new UIPanel();
                iconFrame.Width.Set(iconSize, 0f); iconFrame.Height.Set(iconSize, 0f);
                iconFrame.Left.Set((w - iconSize) / 2f, 0f); iconFrame.Top.Set(iconTop, 0f);
                iconFrame.SetPadding(0);
                iconFrame.BackgroundColor = TierColor(_tier) * 0.15f;
                iconFrame.BorderColor = TierBorder(_tier) * 0.5f;
                bg.Append(iconFrame);

                // 图标 — 使用 NonPremultiplied 修复透明边缘溢色
                Asset<Texture2D> iconTex = LoadShardIcon(_shard?.Id);
                if (iconTex != null)
                {
                    var img = new NonPremultipliedUIImage(iconTex);
                    float imgSize = iconSize * 0.82f;
                    img.Width.Set(imgSize, 0f); img.Height.Set(imgSize, 0f);
                    img.Left.Set((iconSize - imgSize) / 2f, 0f);
                    img.Top.Set((iconSize - imgSize) / 2f, 0f);
                    iconFrame.Append(img);
                }
                else
                {
                    var iconLabel = new UIText(_shard?.GetDisplayName() ?? Language.GetTextValue("Mods.LeagueOfLegendThings.StatShards.Unknown"), 0.9f);
                    iconLabel.HAlign = 0.5f; iconLabel.VAlign = 0.5f;
                    iconLabel.TextColor = _isShardholder ? Color.Gold : Color.White;
                    iconFrame.Append(iconLabel);
                }

                // 描述 — HJSON 未加引号时 \n 是字面量，先转为真实换行再拆分
                float descTop = iconTop + iconSize + 12;
                string desc;
                if (_isShardholder)
                    desc = Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.ShardholderDesc", _shard?.StatValue ?? 0f);
                else if (_shard?.IsDualStat == true)
                    desc = FormatDualStatDesc(_shard);
                else
                    desc = $"{_shard?.GetDisplayName() ?? Language.GetTextValue("Mods.LeagueOfLegendThings.StatShards.Unknown")}\n+{FormatShardValue(_shard)}";

                desc = desc.Replace("\\n", "\n");
                string[] descLines = desc.Split('\n');
                float lineHeight = 22f;
                Color descColor = _isShardholder ? Color.Gold : new Color(220, 220, 235);
                for (int li = 0; li < descLines.Length; li++)
                {
                    var descLine = new UIText(descLines[li], 0.9f);
                    descLine.HAlign = 0.5f;
                    descLine.Top.Set(descTop + li * lineHeight, 0f);
                    descLine.Left.Set(8, 0f);
                    descLine.Width.Set(w - 16, 0f);
                    descLine.TextColor = descColor;
                    bg.Append(descLine);
                }

                // 层标签
                var tierBadge = new UIText(_tier.ToString(), 0.72f);
                tierBadge.HAlign = 0.5f;
                tierBadge.Top.Set(h - 22, 0f);
                tierBadge.Left.Set(10, 0f);
                tierBadge.Width.Set(w - 20, 0f);
                tierBadge.TextColor = TierColor(_tier);
                bg.Append(tierBadge);

                // Shardholder Confirm 按钮
                if (_isShardholder && _warned)
                {
                    float btnW = w * 0.6f, btnH = 30;
                    var btn = new UITextPanel<string>("Confirm", 0.85f);
                    btn.Width.Set(btnW, 0f); btn.Height.Set(btnH, 0f);
                    btn.Left.Set((w - btnW) / 2f, 0f); btn.Top.Set(h - btnH - 36, 0f);
                    btn.SetPadding(4);
                    btn.BackgroundColor = new Color(180, 40, 30);
                    btn.BorderColor = new Color(255, 60, 60);
                    btn.TextColor = Color.White;
                    btn.OnLeftClick += (_, _) => _onClick(_index);
                    bg.Append(btn);
                }
            }

            // 点击 — 全部委托给 OnCardClick，由其处理 Shardholder 两段确认逻辑
            public override void LeftClick(UIMouseEvent evt)
            {
                base.LeftClick(evt);
                _onClick(_index);
            }

            public override void MouseOver(UIMouseEvent evt)
            {
                base.MouseOver(evt);
                _hoverScale = 1.03f;
                SoundEngine.PlaySound(SoundID.MenuTick);
            }

            public override void MouseOut(UIMouseEvent evt)
            {
                base.MouseOut(evt);
                _hoverScale = 0f;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                // 悬浮放大
                if (_hoverScale > 0f)
                {
                    var dims = GetDimensions();
                    var orig = new Vector2(dims.X + dims.Width / 2f, dims.Y + dims.Height / 2f);
                    // 简单放大通过 UIElement 不支持，这里仅做标记
                }
                base.DrawSelf(spriteBatch);
            }

            private static Color TierBorder(StatShardSystem.ShardTier t) => t switch
            {
                StatShardSystem.ShardTier.Silver    => new Color(140, 140, 175),
                StatShardSystem.ShardTier.Gold      => new Color(210, 170, 45),
                StatShardSystem.ShardTier.Prismatic => new Color(185, 65, 210),
                _ => new Color(100, 100, 100)
            };

            private static Color TierColor(StatShardSystem.ShardTier t) => t switch
            {
                StatShardSystem.ShardTier.Silver    => new Color(180, 180, 210),
                StatShardSystem.ShardTier.Gold      => new Color(240, 195, 55),
                StatShardSystem.ShardTier.Prismatic => new Color(220, 90, 240),
                _ => Color.White
            };

            /// <summary>双属性碎片描述 — 同时展示两个受影响的属性</summary>
            private static string FormatDualStatDesc(StatShardSystem.Shard shard)
            {
                if (shard == null) return "";
                float v = shard.StatValue;
                string name = shard.GetDisplayName();
                string suffix = shard.Id.Contains('_') ? shard.Id.Substring(shard.Id.IndexOf('_') + 1) : "";

                return suffix switch
                {
                    "Might" => $"{name}\n+{v:P0} Melee Damage\n+{v:P0} Ranged Damage",
                    "Unbreak" => $"{name}\n+{(int)v} Defense\n+{(int)(v * 2)} Max Life",
                    "Precision" => $"{name}\n+{v:P0} Crit Chance\n+{v:P0} Attack Speed",
                    "Vitality" => $"{name}\n+{(int)v} Max Life\n+{(int)v} Max Mana",
                    _ => $"{name}\n+{FormatShardValue(shard)}"
                };
            }
        }

        // ============ 纯色填充矩形（无边框，避免 UIPanel 9-slice 渲染差异）============

        private class FilledRect : UIElement
        {
            private readonly Color _color;
            public FilledRect(Color color) { _color = color; IgnoresMouseInteraction = true; }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                var dim = GetDimensions();
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value,
                    new Rectangle((int)dim.X, (int)dim.Y, (int)dim.Width, (int)dim.Height),
                    _color);
            }
        }

        // ============ 透明 PNG 无溢色绘制 ============

        private class NonPremultipliedUIImage : UIElement
        {
            private readonly Asset<Texture2D> _texture;
            public NonPremultipliedUIImage(Asset<Texture2D> t) { _texture = t; IgnoresMouseInteraction = true; }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (_texture?.Value == null) return;
                var tex = _texture.Value;
                var dim = GetDimensions();
                float s = System.Math.Min(dim.Width / tex.Width, dim.Height / tex.Height);
                float x = dim.X + (dim.Width - tex.Width * s) / 2f;
                float y = dim.Y + (dim.Height - tex.Height * s) / 2f;
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                    SamplerState.PointClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);
                spriteBatch.Draw(tex, new Vector2(x, y), null, Color.White, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);
            }
        }

    }
}
