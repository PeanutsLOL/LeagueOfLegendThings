using System;
using Terraria.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LeagueOfLegendThings.Content.Config;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.Localization;

namespace LeagueOfLegendThings.Content.UI
{
    /// <summary>
    /// 符文选择界面。
    ///
    /// 视觉参照 101.qq.com/#/data/runes 的符文页：
    ///   - 深色扁平底，不用灰色圆角盒子
    ///   - 每列左侧一条竖直轴，轴上按行分布小圆节点
    ///   - 行与行之间用细横线分隔，线左端带一个小节点
    ///   - 「基石」标题居中，两侧各一段细横线
    ///   - 未选中的符文压暗成灰调，选中的才点亮并套一圈发光的路径色圆环
    ///   - 所有选项常驻铺开，点击即选、再点取消，描述改为悬停查看
    /// </summary>
    public class RuneUIState : UIState
    {
        private DraggableUIPanel _panel;
        private DraggableUITextPanel _mainButton;
        private LockButton _buttonLockToggle;
        // 描述卡片放不下时的自动滚动：> 0 表示需要滚动的距离（px）
        private float _detailScrollMax;
        private double _detailScrollClock;
        private UIText _panelTitle;
        private bool _mainButtonLocked = true;
        private UIElement _primaryGroup;
        private UIElement _secondaryGroup;
        private GridDecor _primaryDecor;
        private GridDecor _secondaryDecor;
        private bool _open;
        private bool _textsRefreshed; // 延迟到首个 Update 刷新文本（本地化就绪）

        private UIPanel _detailPanel;
        private UIText _detailTitle;
        private DetailText _detailDesc;

        /// <summary>
        /// 描述卡片的可用内宽 = 卡片宽 − 左右内边距。
        /// <para/>
        /// UIPanel 自带默认内边距（本版本 = 10），子元素会被整体往里推，所以「在自己宽度里居中」
        /// 并不等于「在卡片里居中」，会差一个内边距。文字元素必须按**内框**取宽并靠 Left = 0 对齐，
        /// 居中的基准才是卡片中线 —— 内框左右对称，内框中心就是卡片中心。
        /// </summary>
        private float DetailInnerW => DetailW - _detailPanel.PaddingLeft - _detailPanel.PaddingRight;

        private readonly string[] Paths = { RuneIds.PathPrecision, RuneIds.PathDomination, RuneIds.PathSorcery, RuneIds.PathResolve, RuneIds.PathInspiration };

        /// <summary>
        /// 每系的配色，取自 101.qq.com 符文模拟器的主题表（ClassicRuneView 的 RuneSimulator）：
        ///   8000 精密 themeColor #C4A168 / bgColor #0c0f14 / rail #866C47
        ///   8100 主宰 themeColor #E63946 / bgColor #0d0c14 / rail #CB413F
        ///   8200 巫术 themeColor #9FAAFC / bgColor #0a0e1d / rail #9EA9FB
        ///   8400 坚决 themeColor #10B981 / bgColor #0a1615 / rail 中心 #A0D386、两侧 #688C5B
        ///   8300 启迪 themeColor #06B6D4 / bgColor #090f19 / rail #48A5B6
        /// </summary>
        private readonly struct PathTheme
        {
            public readonly Color Accent;      // themeColor：选中圆环 / 行节点 / 「基石」文字
            public readonly Color Backdrop;    // bgColor：面板底色
            public readonly Color RailCenter;  // 竖轴中间那条（opacity .30）
            public readonly Color RailSide;    // 竖轴两侧那两条（opacity .10）

            public PathTheme(Color accent, Color backdrop, Color railCenter, Color railSide)
            {
                Accent = accent; Backdrop = backdrop; RailCenter = railCenter; RailSide = railSide;
            }
        }

        private static Color Hex(string rgb) => new Color(
            System.Convert.ToInt32(rgb.Substring(0, 2), 16),
            System.Convert.ToInt32(rgb.Substring(2, 2), 16),
            System.Convert.ToInt32(rgb.Substring(4, 2), 16));

        private static readonly Dictionary<string, PathTheme> PathThemes = new()
        {
            { RuneIds.PathPrecision,   new PathTheme(Hex("C4A168"), Hex("0c0f14"), Hex("866C47"), Hex("866C47")) },
            { RuneIds.PathDomination,  new PathTheme(Hex("E63946"), Hex("0d0c14"), Hex("CB413F"), Hex("CB413F")) },
            { RuneIds.PathSorcery,     new PathTheme(Hex("9FAAFC"), Hex("0a0e1d"), Hex("9EA9FB"), Hex("9EA9FB")) },
            { RuneIds.PathResolve,     new PathTheme(Hex("10B981"), Hex("0a1615"), Hex("A0D386"), Hex("688C5B")) },
            { RuneIds.PathInspiration, new PathTheme(Hex("06B6D4"), Hex("090f19"), Hex("48A5B6"), Hex("48A5B6")) },
        };

        private static readonly PathTheme FallbackTheme = new PathTheme(
            new Color(150, 150, 150), new Color(14, 16, 21), new Color(90, 94, 104), new Color(90, 94, 104));

        private static PathTheme ThemeOf(string pathId)
            => pathId != null && PathThemes.TryGetValue(pathId, out var t) ? t : FallbackTheme;

        /// <summary>
        /// 参考站内容列宽 427px，本实现主系内容列宽 304px —— 所有尺寸按这个比例换算。
        /// </summary>
        private const float RefScale = PrimaryContentW / 427f;   // ≈ 0.712
        /// <summary>行分隔线宽度：参考站 w-[280px] 居中。</summary>
        private const float SeparatorW = 280f * RefScale;        // ≈ 199
        /// <summary>竖轴中间那条的宽度：参考站 6px（中心 opacity .30），两侧 1px（opacity .10）。</summary>
        private const float RailCenterW = 6f * RefScale;         // ≈ 4.3
        private const float RailSideOpacity = 0.10f;
        private const float RailCenterOpacity = 0.30f;
        /// <summary>行节点：参考站 20px 描边圆 + 内嵌 12px 实心圆。</summary>
        private const float NodeOuterR = 10f * RefScale;         // ≈ 7.1
        private const float NodeInnerR = 6f * RefScale;          // ≈ 4.3

        // ================= 网格几何 =================
        private const float SlotSize = 48f;          // 普通符文 / 路径图标
        private const float KeystoneSlotSize = 54f;  // 基石图标
        private const float SlotSpacing = 16f;       // 同行图标间距
        private const float RowGap = 18f;            // 普通行间距
        private const float PathRowGap = 26f;        // 路径行 -> 下一段
        private const float SpineGutter = 30f;       // 左侧竖轴占位宽度
        private const float LabelBandH = 34f;        // 「基石」标题带高度（小字一行 + 装饰线一行）
        private const float ColumnGap = 64f;         // 主系 / 副系之间
        private const float TitleBandH = 26f;
        private const float PadH = 22f;
        private const float PadV = 18f;
        private const float GroupTop = TitleBandH + 10f;

        private const int PathCount = 5;
        private const int SecondaryPathCount = 4;
        private const int RunesPerRow = 3;

        private const float PrimaryContentW = PathCount * SlotSize + (PathCount - 1) * SlotSpacing;                  // 304
        private const float SecondaryContentW = SecondaryPathCount * SlotSize + (SecondaryPathCount - 1) * SlotSpacing; // 240
        private const float PrimaryRowBlockH = RunesPerRow * SlotSize + (RunesPerRow - 1) * RowGap;                  // 180

        private const float PrimaryGroupH = SlotSize + PathRowGap + LabelBandH + KeystoneSlotSize + RowGap + PrimaryRowBlockH; // 356
        // 副系没有基石行，但三行小符文与主系那三行对齐，两列底部齐平、分隔线跨列成一条
        private const float SecondaryGroupH = PrimaryGroupH;                                                                   // 356

        private const float SecondaryDecorLeft = SpineGutter + PrimaryContentW + ColumnGap;  // 398
        private const float SecondaryGroupLeft = SecondaryDecorLeft + SpineGutter;            // 428
        private const float SecondaryDecorW = SpineGutter + SecondaryContentW;                // 270

        private const float PanelW = SpineGutter + PrimaryContentW + ColumnGap + SpineGutter + SecondaryContentW + PadH * 2; // 712

