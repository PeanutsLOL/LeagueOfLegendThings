using System;
using Terraria.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.Linq;
using ReLogic.Content;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;

namespace LeagueOfLegendThings.Content.UI
{
    public class RuneUIState : UIState
    {
        private DraggableUIPanel _panel;
        private UIElement _primaryGroup;
        private UIElement _secondaryGroup;
        private UIElement _primaryOptions;
        private UIElement _secondaryOptions;
        private ConnectorElement _primaryConnector;
        private ConnectorElement _secondaryConnector;
        private bool _open;

        private UIPanel _detailPanel;
        private UIText _detailTitle;
        private UIText _detailDesc;
        private bool _detailVisible;

        private string _activePrimarySlot; // "Keystone" or "Row0/1/2"
        private int _activeSecondarySlot = -1; // 0 or 1

        private float _activePrimarySlotY;
        private float _activeSecondarySlotY;
        private float _activePrimaryPathY;
        private float _activeSecondaryPathY;
        private bool _primaryPathOpen;
        private bool _secondaryPathOpen;

        private readonly string[] Paths = { "Precision", "Domination", "Sorcery", "Resolve", "Inspiration" };

        private static readonly Dictionary<string, Color> PathColors = new()
        {
            { "Precision", new Color(196, 145, 63) },
            { "Domination", new Color(196, 48, 43) },
            { "Sorcery", new Color(110, 80, 196) },
            { "Resolve", new Color(80, 180, 80) },
            { "Inspiration", new Color(80, 180, 220) }
        };

        // 存储主系和副系槽位的位置，用于绘制连接线
        private List<Vector2> _primarySlotCenters = new();
        private List<Vector2> _secondarySlotCenters = new();

        private readonly Dictionary<string, string[]> _keystones = new()
        {
            { "Precision", new[]{"Press the Attack","Lethal Tempo","Fleet Footwork","Conqueror"} },
            { "Domination", new[]{"Electrocute", "Hail of Blades", "Dark Harvest"} },
            { "Sorcery", new[]{"Summon Aery","Arcane Comet","Phase Rush"} },
            { "Resolve", new[]{"Grasp of the Undying","Aftershock","Guardian"} },
            { "Inspiration", new[]{"Glacial Augment","Unsealed Spellbook","First Strike"} },
        };

        private readonly Dictionary<string, string[][]> _rows = new()
        {
            { "Precision", new[]{ new[]{ "Absorb Life", "Triumph","Presence of Mind"}, new[]{"Legend: Alacrity","Legend: Haste","Legend: Bloodline"}, new[]{"Coup de Grace","Cut Down","Last Stand"} } },
            { "Domination", new[]{ new[]{"Cheap Shot","Taste of Blood","Sudden Impact"}, new[]{"Eyeball Collection","Ravenous Hunter","Ingenious Hunter"}, new[]{"Treasure Hunter","Relentless Hunter"} } },
            { "Sorcery", new[]{ new[]{ "Axiom Arcanist", "Manaflow Band","Nimbus Cloak"}, new[]{"Transcendence","Celerity","Absolute Focus"}, new[]{"Scorch","Waterwalking","Gathering Storm"} } },
            { "Resolve", new[]{ new[]{"Demolish","Font of Life","Shield Bash"}, new[]{"Conditioning","Second Wind","Bone Plating"}, new[]{"Overgrowth","Revitalize","Unflinching"} } },
            { "Inspiration", new[]{ new[]{"Hextech Flashtraption","Magical Footwear", "Cash Back" }, new[]{ "Triple Tonic", "Time Warp Tonic", "Biscuit Delivery"}, new[]{"Cosmic Insight","Approach Velocity", "Jack of All Trades" } } },
        };

        private readonly float SlotSize = 48f;
        private readonly float KeystoneSlotSize = 56f;
        private readonly float SlotSpacing = 12f;
        private readonly float RowVerticalSpacing = 100f; // SlotSize + 52 for rune description space
        private readonly float DescWidth = 260f;
        private readonly float PathDescWidth = 300f;

