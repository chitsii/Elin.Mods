using BepInEx.Configuration;
using System;
using System.Collections.Generic;

namespace Elin_LogRefined
{
    public static class ModConfig
    {
        public enum NumberFormat
        {
            None,
            Kilo,
            Million
        }

        public enum LogDetailLevel
        {
            NumberOnly,       // [ 999 ダメージ ]
            WithTarget        // [ XXX が 999 ダメージを受けた ]
        }

        // LogTheme enum is defined in LogThemeData.cs

        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<bool> ShowDamageLog;
        public static ConfigEntry<bool> ShowHealLog;
        public static ConfigEntry<bool> ShowConditionLog;
        public static ConfigEntry<bool> ThrottleConditionLog;
        public static ConfigEntry<float> ConditionThrottleCooldown;
        public static ConfigEntry<NumberFormat> FormatMode;
        public static ConfigEntry<LogDetailLevel> DetailLevel;
        public static ConfigEntry<bool> EnableCommentary;
        public static ConfigEntry<LogTheme> Theme;
        public static ConfigEntry<bool> GenerateTemplates;

        public static ConfigEntry<string> CustomDamageText;
        public static ConfigEntry<string> CustomHealText;
        public static ConfigEntry<string> CustomDebuffText;
        public static ConfigEntry<string> CustomBuffText;
        public static ConfigEntry<string> CustomCommentaryText;
        public static ConfigEntry<string> CustomSurfaceColor;
        public static ConfigEntry<string> CustomDefault;
        public static ConfigEntry<string> CustomTalk;
        public static ConfigEntry<string> CustomNegative;
        public static ConfigEntry<string> CustomOno;

        public static void LoadConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true, "Enable the mod.");
            ShowDamageLog = config.Bind("Damage", "ShowDamageLog", true, "Show damage values in log.");
            ShowHealLog = config.Bind("Heal", "ShowHealLog", true, "Show healing values in log.");
            ShowConditionLog = config.Bind("Condition", "ShowConditionLog", true, "Show condition details (buff/debuff) in log.");
            ThrottleConditionLog = config.Bind("Condition", "ThrottleConditionLog", true,
                "Suppress repeated condition messages for the same character within cooldown period.");
            ConditionThrottleCooldown = config.Bind("Condition", "ConditionThrottleCooldown", 10f,
                new ConfigDescription("Cooldown in seconds for condition message throttling.",
                    new AcceptableValueRange<float>(1f, 60f)));
            FormatMode = config.Bind("Format", "FormatMode", NumberFormat.None, "Number formatting mode.");
            DetailLevel = config.Bind("Format", "DetailLevel", LogDetailLevel.NumberOnly, "Log detail level.");
            Theme = config.Bind("Format", "Theme", LogTheme.Off, "Color theme for log entries.");
            EnableCommentary = config.Bind("Extra", "EnableCommentary", true, "Enable live commentary mode (combat only).");
            GenerateTemplates = config.Bind("Extra", "GenerateTemplates", false, "Turn ON to generate template files.");

            // Subscribe to the GenerateTemplates toggle
            GenerateTemplates.SettingChanged += OnGenerateTemplatesChanged;

            // Custom theme color entries
            var defaults = CustomThemeBuilder.DefaultHex;
            CustomDamageText     = config.Bind("CustomTheme", "DamageText",     defaults[CustomColorSlot.DamageText],     "Damage text color (hex).");
            CustomHealText       = config.Bind("CustomTheme", "HealText",       defaults[CustomColorSlot.HealText],       "Heal text color (hex).");
            CustomDebuffText     = config.Bind("CustomTheme", "DebuffText",     defaults[CustomColorSlot.DebuffText],     "Debuff text color (hex).");
            CustomBuffText       = config.Bind("CustomTheme", "BuffText",       defaults[CustomColorSlot.BuffText],       "Buff text color (hex).");
            CustomCommentaryText = config.Bind("CustomTheme", "CommentaryText", defaults[CustomColorSlot.CommentaryText], "Commentary text color (hex).");
            CustomSurfaceColor   = config.Bind("CustomTheme", "SurfaceColor",   defaults[CustomColorSlot.SurfaceColor],   "Log block background color (hex).");
            CustomDefault        = config.Bind("CustomTheme", "Default",        defaults[CustomColorSlot.Default],        "Default message color (hex).");
            CustomTalk           = config.Bind("CustomTheme", "Talk",           defaults[CustomColorSlot.Talk],           "Talk message color (hex).");
            CustomNegative       = config.Bind("CustomTheme", "Negative",       defaults[CustomColorSlot.Negative],       "Negative message color (hex).");
            CustomOno            = config.Bind("CustomTheme", "Ono",            defaults[CustomColorSlot.Ono],            "Onomatopoeia message color (hex).");

