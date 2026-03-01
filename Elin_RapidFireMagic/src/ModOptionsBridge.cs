using EvilMask.Elin.ModOptions;

namespace Elin_RapidFireMagic
{
    public class ModOptionsBridge
    {
        public void SetTranslations(ModOptionController controller)
        {
            controller.SetTranslation(Plugin.ModGuid, "Rapid Fire Magic", "Rapid Fire Magic", "Rapid Fire Magic");

            controller.SetTranslation("ModTooltip",
                "Hold number keys to rapidly cast hotbar spells.",
                "数字キー長押しでホットバーの魔法を連射します。",
                "长按数字键连续施放快捷栏魔法。");

            controller.SetTranslation("EnableMod", "Enable Mod", "Modを有効化", "启用Mod");
            controller.SetTranslation("EnableMod_tooltip",
                "Toggle the entire mod functionality.\nWhen OFF, holding number keys will not trigger rapid casting.",
                "Mod全体の機能を切り替えます。\nOFFにすると、数字キー長押しによる連射が行われません。",
                "切换Mod整体功能。\n关闭后，长按数字键不会触发连续施法。");

            controller.SetTranslation("PollInterval", "Recast Attempt Interval", "キープレス中の再キャスト試行間隔", "按住期间的重复施法尝试间隔");
            controller.SetTranslation("PollInterval_desc",
                "Interval (sec) between recast attempts while holding key (0.01-0.5).\nActual cast only occurs when your turn comes, so very small values have no extra benefit.\nDefault: 0.1",
                "キー長押し中に再キャストを試みる間隔（秒）。0.01〜0.5\n実際の発動はターンが回ってきた時のみ行われるため、極端に小さくしても効果は変わりません。\nデフォルト: 0.1",
                "按住键时尝试重新施法的间隔（秒）。0.01〜0.5\n实际施法仅在轮到行动时触发，因此设得过小也不会有额外效果。\n默认: 0.1");
        }
    }
}
