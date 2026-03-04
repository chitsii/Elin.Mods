using EvilMask.Elin.ModOptions;

namespace Elin_NiComment
{
    public class ModOptionsBridge
    {
        public void SetTranslations(ModOptionController controller)
        {
            controller.SetTranslation(Plugin.ModGuid, "NiComment", "NiComment", "NiComment");

            controller.SetTranslation("ModTooltip",
                "Niconico-style scrolling comments overlay.",
                "ニコニコ風コメント流しオーバーレイ。",
                "弹幕风格的滚动评论叠加层。");

            controller.SetTranslation("EnableMod", "Enable Mod", "Modを有効化", "启用Mod");
            controller.SetTranslation("EnableMod_tooltip",
                "Toggle the entire mod functionality.",
                "Mod全体の機能を切り替えます。",
                "切换Mod整体功能。");

            controller.SetTranslation("ScrollSpeed", "Scroll Speed", "スクロール速度", "滚动速度");
            controller.SetTranslation("ScrollSpeed_tooltip",
                "Comment scroll speed (px/sec).",
                "コメントのスクロール速度（px/秒）。",
                "评论滚动速度（像素/秒）。");

            controller.SetTranslation("FontSize", "Font Size", "フォントサイズ", "字体大小");
            controller.SetTranslation("FontSize_tooltip",
                "Font size of comments.",
                "コメントのフォントサイズ。",
                "评论的字体大小。");

            controller.SetTranslation("MaxLanes", "Max Lanes", "最大レーン数", "最大行数");
            controller.SetTranslation("MaxLanes_tooltip",
                "Maximum number of comment lanes.",
                "コメントレーンの最大数。",
                "评论行的最大数量。");

            controller.SetTranslation("PoolSize", "Pool Size", "プールサイズ", "缓冲池大小");
            controller.SetTranslation("PoolSize_tooltip",
                "Maximum simultaneous comments.",
                "同時表示コメントの最大数。",
                "同时显示评论的最大数量。");

            controller.SetTranslation("TopMargin", "Top Margin", "上余白", "顶部边距");
            controller.SetTranslation("TopMargin_tooltip",
                "Top margin in pixels.",
                "上部余白（ピクセル）。",
                "顶部边距（像素）。");

            controller.SetTranslation("EnableLlm", "Enable LLM Reactions", "LLMリアクション有効化", "启用LLM反应");
            controller.SetTranslation("EnableLlm_tooltip",
                "Use LLM to generate viewer-style reactions. Requires API key in config file.",
                "LLMで視聴者風リアクションを生成。cfgファイルにAPIキーの設定が必要。",
                "使用LLM生成观众风格的反应。需要在配置文件中设置API密钥。");

            controller.SetTranslation("BatchInterval", "LLM Batch Interval", "LLMバッチ間隔", "LLM批处理间隔");
            controller.SetTranslation("BatchInterval_tooltip",
                "Seconds to accumulate events before sending to LLM.",
                "LLMに送信するまでのイベント蓄積時間（秒）。",
                "发送到LLM之前积累事件的秒数。");
        }
    }
}
