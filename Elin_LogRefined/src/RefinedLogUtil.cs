using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Elin_LogRefined
{
    public enum LogType { Damage, Heal, Debuff, Buff, Commentary }

    public static class RefinedLogUtil
    {
        private const string OverlayName = "LogRefined_TintOverlay";

        private static Sprite _gradientSprite;
        private static Dictionary<int, Color> _remapLookup;
        private static GameColorRemap _cachedRemap;

        private static readonly Dictionary<int, string> _hexCache = new Dictionary<int, string>();

        private static MsgBlock _cachedBlock;
        private static Image _cachedOverlay;

        public static string Colorize(string text, Color color)
        {
            int key = ColorKey(color);
            if (!_hexCache.TryGetValue(key, out string hex))
            {
                hex = ColorUtility.ToHtmlStringRGB(color);
                _hexCache[key] = hex;
            }
            return $"<color=#{hex}>{text}</color>";
        }

        public static Color GetTextColor(LogType type)
        {
            var theme = GetCurrentTheme();

            Color vanillaFallback;
            switch (type)
            {
                case LogType.Damage:     vanillaFallback = Msg.colors.Negative; break;
                case LogType.Heal:       vanillaFallback = Msg.colors.MutateGood; break;
                case LogType.Debuff:     vanillaFallback = Msg.colors.Negative; break;
                case LogType.Buff:       vanillaFallback = Msg.colors.MutateGood; break;
                case LogType.Commentary: vanillaFallback = Msg.colors.Talk; break;
                default:                 vanillaFallback = Color.white; break;
            }

            return theme.GetTextColor(type, vanillaFallback);
        }

        public static Color? GetSurfaceColor()
        {
            return GetCurrentTheme().SurfaceColor;
        }

        public static bool IsLightTheme()
        {
            return GetCurrentTheme().IsLightTheme;
        }

        public static Color? RemapColor(Color gameColor)
        {
            var theme = GetCurrentTheme();
            var remap = theme.ColorRemap;
            if (remap == null) return null;

            if (_cachedRemap != remap)
            {
                _cachedRemap = remap;
                _remapLookup = BuildRemapLookup(remap);
                Debug.Log($"[LogRefined] RemapColor: rebuilt lookup, theme={ModConfig.Theme.Value}, keys={_remapLookup.Count}, remap.Default={remap.Default}");
            }

            int key = ColorKey(gameColor);
            Color result;
            if (_remapLookup.TryGetValue(key, out result))
                return result;
            return null;
        }

        private static int ColorKey(Color c)
        {
            Color32 c32 = c;
            return (c32.r << 24) | (c32.g << 16) | (c32.b << 8) | c32.a;
        }

        private static Dictionary<int, Color> BuildRemapLookup(GameColorRemap remap)
        {
            var lookup = new Dictionary<int, Color>();
            var mc = Msg.colors;

            // Named colors
            lookup[ColorKey(mc.Default)]    = remap.Default;
            lookup[ColorKey(mc.Talk)]       = remap.Talk;
            lookup[ColorKey(mc.TalkGod)]    = remap.TalkGod;
            lookup[ColorKey(mc.Ding)]       = remap.Ding;
            lookup[ColorKey(mc.Ono)]        = remap.Ono;
            lookup[ColorKey(mc.Negative)]   = remap.Negative;
            lookup[ColorKey(mc.Thinking)]   = remap.Thinking;
            lookup[ColorKey(mc.MutateGood)] = remap.MutateGood;
            lookup[ColorKey(mc.MutateBad)]  = remap.MutateBad;

            // Protect named color keys from being overwritten by dict overrides
            var namedKeys = new HashSet<int>(lookup.Keys);

            // Dict colors
            if (mc.colors != null)
            {
                foreach (var kvp in mc.colors)
                {
                    Color dictColor = kvp.Value;
                    int dictKey = ColorKey(dictColor);

                    // Skip if this Color32 belongs to a named color
                    if (namedKeys.Contains(dictKey))
                        continue;

                    // DictOverrides take priority for non-named colors
                    if (remap.DictOverrides != null)
                    {
                        Color overrideColor;
                        if (remap.DictOverrides.TryGetValue(kvp.Key, out overrideColor))
                        {
                            lookup[dictKey] = overrideColor;
                            continue;
                        }
                    }
                }
            }

            return lookup;
        }

        public static void TintLastBlock(LogType type)
        {
            var theme = GetCurrentTheme();

            // SurfaceColor が設定されたテーマでは PatchMsgBlock が背景を担当するためスキップ
            if (theme.SurfaceColor.HasValue) return;

            Color? tintColor = theme.GetTintColor(type);
            if (tintColor == null) return;

            var block = MsgBlock.lastBlock;
            if (block == null) return;

            Image overlay;

            if (_cachedBlock == block && _cachedOverlay != null)
            {
                overlay = _cachedOverlay;
            }
            else
            {
                Transform blockTransform = block.transform;
                Transform existing = blockTransform.Find(OverlayName);

                if (existing != null)
                {
                    overlay = existing.GetComponent<Image>();
                }
                else
                {
                    var go = new GameObject(OverlayName);
                    go.transform.SetParent(blockTransform, false);

                    var le = go.AddComponent<LayoutElement>();
                    le.ignoreLayout = true;

                    overlay = go.AddComponent<Image>();
                    overlay.raycastTarget = false;
                    overlay.sprite = GetGradientSprite();
                    overlay.type = Image.Type.Sliced;

                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    go.transform.SetAsFirstSibling();
                }

                _cachedBlock = block;
                _cachedOverlay = overlay;
            }

            overlay.color = tintColor.Value;
        }

        private static LogThemeData GetCurrentTheme()
        {
            return LogThemeData.Themes[ModConfig.Theme.Value];
        }

        private static Sprite GetGradientSprite()
        {
            if (_gradientSprite != null) return _gradientSprite;
            _gradientSprite = CreateGradientSprite();
            return _gradientSprite;
        }

        private static Sprite CreateGradientSprite()
        {
            const int w = 48;
            const int h = 16;
            const int borderH = 16; // left/right fade pixels
            const int borderV = 4;  // top/bottom fade pixels

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float vFade;
                if (y < borderV)
                    vFade = Mathf.SmoothStep(0f, 1f, (float)y / borderV);
                else if (y >= h - borderV)
                    vFade = Mathf.SmoothStep(0f, 1f, (float)(h - 1 - y) / borderV);
                else
                    vFade = 1f;

                for (int x = 0; x < w; x++)
                {
                    float hFade;
                    if (x < borderH)
                        hFade = Mathf.SmoothStep(0f, 1f, (float)x / borderH);
                    else if (x >= w - borderH)
                        hFade = Mathf.SmoothStep(0f, 1f, (float)(w - 1 - x) / borderH);
                    else
                        hFade = 1f;

                    pixels[y * w + x] = new Color(1f, 1f, 1f, hFade * vFade);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            var border = new Vector4(borderH, borderV, borderH, borderV);
            return Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border
            );
        }

        public static Color? GetElementColor(string elementAlias)
        {
            if (string.IsNullOrEmpty(elementAlias)) return null;
            Color c;
            if (!EClass.Colors.elementColors.TryGetValue(elementAlias, out c))
                return null;
            float avg = (c.r + c.g + c.b) / 3f;
            float boost = (avg > 0.5f) ? 0f : (0.6f - avg);
            return new Color(c.r + boost, c.g + boost, c.b + boost, 1f);
        }

        public static string FormatNumber(long num)
        {
            if (ModConfig.FormatMode.Value == ModConfig.NumberFormat.Million && num >= 1000000)
            {
                double m = num / 1000000.0;
                return m.ToString("#,0.#") + "M";
            }
            if ((ModConfig.FormatMode.Value == ModConfig.NumberFormat.Kilo || ModConfig.FormatMode.Value == ModConfig.NumberFormat.Million) && num >= 1000 && num < 1000000)
            {
                double k = num / 1000.0;
                return k.ToString("#,0.#") + "k";
            }
            return num.ToString("#,0");
        }

        public static string FormatDamageLog(long damage, string targetName, string attackerName, string elementName = null)
        {
            string num = FormatNumber(damage);
            string langCode = Lang.langCode;
            string elePrefix = string.IsNullOrEmpty(elementName) ? "" : $"{elementName}: ";

            switch (ModConfig.DetailLevel.Value)
            {
                case ModConfig.LogDetailLevel.WithTarget:
                    if (langCode == "JP")
                        return $"[ {targetName} に {elePrefix}{num} ダメージ！ ]";
                    if (langCode == "CN")
                        return $"[ {targetName} 受到 {elePrefix}{num} 伤害！ ]";
                    return $"[ {targetName} took {elePrefix}{num} damage! ]";

                default:
                    if (langCode == "JP")
                        return $"[ {elePrefix}{num} ダメージ ]";
                    if (langCode == "CN")
                        return $"[ {elePrefix}{num} 伤害 ]";
                    return $"[ {elePrefix}{num} damage ]";
            }
        }

        public static string FormatHealLog(long healed, string targetName, string healerName)
        {
            string num = FormatNumber(healed);
            string langCode = Lang.langCode;

            switch (ModConfig.DetailLevel.Value)
            {
                case ModConfig.LogDetailLevel.WithTarget:
                    if (langCode == "JP")
                        return $"[ {targetName} が {num} 回復 ]";
                    if (langCode == "CN")
                        return $"[ {targetName} 恢复 {num} ]";
                    return $"[ {targetName} recovered {num} ]";

                default:
                    if (langCode == "JP")
                        return $"[ {num} 回復 ]";
                    if (langCode == "CN")
                        return $"[ 恢复 {num} ]";
                    return $"[ {num} recovered ]";
            }
        }

        public static string FormatDebuffLog(string conditionDetail, string targetName, string inflicterName)
        {
            string langCode = Lang.langCode;

            switch (ModConfig.DetailLevel.Value)
            {
                case ModConfig.LogDetailLevel.WithTarget:
                    if (langCode == "JP")
                        return $"[ {targetName} は {conditionDetail} を受けた ]";
                    if (langCode == "CN")
                        return $"[ {targetName} 受到 {conditionDetail} ]";
                    return $"[ {targetName} affected by {conditionDetail} ]";

                default:
                    return $"[ {conditionDetail} ]";
            }
        }

        public static string FormatBuffLog(string conditionDetail, string targetName, string casterName)
        {
            string langCode = Lang.langCode;

            switch (ModConfig.DetailLevel.Value)
            {
                case ModConfig.LogDetailLevel.WithTarget:
                    if (langCode == "JP")
                        return $"[ {targetName} が {conditionDetail} を得た ]";
                    if (langCode == "CN")
                        return $"[ {targetName} 获得 {conditionDetail} ]";
                    return $"[ {targetName} gained {conditionDetail} ]";

                default:
                    return $"[ {conditionDetail} ]";
            }
        }

        public static string GetText(string key)
        {
            if (Lang.langCode == "JP")
            {
                if (key == "damage") return "ダメージ！";
                if (key == "heal") return "回復";
            }
            if (Lang.langCode == "CN")
            {
                if (key == "damage") return "伤害！";
                if (key == "heal") return "恢复";
            }
            if (key == "heal") return "recovered";
            return key;
        }
    }
}
