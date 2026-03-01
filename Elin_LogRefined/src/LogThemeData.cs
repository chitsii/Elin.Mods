using UnityEngine;
using System.Collections.Generic;

namespace Elin_LogRefined
{
    public enum LogTheme
    {
        Off,
        // Mocha,          // unreleased
        Macchiato,
        // Frappe,         // unreleased
        // Latte,          // unreleased
        EverforestDark,
        // EverforestLight // unreleased
        Custom
    }

    public class GameColorRemap
    {
        public Color Default;
        public Color Talk;
        public Color TalkGod;
        public Color Ding;
        public Color Ono;
        public Color Negative;
        public Color Thinking;
        public Color MutateGood;
        public Color MutateBad;
        public Color Fallback;
        public Dictionary<string, Color> DictOverrides;
    }

    public class LogThemeData
    {
        // null = use vanilla Msg.colors
        public Color? DamageText, HealText, DebuffText, BuffText, CommentaryText;
        // null = no tint (Off theme)
        public Color? DamageTint, HealTint, DebuffTint, BuffTint;
        // null = no surface color change; set to apply uniform bg to all blocks
        public Color? SurfaceColor;
        // null = no remap; set to remap game MsgColors to themed colors
        public GameColorRemap ColorRemap;
        // true = disable text shadow on MsgBlock text (for light themes like Latte)
        public bool IsLightTheme;

        public Color GetTextColor(LogType type, Color vanillaFallback)
        {
            Color? themed;
            switch (type)
            {
                case LogType.Damage:     themed = DamageText; break;
                case LogType.Heal:       themed = HealText; break;
                case LogType.Debuff:     themed = DebuffText; break;
                case LogType.Buff:       themed = BuffText; break;
                case LogType.Commentary: themed = CommentaryText; break;
                default:                 themed = null; break;
            }
            return themed ?? vanillaFallback;
        }

        public Color? GetTintColor(LogType type)
        {
            switch (type)
            {
                case LogType.Damage: return DamageTint;
                case LogType.Heal:   return HealTint;
                case LogType.Debuff: return DebuffTint;
                case LogType.Buff:   return BuffTint;
                default:             return null;
            }
        }

        internal static Color HexColor(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            c.a = alpha;
            return c;
        }

        public static void RegisterCustomTheme(LogThemeData data)
        {
            Themes[LogTheme.Custom] = data;
        }

