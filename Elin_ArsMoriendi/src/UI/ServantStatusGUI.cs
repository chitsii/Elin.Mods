using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// IMGUI HUD widget showing alive servants' HP/MP/SP.
    /// Two-row layout per servant: icon spans both rows.
    /// Row 1: [Icon 40] Name                           Lv.xx
    /// Row 2: [     ] [HP bar] [MP bar] [SP bar]
    /// </summary>
    public static class ServantStatusGUI
    {
        private const int WindowId = 92711;
        private const float RefreshInterval = 0.5f;
        private const int MaxDisplayCount = 10;
        // Layout dimensions
        private const float IconSize = 48f;
        private const float HpBarWidth = 44f;
        private const float SubBarWidth = 32f;
        private const float BarHeight = 6f;
        private const float BarGap = 3f;
        private static float StatWidth = 48f;
        private const float StatGap = 2f;
        private static float NameRowHeight = 14f;
        private static float BarRowHeight = 12f;
        private const float CharGap = 6f;
        private static float LevelWidth = 44f;
        private static float SummaryRowHeight = 14f;
        private const float NameRowIndent = IconSize + 4f;
        private static float ContentWidth => NameRowIndent
            + HpBarWidth + StatGap + StatWidth + BarGap
            + SubBarWidth + StatGap + StatWidth + BarGap
            + SubBarWidth + StatGap + StatWidth;

        private static Rect _windowRect = new(10, 300, 0, 0);
        private static readonly List<Chara> _servants = new();
        private static float _lastRefresh;
        private static int _summaryTotal;
        private static int _summaryActive;
        private static int _summaryStashed;
        private static int _summaryDead;

        // Cached per-servant data (rebuilt every RefreshInterval)
        private static readonly List<BarCache> _barCache = new();

        // Styles & textures (lazy-init)
        private static bool _stylesInitialized;
        private static GUIStyle _bgStyle = null!;
        private static GUIStyle _nameStyle = null!;
        private static GUIStyle _levelStyle = null!;
        private static GUIStyle _statStyle = null!;
        private static GUIStyle _summaryStyle = null!;
        private static Texture2D _barBgTex = null!;
        private static Texture2D _barOutlineTex = null!;
        private static Texture2D _separatorTex = null!;
        private static Texture2D _borderTex = null!;
        private static Texture2D _iconBgTex = null!;
        private static bool _styleCompatibilityMode;
        private static int _currentFontPreset = -1;

        // Inline settings panel
        private static bool _showSettings;
        private static GUIStyle _gearBtnStyle = null!;
        private static GUIStyle _settingsBtnStyle = null!;
        private static GUIStyle _settingsLabelStyle = null!;

        // Colors — high-contrast for HUD
        private static readonly Color BgColor = new(0.06f, 0.05f, 0.04f, 0.70f);
        private static readonly Color BorderColor = new(0.40f, 0.32f, 0.18f, 0.50f);
        private static readonly Color SeparatorColor = new(0.30f, 0.25f, 0.15f, 0.25f);
        private static readonly Color BarBg = new(0.12f, 0.10f, 0.07f, 0.90f);
        private static readonly Color BarOutline = new(0.25f, 0.20f, 0.14f, 0.60f);
        private static readonly Color NameColor = new(0.88f, 0.82f, 0.68f);
        // Bar fills
        private static readonly Color HpFull = new(0.30f, 0.85f, 0.40f);
        private static readonly Color HpLow = new(0.90f, 0.20f, 0.15f);
        private static readonly Color MpBlue = new(0.35f, 0.55f, 0.92f);
        private static readonly Color SpAmber = new(0.92f, 0.75f, 0.28f);
        private static readonly Color StatTextColor = new(0.70f, 0.65f, 0.55f);

        public static void Draw()
        {
            if (!ModConfig.ShowServantWidget.Value) return;
            RefreshCache();
            if (_summaryTotal == 0) return;
            InitStyles();
            var prevGuiColor = GUI.color;
            var prevBgColor = GUI.backgroundColor;
            var prevContentColor = GUI.contentColor;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            try
            {
                _windowRect.width = ContentWidth + 12f;
                float servantH = NameRowHeight + BarRowHeight;
                float bodyH = servantH * _servants.Count
                    + CharGap * Math.Max(0, _servants.Count - 1);
                float summaryH = SummaryRowHeight + (_servants.Count > 0 ? CharGap : 0f);
                float settingsH = _showSettings ? SummaryRowHeight + CharGap : 0f;
                _windowRect.height = summaryH + settingsH + bodyH + 10f;
                _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "", _bgStyle);
                DrawBorder(_windowRect);

                // Prevent click-through: block game input when mouse is over the widget
                if (_windowRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown
                        || Event.current.type == EventType.MouseUp
                        || Event.current.type == EventType.ScrollWheel)
                    {
                        Event.current.Use();
                    }
                    EInput.Consume(true, 2);
                }
            }
            finally
            {
                GUI.color = prevGuiColor;
                GUI.backgroundColor = prevBgColor;
                GUI.contentColor = prevContentColor;
            }
        }

        private static void DrawBorder(Rect r)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), _borderTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1, r.width, 1), _borderTex);
            GUI.DrawTexture(new Rect(r.x, r.y, 1, r.height), _borderTex);
            GUI.DrawTexture(new Rect(r.x + r.width - 1, r.y, 1, r.height), _borderTex);
        }

        private static void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(SummaryRowHeight));
            GUILayout.Label(BuildSummaryText(), _summaryStyle,
                GUILayout.ExpandWidth(true), GUILayout.Height(SummaryRowHeight));
            string settingsBtnText = Lang.isJP ? "[\u8a2d\u5b9a]"
                : Lang.langCode == "CN" ? "[\u8bbe\u7f6e]" : "[Cfg]";
            if (GUILayout.Button(settingsBtnText, _gearBtnStyle,
                GUILayout.Height(SummaryRowHeight)))
                _showSettings = !_showSettings;
            GUILayout.EndHorizontal();

            if (_showSettings)
            {
                DrawSeparator();
                DrawSettingsRow();
            }

            if (_servants.Count > 0)
                DrawSeparator();

            for (int i = 0; i < _servants.Count; i++)
            {
                if (i > 0)
                    DrawSeparator();
                DrawServantRow(i);
            }

            GUI.DragWindow();
        }

        private static void DrawSeparator()
        {
            GUILayout.Space(CharGap * 0.5f - 0.5f);
            var rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUI.DrawTexture(rect, _separatorTex);
            GUILayout.Space(CharGap * 0.5f - 0.5f);
        }

        private static void DrawServantRow(int index)
        {
            var cache = _barCache[index];
            var servant = _servants[index];

            // Sentinel to capture Y position before rows
            var sentinel = GUILayoutUtility.GetRect(0f, 0f);

            // Row 1: Indent + Name (left) + Level (right)
            GUILayout.BeginHorizontal(GUILayout.Height(NameRowHeight));
            GUILayout.Space(NameRowIndent);
            GUILayout.Label(cache.Name, _nameStyle,
                GUILayout.ExpandWidth(true), GUILayout.Height(NameRowHeight));
            GUILayout.Label(cache.LevelText, _levelStyle,
                GUILayout.Width(LevelWidth), GUILayout.Height(NameRowHeight));
            GUILayout.EndHorizontal();

            // Row 2: Indent + HP/MP/SP bars
            GUILayout.BeginHorizontal(GUILayout.Height(BarRowHeight));
            GUILayout.Space(NameRowIndent);
            DrawInlineBar(HpBarWidth, cache.HpRatio, cache.HpColor);
            GUILayout.Space(StatGap);
            GUILayout.Label(cache.HpText, _statStyle, GUILayout.Width(StatWidth), GUILayout.Height(BarRowHeight));
            GUILayout.Space(BarGap);
            DrawInlineBar(SubBarWidth, cache.MpRatio, MpBlue);
            GUILayout.Space(StatGap);
            GUILayout.Label(cache.MpText, _statStyle, GUILayout.Width(StatWidth), GUILayout.Height(BarRowHeight));
            GUILayout.Space(BarGap);
            DrawInlineBar(SubBarWidth, cache.SpRatio, SpAmber);
            GUILayout.Space(StatGap);
            GUILayout.Label(cache.SpText, _statStyle, GUILayout.Width(StatWidth), GUILayout.Height(BarRowHeight));
            GUILayout.EndHorizontal();

            // Draw icon spanning both rows
            float rowsHeight = NameRowHeight + BarRowHeight;
            var iconRect = new Rect(sentinel.x, sentinel.y, IconSize, rowsHeight);
            GUI.DrawTexture(iconRect, _iconBgTex);
            DrawCharaSprite(iconRect, servant);
        }

        private static void DrawInlineBar(float width, float ratio, Color color)
        {
            var rect = GUILayoutUtility.GetRect(width, BarRowHeight);
            // Vertically center the bar
            var barRect = new Rect(rect.x, rect.y + (BarRowHeight - BarHeight) * 0.5f, width, BarHeight);
            GUI.DrawTexture(barRect, _barOutlineTex);
            var inner = new Rect(barRect.x + 1, barRect.y + 1, barRect.width - 2, barRect.height - 2);
            GUI.DrawTexture(inner, _barBgTex);
            if (ratio > 0f)
            {
                var fill = new Rect(inner.x, inner.y, inner.width * ratio, inner.height);
                var prev = GUI.color;
                GUI.color = _styleCompatibilityMode ? UiTheme.ServantWidget.BarFillColor : color;
                GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.color = prev;
            }
        }

        private static void DrawCharaSprite(Rect box, Chara chara)
        {
            var sprite = chara.GetSprite();
            if (sprite == null || sprite.texture == null) return;

            var tex = sprite.texture;
            var texRect = sprite.textureRect;

            float spriteW = texRect.width;
            float spriteH = texRect.height;
            float scale = Mathf.Min(box.width / spriteW, box.height / spriteH);
            float drawW = spriteW * scale;
            float drawH = spriteH * scale;

            var drawRect = new Rect(
                box.x + (box.width - drawW) * 0.5f,
                box.y + (box.height - drawH) * 0.5f,
                drawW, drawH);

            var uvRect = new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(drawRect, tex, uvRect);
        }

        private static bool _tintChecked;

        private static void RefreshCache()
        {
            if (Time.time - _lastRefresh < RefreshInterval) return;
            _lastRefresh = Time.time;

            var mgr = NecromancyManager.Instance;
            if (mgr == null || EClass.game?.cards?.globalCharas == null)
            {
                _servants.Clear();
                _barCache.Clear();
                _summaryTotal = 0;
                _summaryActive = 0;
                _summaryStashed = 0;
                _summaryDead = 0;
                _tintChecked = false;
                return;
            }

            // One-time tint fix for pre-tint servants (runs once per session)
            if (!_tintChecked)
            {
                mgr.RefreshServantVisuals();
                _tintChecked = true;
            }

            _servants.Clear();
            _summaryTotal = 0;
            _summaryActive = 0;
            _summaryStashed = 0;
            _summaryDead = 0;
            var all = mgr.GetAllServants();
            _summaryTotal = all.Count;
            foreach (var (chara, isAlive) in all)
            {
                if (!isAlive)
                {
                    _summaryDead++;
                    continue;
                }

                if (mgr.IsServantStashed(chara.uid))
                {
                    _summaryStashed++;
                    continue;
                }

                _summaryActive++;
                _servants.Add(chara);
            }

            _servants.Sort((a, b) =>
            {
                int lv = b.LV.CompareTo(a.LV);
                if (lv != 0) return lv;
                return a.uid.CompareTo(b.uid);
            });
            if (_servants.Count > MaxDisplayCount)
                _servants.RemoveRange(MaxDisplayCount, _servants.Count - MaxDisplayCount);

            _barCache.Clear();
            foreach (var c in _servants)
            {
                float hpRatio = c.MaxHP > 0 ? Mathf.Clamp01((float)c.hp / c.MaxHP) : 0f;
                float mpRatio = c.mana.max > 0 ? Mathf.Clamp01((float)c.mana.value / c.mana.max) : 0f;
                float spRatio = c.stamina.max > 0 ? Mathf.Clamp01((float)c.stamina.value / c.stamina.max) : 0f;

                _barCache.Add(new BarCache
                {
                    Name = c.NameSimple,
                    LevelText = $"Lv.{c.LV}",
                    HpRatio = hpRatio,
                    MpRatio = mpRatio,
                    SpRatio = spRatio,
                    HpColor = UiTheme.ServantWidget.UseMonochromeBars
                        ? UiTheme.ServantWidget.BarFillColor
                        : Color.Lerp(HpLow, HpFull, hpRatio),
                    HpText = FormatStat(c.hp, c.MaxHP),
                    MpText = FormatStat(c.mana.value, c.mana.max),
                    SpText = FormatStat(c.stamina.value, c.stamina.max),
                });
            }
        }

        private static void InitStyles()
        {
            if (!NeedsStyleRebuild()) return;

            int preset = Mathf.Clamp(ModConfig.WidgetFontScale?.Value ?? 0, 0, 2);
            _currentFontPreset = preset;
            ApplyFontPreset(preset);
            int nameFontSize = preset switch { 1 => 13, 2 => 15, _ => 11 };
            int levelFontSize = preset switch { 1 => 12, 2 => 14, _ => 10 };
            int statFontSize = preset switch { 1 => 11, 2 => 13, _ => 9 };
            int summaryFontSize = preset switch { 1 => 12, 2 => 14, _ => 10 };

            var theme = UiTheme.ServantWidget;
            bool compatibility = theme.IsCompatibilityMode;
            Color bgColor = theme.BgColor;
            Color barBgColor = theme.BarBgColor;
            Color barOutlineColor = theme.BarOutlineColor;
            Color separatorColor = theme.SeparatorColor;
            Color borderColor = theme.BorderColor;
            Color nameColor = theme.NameColor;
            Color statTextColor = theme.StatTextColor;

            var bgTex = MakeTex(1, 1, bgColor);
            _barBgTex = MakeTex(1, 1, barBgColor);
            _barOutlineTex = MakeTex(1, 1, barOutlineColor);
            _separatorTex = MakeTex(1, 1, separatorColor);
            _borderTex = MakeTex(1, 1, borderColor);
            _iconBgTex = MakeTex(1, 1, compatibility
                ? new Color(0.80f, 0.79f, 0.78f, 0.90f)
                : new Color(0.18f, 0.16f, 0.14f, 0.80f));

            _bgStyle = new GUIStyle
            {
                normal = { background = bgTex },
                onNormal = { background = bgTex },
                focused = { background = bgTex },
                onFocused = { background = bgTex },
                hover = { background = bgTex },
                active = { background = bgTex },
                padding = new RectOffset(5, 5, 4, 4)
            };

            var widgetFont = SkinManager.Instance?.fontSet?.widget?.source?.font;

            _nameStyle = new GUIStyle
            {
                font = widgetFont,
                fontSize = nameFontSize,
                                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = nameColor },
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _statStyle = new GUIStyle
            {
                font = widgetFont,
                fontSize = statFontSize,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = statTextColor },
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _levelStyle = new GUIStyle
            {
                font = widgetFont,
                fontSize = levelFontSize,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = statTextColor },
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _summaryStyle = new GUIStyle
            {
                font = widgetFont,
                fontSize = summaryFontSize,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = nameColor },
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };

            var transparentTex = MakeTex(1, 1, Color.clear);
            _gearBtnStyle = new GUIStyle
            {
                font = widgetFont,
                fontSize = summaryFontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = statTextColor, background = transparentTex },
                hover = { textColor = nameColor, background = transparentTex },
                active = { textColor = nameColor, background = transparentTex },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            _settingsBtnStyle = new GUIStyle
            {
                font = widgetFont,
                fontSize = statFontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = statTextColor, background = transparentTex },
                hover = { textColor = nameColor, background = transparentTex },
                active = { textColor = nameColor, background = transparentTex },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            _settingsLabelStyle = new GUIStyle
            {
                font = widgetFont,
                fontSize = statFontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = nameColor },
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _styleCompatibilityMode = compatibility;
            _stylesInitialized = true;
        }

        private static bool NeedsStyleRebuild()
        {
            if (!_stylesInitialized) return true;
            if (_currentFontPreset != (ModConfig.WidgetFontScale?.Value ?? 0)) return true;
            if (_styleCompatibilityMode != UiTheme.ServantWidget.IsCompatibilityMode) return true;
            if (_bgStyle == null || _nameStyle == null || _levelStyle == null || _statStyle == null || _summaryStyle == null
                || _gearBtnStyle == null || _settingsBtnStyle == null || _settingsLabelStyle == null) return true;
            if (_barBgTex == null || _barOutlineTex == null || _separatorTex == null || _borderTex == null || _iconBgTex == null) return true;
            if (_bgStyle.normal.background == null) return true;
            return false;
        }

        private static readonly string[] PresetLabelsJP = { "\u5c0f", "\u4e2d", "\u5927" };
        private static readonly string[] PresetLabelsEN = { "S", "M", "L" };
        private static readonly string[] PresetLabelsCN = { "\u5c0f", "\u4e2d", "\u5927" };

        private static string GetPresetLabel(int preset)
        {
            var labels = Lang.isJP ? PresetLabelsJP
                : Lang.langCode == "CN" ? PresetLabelsCN
                : PresetLabelsEN;
            return labels[Mathf.Clamp(preset, 0, 2)];
        }

        private static string GetFontSizeLabel()
        {
            if (Lang.isJP) return "\u6587\u5b57";
            if (Lang.langCode == "CN") return "\u5b57\u4f53";
            return "Font";
        }

        private static void DrawSettingsRow()
        {
            int current = Mathf.Clamp(ModConfig.WidgetFontScale?.Value ?? 0, 0, 2);
            GUILayout.BeginHorizontal(GUILayout.Height(SummaryRowHeight));
            GUILayout.Label(GetFontSizeLabel(), _settingsLabelStyle,
                GUILayout.Height(SummaryRowHeight));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("\u25c0", _settingsBtnStyle,
                GUILayout.Width(SummaryRowHeight), GUILayout.Height(SummaryRowHeight)))
            {
                int next = (current + 2) % 3;
                ModConfig.WidgetFontScale.Value = next;
            }
            GUILayout.Label(GetPresetLabel(current), _settingsLabelStyle,
                GUILayout.Width(24f), GUILayout.Height(SummaryRowHeight));
            if (GUILayout.Button("\u25b6", _settingsBtnStyle,
                GUILayout.Width(SummaryRowHeight), GUILayout.Height(SummaryRowHeight)))
            {
                int next = (current + 1) % 3;
                ModConfig.WidgetFontScale.Value = next;
            }
            GUILayout.EndHorizontal();
        }

        private static void ApplyFontPreset(int preset)
        {
            switch (preset)
            {
                case 1: // Medium
                    NameRowHeight = 18f; BarRowHeight = 15f; StatWidth = 56f;
                    SummaryRowHeight = 18f; LevelWidth = 52f;
                    break;
                case 2: // Large
                    NameRowHeight = 22f; BarRowHeight = 18f; StatWidth = 64f;
                    SummaryRowHeight = 22f; LevelWidth = 60f;
                    break;
                default: // Small (current)
                    NameRowHeight = 14f; BarRowHeight = 12f; StatWidth = 48f;
                    SummaryRowHeight = 14f; LevelWidth = 44f;
                    break;
            }
        }

        private static string BuildSummaryText()
        {
            int shown = _servants.Count;
            if (Lang.isJP)
                return $"従者 {_summaryTotal}  稼働 {_summaryActive}  退避 {_summaryStashed}  死 {_summaryDead}  表示 {shown}/{MaxDisplayCount}";
            if (Lang.langCode == "CN")
                return $"仆从 {_summaryTotal}  出战 {_summaryActive}  退避 {_summaryStashed}  死亡 {_summaryDead}  显示 {shown}/{MaxDisplayCount}";
            return $"Servants {_summaryTotal}  Active {_summaryActive}  Stashed {_summaryStashed}  Dead {_summaryDead}  Shown {shown}/{MaxDisplayCount}";
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

        private static string FormatValue(int v)
        {
            if (v < 0) v = 0;
            if (v < 10000) return v.ToString();
            if (v < 10000000) return $"{v / 1000}K";
            return $"{v / 1000000}M";
        }

        private static string FormatStat(int cur, int max)
        {
            return $"{FormatValue(cur)}/{FormatValue(max)}";
        }

        private struct BarCache
        {
            public string Name;
            public string LevelText;
            public float HpRatio, MpRatio, SpRatio;
            public Color HpColor;
            public string HpText, MpText, SpText;
        }
    }
}