        // 描述卡：放在面板**右上角那块空白**里（主系路径行右侧、副系路径行上方）。
        // 这一改同时解决两件事：填掉右上角的空洞，并省掉原来占满整行宽、94px 高的底部说明栏
        // —— 面板高度从 538 降到 444（矮 17%），小窗口下也宽裕得多。
        // 尺寸依据：右上空档约 328×86；62 条符文描述最长 50 字（不灭之握），
        // 按 0.72 缩放约每行 17 字，最长也只需 3 行。
        // 横向：与**副系那一列的装饰盒同位**（左边/右边都落在网格线上），
        // 而不是像之前那样浮在两列之间当一个孤立方块：
        //   左边 = 该列竖轴沟槽的起点（= 图标网格左缘再往左一个 SpineGutter）
        //   右边 = 图标网格右缘
        // （第十版曾把卡片右移 15px，让卡片中线对齐副系图标网格中线；
        //   用户澄清要的是**另一个方向**（纵向）的中线，那 15px 已回退。）
        // 纵向：卡片**竖向中线对齐主系「系别选择」那一行（路径行）的中线**。
        //   主系路径行 = _primaryGroup 的第 0 行（见 BuildPrimaryGroup «1) 路径行»）：
        //     inner y = GroupTop(36) .. +SlotSize(48)，中线 = GroupTop + SlotSize/2 = 60
        //   卡片 110 高 ⇒ 上缘 = 60 − 55 = 5
        //   于是卡片与那一行图标上下居中对齐（卡片上缘高出图标行 31px，下缘低 31px）。
        //   卡片下缘 115，仍在副系路径行(150) 上方，留 35px；标题带在左半边，横向不冲突。
        private const float DetailLeft = SecondaryDecorLeft;                            // 398
        private const float DetailTop = GroupTop + SlotSize / 2f - DetailH / 2f;         // 5
        private const float DetailW = SecondaryDecorW;         // 270
        private const float DetailH = 110f;                    // 卡片高；上缘见 DetailTop(5)，下缘 115，仍在副系路径行(150) 上方
        // 卡片内描述文字的起点 / 底部留白 / 字号；可用高度 = DetailH - DescTop - DescBottomPad = 78
        // 字号从 0.72 降到 0.68：行距约 28×0.68 ≈ 19，最长的 50 字换 4 行 = 76 ≤ 78，刚好装下。
        private const float DescTop = 26f;
        private const float DescBottomPad = 6f;
        private const float DescScale = 0.68f;
        private static readonly Color DetailTextColor = new(206, 210, 220);
        private const float PanelH = GroupTop + PrimaryGroupH + 12f + PadV * 2f;  // 444
        private const float LockSize = 24f;                                       // 右下角小锁图标

        // 行 Y（组内局部坐标）
        private const float LabelBandY = SlotSize + PathRowGap;                        // 74
        private const float LabelCenterY = LabelBandY + 11f;                           // 85  「基石」文字中心
        private const float OrnamentY = LabelBandY + 24f;                              // 98  文字下方那条装饰线
        private const float KeystoneRowY = LabelBandY + LabelBandH;                    // 108
        private const float PrimaryRow0Y = KeystoneRowY + KeystoneSlotSize + RowGap;   // 180

        // 副系路径行：底边与主系基石行对齐（109+54-48=114）。
        // 用底边而不是顶边对齐，是为了让它下方那条分隔线（114+48+9=171）
        // 与主系基石行下方那条（108+54+9=171）精确落在同一条水平线上。
        private const float SecondaryPathRowY = KeystoneRowY + KeystoneSlotSize - SlotSize;    // 114

        private readonly Dictionary<string, string[]> _keystones = new()
        {
            { RuneIds.PathPrecision, new[]{RuneIds.RunePressTheAttack,RuneIds.RuneLethalTempo,RuneIds.RuneFleetFootwork,RuneIds.RuneConqueror} },
            { RuneIds.PathDomination, new[]{RuneIds.RuneElectrocute,RuneIds.RuneHailOfBlades,RuneIds.RuneDarkHarvest} },
            { RuneIds.PathSorcery, new[]{RuneIds.RuneSummonAery,RuneIds.RuneArcaneComet,RuneIds.RuneStormraidersSurge,RuneIds.RuneDeathfireTouch} },
            { RuneIds.PathResolve, new[]{RuneIds.RuneGraspOfTheUndying,RuneIds.RuneAftershock,RuneIds.RuneGuardian} },
            { RuneIds.PathInspiration, new[]{RuneIds.RuneGlacialAugment,RuneIds.RuneUnsealedSpellbook,RuneIds.RuneFirstStrike} },
        };

        private readonly Dictionary<string, string[][]> _rows = new()
        {
            { RuneIds.PathPrecision, new[]{ new[]{ RuneIds.RuneAbsorbLife, RuneIds.RuneTriumph,RuneIds.RunePresenceOfMind}, new[]{RuneIds.RuneLegendAlacrity,RuneIds.RuneLegendHaste,RuneIds.RuneLegendBloodline}, new[]{RuneIds.RuneCoupDeGrace,RuneIds.RuneCutDown,RuneIds.RuneLastStand} } },
            { RuneIds.PathDomination, new[]{ new[]{RuneIds.RuneCheapShot,RuneIds.RuneTasteOfBlood,RuneIds.RuneSuddenImpact}, new[]{RuneIds.RuneGrislyMementos,RuneIds.RuneSixthSense,RuneIds.RuneDeepWard}, new[]{RuneIds.RuneTreasureHunter,RuneIds.RuneRelentlessHunter,RuneIds.RuneUltimateHunter} } },
            { RuneIds.PathSorcery, new[]{ new[]{ RuneIds.RuneAxiomArcanist, RuneIds.RuneManaflowBand,RuneIds.RuneNimbusCloak}, new[]{RuneIds.RuneTranscendence,RuneIds.RuneCelerity,RuneIds.RuneAbsoluteFocus}, new[]{RuneIds.RuneScorch,RuneIds.RuneWaterwalking,RuneIds.RuneGatheringStorm} } },
            { RuneIds.PathResolve, new[]{ new[]{RuneIds.RuneDemolish,RuneIds.RuneFontOfLife,RuneIds.RuneShieldBash}, new[]{RuneIds.RuneConditioning,RuneIds.RuneSecondWind,RuneIds.RuneBonePlating}, new[]{RuneIds.RuneOvergrowth,RuneIds.RuneRevitalize,RuneIds.RuneUnflinching} } },
            { RuneIds.PathInspiration, new[]{ new[]{RuneIds.RuneHextechFlashtraption,RuneIds.RuneMagicalFootwear, RuneIds.RuneCashBack }, new[]{ RuneIds.RuneTripleTonic, RuneIds.RuneTimeWarpTonic, RuneIds.RuneBiscuitDelivery}, new[]{RuneIds.RuneCosmicInsight,RuneIds.RuneApproachVelocity, RuneIds.RuneJackOfAllTrades } } },
        };

        private string GetUILabel(string key, string fallback)
        {
            string fullKey = $"Mods.LeagueOfLegendThings.UI.Runes.Labels.{key}";
            string value = Language.GetTextValue(fullKey);
            if (string.IsNullOrEmpty(value) || value == fullKey)
                return fallback;
            return value;
        }

