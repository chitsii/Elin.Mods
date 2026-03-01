using EvilMask.Elin.ModOptions;

namespace Elin_BottleNeckFinder
{
    public class ModOptionsBridge
    {
        public void SetTranslations(ModOptionController controller)
        {
            controller.SetTranslation(Plugin.ModGuid,
                "BottleNeckFinder",
                "BottleNeckFinder",
                "BottleNeckFinder");

            controller.SetTranslation("ModTooltip",
                "Mod performance profiler and error detector.",
                "Modパフォーマンスプロファイラ＆エラー検知。",
                "Mod性能分析和错误检测。");

            controller.SetTranslation("EnableMod",
                "Enable Mod", "Modを有効化", "启用Mod");
            controller.SetTranslation("EnableMod_tooltip",
                "Toggle the entire mod functionality.",
                "Mod全体の機能を切り替えます。",
                "切换Mod整体功能。");

            controller.SetTranslation("ShowOverlay",
                "Show Overlay", "オーバーレイ表示", "显示叠加层");
            controller.SetTranslation("ShowOverlay_tooltip",
                "Show the performance overlay. Start the profiler from the overlay to see mod rankings.",
                "パフォーマンスオーバーレイを表示します。オーバーレイからプロファイラを起動するとModランキングが表示されます。",
                "显示性能叠加层。从叠加层启动分析器可查看Mod排名。");
        }
    }
}