        public override void OnInitialize()
        {
            // Toggle button above defense display area (upper-left), can tweak as needed
            var mainButton = new UITextPanel<string>("Runes", 0.8f)
            {
                Width = { Pixels = 70 },
                Height = { Pixels = 26 },
                Left = { Percent = 1f, Pixels = -100 },
                Top = { Percent = 1f, Pixels = -80 },
                BackgroundColor = new Color(60, 60, 120) * 0.8f
            };
            mainButton.OnLeftClick += (_, __) => ToggleOpen();
            Append(mainButton);

            _panel = new DraggableUIPanel
            {
                Width = { Pixels = 800 },
                Height = { Pixels = 580 },
                Left = { Percent = 1f, Pixels = -880 }, // panel to the left of button
                Top = { Percent = 1f, Pixels = -600 },
                BackgroundColor = new Color(67, 67, 67) * 1f,
                BorderColor = new Color(180, 180, 180) * 0.7f,
                PaddingLeft = 14,
                PaddingRight = 14,
                PaddingTop = 14,
                PaddingBottom = 14
            };

            var title = new UIText("Rune Selection", 0.9f) { HAlign = 0f, Top = { Pixels = 0 } };
            _panel.Append(title);

            // 连接线绘制层（底层）
            _primaryConnector = new ConnectorElement { Width = { Pixels = 100 }, Height = { Pixels = 520 }, Left = { Pixels = 0 }, Top = { Pixels = 30 } };
            _panel.Append(_primaryConnector);
            
            _secondaryConnector = new ConnectorElement { Width = { Pixels = 100 }, Height = { Pixels = 320 }, Left = { Pixels = 420 }, Top = { Pixels = 30 } };
            _panel.Append(_secondaryConnector);

            // 主系和副系
            _primaryGroup = new UIElement { Width = { Pixels = 400 }, Height = { Pixels = 520 }, Left = { Pixels = 0 }, Top = { Pixels = 30 } };
            _panel.Append(_primaryGroup);

            _secondaryGroup = new UIElement { Width = { Pixels = 340 }, Height = { Pixels = 320 }, Left = { Pixels = 420 }, Top = { Pixels = 30 } };
            _panel.Append(_secondaryGroup);

            _primaryOptions = new UIElement { Width = { Pixels = 260 }, Height = { Pixels = 240 } };
            _panel.Append(_primaryOptions);

            _secondaryOptions = new UIElement { Width = { Pixels = 260 }, Height = { Pixels = 240 } };
            _panel.Append(_secondaryOptions);

            _detailPanel = new UIPanel
            {
                Width = { Pixels = 260 },
                Height = { Pixels = 120 },
                Left = { Pixels = 120 },
                Top = { Pixels = 120 },
                BackgroundColor = Color.Transparent,
                BorderColor = Color.Transparent
            };
            _detailPanel.IgnoresMouseInteraction = true;
            _detailTitle = new UIText(string.Empty, 0.85f) { Left = { Pixels = 8 }, Top = { Pixels = 6 } };
            _detailDesc = new UIText(string.Empty, 0.8f) { Left = { Pixels = 8 }, Top = { Pixels = 28 } };
            _detailPanel.Append(_detailTitle);
            _detailPanel.Append(_detailDesc);
            _panel.Append(_detailPanel);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            Refresh();
        }

        private void ToggleOpen()
        {
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

        private void Refresh()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            BuildPrimaryGroup(save);
            BuildSecondaryGroup(save);
            BuildPrimaryOptions(save);
            BuildSecondaryOptions(save);
            
            // 更新连接线
            _primaryConnector.SlotCenters = new List<Vector2>(_primarySlotCenters);
            _secondaryConnector.SlotCenters = new List<Vector2>(_secondarySlotCenters);
        }