            // Rebuild custom theme on any color change
            EventHandler onColorChanged = (s, e) => RebuildCustomTheme();
            CustomDamageText.SettingChanged     += onColorChanged;
            CustomHealText.SettingChanged       += onColorChanged;
            CustomDebuffText.SettingChanged     += onColorChanged;
            CustomBuffText.SettingChanged       += onColorChanged;
            CustomCommentaryText.SettingChanged += onColorChanged;
            CustomSurfaceColor.SettingChanged   += onColorChanged;
            CustomDefault.SettingChanged        += onColorChanged;
            CustomTalk.SettingChanged           += onColorChanged;
            CustomNegative.SettingChanged       += onColorChanged;
            CustomOno.SettingChanged            += onColorChanged;

            RebuildCustomTheme();
        }

        public static Dictionary<CustomColorSlot, string> GetCustomColorValues()
        {
            return new Dictionary<CustomColorSlot, string>
            {
                { CustomColorSlot.DamageText,     CustomDamageText.Value },
                { CustomColorSlot.HealText,       CustomHealText.Value },
                { CustomColorSlot.DebuffText,     CustomDebuffText.Value },
                { CustomColorSlot.BuffText,       CustomBuffText.Value },
                { CustomColorSlot.CommentaryText, CustomCommentaryText.Value },
                { CustomColorSlot.SurfaceColor,   CustomSurfaceColor.Value },
                { CustomColorSlot.Default,        CustomDefault.Value },
                { CustomColorSlot.Talk,           CustomTalk.Value },
                { CustomColorSlot.Negative,       CustomNegative.Value },
                { CustomColorSlot.Ono,            CustomOno.Value }
            };
        }

        public static void RebuildCustomTheme()
        {
            var values = GetCustomColorValues();
            var data = CustomThemeBuilder.Build(values);
            LogThemeData.RegisterCustomTheme(data);
            UnityEngine.Debug.Log($"[LogRefined] RebuildCustomTheme: remap={data.ColorRemap != null}, surface={data.SurfaceColor}, damageText={data.DamageText}, default={data.ColorRemap?.Default}");
        }

        public static ConfigEntry<string> GetConfigEntry(CustomColorSlot slot)
        {
            switch (slot)
            {
                case CustomColorSlot.DamageText:     return CustomDamageText;
                case CustomColorSlot.HealText:       return CustomHealText;
                case CustomColorSlot.DebuffText:     return CustomDebuffText;
                case CustomColorSlot.BuffText:       return CustomBuffText;
                case CustomColorSlot.CommentaryText: return CustomCommentaryText;
                case CustomColorSlot.SurfaceColor:   return CustomSurfaceColor;
                case CustomColorSlot.Default:        return CustomDefault;
                case CustomColorSlot.Talk:           return CustomTalk;
                case CustomColorSlot.Negative:       return CustomNegative;
                case CustomColorSlot.Ono:            return CustomOno;
                default:                             return null;
            }
        }

        private static void OnGenerateTemplatesChanged(object sender, System.EventArgs e)
        {
            if (GenerateTemplates.Value)
            {
                // Reset to OFF first to prevent re-triggering
                GenerateTemplates.Value = false;

                Action onComplete = () =>
                {
                    Dialog.YesNo(
                        "Templates created. Open folder?\nテンプレートを作成しました。フォルダを開きますか？",
                        () => CommentaryData.OpenCommentaryDir()
                    );
                };

                // Check if files exist and prompt
                if (CommentaryData.TemplateFilesExist())
                {
                    Dialog.YesNo(
                        "Template files already exist. Overwrite with defaults?\nテンプレートファイルが既に存在します。上書きしますか？",
                        () =>
                        {
                            CommentaryData.GenerateTemplateFiles(true);
                            onComplete();
                        }
                    );
                }
                else
                {
                    CommentaryData.GenerateTemplateFiles(false);
                    onComplete();
                }
            }
        }
    }
}
