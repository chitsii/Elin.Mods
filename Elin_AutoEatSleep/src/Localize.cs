using System.Collections.Generic;

namespace Elin_AutoEatSleep
{
    public static class Localize
    {
        public static string Get(string key)
        {
            // Simple language check
            string langCode = EClass.core.config.lang;
            bool isJP = langCode == "JP";

            if (isJP)
            {
                if (Japanese.ContainsKey(key)) return Japanese[key];
            }
            else
            {
                if (English.ContainsKey(key)) return English[key];
            }

            return key; // Fallback to key
        }

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>()
        {
            { "Menu_Title", "Auto Eat/Sleep Settings" },
            { "Toggle_Eat", "Auto Eat" },
            { "Slider_Hunger", "Hunger Threshold" },
            { "Label_ContainerFilter", "Use Container Filter" },
            { "Toggle_Sleep", "Auto Sleep" },
            { "Toggle_ResumeAI", "Resume AI on Wake" },
            { "Slider_Stamina", "Stamina Threshold" },
            { "Log_NoFoodInContainer", "No food found in container! " },
            { "Filter_Off", "Filter: OFF" },
            { "Filter_Prefix", "Filter: " },
            { "Filter_Current", " (Current)" },

            // Hunger Phases
            { "Hunger_Bloated", "Bloated" },
            { "Hunger_Filled", "Filled" },
            { "Hunger_Normal", "Normal" },
            { "Hunger_Hungry", "Hungry" },
            { "Hunger_VeryHungry", "VeryHungry" },
            { "Hunger_Starving", "Starving" },

            // Sleep Phases
            { "Sleep_Normal", "Normal" },
            { "Sleep_Sleepy", "Sleepy" },
            { "Sleep_VerySleepy", "VerySleepy" },
            { "Sleep_VeryVerySleepy", "VeryVerySleepy" },
            { "Sleep_Dead", "Dead" },
        };

        private static readonly Dictionary<string, string> Japanese = new Dictionary<string, string>()
        {
            { "Menu_Title", "自動食事・睡眠設定" },
            { "Toggle_Eat", "自動食事" },
            { "Slider_Hunger", "空腹閾値" },
            { "Label_ContainerFilter", "コンテナフィルター使用" },
            { "Toggle_Sleep", "自動睡眠" },
            { "Toggle_ResumeAI", "起床時に行動再開" },
            { "Slider_Stamina", "スタミナ閾値" },
            { "Log_NoFoodInContainer", "指定コンテナに食料がありません！ " },
            { "Filter_Off", "フィルター: OFF" },
            { "Filter_Prefix", "フィルター: " },
            { "Filter_Current", " (適用中)" },

            // Hunger Phases
            { "Hunger_Bloated", "食べ過ぎ" },
            { "Hunger_Filled", "満腹" },
            { "Hunger_Normal", "通常" },
            { "Hunger_Hungry", "空腹" },
            { "Hunger_VeryHungry", "飢餓" },
            { "Hunger_Starving", "餓死寸前" },

            // Sleep Phases
            { "Sleep_Normal", "通常" },
            { "Sleep_Sleepy", "眠気" },
            { "Sleep_VerySleepy", "強い眠気" },
            { "Sleep_VeryVerySleepy", "睡眠不足" },
            { "Sleep_Dead", "気絶" },
        };
    }
}
