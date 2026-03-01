using System.Collections.Generic;
using UnityEngine;

namespace Elin_BottleNeckFinder
{
    public static class OverlayRenderer
    {
        private static GUIStyle _boxStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _warningStyle;
        private static GUIStyle _methodStyle;
        private static GUIStyle _ownerStyle;
        private static GUIStyle _buttonStyle;
        private static bool _stylesInitialized;
        private static Font _overlayFont;

        private static float _exportMessageTimer;
        private static string _exportMessage;

        private static readonly Color ColorGreen = new Color(0.4f, 1f, 0.4f);
        private static readonly Color ColorYellow = new Color(1f, 1f, 0.3f);
        private static readonly Color ColorRed = new Color(1f, 0.3f, 0.3f);
        private static readonly Color ColorWhite = Color.white;
        private static readonly Color ColorDimWhite = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color ColorBar = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        private static Texture2D _barBgTex;
        private static Texture2D _barFillTex;

        private const float PanelWidth = 620;
        private const int NameMaxChars = 24;
        private const float MethodIndent = 20;
        private const float OwnerIndent = 36;

        /// <returns>true if profiler toggle was requested</returns>
        public static bool Draw(float fps, float frameMs,
            IReadOnlyList<ProfilingData.ModProfile> ranking,
            IReadOnlyList<ErrorMonitor.ErrorEntry> errors,
            int totalPatches, int patchFailures,
            bool profilerActive, int topCount)
        {
            bool toggleProfiler = false;
            InitStyles();

            float x = 10;
            float y = 10;

            float height = CalculateHeight(ranking, errors, topCount, profilerActive);
            GUI.Box(new Rect(x, y, PanelWidth, height), GUIContent.none, _boxStyle);

            float cy = y + 8;
            float cx = x + 12;
            float cw = PanelWidth - 24;

            // Title
            GUI.Label(new Rect(cx, cy, cw, 20),
                "BottleNeckFinder", _headerStyle);
            cy += 24;

            // FPS
            Color fpsColor = GetFpsColor(fps);
            DrawColoredLabel(cx, cy, cw,
                $"FPS: {fps:F0} | Frame: {frameMs:F1}ms", fpsColor);
            cy += 20;

            // Patch status
            GUI.Label(new Rect(cx, cy, cw, 20),
                $"Patches: {totalPatches} applied, {patchFailures} failed",
                patchFailures > 0 ? _warningStyle : _labelStyle);
            cy += 24;

            // Profiler toggle button
            string profLabel = profilerActive ? "Stop Profiler" : "Start Profiler";
            if (GUI.Button(new Rect(cx, cy, 160, 24), profLabel, _buttonStyle))
                toggleProfiler = true;
            cy += 32;

            // --- Expanded section (only when profiler is active) ---
            if (profilerActive)
            {
                // Mod ranking
                if (ranking != null && ranking.Count > 0)
                {
                    GUI.Label(new Rect(cx, cy, cw, 20),
                        "Mod Load Ranking", _headerStyle);
                    cy += 22;

                    double maxMs = ranking[0].AvgMs;
                    if (maxMs < 0.01) maxMs = 0.01;

                    float nameWidth = 180;
                    float barStart = cx + nameWidth + 4;
                    float msWidth = 130;
                    float barMaxWidth = cw - nameWidth - msWidth - 10;
                    float msLabelX = cx + cw - msWidth;

                    int count = Mathf.Min(topCount, ranking.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var mod = ranking[i];
                        float barWidth = (float)(mod.AvgMs / maxMs) * barMaxWidth;
                        Color barColor = GetLoadColor(mod.AvgMs);

                        // Bar background
                        GUI.DrawTexture(
                            new Rect(barStart, cy + 2, barMaxWidth, 14), _barBgTex);
                        // Bar fill
                        if (barWidth > 0)
                        {
                            var prevColor = GUI.color;
                            GUI.color = barColor;
                            GUI.DrawTexture(
                                new Rect(barStart, cy + 2, barWidth, 14), _barFillTex);
                            GUI.color = prevColor;
                        }

                        GUI.Label(new Rect(cx, cy, nameWidth, 18),
                            $"{i + 1}. {TruncName(mod.ModName, NameMaxChars)}", _labelStyle);
                        GUI.Label(new Rect(msLabelX, cy, msWidth, 18),
                            $"avg:{mod.AvgMs:F2} peak:{mod.PeakMs:F1}ms", _labelStyle);
                        cy += 18;

                        // Top methods breakdown
                        if (mod.TopMethods != null)
                        {
                            float methodWidth = cw - MethodIndent;
                            float ownerWidth = cw - OwnerIndent;

                            foreach (var method in mod.TopMethods)
                            {
                                if (method.AvgMs < 0.001) continue;

                                string methodText = FormatMethodLine(method);
                                float mh = _methodStyle.CalcHeight(
                                    new GUIContent(methodText), methodWidth);
                                GUI.Label(new Rect(cx + MethodIndent, cy, methodWidth, mh),
                                    methodText, _methodStyle);
                                cy += mh + 1;

                                if (method.PatchOwnerNames != null && method.PatchOwnerNames.Count > 0)
                                {
                                    string ownerText = "<- " + string.Join(", ", method.PatchOwnerNames);
                                    float oh = _ownerStyle.CalcHeight(
                                        new GUIContent(ownerText), ownerWidth);
                                    GUI.Label(new Rect(cx + OwnerIndent, cy, ownerWidth, oh),
                                        ownerText, _ownerStyle);
                                    cy += oh + 1;
                                }
                            }
                        }
                        cy += 2;
                    }
                    cy += 4;
                }

                // Errors
                if (errors != null && errors.Count > 0)
                {
                    GUI.Label(new Rect(cx, cy, cw, 20),
                        $"Recent Errors ({errors.Count})", _headerStyle);
                    cy += 22;

                    int maxErr = ModConfig.MaxErrorHistory.Value;
                    int errCount = Mathf.Min(maxErr, errors.Count);
                    for (int i = errors.Count - errCount; i < errors.Count; i++)
                    {
                        var err = errors[i];
                        string mod = err.ModName ?? "Unknown";
                        string prefix = err.IsPatchFailure ? "[PATCH] " : "";
                        DrawColoredLabel(cx, cy, cw,
                            $"{prefix}{mod}: {err.Summary}",
                            err.IsPatchFailure ? ColorYellow : ColorRed);
                        cy += 16;
                    }
                    cy += 4;
                }

                // Export button
                cy += 4;
                if (GUI.Button(new Rect(cx, cy, 160, 24), "Export Report", _buttonStyle))
                {
                    if (ProfileExporter.Export(fps, frameMs))
                    {
                        _exportMessage = "Exported: " + ProfileExporter.LastExportPath;
                        _exportMessageTimer = 5f;
                    }
                    else
                    {
                        _exportMessage = "Export failed! Check log.";
                        _exportMessageTimer = 5f;
                    }
                }
                cy += 28;

                // Export status message
                if (_exportMessageTimer > 0)
                {
                    _exportMessageTimer -= Time.unscaledDeltaTime;
                    DrawColoredLabel(cx, cy, cw, _exportMessage, ColorGreen);
                }
            }

            return toggleProfiler;
        }