        public override void OnInitialize()
        {
            // 开关按钮（右下角，可拖动）
            _mainButton = new DraggableUITextPanel(GetUILabel("MainButton", "Runes"), 0.8f)
            {
                Width = { Pixels = 70 },
                Height = { Pixels = 26 },
                Left = { Percent = 1f, Pixels = -100 },
                Top = { Percent = 1f, Pixels = -80 },
                BackgroundColor = new Color(60, 60, 120) * 0.8f
            };
            _mainButton.OnLeftClick += (_, __) => ToggleOpen();
            _mainButton.DragEnabled = !_mainButtonLocked;

            _mayhemActive = ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune;
            if (!_mayhemActive)
                Append(_mainButton);

            _panel = new DraggableUIPanel
            {
                Width = { Pixels = PanelW },
                Height = { Pixels = PanelH },
                Left = { Percent = 1f, Pixels = -(PanelW + 118) },
                Top = { Percent = 1f, Pixels = -(PanelH + 40) },
                BackgroundColor = new Color(14, 16, 21) * 0.97f,
                BorderColor = new Color(58, 62, 72) * 0.9f,
                PaddingLeft = (int)PadH,
                PaddingRight = (int)PadH,
                PaddingTop = (int)PadV,
                PaddingBottom = (int)PadV
            };

            _panelTitle = new UIText(GetUILabel("PanelTitle", "Rune Selection"), 0.9f)
            {
                HAlign = 0f,
                Top = { Pixels = 0 },
                TextColor = new Color(214, 216, 222)
            };
            _panel.Append(_panelTitle);

            // 装饰层（竖轴 / 节点 / 分割线 / 标题带），必须在图标层之前 Append
            _primaryDecor = new GridDecor
            {
                Width = { Pixels = SpineGutter + PrimaryContentW },
                Height = { Pixels = PrimaryGroupH },
                Left = { Pixels = 0 },
                Top = { Pixels = GroupTop },
                IgnoresMouseInteraction = true
            };
            _panel.Append(_primaryDecor);

            _secondaryDecor = new GridDecor
            {
                Width = { Pixels = SecondaryDecorW },
                Height = { Pixels = SecondaryGroupH },
                Left = { Pixels = SecondaryDecorLeft },
                Top = { Pixels = GroupTop },
                IgnoresMouseInteraction = true
            };
            _panel.Append(_secondaryDecor);

            _primaryGroup = new UIElement
            {
                Width = { Pixels = PrimaryContentW },
                Height = { Pixels = PrimaryGroupH },
                Left = { Pixels = SpineGutter },
                Top = { Pixels = GroupTop }
            };
            _panel.Append(_primaryGroup);

            _secondaryGroup = new UIElement
            {
                Width = { Pixels = SecondaryContentW },
                Height = { Pixels = SecondaryGroupH },
                Left = { Pixels = SecondaryGroupLeft },
                Top = { Pixels = GroupTop }
            };
            _panel.Append(_secondaryGroup);

            _detailPanel = new UIPanel
            {
                Width = { Pixels = DetailW },
                Height = { Pixels = DetailH },
                Left = { Pixels = DetailLeft },
                Top = { Pixels = DetailTop },
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            _detailPanel.IgnoresMouseInteraction = true;
            // 说明：UIText 在自身宽度内是**居中**绘制文字的（即使 HAlign = 0 也一样），
            // 上面试过 TextOrigin —— 这个 tModLoader 版本的 UIText 没有该成员。
            // 而这个居中恰好就是参考站的做法：DataRunesView 的系别卡片里，
            // 路径名与描述也是居中排在路径图标下方的，所以保持居中即可。
            // HAlign/VAlign 仍要显式置 0，否则元素自身会被父容器居中。
            // 描述字号 0.72（原来是 0.78）—— 卡片宽度只有原来一半，这是让最长的 50 字
            // 仍能压进 3 行的上限；标题相应用 0.86。
            //
            // 横向居中的关键（之前错在这里）：UIPanel 自带默认内边距（本版本 = 10），
            // 子元素已被整体推进去 10px；再叠一个 Left = 10，文字就比卡片中线偏右 10px，
            // 表现为最长的一行几乎贴住右边框、左边却空出一大条 —— 看着就是没居中。
            // 修法：文字元素铺满卡片的**内框**（Left = 0、宽度 = DetailInnerW）。
            // 内框左右对称 ⇒ 内框中心 == 卡片中心，每行居中的落点才是真正的正中。
            // 纵向（Top / DescTop）保持原样不动，观感与之前完全一致。
            _detailTitle = new UIText(string.Empty, 0.86f) { HAlign = 0f, VAlign = 0f, Left = { Pixels = 0 }, Top = { Pixels = 2 }, Width = { Pixels = DetailInnerW } };
            _detailDesc = new DetailText
            {
                HAlign = 0f,
                VAlign = 0f,
                Left = { Pixels = 0 },
                Top = { Pixels = DescTop },
                Width = { Pixels = DetailInnerW }
            };
            _detailPanel.Append(_detailTitle);
            _detailPanel.Append(_detailDesc);
            _panel.Append(_detailPanel);

            // 右下角的小锁图标（原来是个 88×24 的文字按钮摆在标题行右侧，占地方又与整体风格不搭）。
            // 必须**最后** Append：副系那一列的元素框（x 428..668、y 36..396）正好覆盖右下角，
            // 早于它就 Append 的话点击会被那一列抢走。
            _buttonLockToggle = new LockButton(LockSize,
                () => _mainButtonLocked,
                () => _mainButtonLocked
                        ? GetUILabel("ButtonLocked", "Locked")
                        : GetUILabel("ButtonUnlocked", "Unlocked"))
            {
                Left = { Pixels = PanelW - PadH * 2 - LockSize - 2f },
                Top = { Pixels = PanelH - PadV * 2 - LockSize - 2f }
            };
            _buttonLockToggle.OnLeftClick += (_, __) => ToggleMainButtonLock();
            _panel.Append(_buttonLockToggle);
            UpdateMainButtonLockVisual();
        }

        private bool _mayhemActive;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            UpdateDetailScroll(gameTime);

            // 首次 Update 时刷新所有文本（本地化此时已就绪）
            if (!_textsRefreshed && Main.screenWidth > 0)
            {
                _textsRefreshed = true;
                _mainButton?.SetText(GetUILabel("MainButton", "Runes"));
                // 面板标题在 OnInitialize 时本地化尚未就绪，这里补一次
                _panelTitle?.SetText(GetUILabel("PanelTitle", "Rune Selection"));
                UpdateMainButtonLockVisual();
            }

            bool mayhem = ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune;
            if (mayhem != _mayhemActive)
            {
                _mayhemActive = mayhem;
                if (mayhem)
                {
                    if (_open) ToggleOpen();
                    _mainButton?.Remove();
                }
                else
                {
                    if (_mainButton != null && _mainButton.Parent == null)
                        Append(_mainButton);
                }
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();
            Refresh();
        }

        private void ToggleOpen()
        {
            // Mayhem 模式启用时不打开符文面板
            if (ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;

            _open = !_open;
            if (_open)
            {
                if (_panel.Parent == null)
                    Append(_panel);
                _panel.StopDrag();
                Refresh();
            }
            else
            {
                _panel.StopDrag();
                _panel.Remove();
            }
        }

        private void ToggleMainButtonLock()
        {
            _mainButtonLocked = !_mainButtonLocked;
            if (_mainButton != null)
            {
                _mainButton.DragEnabled = !_mainButtonLocked;
                if (_mainButtonLocked)
                    _mainButton.StopDrag();
            }
            UpdateMainButtonLockVisual();
        }

        /// <summary>
        /// 锁图标自己通过委托读 _mainButtonLocked（连文字标签也是委托里现取的），
        /// 所以这里只需要把「能否拖动」这个副作用同步过去。
        /// </summary>
        private void UpdateMainButtonLockVisual()
        {
            if (_mainButton == null) return;
            _mainButton.DragEnabled = !_mainButtonLocked;
        }

        internal Vector2 GetPanelPosition() => _panel.GetDimensions().Position();

        internal bool IsSelectionModeActive() => true;

        private void Refresh()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            // 面板底色跟随主系（参考站每个系的 bgColor 各不相同）
            _panel.BackgroundColor = ThemeOf(save.PrimaryPath).Backdrop * 0.98f;
            BuildPrimaryGroup(save);
            BuildSecondaryGroup(save);
            // 底栏默认展示当前主系路径说明，保证常驻有内容
            HideDetail();
        }

        private static float RowLeft(int count, float size, float contentW)
            => (contentW - (count * size + (count - 1) * SlotSpacing)) / 2f;

        /// <summary>主系：路径行 + 「基石」标题带 + 基石行 + 3 行小符文。</summary>
        private void BuildPrimaryGroup(RuneSaveSystem save)
        {
            _primaryGroup.RemoveAllChildren();

            var theme = ThemeOf(save.PrimaryPath);
            var accent = theme.Accent;
            var nodes = new List<float>();
            var seps = new List<float>();

            // 1) 路径行
            float y = 0f;
            float x = RowLeft(Paths.Length, SlotSize, PrimaryContentW);
            foreach (var p in Paths)
            {
                string path = p;
                bool isCurrent = save.PrimaryPath.Equals(path);
                var btn = MakeIconButton(path, x, y, SlotSize, isPath: true, (evt, elem) =>
                {
                    if (save.PrimaryPath.Equals(path)) return;
                    SetPrimary(path);
                    HideDetail();
                });
                btn.SetVisual(isCurrent, true);
                btn.RingColor = ThemeOf(path).Accent;
                _primaryGroup.Append(btn);
                x += SlotSize + SlotSpacing;
            }
            nodes.Add(y + SlotSize / 2f);

            // 2) 基石行（带「基石」标题带）
            y = KeystoneRowY;
            var keystones = _keystones[save.PrimaryPath];
            x = RowLeft(keystones.Length, KeystoneSlotSize, PrimaryContentW);
            foreach (var r in keystones)
            {
                string rune = r;
                var btn = MakeIconButton(rune, x, y, KeystoneSlotSize, isPath: false, (evt, elem) =>
                {
                    save.PrimaryKeystone = save.PrimaryKeystone.Equals(rune) ? "" : rune;
                    HideDetail();
                    Refresh();
                });
                btn.SetVisual(save.PrimaryKeystone.Equals(rune), true);
                btn.RingColor = accent;
                _primaryGroup.Append(btn);
                x += KeystoneSlotSize + SlotSpacing;
            }
            nodes.Add(y + KeystoneSlotSize / 2f);
            seps.Add(y + KeystoneSlotSize + RowGap / 2f);   // 基石行下方的分隔线

            // 3) 三行小符文（每行互斥）
            y = PrimaryRow0Y;
            for (int row = 0; row < 3; row++)
            {
                var rowRunes = _rows[save.PrimaryPath][row];
                x = RowLeft(rowRunes.Length, SlotSize, PrimaryContentW);
                int capturedRow = row;
                foreach (var r in rowRunes)
                {
                    string rune = r;
                    var btn = MakeIconButton(rune, x, y, SlotSize, isPath: false, (evt, elem) =>
                    {
                        string cur = GetPrimaryRowValue(save, capturedRow);
                        SetPrimaryRowValue(save, capturedRow, cur.Equals(rune) ? "" : rune);
                        HideDetail();
                        Refresh();
                    });
                    btn.SetVisual(GetPrimaryRowValue(save, row).Equals(rune), true);
                    btn.RingColor = accent;
                    _primaryGroup.Append(btn);
                    x += SlotSize + SlotSpacing;
                }
                nodes.Add(y + SlotSize / 2f);
                if (row < 2) seps.Add(y + SlotSize + RowGap / 2f);
                y += SlotSize + RowGap;
            }

            _primaryDecor.Theme = theme;
            _primaryDecor.NodeYs = nodes;
            _primaryDecor.SeparatorYs = seps;
            _primaryDecor.ContentX0 = SpineGutter;
            _primaryDecor.ContentX1 = SpineGutter + PrimaryContentW;
            _primaryDecor.Label = GetUILabel("Keystone", "Keystone");
            _primaryDecor.LabelCenterY = LabelCenterY;
            _primaryDecor.OrnamentY = OrnamentY;
        }

        /// <summary>副系：路径行 + 3 行小符文（跨行任选 2 个）。</summary>
        private void BuildSecondaryGroup(RuneSaveSystem save)
        {
            _secondaryGroup.RemoveAllChildren();

            var theme = ThemeOf(save.SecondaryPath);
            var accent = theme.Accent;
            var nodes = new List<float>();
            var seps = new List<float>();

            // 1) 路径行（排除主系路径）。位置下移，与主系基石行齐平，避免中段大片留空
            float y = SecondaryPathRowY;
            var others = Paths.Where(p => !p.Equals(save.PrimaryPath)).ToArray();
            float x = RowLeft(others.Length, SlotSize, SecondaryContentW);
            foreach (var p in others)
            {
                string path = p;
                bool isCurrent = save.SecondaryPath.Equals(path);
                var btn = MakeIconButton(path, x, y, SlotSize, isPath: true, (evt, elem) =>
                {
                    if (save.SecondaryPath.Equals(path)) return;
                    SetSecondary(path);
                    HideDetail();
                });
                btn.SetVisual(isCurrent, true);
                btn.RingColor = ThemeOf(path).Accent;
                _secondaryGroup.Append(btn);
                x += SlotSize + SlotSpacing;
            }
            nodes.Add(y + SlotSize / 2f);
            seps.Add(y + SlotSize + RowGap / 2f);   // 与主系基石行下方那条落在同一水平线

            // 2) 三行小符文（与主系的三行对齐）
            y = PrimaryRow0Y;
            var subRows = _rows[save.SecondaryPath];
            for (int row = 0; row < subRows.Length; row++)
            {
                x = RowLeft(subRows[row].Length, SlotSize, SecondaryContentW);
                int capturedRow = row;
                foreach (var r in subRows[row])
                {
                    string rune = r;
                    var btn = MakeIconButton(rune, x, y, SlotSize, isPath: false, (evt, elem) =>
                    {
                        ToggleSecondaryPick(save, rune, capturedRow);
                        HideDetail();
                        Refresh();
                    });
                    btn.SetVisual(save.SecondaryPick1.Equals(rune) || save.SecondaryPick2.Equals(rune), true);
                    btn.RingColor = accent;
                    _secondaryGroup.Append(btn);
                    x += SlotSize + SlotSpacing;
                }
                nodes.Add(y + SlotSize / 2f);
                if (row < subRows.Length - 1) seps.Add(y + SlotSize + RowGap / 2f);
                y += SlotSize + RowGap;
            }

            _secondaryDecor.Theme = theme;
            _secondaryDecor.NodeYs = nodes;
            _secondaryDecor.SeparatorYs = seps;
            _secondaryDecor.ContentX0 = SpineGutter;
            _secondaryDecor.ContentX1 = SpineGutter + SecondaryContentW;
            _secondaryDecor.Label = null;
            _secondaryDecor.LabelCenterY = 0f;
            _secondaryDecor.OrnamentY = 0f;
        }

        /// <summary>
        /// 副系选择规则（沿用原有语义）：
        /// 点已选 -> 取消；否则占「同行的那个」-> 第一个空位 -> 替换第二个。
        /// </summary>
        private static void ToggleSecondaryPick(RuneSaveSystem save, string rune, int row)
        {
            if (save.SecondaryPick1.Equals(rune)) { save.SecondaryPick1 = ""; save.SecondaryPick1Row = -1; return; }
            if (save.SecondaryPick2.Equals(rune)) { save.SecondaryPick2 = ""; save.SecondaryPick2Row = -1; return; }
            if (save.SecondaryPick1Row == row) { save.SecondaryPick1 = rune; save.SecondaryPick1Row = row; return; }
            if (save.SecondaryPick2Row == row) { save.SecondaryPick2 = rune; save.SecondaryPick2Row = row; return; }
            if (string.IsNullOrEmpty(save.SecondaryPick1)) { save.SecondaryPick1 = rune; save.SecondaryPick1Row = row; return; }
            if (string.IsNullOrEmpty(save.SecondaryPick2)) { save.SecondaryPick2 = rune; save.SecondaryPick2Row = row; return; }
            save.SecondaryPick2 = rune; save.SecondaryPick2Row = row;
        }

        private void SetPrimary(string path)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.PrimaryPath == path) return;
            save.PrimaryPath = path;
            save.PrimaryKeystone = "";
            save.PrimaryRow1 = "";
            save.PrimaryRow2 = "";
            save.PrimaryRow3 = "";
            if (save.SecondaryPath == path)
            {
                foreach (var p in Paths)
                {
                    if (p != path) { save.SecondaryPath = p; break; }
                }
                save.SecondaryPick1 = "";
                save.SecondaryPick2 = "";
                save.SecondaryPick1Row = -1;
                save.SecondaryPick2Row = -1;
            }
            Refresh();
        }