        private void BuildPrimaryGroup(RuneSaveSystem save)
        {
            _primaryGroup.RemoveAllChildren();
            _primarySlotCenters.Clear();
            float y = 0f;
            float slotCenterX = SlotSize / 2f;
            bool showDesc = !_primaryPathOpen && string.IsNullOrEmpty(_activePrimarySlot);

            // Path slot
            float slotYPath = y;
            var pathSlot = MakeSlotButton(save.PrimaryPath, 0f, slotYPath, () =>
            {
                _primaryPathOpen = !_primaryPathOpen;
                _activePrimarySlot = null;
                HideDetail();
                _activePrimaryPathY = _primaryGroup.Top.Pixels + slotYPath;
                BuildPrimaryOptions(save);
            }, SlotSize);
            pathSlot.RingColor = PathColors.GetValueOrDefault(save.PrimaryPath, Color.Gray);
            _primaryGroup.Append(pathSlot);
            _primarySlotCenters.Add(new Vector2(slotCenterX, slotYPath + SlotSize / 2f));
            if (showDesc)
            {
                AddPathDescLine(_primaryGroup, save.PrimaryPath, SlotSize + 16f, slotYPath + 2f);
            }
            y += RowVerticalSpacing;

            // Keystone slot - 居中对齐使连接线垂直
            var keystoneSlotY = y;
            float keystoneOffsetX = -(KeystoneSlotSize - SlotSize) / 2f; // 向左偏移使中心对齐
            var keystoneSlot = MakeSlotButton(save.PrimaryKeystone, keystoneOffsetX, keystoneSlotY, () =>
            {
                _activePrimarySlot = _activePrimarySlot == "Keystone" ? null : "Keystone";
                _primaryPathOpen = false;
                _activePrimarySlotY = _primaryGroup.Top.Pixels + keystoneSlotY;
                if (!string.IsNullOrEmpty(save.PrimaryKeystone))
                {
                    ShowDetail(save.PrimaryKeystone, _primaryGroup.Left.Pixels + SlotSize + 16f, _activePrimarySlotY);
                }
                else
                {
                    HideDetail();
                }
                BuildPrimaryOptions(save);
            }, KeystoneSlotSize);
            keystoneSlot.RingColor = PathColors.GetValueOrDefault(save.PrimaryPath, Color.Gray);
            _primaryGroup.Append(keystoneSlot);
            _primarySlotCenters.Add(new Vector2(slotCenterX, keystoneSlotY + KeystoneSlotSize / 2f)); // 使用统一的中心X
            if (showDesc && !string.IsNullOrEmpty(save.PrimaryKeystone))
            {
                AddRuneDescLine(_primaryGroup, save.PrimaryKeystone, SlotSize + 16f, keystoneSlotY + 2f, Color.White);
            }
            y += KeystoneSlotSize + (RowVerticalSpacing - SlotSize);

            // Row 1/2/3 slots
            for (int row = 0; row < 3; row++)
            {
                string slotKey = $"Row{row}";
                float slotY = y;
                var slot = MakeSlotButton(GetPrimaryRowValue(save, row), 0f, slotY, () =>
                {
                    _activePrimarySlot = _activePrimarySlot == slotKey ? null : slotKey;
                    _primaryPathOpen = false;
                    _activePrimarySlotY = _primaryGroup.Top.Pixels + slotY;
                    string current = GetPrimaryRowValue(save, row);
                    if (!string.IsNullOrEmpty(current))
                    {
                        ShowDetail(current, _primaryGroup.Left.Pixels + SlotSize + 16f, _activePrimarySlotY);
                    }
                    else
                    {
                        HideDetail();
                    }
                    BuildPrimaryOptions(save);
                }, SlotSize);
                slot.RingColor = PathColors.GetValueOrDefault(save.PrimaryPath, Color.Gray);
                _primaryGroup.Append(slot);
                _primarySlotCenters.Add(new Vector2(slotCenterX, slotY + SlotSize / 2f));
                string picked = GetPrimaryRowValue(save, row);
                if (showDesc && !string.IsNullOrEmpty(picked))
                {
                    AddRuneDescLine(_primaryGroup, picked, SlotSize + 16f, slotY + 2f, Color.White);
                }
                y += RowVerticalSpacing;
            }
        }