        private static float CalculateHeight(
            IReadOnlyList<ProfilingData.ModProfile> ranking,
            IReadOnlyList<ErrorMonitor.ErrorEntry> errors,
            int topCount, bool profilerActive)
        {
            // Compact: title + FPS + patches + button + padding
            float h = 108;

            if (!profilerActive) return h;

            // Expanded sections
            float methodWidth = PanelWidth - 24 - MethodIndent;
            float ownerWidth = PanelWidth - 24 - OwnerIndent;

            if (ranking != null && ranking.Count > 0)
            {
                int count = Mathf.Min(topCount, ranking.Count);
                h += 22; // header
                for (int i = 0; i < count; i++)
                {
                    h += 18; // mod row
                    var mod = ranking[i];
                    if (mod.TopMethods != null)
                    {
                        foreach (var m in mod.TopMethods)
                        {
                            if (m.AvgMs >= 0.001)
                            {
                                string methodText = FormatMethodLine(m);
                                h += _methodStyle.CalcHeight(
                                    new GUIContent(methodText), methodWidth) + 1;
                                if (m.PatchOwnerNames != null && m.PatchOwnerNames.Count > 0)
                                {
                                    string ownerText = "<- " + string.Join(", ", m.PatchOwnerNames);
                                    h += _ownerStyle.CalcHeight(
                                        new GUIContent(ownerText), ownerWidth) + 1;
                                }
                            }
                        }
                    }
                    h += 2; // spacing
                }
                h += 4;
            }
            if (errors != null && errors.Count > 0)
                h += 22 + Mathf.Min(ModConfig.MaxErrorHistory.Value, errors.Count) * 16 + 4;
            h += 36; // export button
            if (_exportMessageTimer > 0) h += 18;
            return h;
        }

