using EvilMask.Elin.ModOptions;

namespace Elin_ArsMoriendi
{
    public class ModOptionsBridge
    {
        public void SetTranslations(ModOptionController controller)
        {
            controller.SetTranslation(Plugin.ModGuid, "Ars Moriendi", "Ars Moriendi", "Ars Moriendi");

            controller.SetTranslation("ModTooltip",
                "A necromancy mod for Elin - capture souls, raise undead servants.",
                "Elin用の死霊術Mod - 魂を捕獲し、アンデッドの従者を蘇らせる。",
                "Elin死灵术Mod - 捕获灵魂，复活亡灵仆从。");

            controller.SetTranslation("ShowServantAura",
                "Servant Aura", "従者のオーラ", "仆从光环");
            controller.SetTranslation("ShowServantAura_tooltip",
                "Show shadow aura VFX around undead servants.",
                "アンデッド従者にまとわりつく影のオーラを表示します。",
                "显示亡灵仆从周围的暗影光环特效。");

            controller.SetTranslation("EnableUiCompatibilityMode",
                "UI Compatibility Mode",
                "UI互換モード",
                "UI兼容模式");
            controller.SetTranslation("EnableUiCompatibilityMode_tooltip",
                "Use light-background UI for older/integrated GPUs.",
                "古い/内蔵GPU向けに明るい背景の簡易UIへ切り替えます。",
                "为老旧/集成显卡切换到浅色背景简化UI。");

            controller.SetTranslation("DebugMode", "Debug Mode", "デバッグモード", "调试模式");
            controller.SetTranslation("DebugMode_tooltip",
                "Skip quest progression cooldowns for testing.",
                "クエスト進行のクールダウンをスキップします（テスト用）。",
                "跳过任务进度冷却时间（测试用）。");

            controller.SetTranslation("EnableApotheosisStatBonuses",
                "Necro Divinity Mode",
                "死霊の神性モード",
                "死灵神性模式");
            controller.SetTranslation("EnableApotheosisStatBonuses_tooltip",
                "Switch Necro Divinity mode in real time (ON: Full, OFF: Lite).",
                "死霊の神性モードをリアルタイムで切り替えます（ON: 完全版 / OFF: Lite）。",
                "实时切换死灵神性模式（ON: 完整版 / OFF: 轻量版）。");
        }
    }
}