        private void BuildSecondaryGroup(RuneSaveSystem save)
        {
            _secondaryGroup.RemoveAllChildren();
            _secondarySlotCenters.Clear();
            float y = 0f;
            float slotCenterX = SlotSize / 2f;
            bool showDesc = !_secondaryPathOpen && _activeSecondarySlot == -1;

            // Path slot
            float slotYPath = y;
            var pathSlot = MakeSlotButton(save.SecondaryPath, 0f, slotYPath, () =>
            {
                _secondaryPathOpen = !_secondaryPathOpen;
                _activeSecondarySlot = -1;
                _activeSecondarySlotY = 0f;
                HideDetail();
                _activeSecondaryPathY = _secondaryGroup.Top.Pixels + slotYPath;
                BuildSecondaryOptions(save);
            }, SlotSize);
            pathSlot.RingColor = PathColors.GetValueOrDefault(save.SecondaryPath, Color.Gray);
            _secondaryGroup.Append(pathSlot);
            _secondarySlotCenters.Add(new Vector2(slotCenterX, slotYPath + SlotSize / 2f));
            if (showDesc)
            {
                AddPathDescLine(_secondaryGroup, save.SecondaryPath, SlotSize + 16f, slotYPath + 2f);
            }
            y += RowVerticalSpacing;

            // Secondary pick 1
            var slot1Y = y;
            var slot1 = MakeSlotButton(save.SecondaryPick1, 0f, slot1Y, () => { _activeSecondarySlot = _activeSecondarySlot == 0 ? -1 : 0; _secondaryPathOpen = false; _activeSecondarySlotY = _secondaryGroup.Top.Pixels + slot1Y; BuildSecondaryOptions(save); }, SlotSize);
            if (!string.IsNullOrEmpty(save.SecondaryPick1))
            {
                ShowDetail(save.SecondaryPick1, _secondaryGroup.Left.Pixels + SlotSize + 16f, _secondaryGroup.Top.Pixels + slot1Y);
            }
            slot1.RingColor = PathColors.GetValueOrDefault(save.SecondaryPath, Color.Gray);
            _secondaryGroup.Append(slot1);
            _secondarySlotCenters.Add(new Vector2(slotCenterX, slot1Y + SlotSize / 2f));
            if (showDesc && !string.IsNullOrEmpty(save.SecondaryPick1))
            {
                AddRuneDescLine(_secondaryGroup, save.SecondaryPick1, SlotSize + 16f, slot1Y + 2f, Color.White);
            }

            // Secondary pick 2
            var slot2Y = y + RowVerticalSpacing;
            var slot2 = MakeSlotButton(save.SecondaryPick2, 0f, slot2Y, () => { _activeSecondarySlot = _activeSecondarySlot == 1 ? -1 : 1; _secondaryPathOpen = false; _activeSecondarySlotY = _secondaryGroup.Top.Pixels + slot2Y; BuildSecondaryOptions(save); }, SlotSize);
            if (!string.IsNullOrEmpty(save.SecondaryPick2))
            {
                ShowDetail(save.SecondaryPick2, _secondaryGroup.Left.Pixels + SlotSize + 16f, _secondaryGroup.Top.Pixels + slot2Y);
            }
            slot2.RingColor = PathColors.GetValueOrDefault(save.SecondaryPath, Color.Gray);
            _secondaryGroup.Append(slot2);
            _secondarySlotCenters.Add(new Vector2(slotCenterX, slot2Y + SlotSize / 2f));
            if (showDesc && !string.IsNullOrEmpty(save.SecondaryPick2))
            {
                AddRuneDescLine(_secondaryGroup, save.SecondaryPick2, SlotSize + 16f, slot2Y + 2f, Color.White);
            }
        }

        private void BuildPrimaryOptions(RuneSaveSystem save)
        {
            _primaryOptions.RemoveAllChildren();
            _primaryOptions.Left.Pixels = _primaryGroup.Left.Pixels + 80f;

            if (_primaryPathOpen)
            {
                _primaryOptions.Top.Pixels = _activePrimaryPathY;
                float x = 0f;
                foreach (var p in Paths)
                {
                    var btn = MakeIconButton(p, x, 0f, (evt, elem) =>
                    {
                        SetPrimary(p);
                        _primaryPathOpen = false;
                        _activePrimarySlot = null;
                    });
                    btn.SetPlaceholder(false);
                    btn.SetVisual(save.PrimaryPath == p, true);
                    _primaryOptions.Append(btn);
                    x += SlotSize + SlotSpacing;
                }
                return;
            }

            if (string.IsNullOrEmpty(_activePrimarySlot))
            {
                _primaryOptions.Left.Pixels = -10000f;
                _primaryOptions.Top.Pixels = -10000f;
                HideDetail();
                return;
            }
            _primaryOptions.Top.Pixels = _activePrimarySlotY;

            string[] list;
            if (_activePrimarySlot == "Keystone")
            {
                list = _keystones[save.PrimaryPath];
            }
            else
            {
                int row = int.Parse(_activePrimarySlot.Substring(3));
                list = _rows[save.PrimaryPath][row];
            }

            float size = _activePrimarySlot == "Keystone" ? KeystoneSlotSize : SlotSize;
            float gap = SlotSpacing;
            // 基石一行排开不限个数，Row 1/2/3 一行排开但最多3个
            int maxCols = _activePrimarySlot == "Keystone" ? list.Length : 3;
            int col = 0; int rowIdx = 0;
            foreach (var r in list)
            {
                var btn = MakeIconButton(r, col * (size + gap), rowIdx * (size + gap), (evt, elem) =>
                {
                    if (_activePrimarySlot == "Keystone")
                    {
                        save.PrimaryKeystone = save.PrimaryKeystone == r ? "" : r;
                        if (!string.IsNullOrEmpty(save.PrimaryKeystone))
                        {
                            ShowDetail(save.PrimaryKeystone, _primaryGroup.Left.Pixels + SlotSize + 16f, _activePrimarySlotY);
                        }
                    }
                    else
                    {
                        int rowSel = int.Parse(_activePrimarySlot.Substring(3));
                        string current = GetPrimaryRowValue(save, rowSel);
                        SetPrimaryRowValue(save, rowSel, current == r ? "" : r);
                        string updated = GetPrimaryRowValue(save, rowSel);
                        if (!string.IsNullOrEmpty(updated))
                        {
                            ShowDetail(updated, _primaryGroup.Left.Pixels + SlotSize + 16f, _activePrimarySlotY);
                        }
                    }
                    _activePrimarySlot = null; // auto-collapse
                    Refresh();
                }, size);
                btn.SetPlaceholder(false);
                bool selected = (_activePrimarySlot == "Keystone" && save.PrimaryKeystone == r) || (_activePrimarySlot != "Keystone" && GetPrimaryRowValue(save, int.Parse(_activePrimarySlot.Substring(3))) == r);
                btn.SetVisual(selected, true);
                _primaryOptions.Append(btn);
                col++;
                if (col >= maxCols) { col = 0; rowIdx++; }
            }
        }