        private static int GetTargetFps()
        {
            int target = Application.targetFrameRate;
            if (target > 0) return target;
            // VSync enabled: use screen refresh rate divided by vSyncCount
            int vsync = QualitySettings.vSyncCount;
            if (vsync > 0)
            {
                int refresh = Screen.currentResolution.refreshRate;
                return refresh > 0 ? refresh / vsync : 60;
            }
            return 60;
        }

        private static Color GetFpsColor(float fps)
        {
            int target = GetTargetFps();
            if (fps >= target * 0.9f) return ColorGreen;
            if (fps >= target * 0.5f) return ColorYellow;
            return ColorRed;
        }

        private static void DrawColoredLabel(
            float x, float y, float w, string text, Color color)
        {
            var prev = _labelStyle.normal.textColor;
            _labelStyle.normal.textColor = color;
            GUI.Label(new Rect(x, y, w, 18), text, _labelStyle);
            _labelStyle.normal.textColor = prev;
        }

        private static string FormatMethodLine(ProfilingData.MethodProfile method)
        {
            string baseModStr;
            if (method.PatchOwnerNames == null || method.PatchOwnerNames.Count == 0)
                baseModStr = "(vanilla)";
            else
                baseModStr = $"(base:{method.AvgBaseMs:F2} mod:{method.AvgModMs:F2})";

            string callStr = method.AvgCallCount > 0
                ? $" x{method.AvgCallCount}" : "";

            string peakStr = method.PeakMs >= 0.1
                ? $" peak:{method.PeakMs:F1}" : "";

            return $"{method.MethodName}  {method.AvgMs:F2}ms{peakStr} {baseModStr}{callStr}";
        }

        private static Color GetLoadColor(double ms)
        {
            if (ms < 1.0) return ColorGreen;
            if (ms < 5.0) return ColorYellow;
            return ColorRed;
        }

        private static string TruncName(string name, int max)
        {
            if (name.Length <= max) return name;
            return name.Substring(0, max - 2) + "..";
        }

        private static void InitStyles()
        {
            // Re-create if font was destroyed by Resources.UnloadUnusedAssets()
            if (_stylesInitialized && _overlayFont != null) return;

            _overlayFont = Font.CreateDynamicFontFromOSFont("Consolas", 13)
                        ?? Font.CreateDynamicFontFromOSFont("Arial", 13);
            if (_overlayFont != null)
                _overlayFont.hideFlags = HideFlags.HideAndDontSave;

            var bgTex = MakeTex(1, 1, new Color(0, 0, 0, 0.75f));
            var btnNormalTex = MakeTex(1, 1, new Color(0.25f, 0.25f, 0.25f, 0.9f));
            var btnHoverTex = MakeTex(1, 1, new Color(0.35f, 0.35f, 0.35f, 0.9f));
            var btnActiveTex = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f, 0.9f));

            _boxStyle = new GUIStyle
            {
                normal = { background = bgTex }
            };

            _labelStyle = new GUIStyle
            {
                font = _overlayFont,
                fontSize = 13,
                normal = { textColor = ColorWhite },
                wordWrap = false
            };

            _headerStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            _warningStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = ColorYellow }
            };

            _methodStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = ColorDimWhite }
            };

            _ownerStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = ColorDimWhite }
            };

            _buttonStyle = new GUIStyle
            {
                font = _overlayFont,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = ColorWhite },
                hover = { background = btnHoverTex, textColor = ColorWhite },
                active = { background = btnActiveTex, textColor = ColorWhite },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 4, 4)
            };

            _barBgTex = MakeTex(1, 1, ColorBar);
            _barFillTex = MakeTex(1, 1, Color.white);

            _stylesInitialized = true;
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
