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

            controller.SetTranslation("InvincibleMode", "DOOM Invincible", "DOOM無敵モード", "DOOM无敌模式");
            controller.SetTranslation("InvincibleMode_tooltip",
                "Enable invincibility while playing DOOM.",
                "DOOMプレイ中に無敵を有効化します。",
                "在游玩DOOM时启用无敌。");

            controller.SetTranslation("MouseTurnSensitivity", "Mouse Turn Sensitivity", "マウス旋回感度", "鼠标转向灵敏度");
            controller.SetTranslation("MouseTurnSensitivity_tooltip",
                "Horizontal mouse turn sensitivity for DOOM.",
                "DOOMの水平旋回マウス感度です。",
                "DOOM水平转向的鼠标灵敏度。");
        }
    }
}