        private void BuildSecondaryOptions(RuneSaveSystem save)
        {
            _secondaryOptions.RemoveAllChildren();
            _secondaryOptions.Left.Pixels = _secondaryGroup.Left.Pixels + 80f;

            if (_secondaryPathOpen)
            {
                _secondaryOptions.Top.Pixels = _secondaryGroup.Top.Pixels + 32f; // fixed position
                float x = 0f;
                foreach (var p in Paths)
                {
                    if (p == save.PrimaryPath) continue;
                    var btn = MakeIconButton(p, x, 0f, (evt, elem) =>
                    {
                        SetSecondary(p);
                        _secondaryPathOpen = false;
                        _activeSecondarySlot = -1;
                    });
                    btn.SetPlaceholder(false);
                    btn.SetVisual(save.SecondaryPath == p, true);
                    _secondaryOptions.Append(btn);
                    x += SlotSize + SlotSpacing;
                }
                return;
            }

            if (_activeSecondarySlot == -1)
            {
                _secondaryOptions.Left.Pixels = -10000f;
                _secondaryOptions.Top.Pixels = -10000f;
                HideDetail();
                return;
            }
            _secondaryOptions.Top.Pixels = _secondaryGroup.Top.Pixels + 32f; // fixed menu position

            var subRows = _rows[save.SecondaryPath];
            float sizeS = SlotSize;
            float gapS = SlotSpacing;
            int colS = 0; int rowS = 0;
            for (int row = 0; row < subRows.Length; row++)
            {
                foreach (var r in subRows[row])
                {
                    int capturedRow = row;
                    var btn = MakeIconButton(r, colS * (sizeS + gapS), rowS * (sizeS + gapS), (evt, elem) =>
                    {
                        if (save.SecondaryPick1 == r)
                        {
                            save.SecondaryPick1 = ""; save.SecondaryPick1Row = -1;
                        }
                        else if (save.SecondaryPick2 == r)
                        {
                            save.SecondaryPick2 = ""; save.SecondaryPick2Row = -1;
                        }
                        else
                        {
                            if (save.SecondaryPick1Row == capturedRow)
                            {
                                save.SecondaryPick1 = r; save.SecondaryPick1Row = capturedRow;
                            }
                            else if (save.SecondaryPick2Row == capturedRow)
                            {
                                save.SecondaryPick2 = r; save.SecondaryPick2Row = capturedRow;
                            }
                            else if (_activeSecondarySlot == 0 || string.IsNullOrEmpty(save.SecondaryPick1))
                            {
                                save.SecondaryPick1 = r; save.SecondaryPick1Row = capturedRow;
                            }
                            else if (_activeSecondarySlot == 1 || string.IsNullOrEmpty(save.SecondaryPick2))
                            {
                                save.SecondaryPick2 = r; save.SecondaryPick2Row = capturedRow;
                            }
                            else
                            {
                                save.SecondaryPick2 = r; save.SecondaryPick2Row = capturedRow;
                            }
                        }
                        string picked = _activeSecondarySlot == 0 ? save.SecondaryPick1 : save.SecondaryPick2;
                        if (!string.IsNullOrEmpty(picked))
                        {
                            ShowDetail(picked, _secondaryGroup.Left.Pixels + SlotSize + 16f, _secondaryGroup.Top.Pixels + 32f);
                        }
                        _activeSecondarySlot = -1; // auto-collapse
                        Refresh();
                    }, sizeS);
                    btn.SetPlaceholder(false);
                    bool selected = save.SecondaryPick1 == r || save.SecondaryPick2 == r;
                    btn.SetVisual(selected, true);
                    _secondaryOptions.Append(btn);
                    colS++;
                    if (colS >= 3) { colS = 0; rowS++; }
                }
            }
        }

