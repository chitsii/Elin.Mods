using UnityEngine;
using System.Collections.Generic;

namespace Elin_LogRefined
{
    public enum CustomColorSlot
    {
        DamageText,
        HealText,
        DebuffText,
        BuffText,
        CommentaryText,
        SurfaceColor,
        Default,
        Talk,
        Negative,
        Ono
    }

    public static class CustomThemeBuilder
    {
        public static readonly Dictionary<CustomColorSlot, string> DefaultHex = new Dictionary<CustomColorSlot, string>
        {
            { CustomColorSlot.DamageText,     "#f5bde6" },
            { CustomColorSlot.HealText,       "#a6da95" },
            { CustomColorSlot.DebuffText,     "#eed49f" },
            { CustomColorSlot.BuffText,       "#8aadf4" },
            { CustomColorSlot.CommentaryText, "#f4dbd6" },
            { CustomColorSlot.SurfaceColor,   "#24273a" },
            { CustomColorSlot.Default,        "#cad3f5" },
            { CustomColorSlot.Talk,           "#b7bdf8" },
            { CustomColorSlot.Negative,       "#c6a0f6" },
            { CustomColorSlot.Ono,            "#f5a97f" }
        };

        public static LogThemeData Build(Dictionary<CustomColorSlot, string> values)
        {
            Color damage     = ParseOrDefault(values, CustomColorSlot.DamageText);
            Color heal       = ParseOrDefault(values, CustomColorSlot.HealText);
            Color debuff     = ParseOrDefault(values, CustomColorSlot.DebuffText);
            Color buff       = ParseOrDefault(values, CustomColorSlot.BuffText);
            Color commentary = ParseOrDefault(values, CustomColorSlot.CommentaryText);
            Color surface    = ParseOrDefault(values, CustomColorSlot.SurfaceColor);
            Color defColor   = ParseOrDefault(values, CustomColorSlot.Default);
            Color talk       = ParseOrDefault(values, CustomColorSlot.Talk);
            Color negative   = ParseOrDefault(values, CustomColorSlot.Negative);
            Color ono        = ParseOrDefault(values, CustomColorSlot.Ono);

            return new LogThemeData
            {
                DamageText     = damage,
                HealText       = heal,
                DebuffText     = debuff,
                BuffText       = buff,
                CommentaryText = commentary,
                SurfaceColor   = new Color(surface.r, surface.g, surface.b, 0.85f),
                ColorRemap     = new GameColorRemap
                {
                    Default    = defColor,
                    Talk       = talk,
                    TalkGod    = debuff,
                    Ding       = debuff,
                    Ono        = ono,
                    Negative   = negative,
                    Thinking   = talk,
                    MutateGood = heal,
                    MutateBad  = damage,
                    Fallback   = defColor,
                    DictOverrides = BuildDictOverrides(damage, heal, debuff, buff, defColor, talk, negative, ono)
                }
            };
        }

        private static Dictionary<string, Color> BuildDictOverrides(
            Color damage, Color heal, Color debuff, Color buff,
            Color defColor, Color talk, Color negative, Color ono)
        {
            return new Dictionary<string, Color>
            {
                { "attack_pc", ono },
                { "save",      buff },
                { "dead",      damage },
                { "enemy",     damage },
                { "critical",  debuff },
                { "sign",      debuff },
                { "warning",   ono },
                { "ability",   talk },
                { "positive",  buff },
                { "loose",     negative },
                { "nice",      heal },
                { "special",   heal },
                { "bad",       ono },
                { "mutation",  damage },
                { "statue",    MidTone(defColor, 0.6f) },
                { "dmg0",      MidTone(defColor, 0.85f) },
                { "dmg1",      MidTone(defColor, 0.95f) },
                { "dmg2",      ono },
                { "dmg3",      ono },
                { "dmg4",      damage },
                { "dmg5",      damage },
                { "dmgEle",    negative }
            };
        }

        private static Color MidTone(Color baseColor, float factor)
        {
            return new Color(baseColor.r * factor, baseColor.g * factor, baseColor.b * factor, 1f);
        }

        private static Color ParseOrDefault(Dictionary<CustomColorSlot, string> values, CustomColorSlot slot)
        {
            string hex;
            if (values != null && values.TryGetValue(slot, out hex) && !string.IsNullOrEmpty(hex))
            {
                if (ColorUtility.TryParseHtmlString(hex, out Color c))
                    return c;
            }
            ColorUtility.TryParseHtmlString(DefaultHex[slot], out Color fallback);
            return fallback;
        }
    }
}