        private void SetSecondary(string path)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (path == save.PrimaryPath) return;
            if (save.SecondaryPath == path) return;
            save.SecondaryPath = path;
            save.SecondaryPick1 = "";
            save.SecondaryPick2 = "";
            save.SecondaryPick1Row = -1;
            save.SecondaryPick2Row = -1;
            Refresh();
        }

        private IconButton MakeIconButton(string id, float x, float y, float size, bool isPath, UIElement.MouseEvent click)
        {
            var tex = string.IsNullOrEmpty(id) ? null : LoadRuneTexture(IdToDisplay(id));
            var btn = new IconButton(tex, size, id, isPath, this)
            {
                Left = { Pixels = x },
                Top = { Pixels = y }
            };
            btn.OnLeftClick += click;
            btn.Tooltip = string.Empty;
            return btn;
        }

        /// <summary>稳定 ID -> 英文显示名（仅用于查纹理与拼本地化键）。</summary>
        private static string IdToDisplay(string id) => RuneRegistry.DisplayName(id);

        private Asset<Texture2D> LoadRuneTexture(string name)
        {
            string safe = SanitizeName(name);
            var candidates = new List<string> { safe, safe + "_" };
            if (safe.StartsWith("Legend_", System.StringComparison.Ordinal) && !safe.Contains("-_"))
            {
                candidates.Add(safe.Replace("Legend_", "Legend-_"));
            }

            // wiki 下载的文件名: 空格变成下划线，保留标点符号
            var wikiNames = new List<string>();
            if (name.Contains(' '))
                wikiNames.Add(name.Replace(' ', '_'));      // Absorb Life → Absorb_Life
            wikiNames.Add(name);                             // 原始名（单字或回退）
            if (name.Contains(": "))
                wikiNames.Add(name.Replace(": ", " - "));   // Legend: Alacrity → Legend - Alacrity

            foreach (string folder in new[] { "Runes" })
            {
                foreach (var rawName in wikiNames)
                {
                    if (TryLoadTexture($"LeagueOfLegendThings/Content/Icon/{folder}/{rawName}", out var tex))
                        return tex;
                }
                foreach (var c in candidates)
                {
                    if (TryLoadTexture($"LeagueOfLegendThings/Content/Icon/{folder}/{c}", out var tex))
                        return tex;
                }
            }

            // 回退到旧 Icon 文件夹
            foreach (var c in candidates)
            {
                if (TryLoadTexture($"LeagueOfLegendThings/Content/Icon/{c}", out var tex))
                    return tex;
            }
            return TextureAssets.MagicPixel;
        }

        private static bool TryLoadTexture(string path, out Asset<Texture2D> result)
        {
            if (ModContent.HasAsset(path))
            {
                result = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
                return true;
            }
            result = null;
            return false;
        }

        private string SanitizeName(string name)
        {
            var chars = name.Select(ch =>
                (ch == '\'' || ch == '’') ? (char)0 :  // 撇号直接移除
                char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var s = new string(chars).Replace("\0", "");
            while (s.Contains("__")) s = s.Replace("__", "_");
            return s.Trim('_');
        }

        private static string LocalizedOr(string key, string fallback)
        {
            string v = Language.GetTextValue(key);
            return (string.IsNullOrEmpty(v) || v == key) ? fallback : v;
        }

        private string GetRuneTitle(string id)
        {
            var resolved = RuneRegistry.ToId(id);
            if (resolved != null)
                return LocalizedOr(RuneRegistry.NameKey(resolved), RuneRegistry.DisplayName(resolved));
            return id;
        }

        private string GetRuneDescription(string id)
        {
            var resolved = RuneRegistry.ToId(id);
            if (resolved != null)
                return LocalizedOr(RuneRegistry.DescKey(resolved), "");
            return "";
        }

        /// <summary>路径名 / 路径描述（与符文本体分开取键）。</summary>
        private string GetPathTitle(string id)
        {
            var resolved = RuneRegistry.ToId(id);
            if (resolved != null)
                return LocalizedOr(RuneRegistry.PathNameKey(resolved), RuneRegistry.DisplayName(resolved));
            return id;
        }

        private string GetPathDescription(string id)
        {
            var resolved = RuneRegistry.ToId(id);
            if (resolved != null)
                return LocalizedOr(RuneRegistry.PathDescKey(resolved), "");
            return "";
        }

        /// <summary>悬停某个符文 / 路径 -> 底部描述栏显示它。</summary>
        internal void ShowDetailFor(UIElement btn, string id, bool isPath)
            => SetDetail(isPath ? GetPathTitle(id) : GetRuneTitle(id),
                         isPath ? GetPathDescription(id) : GetRuneDescription(id));

        /// <summary>鼠标离开 -> 底栏回落到当前主系路径的说明（不再留空白带）。</summary>
        internal void HideDetail()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            SetDetail(GetPathTitle(save.PrimaryPath), GetPathDescription(save.PrimaryPath));
        }

        private void SetDetail(string title, string desc)
        {
            _detailTitle.SetText(title ?? string.Empty);

            // 折行宽度与文字元素使用同一个基准（内框宽 − 2），换行点才不会跟着内边距漂
            string wrapped = WrapText(desc ?? string.Empty, DetailInnerW - 2f, DescScale);
            _detailDesc.SetLines(wrapped, DescScale, DetailTextColor);

            // 高度直接由「行数 × 行距」得出（比 MeasureString 量多行块更准），超出可用高度才启用滚动。
            float avail = DetailH - DescTop - DescBottomPad;
            _detailScrollMax = MathF.Max(0f, _detailDesc.TotalHeight - avail);
            _detailScrollClock = 0;
            _detailDesc.Top.Set(DescTop, 0f);

            // 对比度要够：卡片底色比面板本体亮一档，边框用中性灰蓝
            _detailPanel.BackgroundColor = new Color(27, 30, 38) * 0.98f;
            _detailPanel.BorderColor = new Color(96, 102, 120) * 0.9f;
            _detailPanel.Recalculate();
        }

        /// <summary>
        /// 描述超出卡片时的自动滚动：停 1.3s → 匀速上滚 → 停 1.3s → 回顶，循环。
        /// 只改 UIText 的 Top 并让它重算，不动文本内容。
        /// </summary>
        private void UpdateDetailScroll(GameTime gameTime)
        {
            if (_detailScrollMax <= 0.5f) return;

            _detailScrollClock += gameTime.ElapsedGameTime.TotalSeconds;
            const double pauseSec = 1.3;
            const float scrollSpeed = 16f;                       // px/s
            double moveSec = _detailScrollMax / scrollSpeed;
            double cycle = pauseSec * 2 + moveSec;
            double p = _detailScrollClock % cycle;

            float offset;
            if (p < pauseSec) offset = 0f;
            else if (p < pauseSec + moveSec) offset = (float)((p - pauseSec) / moveSec) * _detailScrollMax;
            else offset = _detailScrollMax;

            float want = DescTop - offset;
            if (MathF.Abs(_detailDesc.Top.Pixels - want) > 0.05f)
            {
                _detailDesc.Top.Set(want, 0f);
                _detailDesc.Recalculate();
            }
        }

        private static string WrapText(string text, float maxPixelWidth, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var font = Terraria.GameContent.FontAssets.MouseText.Value;
            if (font.MeasureString(text).X * scale <= maxPixelWidth)
                return text;

            var parts = new List<string>();
            int idx = 0;
            while (idx < text.Length)
            {
                int remaining = text.Length - idx;
                if (remaining == 0)
                    break;

                // 二分查找最大可容纳的字符数
                int left = 1;
                int right = remaining;
                int bestPos = 1;

                while (left <= right)
                {
                    int mid = (left + right) / 2;
                    string testStr = text.Substring(idx, mid);
                    float width = font.MeasureString(testStr).X * scale;

                    if (width <= maxPixelWidth)
                    {
                        bestPos = mid;
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }

                // 尝试在标点或空格处优化断点
                int breakPos = bestPos;

                char[] breakChars = { '。', '，', '、', '；', '：', '！', '？', '.', ',', ';', ':', ' ', '）', ')', '}', ']' };
                for (int i = System.Math.Min(bestPos, remaining - 1); i > bestPos / 2 && i > 0; i--)
                {
                    char ch = text[idx + i];
                    if (System.Array.IndexOf(breakChars, ch) >= 0)
                    {
                        string testStr = text.Substring(idx, i + 1);
                        float width = font.MeasureString(testStr).X * scale;
                        if (width <= maxPixelWidth)
                        {
                            breakPos = i + 1;
                            break;
                        }
                    }
                }

                if (breakPos == 0)
                    breakPos = 1;

                parts.Add(text.Substring(idx, breakPos).TrimEnd());
                idx += breakPos;
            }
            return string.Join("\n", parts);
        }

        // ================= 圆形贴图（懒生成，避免每帧几十条线段） =================
        private static Texture2D _discTex;
        private static Texture2D _ringThinTex;
        private static Texture2D _ringThickTex;
        private const int RoundTexSize = 64;

        private static void EnsureRoundTextures()
        {
            if (_discTex != null && !_discTex.IsDisposed) return;
            var dev = Main.instance?.GraphicsDevice;
            if (dev == null) return;
            try
            {
                _discTex = BuildRoundTex(dev, RoundTexSize, 0f);
                _ringThinTex = BuildRoundTex(dev, RoundTexSize, 4f);
                _ringThickTex = BuildRoundTex(dev, RoundTexSize, 7f);
            }
            catch
            {
                _discTex = null;
            }
        }

        /// <summary>innerShrink == 0 生成实心圆盘；&gt; 0 生成圆环。</summary>
        private static Texture2D BuildRoundTex(GraphicsDevice dev, int size, float innerShrink)
        {
            var tex = new Texture2D(dev, size, size);
            var px = new Color[size * size];
            float c = size / 2f;
            float rOuter = c - 1f;
            float rInner = MathF.Max(0f, rOuter - innerShrink);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    float aOuter = Math.Clamp(rOuter - d + 0.5f, 0f, 1f);
                    float a = innerShrink <= 0f ? aOuter
                            : aOuter * Math.Clamp(d - rInner + 0.5f, 0f, 1f);
                    px[y * size + x] = Color.White * a;
                }
            }
            tex.SetData(px);
            return tex;
        }

        private static void DrawTex(SpriteBatch sb, Texture2D tex, Vector2 center, float radius, Color color)
        {
            if (tex == null) return;
            float d = radius * 2f;
            sb.Draw(tex, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)d, (int)d), color);
        }

        /// <summary>
        /// 单个符文图标。视觉状态：
        ///   未选中 -> 深色圆盘 + 压暗成灰调的图标 + 深色细圆环
        ///   选中   -> 路径色外发光 + 亮图标 + 加粗的路径色圆环
        /// </summary>
        private class IconButton : UIElement
        {
            private readonly Texture2D _texture;
            private readonly float _size;
            private readonly string _id;
            private readonly bool _isPath;
            private readonly RuneUIState _parentState;
            private bool _selected;
            private bool _enabled = true;
            private bool _wasHovering;
            public string Tooltip { get; set; } = string.Empty;
            public Color RingColor { get; set; } = Color.Transparent;

            private static readonly Color PlateIdle = new(23, 25, 31);
            private static readonly Color PlateActive = new(30, 32, 40);
            private static readonly Color RingIdle = new(48, 52, 62);
            private static readonly Color IconIdle = new(118, 122, 133);
            private static readonly Color IconHover = new(186, 189, 198);

            public IconButton(Asset<Texture2D> texture, float size, string id, bool isPath, RuneUIState parentState)
            {
                _texture = texture?.Value ?? TextureAssets.MagicPixel.Value;
                _size = size;
                _id = id;
                _isPath = isPath;
                _parentState = parentState;
                Width.Set(size, 0f);
                Height.Set(size, 0f);
            }

            public void SetVisual(bool selected, bool enabled)
            {
                _selected = selected;
                _enabled = enabled;
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                bool isHoveringNow = IsMouseHovering;
                if (isHoveringNow && !_wasHovering && !string.IsNullOrEmpty(_id) && _parentState != null && _parentState.IsSelectionModeActive())
                {
                    _parentState.ShowDetailFor(this, _id, _isPath);
                }
                else if (!isHoveringNow && _wasHovering && _parentState != null)
                {
                    _parentState.HideDetail();
                }
                _wasHovering = isHoveringNow;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                var dims = GetDimensions();
                var center = dims.Position() + new Vector2(dims.Width * 0.5f, dims.Height * 0.5f);
                float radius = dims.Width * 0.5f;
                bool hover = IsMouseHovering;
                Color accent = RingColor == Color.Transparent ? new Color(150, 150, 150) : RingColor;

                EnsureRoundTextures();

                // 选中：路径色外发光
                if (_selected)
                    DrawTex(spriteBatch, _discTex, center, radius + 6f, accent * 0.20f);

                // 悬停：一层极淡的白
                if (hover && !_selected)
                    DrawTex(spriteBatch, _discTex, center, radius + 2f, Color.White * 0.09f);

                // 圆盘底座
                DrawTex(spriteBatch, _discTex, center, radius, _selected ? PlateActive : PlateIdle);

                // 图标本体（非预乘批，防溢色）
                if (_texture != null)
                {
                    float target = dims.Width * (_selected ? 0.82f : 0.74f);
                    float scale = target / MathF.Max(1, MathF.Max(_texture.Width, _texture.Height));
                    float drawW = _texture.Width * scale;
                    float drawH = _texture.Height * scale;
                    var iconRect = new Rectangle(
                        (int)(center.X - drawW / 2f), (int)(center.Y - drawH / 2f),
                        Math.Max(1, (int)drawW), Math.Max(1, (int)drawH));

                    Color iconTint = !_enabled ? IconIdle
                                   : _selected ? Color.White
                                   : hover ? IconHover
                                   : IconIdle;

                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                        SamplerState.PointClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.UIScaleMatrix);
                    spriteBatch.Draw(_texture, iconRect, null, iconTint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.PointClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.UIScaleMatrix);
                }

                // 圆环
                DrawTex(spriteBatch, _selected ? _ringThickTex : _ringThinTex, center, radius,
                    _selected ? accent : RingIdle);

                // 选中：内层一道亮描边，让环更立体
                if (_selected)
                    DrawTex(spriteBatch, _ringThinTex, center, radius - 2.5f, Color.White * 0.30f);
            }
        }

        /// <summary>
        /// 描述文字。**不能用 UIText** —— 它把整个多行文本块当作一体居中，
        /// 于是当每行的宽度都接近元素宽度时（换行后的正常情况），看上去就是左对齐。
        /// 这里自己按行居中绘制：每行独立测宽、独立算左起点。
        /// 顺带把总高度暴露出来（行数 × 行距），比用 MeasureString 量多行块更准，
        /// 自动滚动的阈值就靠它。
        /// <para/>
        /// 注意：这里的「居中」是相对元素自身宽度而言的，所以元素必须铺满卡片内框
        /// （Left = 0、宽度 = DetailInnerW，见 OnInitialize），否则会整体偏右一个内边距。
        /// </summary>
        private class DetailText : UIElement
        {
            private string[] _lines = Array.Empty<string>();
            private float _scale = 0.7f;
            private Color _color = Color.White;

            public float LineHeight { get; private set; }
            public float TotalHeight => _lines.Length * LineHeight;

            public void SetLines(string wrapped, float scale, Color color)
            {
                _scale = scale;
                _color = color;
                _lines = string.IsNullOrEmpty(wrapped)
                    ? Array.Empty<string>()
                    : wrapped.Split('\n');
                LineHeight = FontAssets.MouseText.Value.LineSpacing * scale;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                if (_lines.Length == 0) return;

                var d = GetDimensions();
                var font = FontAssets.MouseText.Value;
                float y = d.Y;
                foreach (string line in _lines)
                {
                    float w = font.MeasureString(line).X * _scale;
                    float x = d.X + (d.Width - w) / 2f;      // 每行各自居中
                    Terraria.Utils.DrawBorderString(spriteBatch, line, new Vector2(x, y), _color, _scale);
                    y += LineHeight;
                }
            }
        }

        /// <summary>
        /// 面板右下角的小锁图标：控制主按钮（符文）能否拖动。
        /// 原来是一个 88×24、写着「锁定 / 解锁」的文字按钮摆在标题行右侧 —— 占地方又与整体风格不搭，
        /// 改成程序化绘制的小挂锁（不引入外部贴图）：锁上=闭口挂锁，解锁=开口挂锁。
        /// 悬停时在左侧浮出文字说明，兼顾可发现性。
        /// </summary>
        private class LockButton : UIElement
        {
            private readonly Texture2D _dot = TextureAssets.MagicPixel.Value;
            private readonly Func<bool> _isLocked;
            private readonly Func<string> _label;
            private bool _hover;

            // 照原版热键栏锁的配色 —— Terraria 的 DrawHotbarLockIcon 是代码画的，
            // Content/Images 下没有任何 lock 贴图可引用，所以这里复刻它的观感：
            // 深色描边 + 金色锁体 + 顶部高光 + 钥匙孔。
            private static readonly Color Plate = new(27, 30, 38);
            private static readonly Color Outline = new(34, 26, 10);
            private static readonly Color GoldBody = new(226, 186, 84);
            private static readonly Color GoldLight = new(248, 228, 152);
            private static readonly Color GoldDark = new(122, 88, 26);
            private static readonly Color GreyBody = new(148, 154, 166);
            private static readonly Color GreyLight = new(198, 204, 216);
            private static readonly Color GreyDark = new(74, 78, 88);

            public LockButton(float size, Func<bool> isLocked, Func<string> label)
            {
                _isLocked = isLocked;
                _label = label;
                Width.Set(size, 0f);
                Height.Set(size, 0f);
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                _hover = IsMouseHovering;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                var d = GetDimensions();
                var center = d.Position() + new Vector2(d.Width * 0.5f, d.Height * 0.5f);
                bool locked = _isLocked();

                Color body = locked ? GoldBody : GreyBody;
                Color light = locked ? GoldLight : GreyLight;
                Color dark = locked ? GoldDark : GreyDark;
                if (_hover)
                {
                    body = Color.Lerp(body, Color.White, 0.25f);
                    light = Color.Lerp(light, Color.White, 0.25f);
                }

                // 底板：锁上时带一圈金边，呼应原版那个黄色框
                spriteBatch.Draw(_dot, new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height),
                    Plate * (_hover ? 1f : 0.9f));
                if (locked || _hover)
                    DrawBorder(spriteBatch, new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height),
                        1, (locked ? GoldDark : dark) * (_hover ? 1f : 0.85f));

                // ---- 锁体：外描边 → 内芯 → 顶部高光 / 底部压暗 ----
                float bodyW = d.Width * 0.60f;
                float bodyH = d.Height * 0.46f;
                float bodyX = center.X - bodyW / 2f;
                float bodyY = center.Y - bodyH / 2f + d.Height * 0.14f;
                spriteBatch.Draw(_dot,
                    new Rectangle((int)bodyX - 1, (int)bodyY - 1, (int)bodyW + 2, (int)bodyH + 2), Outline);
                spriteBatch.Draw(_dot, new Rectangle((int)bodyX, (int)bodyY, (int)bodyW, (int)bodyH), body);
                spriteBatch.Draw(_dot,
                    new Rectangle((int)bodyX + 1, (int)bodyY + 1, Math.Max(1, (int)bodyW - 2), 1), light);
                spriteBatch.Draw(_dot,
                    new Rectangle((int)bodyX + 1, (int)(bodyY + bodyH) - 2, Math.Max(1, (int)bodyW - 2), 1), dark);

                // ---- 钥匙孔：上圆 + 下竖槽 ----
                float khTop = bodyY + bodyH * 0.24f;
                spriteBatch.Draw(_dot, new Rectangle((int)center.X - 2, (int)khTop, 4, 4), Outline);
                spriteBatch.Draw(_dot,
                    new Rectangle((int)center.X - 1, (int)(khTop + 3f), 2, Math.Max(2, (int)(bodyH * 0.42f))), Outline);

                // ---- 锁梁：先描边再压亮色；解锁时整体右移并砍掉右上一段 ----
                float r = d.Width * 0.20f;
                var arcCenter = new Vector2(center.X + (locked ? 0f : r * 0.62f), bodyY - 1f);
                const int segs = 16;
                int drawSegs = locked ? segs : (int)(segs * 0.70f);
                for (int pass = 0; pass < 2; pass++)
                {
                    int thickness = pass == 0 ? 4 : 2;
                    Color col = pass == 0 ? Outline : light;
                    for (int i = 0; i < drawSegs; i++)
                    {
                        float a0 = MathHelper.Pi + MathHelper.Pi * i / segs;
                        float a1 = MathHelper.Pi + MathHelper.Pi * (i + 1) / segs;
                        DrawLine(spriteBatch,
                            arcCenter + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * r,
                            arcCenter + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * r, thickness, col);
                    }
                }

                // 悬停时在左侧浮出文字，避免只有图标看不懂
                if (_hover && _label != null)
                {
                    string txt = _label();
                    var font = FontAssets.MouseText.Value;
                    var sz = font.MeasureString(txt) * 0.72f;
                    float tx = d.X - sz.X - 10f;
                    if (tx < 4f) tx = d.X + d.Width + 6f;
                    Terraria.Utils.DrawBorderString(spriteBatch, txt,
                        new Vector2(tx, center.Y - sz.Y / 2f), new Color(222, 224, 232), 0.72f);
                }
            }

            private void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, int thickness, Color color)
            {
                var e = b - a;
                float len = e.Length();
                if (len < 0.5f) return;
                sb.Draw(_dot, new Rectangle((int)a.X, (int)a.Y, (int)len, thickness), null, color,
                    MathF.Atan2(e.Y, e.X), Vector2.Zero, SpriteEffects.None, 0f);
            }

            private static void DrawBorder(SpriteBatch sb, Rectangle rect, int thickness, Color color)
            {
                var px = TextureAssets.MagicPixel.Value;
                sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
                sb.Draw(px, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
                sb.Draw(px, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
            }
        }

        /// <summary>
        /// 列装饰。尺寸与样式全部照搬 101.qq.com 符文模拟器的 Tailwind 类：
        ///   竖轴   absolute left-[50px] w-[12px]，内含 1px@opacity-10 / 6px@opacity-30 / 1px@opacity-10 三条
        ///   节点   w-[20px] rounded-full border(themeColor) + 内嵌 w-[12px] rounded-full(themeColor)
        ///   分隔线 w-[280px] mx-auto h-[1px]，
        ///          background: linear-gradient(90deg, rgba(50,52,61,0) 0%, #32343d 50%, rgba(50,52,61,0) 100%)
        ///          —— 居中、两端渐隐到透明、左端没有任何端点装饰
        ///   基石   span(text-xs tracking-widest, themeColor) 在上，img(451x6, mx-auto) 在下方
        /// 注意：参考站的竖轴是「三条并排竖条」叠出的渐隐光带，不是一条实线。
        /// </summary>
        private class GridDecor : UIElement
        {
            private readonly Texture2D _dot = TextureAssets.MagicPixel.Value;

            public List<float> NodeYs = new();
            public List<float> SeparatorYs = new();
            public float ContentX0 = 30f;
            public float ContentX1 = 300f;
            public float SpineX = 14f;
            public PathTheme Theme = FallbackTheme;
            public string Label;
            public float LabelCenterY;
            public float OrnamentY;

            private const int FadeSegments = 56;
            private static readonly Color SepColor = new(50, 52, 61);   // 参考站 #32343d

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                // 装饰层先于图标层 Append，首帧需自己保证圆形贴图已就绪
                EnsureRoundTextures();

                var o = GetDimensions().Position();
                float contentCenterX = o.X + (ContentX0 + ContentX1) / 2f;
                int spineCx = (int)(o.X + SpineX);

                // ---------- 竖轴：1px@10% / 4px@30% / 1px@10% ----------
                if (NodeYs.Count > 0)
                {
                    // 竖轴严格从第一个节点的中心到最后一个节点的中心，两端由节点自己盖住。
                    // 参考站 CSS 写的是 -top-16 / bottom-[32px]（比节点范围更长），照搬的话
                    // 线会从首尾节点上方/下方各戳出来一截 —— 实测观感很差，这里按视觉收口。
                    float y0 = o.Y + NodeYs[0];
                    float y1 = o.Y + NodeYs[NodeYs.Count - 1];
                    int h = Math.Max(1, (int)(y1 - y0));
                    int centerW = Math.Max(1, (int)MathF.Round(RailCenterW));
                    int leftEdge = spineCx - centerW / 2;

                    spriteBatch.Draw(_dot, new Rectangle(leftEdge - 1, (int)y0, 1, h), Theme.RailSide * RailSideOpacity);
                    spriteBatch.Draw(_dot, new Rectangle(leftEdge, (int)y0, centerW, h), Theme.RailCenter * RailCenterOpacity);
                    spriteBatch.Draw(_dot, new Rectangle(leftEdge + centerW, (int)y0, 1, h), Theme.RailSide * RailSideOpacity);

                    // ---------- 行节点：20px 描边圆 + 内嵌 12px 实心圆 ----------
                    foreach (float ny in NodeYs)
                    {
                        var c = new Vector2(spineCx + 0.5f, o.Y + ny);
                        DrawTex(spriteBatch, _ringThinTex, c, NodeOuterR, Theme.Accent);
                        DrawTex(spriteBatch, _discTex, c, NodeInnerR, Theme.Accent);
                    }
                }

                // ---------- 行分隔线：居中、两端渐隐 ----------
                foreach (float sy in SeparatorYs)
                    DrawFadeLine(spriteBatch, contentCenterX, o.Y + sy, SeparatorW, SepColor);

                // ---------- 标题带：小字居中（带字距）+ 下方一条装饰线 ----------
                if (!string.IsNullOrEmpty(Label))
                {
                    DrawTrackedText(spriteBatch, Label, new Vector2(contentCenterX, o.Y + LabelCenterY),
                        Theme.Accent, 0.62f, 3f);
                    DrawOrnament(spriteBatch, o.X + ContentX0, ContentX1 - ContentX0,
                        o.Y + OrnamentY, Theme.RailCenter);
                }
            }

            /// <summary>
            /// 「基石」下方那条装饰线。参考站用的是 rune-{系id}-top.png（451×6），
            /// 逐像素量出来的结构是：
            ///   · 一条横贯整列的 2px 底线，alpha 呈帐篷形 —— 两端 0、正中 0.5
            ///   · 左 1/3 上方另有一条更亮的 1px 线，alpha ≈ t×2.93 线性渐亮，
            ///     在 t≈0.30 处沿 y 陡降并入底线，于 t≈0.328 结束
            /// 颜色就是该系的 railColor：精密 #866C47 与采样 RGB(134,108,71) 逐位吻合
            /// （8200/8400/8300 同样吻合，即 railCenter 槽位）。
            /// 这里按同一结构程序化绘制，不依赖任何外部贴图。
            /// </summary>
            private void DrawOrnament(SpriteBatch sb, float x0, float width, float y, Color color)
            {
                const int segs = 96;
                const float joinStart = 0.310f;  // 亮线开始下坠（对参考图 x≈140）
                const float joinHold = 0.320f;   // 下坠到位（对参考图 x≈145）
                const float joinEnd = 0.332f;    // 亮线结束（对参考图 x≈148）
                float stepDrop = 2f * RefScale;  // 下坠量：参考图是 2px
                float barY = y + 3f;
                float segW = width / segs;

                for (int i = 0; i < segs; i++)
                {
                    float t = (i + 0.5f) / segs;
                    float tent = 1f - MathF.Abs(t - 0.5f) * 2f;   // 0 → 1 → 0
                    int px0 = (int)(x0 + i * segW);
                    int px1 = (int)(x0 + (i + 1) * segW);
                    int pw = Math.Max(1, px1 - px0);

                    // 底线（2px）。阈值要够小 —— 参考图两端 alpha 衰减到 0 之前一直都还在画
                    float barA = tent * 0.5f;
                    if (barA > 0.004f)
                        sb.Draw(_dot, new Rectangle(px0, (int)barY, pw, 2), color * barA);

                    // 左 1/3 上方那条亮线（1px）：渐亮 → 下坠 2px → 保持一小段 → 结束
                    if (t <= joinEnd)
                    {
                        float a = MathF.Min(1f, t * 2.93f);
                        float dy = t <= joinStart ? 0f
                                 : t <= joinHold ? (t - joinStart) / (joinHold - joinStart) * stepDrop
                                 : stepDrop;
                        float ly = y + dy;

                        // 下坠段必须按小数位把 alpha 摊到相邻两行，否则会出现整数台阶
                        // （参考图在 x≈146 的 alpha 是 45/226/30 摊在三行上的）
                        int ly0 = (int)MathF.Floor(ly);
                        float frac = ly - ly0;
                        if (frac > 0.02f && frac < 0.98f)
                        {
                            sb.Draw(_dot, new Rectangle(px0, ly0, pw, 1), color * (a * (1f - frac)));
                            sb.Draw(_dot, new Rectangle(px0, ly0 + 1, pw, 1), color * (a * frac));
                        }
                        else
                        {
                            sb.Draw(_dot, new Rectangle(px0, (int)MathF.Round(ly), pw, 1), color * a);
                        }
                    }
                }
            }

            /// <summary>等价于 linear-gradient(90deg, transparent 0%, color 50%, transparent 100%)。</summary>
            private void DrawFadeLine(SpriteBatch sb, float centerX, float y, float width, Color color)
            {
                float x0 = centerX - width / 2f;
                float segW = width / FadeSegments;
                for (int i = 0; i < FadeSegments; i++)
                {
                    float t = (i + 0.5f) / FadeSegments;
                    float a = 1f - MathF.Abs(t - 0.5f) * 2f;   // 两端 0、正中 1
                    if (a <= 0.01f) continue;
                    // 用相邻边界取宽，保证分段严丝合缝（用 Ceiling 会互相重叠、alpha 叠加）
                    int px0 = (int)(x0 + i * segW);
                    int px1 = (int)(x0 + (i + 1) * segW);
                    sb.Draw(_dot, new Rectangle(px0, (int)y, Math.Max(1, px1 - px0), 1), color * a);
                }
            }

            /// <summary>带字距的居中文字（对应参考站的 tracking-widest）。</summary>
            private static void DrawTrackedText(SpriteBatch sb, string text, Vector2 center, Color color, float scale, float tracking)
            {
                var font = FontAssets.MouseText.Value;
                float total = -tracking;
                for (int i = 0; i < text.Length; i++)
                    total += font.MeasureString(text[i].ToString()).X * scale + tracking;

                float x = center.X - total / 2f;
                for (int i = 0; i < text.Length; i++)
                {
                    string ch = text[i].ToString();
                    var sz = font.MeasureString(ch) * scale;
                    Terraria.Utils.DrawBorderString(sb, ch, new Vector2(x, center.Y - sz.Y / 2f), color, scale);
                    x += sz.X + tracking;
                }
            }
        }

        private class DraggableUIPanel : UIPanel
        {
            private bool _dragging;
            private Vector2 _dragOffset;

            public void StopDrag() => _dragging = false;

            public override void LeftMouseDown(UIMouseEvent evt)
            {
                base.LeftMouseDown(evt);
                if (evt.Target != this)
                    return;
                _dragging = true;
                Left.Percent = 0f;
                Top.Percent = 0f;
                _dragOffset = evt.MousePosition - GetDimensions().Position();
            }

            public override void LeftMouseUp(UIMouseEvent evt)
            {
                base.LeftMouseUp(evt);
                _dragging = false;
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (_dragging && !Main.mouseLeft)
                {
                    _dragging = false;
                }
                if (_dragging)
                {
                    Vector2 mouse = Main.MouseScreen;
                    Left.Set(mouse.X - _dragOffset.X, 0f);
                    Top.Set(mouse.Y - _dragOffset.Y, 0f);
                    Recalculate();
                }
            }
        }

        private class DraggableUITextPanel : UITextPanel<string>
        {
            private bool _dragging;
            private Vector2 _dragOffset;

            public bool DragEnabled { get; set; }

            public DraggableUITextPanel(string text, float textScale = 1f) : base(text, textScale)
            {
            }

            public void StopDrag() => _dragging = false;

            public override void LeftMouseDown(UIMouseEvent evt)
            {
                base.LeftMouseDown(evt);
                if (!DragEnabled)
                    return;

                _dragging = true;
                Left.Percent = 0f;
                Top.Percent = 0f;
                _dragOffset = evt.MousePosition - GetDimensions().Position();
            }

            public override void LeftMouseUp(UIMouseEvent evt)
            {
                base.LeftMouseUp(evt);
                _dragging = false;
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (!DragEnabled)
                {
                    _dragging = false;
                    return;
                }

                if (_dragging && !Main.mouseLeft)
                {
                    _dragging = false;
                }
                if (_dragging)
                {
                    Vector2 mouse = Main.MouseScreen;
                    Left.Set(mouse.X - _dragOffset.X, 0f);
                    Top.Set(mouse.Y - _dragOffset.Y, 0f);
                    Recalculate();
                }
            }
        }

        private string GetPrimaryRowValue(RuneSaveSystem save, int row) => row switch
        {
            0 => save.PrimaryRow1,
            1 => save.PrimaryRow2,
            _ => save.PrimaryRow3
        };

        private void SetPrimaryRowValue(RuneSaveSystem save, int row, string value)
        {
            switch (row)
            {
                case 0: save.PrimaryRow1 = value; break;
                case 1: save.PrimaryRow2 = value; break;
                case 2: save.PrimaryRow3 = value; break;
            }
        }
    }
}
