using EvilMask.Elin.ModOptions;

namespace Elin_ModTemplate
{
    public class ModOptionsBridge
    {
        public void SetTranslations(ModOptionController controller)
        {
            // Mod title shown in ModOptions list (EN, JP, CN)
            controller.SetTranslation(Plugin.ModGuid, "My Mod Name", "My Mod Name", "My Mod Name");

            // Tooltip shown when hovering the mod entry
            controller.SetTranslation("ModTooltip",
                "Short description of your mod.",
                "Modの短い説明文。",
                "Mod的简短描述。");

            // EnableMod toggle
            controller.SetTranslation("EnableMod", "Enable Mod", "Modを有効化", "启用Mod");
            controller.SetTranslation("EnableMod_tooltip",
                "Toggle the entire mod functionality.",
                "Mod全体の機能を切り替えます。",
                "切换Mod整体功能。");

            // TODO: Add translations for your settings here
            // Example:
            // controller.SetTranslation("MySlider", "My Slider", "スライダー", "滑块");
            // controller.SetTranslation("MySlider_tooltip",
            //     "Description in English.",
            //     "日本語の説明。",
            //     "中文说明。");
        }
    }
}
