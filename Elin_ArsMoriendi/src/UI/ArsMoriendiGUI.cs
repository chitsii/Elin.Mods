using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// IMGUI-based UI for the Ars Moriendi forbidden tome.
    /// Drawn from Plugin.OnGUI(). Three tabs: Knowledge, Ritual, Servants.
    /// Visual theme: Forbidden Grimoire — warm leather, charred vellum, gilded gold.
    /// </summary>
    public static partial class ArsMoriendiGUI
    {
        // ── State ──
        public static bool IsVisible;
        private static Rect _windowRect;
        private static int _currentTab;
        private static Vector2 _scrollPos;
        // Confirmation dialog state
        private static bool _pendingConfirm;
        private static string _confirmMessage = "";
        private static Action? _confirmAction;

        private const int WindowId = 92710;
        private const float WindowWidth = 720f;
        private const float WindowHeight = 580f;
        private const int MaxStockDisplay = 50;
        private const float StockLabelWidth = 170f;
        private const float StockBarWidth = 100f;
        private const int CorpsePageSize = 10;
        private const float CorpseIconSize = 32f;

        // ── Styles ──
        private static bool _stylesInitialized;
        private static Font? _font;
        private static GUIStyle _boxStyle = null!;
        private static GUIStyle _titleStyle = null!;
        private static GUIStyle _headerStyle = null!;
        private static GUIStyle _labelStyle = null!;
        private static GUIStyle _descStyle = null!;
        private static GUIStyle _flavorStyle = null!;
        private static GUIStyle _buttonStyle = null!;
        private static GUIStyle _selectedButtonStyle = null!;
        private static GUIStyle _warningStyle = null!;
        private static GUIStyle _goodStyle = null!;
        private static GUIStyle _badStyle = null!;
        private static GUIStyle _toolbarStyle = null!;
        private static GUIStyle _scrollStyle = null!;
        private static GUIStyle _closeButtonStyle = null!;
        private static GUIStyle _confirmButtonStyle = null!;
        private static GUIStyle _confirmLabelStyle = null!;
        private static GUIStyle _summaryLabelStyle = null!;
        private static GUIStyle _summaryValueStyle = null!;
        private static GUIStyle _summaryMissingStyle = null!;
        private static GUIStyle _smallLabelStyle = null!;
        private static GUIStyle _corpseButtonStyle = null!;

        // ── Tab names ──
        private static string[] TabNames = new string[3];

        // Chapter Four tab styles
        private static GUIStyle _journalStyle = null!;
        private static GUIStyle _journalHeaderStyle = null!;
        private static GUIStyle _journalAuthorStyle = null!;

        // ── Color Palette: Forbidden Grimoire ──

        // Surfaces
        private static readonly Color Obsidian = new(0.07f, 0.06f, 0.05f, 0.94f);
        private static readonly Color CharredVellum = new(0.10f, 0.08f, 0.06f, 0.70f);
        private static readonly Color WornLeather = new(0.18f, 0.13f, 0.08f, 0.90f);
        private static readonly Color WarmLeather = new(0.25f, 0.18f, 0.10f, 0.92f);
        private static readonly Color PressedHide = new(0.12f, 0.09f, 0.05f, 0.95f);
        private static readonly Color BloodSeal = new(0.30f, 0.10f, 0.08f, 0.92f);
        private static readonly Color AshenTab = new(0.13f, 0.10f, 0.07f, 0.90f);
        private static readonly Color EmberTab = new(0.28f, 0.12f, 0.08f, 0.95f);
        private static readonly Color ConfirmOverlay = new(0.03f, 0.02f, 0.02f, 0.85f);

        // Text
        private static readonly Color Parchment = new(0.82f, 0.76f, 0.65f);
        private static readonly Color FadedInk = new(0.60f, 0.55f, 0.47f);
        private static readonly Color GildedGold = new(0.85f, 0.72f, 0.35f);
        private static readonly Color TarnishedGold = new(0.75f, 0.62f, 0.30f);
        private static readonly Color BoneWhite = new(0.65f, 0.60f, 0.52f);
        private static readonly Color SpectralGreen = new(0.40f, 0.78f, 0.35f);
        private static readonly Color BloodCrimson = new(0.82f, 0.22f, 0.18f);
        private static readonly Color AmberWarning = new(0.90f, 0.72f, 0.20f);
        private static readonly Color GhostlyViolet = new(0.62f, 0.55f, 0.70f);

        // Utility
        private static readonly Color DividerGold = new(0.55f, 0.45f, 0.20f, 0.50f);
        private static readonly Color BarBackground = new(0.15f, 0.12f, 0.08f, 0.80f);

        // ── Textures (lazy-created) ──
        private static Texture2D? _dividerTex;
        private static Texture2D? _barBgTex;
        private static Texture2D? _confirmOverlayTex;
        private static readonly Dictionary<uint, Texture2D> _solidTexCache = new();
        private static bool _styleCompatibilityMode;

        // ── Public API ──

        public static void Show()
        {
            IsVisible = true;
            _currentTab = 0;
            _scrollPos = Vector2.zero;
            _pendingConfirm = false;
            _confirmAction = null;
            ResetRitualState();
            ResetServantState();
            RebuildTabNames();
        }

        public static void Hide()
        {
            IsVisible = false;
            _pendingConfirm = false;
            _confirmAction = null;
            EInput.haltInput = false;
            EInput.Consume(true, 2);
        }

        public static void Draw()
        {
            if (!IsVisible) return;

            InitStyles();
            var prevGuiColor = GUI.color;
            var prevBgColor = GUI.backgroundColor;
            var prevContentColor = GUI.contentColor;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            try
            {
                // Escape handling
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    if (_pendingConfirm)
                    {
                        _pendingConfirm = false;
                        _confirmAction = null;
                    }
                    else if (_selectedServant != null)
                    {
                        _selectedServant = null;
                    }
                    else
                    {
                        Hide();
                    }
                    Event.current.Use();
                    return;
                }

                _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "", _boxStyle);

                // Close if clicked outside window (but not during confirmation)
                if (Event.current.type == EventType.MouseDown
                    && !_windowRect.Contains(Event.current.mousePosition)
                    && !_pendingConfirm)
                {
                    Hide();
                    Event.current.Use();
                }
            }
            finally
            {
                GUI.color = prevGuiColor;
                GUI.backgroundColor = prevBgColor;
                GUI.contentColor = prevContentColor;
            }
        }

        // ── Window Drawing ──

        private static void DrawWindow(int id)
        {
            // Title bar
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("✦ Ars Moriendi ✦", "✦ Ars Moriendi ✦"), _titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L("商人を呼ぶ", "Summon Merchant", "召唤商人"), _buttonStyle, GUILayout.Width(Lang.langCode == "CN" || Lang.isJP ? 100 : 182), GUILayout.Height(22)))
            {
                TraitArsMoriendi.SummonMerchant();
            }
            if (GUILayout.Button("✕", _closeButtonStyle, GUILayout.Width(28), GUILayout.Height(22)))
            {
                if (!_pendingConfirm) Hide();
            }
            GUILayout.EndHorizontal();

            if (Plugin.HasCriticalPatchFailures)
            {
                GUILayout.Label(
                    L(
                        "※ 重要パッチの適用に失敗しています。死亡関連の機能が一部無効の可能性があります。ログを確認してください。",
                        "※ Critical patches failed to apply. Some death-related features may be disabled. Check logs.",
                        "※ 关键补丁应用失败。部分死亡相关功能可能已失效。请检查日志。"
                    ),
                    _badStyle
                );
                string summary = Plugin.GetPatchFailureSummary();
                if (!string.IsNullOrEmpty(summary))
                    GUILayout.Label($"Patch: {summary}", _warningStyle);
                GUILayout.Space(2);
            }
            else if (Plugin.HasAnyPatchFailures)
            {
                GUILayout.Label(
                    L(
                        "※ 一部パッチの適用に失敗しています。挙動に不整合が出る可能性があります。",
                        "※ Some patches failed to apply. Behavior may be partially degraded.",
                        "※ 部分补丁应用失败，行为可能出现不一致。"
                    ),
                    _warningStyle
                );
                string summary = Plugin.GetPatchFailureSummary();
                if (!string.IsNullOrEmpty(summary))
                    GUILayout.Label($"Patch: {summary}", _smallLabelStyle);
                GUILayout.Space(2);
            }

            if (Plugin.HasCriticalRuntimePatchFailures)
            {
                GUILayout.Label(
                    L(
                        "※ 重要パッチで実行時エラーを検知しています。機能が劣化している可能性があります。",
                        "※ Critical runtime errors detected in patches. Behavior may be degraded.",
                        "※ 检测到关键补丁运行时错误，功能可能已降级。"
                    ),
                    _badStyle
                );
                string summary = Plugin.GetRuntimePatchFailureSummary();
                if (!string.IsNullOrEmpty(summary))
                    GUILayout.Label($"Runtime: {summary}", _warningStyle);
                GUILayout.Space(2);
            }
            else if (Plugin.HasAnyRuntimePatchFailures)
            {
                GUILayout.Label(
                    L(
                        "※ 一部パッチで実行時エラーを検知しています。ログを確認してください。",
                        "※ Runtime errors detected in some patches. Check logs.",
                        "※ 检测到部分补丁运行时错误。请检查日志。"
                    ),
                    _warningStyle
                );
                string summary = Plugin.GetRuntimePatchFailureSummary();
                if (!string.IsNullOrEmpty(summary))
                    GUILayout.Label($"Runtime: {summary}", _smallLabelStyle);
                GUILayout.Space(2);
            }

            DrawDivider();
            GUILayout.Space(2);

            // Tab toolbar — disable during confirmation
            bool wasEnabled = GUI.enabled;
            if (_pendingConfirm) GUI.enabled = false;
            try
            {
                _currentTab = GUILayout.Toolbar(_currentTab, TabNames, _toolbarStyle, GUILayout.Height(30));

                GUILayout.Space(4);

                // Scrollable tab content
                _scrollPos = GUILayout.BeginScrollView(_scrollPos, _scrollStyle);
                try
                {
                    switch (_currentTab)
                    {
                        case 0: DrawKnowledgeTab(); break;
                        case 1: DrawRitualTab(); break;
                        case 2: DrawServantTab(); break;
                        case 3: DrawChapterFourTab(); break;
                    }
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
            finally
            {
                GUI.enabled = wasEnabled;
            }

            // Confirmation overlay (drawn last, on top)
            if (_pendingConfirm)
                DrawConfirmOverlay();

            // Make window draggable by title area
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 30));
        }

        // ── Confirmation Overlay ──

        private static void ShowConfirmDialog(string message, Action action)
        {
            _pendingConfirm = true;
            _confirmMessage = message;
            _confirmAction = action;
        }

        private static void DrawConfirmOverlay()
        {
            var theme = UiTheme.Ars;
            if (theme.UseSimpleConfirmOverlay)
            {
                float compatBoxW = 460;
                float compatBoxH = 130;
                float compatBoxX = (_windowRect.width - compatBoxW) / 2f;
                float compatBoxY = (_windowRect.height - compatBoxH) / 2f;
                var compatBoxRect = new Rect(compatBoxX, compatBoxY, compatBoxW, compatBoxH);
                GUI.Box(compatBoxRect, GUIContent.none, _scrollStyle);

                var compatMsgRect = new Rect(compatBoxX + 20, compatBoxY + 18, compatBoxW - 40, 56);
                GUI.Label(compatMsgRect, _confirmMessage, _confirmLabelStyle);

                float compatBtnW = 120;
                float compatBtnH = 30;
                float compatBtnY = compatBoxY + compatBoxH - compatBtnH - 18;
                float compatCenterX = compatBoxX + compatBoxW / 2f;
                var compatExecRect = new Rect(compatCenterX - compatBtnW - 10, compatBtnY, compatBtnW, compatBtnH);
                var compatCancelRect = new Rect(compatCenterX + 10, compatBtnY, compatBtnW, compatBtnH);
                if (GUI.Button(compatExecRect, L("✓ 実行", "✓ Confirm", "✓ 确认"), _confirmButtonStyle))
                {
                    _pendingConfirm = false;
                    _confirmAction?.Invoke();
                    _confirmAction = null;
                }
                if (GUI.Button(compatCancelRect, L("✕ " + LangHelper.Get("cancel"), "✕ " + LangHelper.Get("cancel")), _buttonStyle))
                {
                    _pendingConfirm = false;
                    _confirmAction = null;
                }
                return;
            }

            var fullRect = new Rect(0, 0, _windowRect.width, _windowRect.height);
            if (_confirmOverlayTex == null)
                _confirmOverlayTex = MakeTex(1, 1, ConfirmOverlay);
            GUI.DrawTexture(fullRect, _confirmOverlayTex);

            float boxW = 460;
            float boxH = 130;
            float boxX = (_windowRect.width - boxW) / 2f;
            float boxY = (_windowRect.height - boxH) / 2f;
            var boxRect = new Rect(boxX, boxY, boxW, boxH);

            GUI.DrawTexture(boxRect, GetSolidTex(new Color(0.12f, 0.09f, 0.06f, 0.97f)));

            var borderColor = DividerGold;
            var borderTex = GetSolidTex(borderColor);
            GUI.DrawTexture(new Rect(boxX, boxY, boxW, 1), borderTex);
            GUI.DrawTexture(new Rect(boxX, boxY + boxH - 1, boxW, 1), borderTex);
            GUI.DrawTexture(new Rect(boxX, boxY, 1, boxH), borderTex);
            GUI.DrawTexture(new Rect(boxX + boxW - 1, boxY, 1, boxH), borderTex);

            var msgRect = new Rect(boxX + 20, boxY + 18, boxW - 40, 56);
            GUI.Label(msgRect, _confirmMessage, _confirmLabelStyle);

            float btnW = 120;
            float btnH = 30;
            float btnY = boxY + boxH - btnH - 18;
            float centerX = boxX + boxW / 2f;

            var execRect = new Rect(centerX - btnW - 10, btnY, btnW, btnH);
            var cancelRect = new Rect(centerX + 10, btnY, btnW, btnH);

            if (GUI.Button(execRect, L("✓ 実行", "✓ Confirm", "✓ 确认"), _confirmButtonStyle))
            {
                _pendingConfirm = false;
                _confirmAction?.Invoke();
                _confirmAction = null;
            }
            if (GUI.Button(cancelRect, L("✕ " + LangHelper.Get("cancel"), "✕ " + LangHelper.Get("cancel")), _buttonStyle))
            {
                _pendingConfirm = false;
                _confirmAction = null;
            }
        }

        // ── Knowledge Tab ──

        private static void DrawKnowledgeTab()
        {
            var mgr = NecromancyManager.Instance;

            GUILayout.Label(LangHelper.Get("tomeFlavor"), _flavorStyle);
            GUILayout.Space(6);
            DrawDivider();
            GUILayout.Space(6);

            // ── Spell List ──
            GUILayout.Label(L("◆ 呪文一覧", "◆ Spells", "◆ 咒文一览"), _headerStyle);
            GUILayout.Space(4);

            foreach (var spell in NecromancyManager.SpellUnlocks)
            {
                bool unlocked = mgr.IsUnlocked(spell.Alias);

                GUILayout.BeginHorizontal();

                GUILayout.Label(spell.GetName(), _labelStyle, GUILayout.Width(220));

                if (unlocked)
                {
                    GUILayout.Label(L("✓ 習得済", "✓ Learned", "✓ 已习得"), _goodStyle, GUILayout.Width(90));
                    GUILayout.FlexibleSpace();
                    int stock = GetSpellStock(spell.ElementId);
                    DrawStockBar(stock);
                }
                else if (spell.InitiallyUnlocked)
                {
                    GUILayout.Label(L("✓ 習得済", "✓ Learned", "✓ 已习得"), _goodStyle, GUILayout.Width(90));
                }
                else
                {
                    GUILayout.Label(L("✗ 未習得", "✗ Not Learned", "✗ 未习得"), _badStyle, GUILayout.Width(90));
                    GUILayout.FlexibleSpace();

                    int owned = mgr.CountItemsInInventory(EClass.pc, spell.RequiredSoulId);
                    bool enough = owned >= spell.RequiredSoulCount;
                    string costText = $"{spell.GetSoulName()} x{spell.RequiredSoulCount} ({owned}/{spell.RequiredSoulCount})";
                    GUILayout.Label(costText, enough ? _goodStyle : _badStyle, GUILayout.Width(180));

                    if (GUILayout.Button(L("習得", "Learn", "习得"), _buttonStyle, GUILayout.Width(70)))
                    {
                        if (mgr.TryUnlockWithSouls(spell.Alias))
                        {
                            LangHelper.Say("spellLearned");
                            EClass.pc.pos.PlayEffect("buff");
                        }
                        else
                        {
                            LangHelper.Say("notEnoughSouls");
                        }
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(1);
                GUILayout.Label(spell.GetDesc(), _descStyle);
                GUILayout.Space(8);
            }

        }

        // ── Drawing Helpers ──

        private static void DrawCharaIcon(Rect rect, string? sourceId = null, Chara? chara = null)
        {
            Sprite? sprite = null;
            if (chara != null)
                sprite = chara.GetSprite();
            else if (sourceId != null && EClass.sources.charas.map.TryGetValue(sourceId, out var row))
                sprite = row.GetSprite();

            if (sprite == null || sprite.texture == null) return;

            var tex = sprite.texture;
            var texRect = sprite.textureRect;
            var uvRect = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height
            );
            GUI.DrawTextureWithTexCoords(rect, tex, uvRect);
        }

        private static void DrawDivider()
        {
            if (UiTheme.Ars.UseSimpleDivider)
            {
                var simpleRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
                GUI.DrawTexture(simpleRect, GetSolidTex(new Color(0.66f, 0.64f, 0.61f, 1f)));
                return;
            }

            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            if (_dividerTex == null)
                _dividerTex = MakeTex(1, 1, DividerGold);
            GUI.DrawTexture(rect, _dividerTex);
        }

        private static void DrawStockBar(int stock)
        {
            GUILayout.Label($"{L("ストック", "Stock", "蓄力")}: {stock}", _labelStyle, GUILayout.Width(StockLabelWidth));
            GUILayout.Space(6);
            if (UiTheme.Ars.UseSimpleStockBar) return;

            var barRect = GUILayoutUtility.GetRect(StockBarWidth, 12, GUILayout.Width(StockBarWidth));
            if (_barBgTex == null)
                _barBgTex = MakeTex(1, 1, BarBackground);
            GUI.DrawTexture(barRect, _barBgTex);

            float ratio = Mathf.Clamp01((float)stock / MaxStockDisplay);
            if (ratio > 0)
            {
                Color barColor;
                if (ratio >= 0.5f) barColor = SpectralGreen;
                else if (ratio >= 0.2f) barColor = AmberWarning;
                else barColor = BloodCrimson;

                var fillRect = new Rect(barRect.x, barRect.y, barRect.width * ratio, barRect.height);
                GUI.DrawTexture(fillRect, GetSolidTex(barColor));
            }
        }

        private static string L(string jp, string en, string? cn = null)
        {
            if (Lang.langCode == "CN") return cn ?? en;
            return Lang.isJP ? jp : en;
        }

        /// <summary>Get the creature LV from a corpse (stored int or SourceChara fallback).</summary>
        private static int GetCorpseLv(Thing? corpse)
        {
            if (corpse == null) return 0;
            int lv = corpse.GetInt(NecromancyManager.CorpseLvIntId);
            if (lv > 0) return lv;
            var sourceId = corpse.c_idRefCard;
            if (!string.IsNullOrEmpty(sourceId) && EClass.sources.charas.map.TryGetValue(sourceId, out var row))
                return row.LV;
            return 1;
        }

        private static int GetSpellStock(int elementId)
        {
            var element = EClass.pc?.elements?.GetElement(elementId);
            return element?.vPotential ?? 0;
        }

        // ── Style Initialization ──

        private static void InitStyles()
        {
            if (!NeedsStyleRebuild()) return;

            var theme = UiTheme.Ars;
            bool compatibility = theme.IsCompatibilityMode;
            _font = SkinManager.Instance?.fontSet?.ui?.source?.font;

            RebuildTabNames();

            _windowRect = new Rect(
                (Screen.width - WindowWidth) / 2f,
                (Screen.height - WindowHeight) / 2f,
                WindowWidth, WindowHeight);

            if (compatibility)
            {
                var skin = GUI.skin;
                var compatWindowBg = MakeTex(1, 1, theme.CompatWindowBg);
                var compatScrollBg = MakeTex(1, 1, theme.CompatScrollBg);
                var compatButtonBg = MakeTex(1, 1, new Color(0.87f, 0.86f, 0.85f, 1f));
                var compatButtonHoverBg = MakeTex(1, 1, new Color(0.82f, 0.81f, 0.80f, 1f));
                var compatButtonActiveBg = MakeTex(1, 1, new Color(0.76f, 0.75f, 0.74f, 1f));
                var compatConfirmBg = MakeTex(1, 1, new Color(0.77f, 0.83f, 0.75f, 1f));
                var compatConfirmHoverBg = MakeTex(1, 1, new Color(0.72f, 0.79f, 0.70f, 1f));
                var compatTabActiveBg = MakeTex(1, 1, new Color(0.79f, 0.78f, 0.77f, 1f));

                _boxStyle = new GUIStyle(skin.window)
                {
                    normal = { background = compatWindowBg },
                    onNormal = { background = compatWindowBg },
                    focused = { background = compatWindowBg },
                    onFocused = { background = compatWindowBg },
                    hover = { background = compatWindowBg },
                    active = { background = compatWindowBg },
                    onHover = { background = compatWindowBg },
                    onActive = { background = compatWindowBg },
                    padding = new RectOffset(14, 14, 10, 10)
                };

                _labelStyle = new GUIStyle(skin.label)
                {
                    font = _font ?? skin.label.font,
                    fontSize = 14,
                    wordWrap = false,
                    padding = new RectOffset(2, 2, 2, 2)
                };

                _titleStyle = new GUIStyle(_labelStyle) { fontSize = 20 };
                _headerStyle = new GUIStyle(_labelStyle) { fontSize = 16 };
                _descStyle = new GUIStyle(_labelStyle) { fontSize = 13, wordWrap = true };
                _flavorStyle = new GUIStyle(_labelStyle) { fontSize = 13, fontStyle = FontStyle.Italic, wordWrap = true };
                _goodStyle = new GUIStyle(_labelStyle);
                _badStyle = new GUIStyle(_labelStyle);
                _warningStyle = new GUIStyle(_labelStyle);

                _buttonStyle = new GUIStyle(skin.button)
                {
                    font = _font ?? skin.button.font,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { background = compatButtonBg },
                    onNormal = { background = compatButtonActiveBg },
                    focused = { background = compatButtonBg },
                    onFocused = { background = compatButtonActiveBg },
                    hover = { background = compatButtonHoverBg },
                    onHover = { background = compatButtonActiveBg },
                    active = { background = compatButtonActiveBg },
                    onActive = { background = compatButtonActiveBg },
                    padding = new RectOffset(8, 8, 4, 4),
                    margin = new RectOffset(2, 2, 2, 2)
                };
                _selectedButtonStyle = new GUIStyle(_buttonStyle)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { background = compatTabActiveBg },
                    onNormal = { background = compatTabActiveBg },
                    focused = { background = compatTabActiveBg },
                    onFocused = { background = compatTabActiveBg },
                    hover = { background = compatTabActiveBg },
                    onHover = { background = compatTabActiveBg },
                    active = { background = compatTabActiveBg },
                    onActive = { background = compatTabActiveBg }
                };
                _closeButtonStyle = new GUIStyle(_buttonStyle) { fontSize = 13 };
                _confirmButtonStyle = new GUIStyle(_buttonStyle)
                {
                    normal = { background = compatConfirmBg },
                    onNormal = { background = compatConfirmBg },
                    focused = { background = compatConfirmBg },
                    onFocused = { background = compatConfirmBg },
                    hover = { background = compatConfirmHoverBg },
                    onHover = { background = compatConfirmHoverBg },
                    active = { background = compatButtonActiveBg },
                    onActive = { background = compatButtonActiveBg }
                };
                _confirmLabelStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = 15,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };
                _toolbarStyle = new GUIStyle(_buttonStyle)
                {
                    fontSize = 15,
                    normal = { background = compatButtonBg },
                    onNormal = { background = compatTabActiveBg },
                    focused = { background = compatButtonBg },
                    onFocused = { background = compatTabActiveBg },
                    hover = { background = compatButtonHoverBg },
                    onHover = { background = compatTabActiveBg },
                    active = { background = compatButtonActiveBg },
                    onActive = { background = compatTabActiveBg }
                };
                _scrollStyle = new GUIStyle(skin.box)
                {
                    normal = { background = compatScrollBg },
                    onNormal = { background = compatScrollBg },
                    focused = { background = compatScrollBg },
                    onFocused = { background = compatScrollBg },
                    hover = { background = compatScrollBg },
                    onHover = { background = compatScrollBg },
                    active = { background = compatScrollBg },
                    onActive = { background = compatScrollBg },
                    padding = new RectOffset(8, 8, 6, 6)
                };

                _summaryLabelStyle = new GUIStyle(_labelStyle);
                _summaryValueStyle = new GUIStyle(_labelStyle);
                _summaryMissingStyle = new GUIStyle(_labelStyle) { fontStyle = FontStyle.Italic };
                _smallLabelStyle = new GUIStyle(_labelStyle) { fontSize = 12 };
                _corpseButtonStyle = new GUIStyle(_buttonStyle)
                {
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true
                };
                _journalStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = 14,
                    wordWrap = true,
                    padding = new RectOffset(8, 8, 4, 4),
                    richText = false
                };
                _journalHeaderStyle = new GUIStyle(_headerStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15
                };
                _journalAuthorStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 0, 0)
                };

                // Dark text colors for light background (compatibility mode)
                var lbl = new Color(0.10f, 0.10f, 0.10f);
                var hdr = new Color(0.12f, 0.08f, 0.00f);
                var desc = new Color(0.35f, 0.33f, 0.30f);
                var flavor = new Color(0.38f, 0.28f, 0.50f);
                var good = new Color(0.06f, 0.44f, 0.06f);
                var bad = new Color(0.68f, 0.06f, 0.06f);
                var warn = new Color(0.68f, 0.46f, 0.00f);
                var btn = new Color(0.16f, 0.16f, 0.16f);

                _labelStyle.normal.textColor = lbl;
                _titleStyle.normal.textColor = hdr;
                _headerStyle.normal.textColor = hdr;
                _descStyle.normal.textColor = desc;
                _flavorStyle.normal.textColor = flavor;
                _goodStyle.normal.textColor = good;
                _badStyle.normal.textColor = bad;
                _warningStyle.normal.textColor = warn;

                _buttonStyle.normal.textColor = btn;
                _buttonStyle.hover.textColor = btn;
                _buttonStyle.active.textColor = btn;
                _selectedButtonStyle.normal.textColor = btn;
                _selectedButtonStyle.hover.textColor = btn;
                _selectedButtonStyle.active.textColor = btn;
                _closeButtonStyle.normal.textColor = bad;
                _closeButtonStyle.hover.textColor = bad;
                _confirmButtonStyle.normal.textColor = good;
                _confirmButtonStyle.hover.textColor = good;
                _confirmLabelStyle.normal.textColor = lbl;
                _toolbarStyle.normal.textColor = lbl;
                _toolbarStyle.hover.textColor = btn;
                _toolbarStyle.active.textColor = btn;
                _toolbarStyle.onNormal.textColor = btn;
                _toolbarStyle.onHover.textColor = btn;
                _corpseButtonStyle.normal.textColor = btn;
                _corpseButtonStyle.hover.textColor = btn;

                _summaryLabelStyle.normal.textColor = hdr;
                _summaryValueStyle.normal.textColor = lbl;
                _summaryMissingStyle.normal.textColor = bad;
                _smallLabelStyle.normal.textColor = desc;
                _journalStyle.normal.textColor = lbl;
                _journalHeaderStyle.normal.textColor = hdr;
                _journalAuthorStyle.normal.textColor = desc;

                _styleCompatibilityMode = compatibility;
                _stylesInitialized = true;
                return;
            }

            var bgTex = MakeTex(1, 1, Obsidian);
            var scrollBgTex = MakeTex(1, 1, CharredVellum);
            var btnNormal = MakeTex(1, 1, WornLeather);
            var btnHover = MakeTex(1, 1, WarmLeather);
            var btnActive = MakeTex(1, 1, PressedHide);
            var btnSelected = MakeTex(1, 1, BloodSeal);
            var tabNormal = MakeTex(1, 1, AshenTab);
            var tabActive = MakeTex(1, 1, EmberTab);

            _boxStyle = new GUIStyle
            {
                normal = { background = bgTex },
                onNormal = { background = bgTex },
                focused = { background = bgTex },
                onFocused = { background = bgTex },
                hover = { background = bgTex },
                active = { background = bgTex },
                padding = new RectOffset(14, 14, 10, 10)
            };

            _labelStyle = new GUIStyle
            {
                font = _font,
                fontSize = 14,
                normal = { textColor = Parchment },
                wordWrap = false,
                padding = new RectOffset(2, 2, 2, 2)
            };

            _titleStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 20,
                normal = { textColor = GildedGold },
                padding = new RectOffset(2, 2, 4, 4)
            };

            _headerStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 16,
                normal = { textColor = TarnishedGold }
            };

            _descStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = FadedInk }
            };

            _flavorStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                normal = { textColor = GhostlyViolet }
            };

            _goodStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = SpectralGreen },
                padding = new RectOffset(2, 2, 2, 2)
            };

            _badStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = BloodCrimson },
                padding = new RectOffset(2, 2, 2, 2)
            };

            _warningStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = AmberWarning },
                padding = new RectOffset(2, 2, 2, 2)
            };

            _buttonStyle = new GUIStyle
            {
                font = _font,
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormal, textColor = Parchment },
                hover = { background = btnHover, textColor = Parchment },
                active = { background = btnActive, textColor = Parchment },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            _selectedButtonStyle = new GUIStyle(_buttonStyle)
            {
                normal = { background = btnSelected, textColor = Parchment },
            };

            _closeButtonStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = 13,
                normal = { background = btnNormal, textColor = BloodCrimson },
                hover = { background = btnHover, textColor = BloodCrimson },
                padding = new RectOffset(4, 4, 2, 2)
            };

            _confirmButtonStyle = new GUIStyle(_buttonStyle)
            {
                normal = { background = MakeTex(1, 1, new Color(0.20f, 0.35f, 0.15f, 0.95f)), textColor = Parchment },
                hover = { background = MakeTex(1, 1, new Color(0.25f, 0.45f, 0.18f, 0.95f)), textColor = Parchment },
                active = { background = MakeTex(1, 1, new Color(0.15f, 0.25f, 0.10f, 0.95f)), textColor = Parchment },
            };

            _confirmLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 15,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Parchment }
            };

            _toolbarStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = 15,
                normal = { background = tabNormal, textColor = BoneWhite },
                hover = { background = btnHover, textColor = Parchment },
                active = { background = tabActive, textColor = Parchment },
                onNormal = { background = tabActive, textColor = Parchment },
                onHover = { background = tabActive, textColor = Parchment },
                padding = new RectOffset(12, 12, 5, 5),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _scrollStyle = new GUIStyle
            {
                normal = { background = scrollBgTex },
                onNormal = { background = scrollBgTex },
                focused = { background = scrollBgTex },
                onFocused = { background = scrollBgTex },
                padding = new RectOffset(8, 8, 6, 6)
            };

            _summaryLabelStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = TarnishedGold }
            };

            _summaryValueStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = Parchment }
            };

            _summaryMissingStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = BloodCrimson },
                fontStyle = FontStyle.Italic
            };

            _smallLabelStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 12,
                normal = { textColor = FadedInk }
            };

            _corpseButtonStyle = new GUIStyle(_buttonStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _journalStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 14,
                wordWrap = true,
                padding = new RectOffset(8, 8, 4, 4),
                richText = false
            };

            _journalHeaderStyle = new GUIStyle(_headerStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15
            };

            _journalAuthorStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 0, 0)
            };

            _styleCompatibilityMode = compatibility;
            _stylesInitialized = true;
        }

        private static bool NeedsStyleRebuild()
        {
            if (!_stylesInitialized) return true;
            bool compatibility = UiTheme.Ars.IsCompatibilityMode;
            if (_styleCompatibilityMode != compatibility) return true;
            if (_boxStyle == null || _scrollStyle == null || _buttonStyle == null || _labelStyle == null) return true;
            if (_boxStyle.normal.background == null) return true;
            if (_scrollStyle.normal.background == null) return true;
            if (_buttonStyle.normal.background == null) return true;
            if (compatibility)
            {
                if (_scrollStyle.hover.background == null) return true;
                if (_buttonStyle.hover.background == null) return true;
                if (_toolbarStyle == null || _toolbarStyle.normal.background == null) return true;
                return false;
            }
            return false;
        }

        private static Texture2D GetSolidTex(Color col)
        {
            var c = (Color32)col;
            uint key = ((uint)c.a << 24) | ((uint)c.r << 16) | ((uint)c.g << 8) | c.b;
            if (_solidTexCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var tex = MakeTex(1, 1, col);
            _solidTexCache[key] = tex;
            return tex;
        }

        private static void RebuildTabNames()
        {
            bool showChapter4 = NecromancyManager.Instance.QuestPath.IsStarted;
            if (showChapter4)
            {
                TabNames = new string[4];
                TabNames[0] = L("第壱章 知識", "I · Knowledge", "第壹章 知识");
                TabNames[1] = L("第弐章 儀式", "II · Ritual", "第贰章 仪式");
                TabNames[2] = L("第参章 従者", "III · Servants", "第叁章 仆从");
                TabNames[3] = LangHelper.Get("chapterFourTitle");
            }
            else
            {
                TabNames = new string[3];
                TabNames[0] = L("第壱章 知識", "I · Knowledge", "第壹章 知识");
                TabNames[1] = L("第弐章 儀式", "II · Ritual", "第贰章 仪式");
                TabNames[2] = L("第参章 従者", "III · Servants", "第叁章 仆从");
            }
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