        private void SetPrimary(string path)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.PrimaryPath == path) { _primaryPathOpen = false; Refresh(); return; }
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
            _primaryPathOpen = false;
            _activePrimarySlot = null;
            _activeSecondarySlot = -1;
            Refresh();
        }

        private void SetSecondary(string path)
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (path == save.PrimaryPath) return;
            if (save.SecondaryPath == path) { _secondaryPathOpen = false; Refresh(); return; }
            save.SecondaryPath = path;
            save.SecondaryPick1 = "";
            save.SecondaryPick2 = "";
            save.SecondaryPick1Row = -1;
            save.SecondaryPick2Row = -1;
            _secondaryPathOpen = false;
            _activeSecondarySlot = -1;
            Refresh();
        }

        private IconButton MakeSlotButton(string runeName, float x, float y, Action onClick, float size = 48f)
        {
            bool hasValue = !string.IsNullOrEmpty(runeName);
            var btn = MakeIconButton(hasValue ? runeName : null, x, y, (evt, elem) => onClick?.Invoke(), size);
            btn.SetPlaceholder(!hasValue);
            btn.SetVisual(false, true);
            return btn;
        }

        private IconButton MakeIconButton(string name, float x, float y, UIElement.MouseEvent click, float size = 48f)
        {
            var tex = string.IsNullOrEmpty(name) ? null : LoadRuneTexture(name);
            var btn = new IconButton(tex, size)
            {
                Left = { Pixels = x },
                Top = { Pixels = y }
            };
            btn.OnLeftClick += click;
            btn.Tooltip = string.Empty;
            return btn;
        }

        private Asset<Texture2D> LoadRuneTexture(string name)
        {
            string safe = SanitizeName(name);
            var candidates = new List<string> { safe, safe + "_" };
            if (safe.StartsWith("Legend_", System.StringComparison.Ordinal) && !safe.Contains("-_"))
            {
                candidates.Add(safe.Replace("Legend_", "Legend-_"));
            }

            foreach (var c in candidates)
            {
                string path = $"LeagueOfLegendThings/Content/Buffs/{c}";
                try
                {
                    return ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
                }
                catch
                {
                    // try next
                }
            }
            return TextureAssets.MagicPixel; // ռλ
        }

        private string SanitizeName(string name)
        {
            var chars = name.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var s = new string(chars);
            while (s.Contains("__")) s = s.Replace("__", "_");
            return s.Trim('_');
        }

        private string GetRuneDescription(string name)
        {
            string key = GetDescriptionKey(name);
            string localized = Language.GetTextValue(key);
            if (!string.IsNullOrEmpty(localized) && localized != key)
                return localized;
            return "";
        }

        private string GetPathDisplayText(string path)
        {
            string nameKey = GetPathNameKey(path);
            string descKey = GetPathDescKey(path);
            string name = Language.GetTextValue(nameKey);
            string desc = Language.GetTextValue(descKey);
            if (string.IsNullOrEmpty(name) || name == nameKey)
                name = path;
            if (string.IsNullOrEmpty(desc) || desc == descKey)
                desc = "";
            return string.IsNullOrEmpty(desc) ? name : $"{name}: {desc}";
        }

        private string GetDescriptionKey(string name)
        {
            return $"Mods.LeagueOfLegendThings.UI.Runes.Desc.{SanitizeName(name)}";
        }

        private string GetPathNameKey(string path)
        {
            return $"Mods.LeagueOfLegendThings.UI.Runes.Path.{SanitizeName(path)}.Name";
        }

        private string GetPathDescKey(string path)
        {
            return $"Mods.LeagueOfLegendThings.UI.Runes.Path.{SanitizeName(path)}.Desc";
        }

        private void ShowDetail(string runeName, float left, float top)
        {
            HideDetail();
        }

        private void HideDetail()
        {
            _detailTitle.SetText(string.Empty);
            _detailDesc.SetText(string.Empty);
            SetDetailVisible(false);
        }

        private void SetDetailVisible(bool visible)
        {
            _detailVisible = visible;
            _detailPanel.BackgroundColor = visible ? new Color(50, 50, 50) * 0.9f : Color.Transparent;
            _detailPanel.BorderColor = visible ? new Color(120, 120, 120) * 0.9f : Color.Transparent;
        }

        private static string WrapText(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLen)
                return text;

            var parts = new List<string>();
            int idx = 0;
            while (idx < text.Length)
            {
                int len = System.Math.Min(maxLen, text.Length - idx);
                parts.Add(text.Substring(idx, len));
                idx += len;
            }
            return string.Join("\n", parts);
        }

        private void AddRuneDescLine(UIElement parent, string runeName, float x, float y, Color titleColor)
        {
            string desc = GetRuneDescription(runeName);
            string title = runeName;
            string body = WrapText(desc, 42);
            var line = new RuneDescBlock(title, body, titleColor, DescWidth)
            {
                Left = { Pixels = x },
                Top = { Pixels = y }
            };
            parent.Append(line);
        }

        private void AddPathDescLine(UIElement parent, string path, float x, float y)
        {
            if (string.IsNullOrEmpty(path))
                return;
            string name = Language.GetTextValue(GetPathNameKey(path));
            if (string.IsNullOrEmpty(name) || name == GetPathNameKey(path))
                name = path;
            string desc = Language.GetTextValue(GetPathDescKey(path));
            if (string.IsNullOrEmpty(desc) || desc == GetPathDescKey(path))
                desc = string.Empty;
            var color = PathColors.GetValueOrDefault(path, Color.White);
            var line = new RuneDescBlock(name, WrapText(desc, 48), color, PathDescWidth)
            {
                Left = { Pixels = x },
                Top = { Pixels = y }
            };
            parent.Append(line);
        }

        private void Highlight(IconButton btn, bool selected, bool enabled = true)
        {
            btn.SetVisual(selected, enabled);
        }

        private class IconButton : UIElement
        {
            private readonly Texture2D _texture;
            private readonly Texture2D _dotTex = TextureAssets.MagicPixel.Value;
            private readonly float _size;
            private bool _selected;
            private bool _enabled = true;
            private bool _placeholder;
            public string Tooltip { get; set; } = string.Empty;
            public Color RingColor { get; set; } = Color.Transparent; // 圆环颜色

            public IconButton(Asset<Texture2D> texture, float size)
            {
                _texture = texture?.Value ?? TextureAssets.MagicPixel.Value;
                _size = size;
                Width.Set(size, 0f);
                Height.Set(size, 0f);
            }

            public void SetPlaceholder(bool placeholder) => _placeholder = placeholder;

            public void SetVisual(bool selected, bool enabled)
            {
                _selected = selected;
                _enabled = enabled;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                var dims = GetDimensions();
                var center = dims.Position() + new Vector2(dims.Width * 0.5f, dims.Height * 0.5f);
                float targetSize = _selected ? _size * 1.05f : _size * 0.92f;
                float scale = targetSize / MathF.Max(_texture.Width, _texture.Height);
                var iconColor = _enabled ? Color.White : Color.Gray;

                bool hoverNow = IsMouseHovering;

                if (hoverNow || _selected)
                {
                    var bgCol = new Color(60, 60, 60) * (_enabled ? 0.6f : 0.4f);
                    spriteBatch.Draw(_dotTex, new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height), bgCol);
                }

                if (_placeholder)
                {
                    // Draw a subtle placeholder plus
                    var border = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
                    DrawBorder(spriteBatch, border, 1, new Color(90, 90, 90));
                    spriteBatch.Draw(_dotTex, new Rectangle((int)center.X - 8, (int)center.Y - 1, 16, 2), new Color(120, 120, 120));
                    spriteBatch.Draw(_dotTex, new Rectangle((int)center.X - 1, (int)center.Y - 8, 2, 16), new Color(120, 120, 120));
                }
                else
                {
                    spriteBatch.Draw(_texture, center, null, iconColor, 0f, _texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }

                // 绘制圆环在图标上方
                if (RingColor != Color.Transparent)
                {
                    DrawRing(spriteBatch, center, _size / 2f + 4f, 3, RingColor);
                }

                if (_selected)
                {
                    var border = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
                    DrawBorder(spriteBatch, border, 2, new Color(196, 145, 63));
                }

                if (hoverNow && !string.IsNullOrEmpty(Tooltip))
                {
                    // hover tooltip disabled
                }
            }

            private void DrawRing(SpriteBatch spriteBatch, Vector2 center, float radius, int thickness, Color color)
            {
                // 使用分段绘制圆环
                int segments = 64;
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = MathHelper.TwoPi * i / segments;
                    float angle2 = MathHelper.TwoPi * (i + 1) / segments;
                    
                    Vector2 p1 = center + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
                    Vector2 p2 = center + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;
                    
                    DrawLine(spriteBatch, p1, p2, thickness, color);
                }
            }

            private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, int thickness, Color color)
            {
                Vector2 edge = end - start;
                float angle = MathF.Atan2(edge.Y, edge.X);
                float length = edge.Length();

                spriteBatch.Draw(_dotTex,
                    new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
                    null, color, angle, Vector2.Zero, SpriteEffects.None, 0f);
            }

            private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
            {
                spriteBatch.Draw(_dotTex, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
                spriteBatch.Draw(_dotTex, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
                spriteBatch.Draw(_dotTex, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
                spriteBatch.Draw(_dotTex, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
            }

            public override void MouseOver(UIMouseEvent evt)
            {
                base.MouseOver(evt);
            }
        }

        private class RuneDescBlock : UIElement
        {
            private readonly UIText _title;
            private readonly UIText _body;

            public RuneDescBlock(string title, string body, Color titleColor, float width)
            {
                IgnoresMouseInteraction = true;
                Width.Set(width, 0f);
                Height.Set(54f, 0f);
                _title = new UIText(title, 0.85f)
                {
                    Left = { Pixels = 0f },
                    Top = { Pixels = 0f },
                    TextColor = titleColor
                };
                _body = new UIText(body, 0.72f)
                {
                    Left = { Pixels = 0f },
                    Top = { Pixels = 18f },
                    TextColor = new Color(190, 190, 190)
                };
                Append(_title);
                Append(_body);
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

        private class ConnectorElement : UIElement
        {
            private readonly Texture2D _dotTex = TextureAssets.MagicPixel.Value;
            public List<Vector2> SlotCenters { get; set; } = new();
            public Color LineColor { get; set; } = Color.White;

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                if (SlotCenters.Count < 2) return;

                var dims = GetDimensions();
                var origin = dims.Position();

                // 连接线分段绘制（避开图标区域）
                for (int i = 0; i < SlotCenters.Count - 1; i++)
                {
                    var start = origin + SlotCenters[i];
                    var end = origin + SlotCenters[i + 1];
                    
                    // 计算图标区域的边界（避开图标）
                    float iconRadius = 28f; // 大约是槽位大小的一半
                    Vector2 direction = Vector2.Normalize(end - start);
                    Vector2 adjustedStart = start + direction * iconRadius;
                    Vector2 adjustedEnd = end - direction * iconRadius;
                    
                    // 只有当调整后的线段有意义时才绘制
                    if (Vector2.Distance(adjustedStart, adjustedEnd) > 5f)
                    {
                        // 黑色描边
                        DrawLine(spriteBatch, adjustedStart, adjustedEnd, 5, Color.Black);
                        // 白色线条
                        DrawLine(spriteBatch, adjustedStart, adjustedEnd, 3, Color.White);
                    }
                }
            }

            private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, int thickness, Color color)
            {
                Vector2 edge = end - start;
                float angle = MathF.Atan2(edge.Y, edge.X);
                float length = edge.Length();

                spriteBatch.Draw(_dotTex,
                    new Rectangle((int)start.X, (int)(start.Y - thickness / 2), (int)length, thickness),
                    null, color, angle, Vector2.Zero, SpriteEffects.None, 0f);
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