        public static readonly Dictionary<LogTheme, LogThemeData> Themes = new Dictionary<LogTheme, LogThemeData>
        {
            { LogTheme.Off, new LogThemeData() },

            /* Mocha — unreleased
            { LogTheme.Mocha, new LogThemeData
                {
                    DamageText     = HexColor("#f38ba8"),
                    HealText       = HexColor("#a6e3a1"),
                    DebuffText     = HexColor("#f9e2af"),
                    BuffText       = HexColor("#89b4fa"),
                    CommentaryText = HexColor("#f5e0dc"),
                    DamageTint  = HexColor("#f38ba8", 0.22f),
                    HealTint    = HexColor("#a6e3a1", 0.22f),
                    DebuffTint  = HexColor("#f9e2af", 0.22f),
                    BuffTint    = HexColor("#89b4fa", 0.22f),
                    SurfaceColor   = HexColor("#1e1e2e", 0.85f),
                    ColorRemap     = new GameColorRemap
                    {
                        Default    = HexColor("#cdd6f4"),  // Text
                        Talk       = HexColor("#b4befe"),  // Lavender
                        TalkGod    = HexColor("#f9e2af"),  // Yellow
                        Ding       = HexColor("#f9e2af"),  // Yellow
                        Ono        = HexColor("#fab387"),  // Peach
                        Negative   = HexColor("#cba6f7"),  // Mauve
                        Thinking   = HexColor("#b4befe"),  // Lavender
                        MutateGood = HexColor("#a6e3a1"),  // Green
                        MutateBad  = HexColor("#eba0ac"),  // Maroon
                        Fallback   = HexColor("#cdd6f4"),  // Text
                        DictOverrides = new Dictionary<string, Color>
                        {
                            { "attack_pc", HexColor("#fab387") },  // Peach
                            { "save",      HexColor("#89b4fa") },  // Blue
                            { "dead",      HexColor("#f38ba8") },  // Red
                            { "enemy",     HexColor("#f38ba8") },  // Red
                            { "critical",  HexColor("#f9e2af") },  // Yellow
                            { "sign",      HexColor("#f9e2af") },  // Yellow
                            { "warning",   HexColor("#fab387") },  // Peach
                            { "ability",   HexColor("#b4befe") },  // Lavender
                            { "positive",  HexColor("#74c7ec") },  // Sapphire
                            { "loose",     HexColor("#cba6f7") },  // Mauve
                            { "nice",      HexColor("#a6e3a1") },  // Green
                            { "special",   HexColor("#94e2d5") },  // Teal
                            { "bad",       HexColor("#f2cdcd") },  // Flamingo
                            { "mutation",  HexColor("#eba0ac") },  // Maroon
                            { "statue",    HexColor("#7f849c") },  // Overlay1
                            { "dmg0",      HexColor("#bac2de") },  // Subtext1
                            { "dmg1",      HexColor("#f5e0dc") },  // Rosewater
                            { "dmg2",      HexColor("#f2cdcd") },  // Flamingo
                            { "dmg3",      HexColor("#fab387") },  // Peach
                            { "dmg4",      HexColor("#eba0ac") },  // Maroon
                            { "dmg5",      HexColor("#eba0ac") },  // Maroon
                            { "dmgEle",    HexColor("#cba6f7") }   // Mauve
                        }
                    }
                }
            },
            */

            { LogTheme.Macchiato, new LogThemeData
                {
                    DamageText     = HexColor("#f5bde6"),
                    HealText       = HexColor("#a6da95"),
                    DebuffText     = HexColor("#eed49f"),
                    BuffText       = HexColor("#8aadf4"),
                    CommentaryText = HexColor("#f4dbd6"),
                    SurfaceColor   = HexColor("#24273a", 0.85f),
                    ColorRemap     = new GameColorRemap
                    {
                        Default    = HexColor("#cad3f5"),  // Text
                        Talk       = HexColor("#b7bdf8"),  // Lavender
                        TalkGod    = HexColor("#eed49f"),  // Yellow + bold
                        Ding       = HexColor("#eed49f"),  // Yellow
                        Ono        = HexColor("#f5a97f"),  // Peach
                        Negative   = HexColor("#c6a0f6"),  // Mauve
                        Thinking   = HexColor("#b7bdf8"),  // Lavender (== Talk, same vanilla Color32)
                        MutateGood = HexColor("#a6da95"),  // Green
                        MutateBad  = HexColor("#ee99a0"),  // Maroon
                        Fallback   = HexColor("#cad3f5"),  // Text
                        DictOverrides = new Dictionary<string, Color>
                        {
                            { "attack_pc", HexColor("#f5a97f") },  // Peach
                            { "save",      HexColor("#8aadf4") },  // Blue
                            { "dead",      HexColor("#ed8796") },  // Red
                            { "enemy",     HexColor("#ed8796") },  // Red
                            { "critical",  HexColor("#eed49f") },  // Yellow
                            { "sign",      HexColor("#eed49f") },  // Yellow
                            { "warning",   HexColor("#f5a97f") },  // Peach
                            { "ability",   HexColor("#b7bdf8") },  // Lavender
                            { "positive",  HexColor("#7dc4e4") },  // Sapphire
                            { "loose",     HexColor("#c6a0f6") },  // Mauve
                            { "nice",      HexColor("#a6da95") },  // Green
                            { "special",   HexColor("#8bd5ca") },  // Teal
                            { "bad",       HexColor("#f0c6c6") },  // Flamingo
                            { "mutation",  HexColor("#ee99a0") },  // Maroon
                            { "statue",    HexColor("#8087a2") },  // Overlay1
                            { "dmg0",      HexColor("#b8c0e0") },  // Subtext1
                            { "dmg1",      HexColor("#f4dbd6") },  // Rosewater
                            { "dmg2",      HexColor("#f0c6c6") },  // Flamingo
                            { "dmg3",      HexColor("#f5a97f") },  // Peach
                            { "dmg4",      HexColor("#ee99a0") },  // Maroon
                            { "dmg5",      HexColor("#ee99a0") },  // Maroon
                            { "dmgEle",    HexColor("#c6a0f6") }   // Mauve
                        }
                    }
                }
            },

            /* Frappé — unreleased
            { LogTheme.Frappe, new LogThemeData
                {
                    DamageText     = HexColor("#dc495f"),
                    HealText       = HexColor("#73b95a"),
                    DebuffText     = HexColor("#e2ab57"),
                    BuffText       = HexColor("#5588f2"),
                    CommentaryText = HexColor("#e7b0a4"),
                    DamageTint  = HexColor("#dc495f", 0.22f),
                    HealTint    = HexColor("#73b95a", 0.22f),
                    DebuffTint  = HexColor("#e2ab57", 0.22f),
                    BuffTint    = HexColor("#5588f2", 0.22f),
                    SurfaceColor   = HexColor("#303446", 0.85f),
                    ColorRemap     = new GameColorRemap
                    {
                        Default    = HexColor("#8990af"),
                        Talk       = HexColor("#96a1f7"),
                        TalkGod    = HexColor("#e2ab57"),
                        Ding       = HexColor("#e2ab57"),
                        Ono        = HexColor("#f78241"),
                        Negative   = HexColor("#a96ceb"),
                        Thinking   = HexColor("#96a1f7"),
                        MutateGood = HexColor("#73b95a"),
                        MutateBad  = HexColor("#e86f78"),
                        Fallback   = HexColor("#8990af"),
                        DictOverrides = new Dictionary<string, Color>
                        {
                            { "attack_pc", HexColor("#f78241") },
                            { "save",      HexColor("#5588f2") },
                            { "dead",      HexColor("#dc495f") },
                            { "enemy",     HexColor("#dc495f") },
                            { "critical",  HexColor("#e2ab57") },
                            { "sign",      HexColor("#e2ab57") },
                            { "warning",   HexColor("#f78241") },
                            { "ability",   HexColor("#96a1f7") },
                            { "positive",  HexColor("#53b0c9") },
                            { "loose",     HexColor("#a96ceb") },
                            { "nice",      HexColor("#73b95a") },
                            { "special",   HexColor("#4cadac") },
                            { "bad",       HexColor("#e69b9b") },
                            { "mutation",  HexColor("#e86f78") },
                            { "statue",    HexColor("#888da4") },
                            { "dmg0",      HexColor("#898fad") },
                            { "dmg1",      HexColor("#e7b0a4") },
                            { "dmg2",      HexColor("#e69b9b") },
                            { "dmg3",      HexColor("#f78241") },
                            { "dmg4",      HexColor("#e86f78") },
                            { "dmg5",      HexColor("#e86f78") },
                            { "dmgEle",    HexColor("#a96ceb") }
                        }
                    }
                }
            },
            */

            /* Latte — unreleased
            { LogTheme.Latte, new LogThemeData
                {
                    DamageText     = HexColor("#d20f39"),
                    HealText       = HexColor("#40a02b"),
                    DebuffText     = HexColor("#df8e1d"),
                    BuffText       = HexColor("#1e66f5"),
                    CommentaryText = HexColor("#dc8a78"),
                    DamageTint  = HexColor("#d20f39", 0.25f),
                    HealTint    = HexColor("#40a02b", 0.25f),
                    DebuffTint  = HexColor("#df8e1d", 0.25f),
                    BuffTint    = HexColor("#1e66f5", 0.25f),
                    SurfaceColor   = HexColor("#eff1f5", 0.85f),
                    IsLightTheme = true,
                    ColorRemap     = new GameColorRemap
                    {
                        Default    = HexColor("#4c4f69"),
                        Talk       = HexColor("#7287fd"),
                        TalkGod    = HexColor("#df8e1d"),
                        Ding       = HexColor("#df8e1d"),
                        Ono        = HexColor("#fe640b"),
                        Negative   = HexColor("#8839ef"),
                        Thinking   = HexColor("#7287fd"),
                        MutateGood = HexColor("#40a02b"),
                        MutateBad  = HexColor("#e64553"),
                        Fallback   = HexColor("#4c4f69"),
                        DictOverrides = new Dictionary<string, Color>
                        {
                            { "attack_pc", HexColor("#fe640b") },
                            { "save",      HexColor("#1e66f5") },
                            { "dead",      HexColor("#d20f39") },
                            { "enemy",     HexColor("#d20f39") },
                            { "critical",  HexColor("#df8e1d") },
                            { "sign",      HexColor("#df8e1d") },
                            { "warning",   HexColor("#fe640b") },
                            { "ability",   HexColor("#7287fd") },
                            { "positive",  HexColor("#209fb5") },
                            { "loose",     HexColor("#8839ef") },
                            { "nice",      HexColor("#40a02b") },
                            { "special",   HexColor("#179299") },
                            { "bad",       HexColor("#dd7878") },
                            { "mutation",  HexColor("#e64553") },
                            { "statue",    HexColor("#8c8fa1") },
                            { "dmg0",      HexColor("#5c5f77") },
                            { "dmg1",      HexColor("#dc8a78") },
                            { "dmg2",      HexColor("#dd7878") },
                            { "dmg3",      HexColor("#fe640b") },
                            { "dmg4",      HexColor("#e64553") },
                            { "dmg5",      HexColor("#e64553") },
                            { "dmgEle",    HexColor("#8839ef") }
                        }
                    }
                }
            },
            */

            { LogTheme.EverforestDark, new LogThemeData
                {
                    DamageText     = HexColor("#E67E80"),
                    HealText       = HexColor("#A7C080"),
                    DebuffText     = HexColor("#DBBC7F"),
                    BuffText       = HexColor("#7FBBB3"),
                    CommentaryText = HexColor("#9DA9A0"),
                    DamageTint  = HexColor("#E67E80", 0.22f),
                    HealTint    = HexColor("#A7C080", 0.22f),
                    DebuffTint  = HexColor("#DBBC7F", 0.22f),
                    BuffTint    = HexColor("#7FBBB3", 0.22f),
                    SurfaceColor   = HexColor("#2D353B", 0.85f),
                    ColorRemap     = new GameColorRemap
                    {
                        Default    = HexColor("#D3C6AA"),  // fg
                        Talk       = HexColor("#7FBBB3"),  // Blue
                        TalkGod    = HexColor("#DBBC7F"),  // Yellow
                        Ding       = HexColor("#DBBC7F"),  // Yellow
                        Ono        = HexColor("#E69875"),  // Orange
                        Negative   = HexColor("#D699B6"),  // Purple
                        Thinking   = HexColor("#7FBBB3"),  // Blue
                        MutateGood = HexColor("#A7C080"),  // Green
                        MutateBad  = HexColor("#E67E80"),  // Red
                        Fallback   = HexColor("#D3C6AA"),  // fg
                        DictOverrides = new Dictionary<string, Color>
                        {
                            { "attack_pc", HexColor("#E69875") },  // Orange
                            { "save",      HexColor("#83C092") },  // Aqua
                            { "dead",      HexColor("#E67E80") },  // Red
                            { "enemy",     HexColor("#E67E80") },  // Red
                            { "critical",  HexColor("#DBBC7F") },  // Yellow
                            { "sign",      HexColor("#DBBC7F") },  // Yellow
                            { "warning",   HexColor("#E69875") },  // Orange
                            { "ability",   HexColor("#7FBBB3") },  // Blue
                            { "positive",  HexColor("#83C092") },  // Aqua
                            { "loose",     HexColor("#D699B6") },  // Purple
                            { "nice",      HexColor("#A7C080") },  // Green
                            { "special",   HexColor("#83C092") },  // Aqua
                            { "bad",       HexColor("#E69875") },  // Orange
                            { "mutation",  HexColor("#E67E80") },  // Red
                            { "statue",    HexColor("#7A8478") },  // grey0
                            { "dmg0",      HexColor("#859289") },  // grey1
                            { "dmg1",      HexColor("#9DA9A0") },  // grey2
                            { "dmg2",      HexColor("#E69875") },  // Orange
                            { "dmg3",      HexColor("#E69875") },  // Orange
                            { "dmg4",      HexColor("#E67E80") },  // Red
                            { "dmg5",      HexColor("#E67E80") },  // Red
                            { "dmgEle",    HexColor("#D699B6") }   // Purple
                        }
                    }
                }
            },

            { LogTheme.Custom, new LogThemeData() },

            /* EverforestLight — unreleased
            { LogTheme.EverforestLight, new LogThemeData
                {
                    DamageText     = HexColor("#E67E80"),
                    HealText       = HexColor("#A7C080"),
                    DebuffText     = HexColor("#E69875"),  // Orange (Yellow has low contrast on cream bg)
                    BuffText       = HexColor("#7FBBB3"),
                    CommentaryText = HexColor("#7A8478"),
                    DamageTint  = HexColor("#E67E80", 0.25f),
                    HealTint    = HexColor("#A7C080", 0.25f),
                    DebuffTint  = HexColor("#E69875", 0.25f),
                    BuffTint    = HexColor("#7FBBB3", 0.25f),
                    SurfaceColor   = HexColor("#FDF6E3", 0.85f),
                    IsLightTheme   = true,
                    ColorRemap     = new GameColorRemap
                    {
                        Default    = HexColor("#5C6A72"),  // fg
                        Talk       = HexColor("#7FBBB3"),  // Blue
                        TalkGod    = HexColor("#DBBC7F"),  // Yellow
                        Ding       = HexColor("#DBBC7F"),  // Yellow
                        Ono        = HexColor("#E69875"),  // Orange
                        Negative   = HexColor("#D699B6"),  // Purple
                        Thinking   = HexColor("#7FBBB3"),  // Blue
                        MutateGood = HexColor("#A7C080"),  // Green
                        MutateBad  = HexColor("#E67E80"),  // Red
                        Fallback   = HexColor("#5C6A72"),  // fg
                        DictOverrides = new Dictionary<string, Color>
                        {
                            { "attack_pc", HexColor("#E69875") },  // Orange
                            { "save",      HexColor("#83C092") },  // Aqua
                            { "dead",      HexColor("#E67E80") },  // Red
                            { "enemy",     HexColor("#E67E80") },  // Red
                            { "critical",  HexColor("#DBBC7F") },  // Yellow
                            { "sign",      HexColor("#DBBC7F") },  // Yellow
                            { "warning",   HexColor("#E69875") },  // Orange
                            { "ability",   HexColor("#7FBBB3") },  // Blue
                            { "positive",  HexColor("#83C092") },  // Aqua
                            { "loose",     HexColor("#D699B6") },  // Purple
                            { "nice",      HexColor("#A7C080") },  // Green
                            { "special",   HexColor("#83C092") },  // Aqua
                            { "bad",       HexColor("#E69875") },  // Orange
                            { "mutation",  HexColor("#E67E80") },  // Red
                            { "statue",    HexColor("#7A8478") },  // grey0
                            { "dmg0",      HexColor("#859289") },  // grey1
                            { "dmg1",      HexColor("#9DA9A0") },  // grey2
                            { "dmg2",      HexColor("#E69875") },  // Orange
                            { "dmg3",      HexColor("#E69875") },  // Orange
                            { "dmg4",      HexColor("#E67E80") },  // Red
                            { "dmg5",      HexColor("#E67E80") },  // Red
                            { "dmgEle",    HexColor("#D699B6") }   // Purple
                        }
                    }
                }
            }
            */
        };
    }
}
