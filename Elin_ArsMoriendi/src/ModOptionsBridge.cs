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

            controller.SetTranslation("ModSectionServants",
                "Servants",
                "従者",
                "仆从");
            controller.SetTranslation("ModSectionHotkey",
                "Hotkey",
                "ホットキー",
                "快捷键");
            controller.SetTranslation("ModSectionOther",
                "Other",
                "その他",
                "其他");

            controller.SetTranslation("ShowServantAura",
                "Servant Aura", "従者のオーラ", "仆从光环");
            controller.SetTranslation("ShowServantAura_tooltip",
                "Show shadow aura VFX around undead servants.",
                "アンデッド従者にまとわりつく影のオーラを表示します。",
                "显示亡灵仆从周围的暗影光环特效。");

            controller.SetTranslation("EnableStashedServantHomeContribution",
                "Stashed Home Contribution",
                "退避中も拠点貢献",
                "退避时也参与据点活动");
            controller.SetTranslation("EnableStashedServantHomeContribution_tooltip",
                "Stashed servants can move, work, and fight in the home zone. They still do not follow the player and remain excluded from active servant squad handling.",
                "退避中の従者が拠点で移動・仕事・戦闘を行えるようにします。プレイヤー追従はせず、従者戦力の集計対象にもなりません。",
                "让退避中的仆从在据点内移动、工作并参与战斗。它们仍不会跟随玩家，也不会计入主动仆从战力。");

            controller.SetTranslation("TomeHotkeyKey",
                "Servant UI Hotkey",
                "従者UIホットキー",
                "仆从UI快捷键");
            controller.SetTranslation("TomeHotkeyKey_tooltip",
                "Main key for toggling the Ars Moriendi servant UI.",
                "Ars Moriendi の従者UIを開閉するメインキーです。",
                "用于开关 Ars Moriendi 仆从UI 的主按键。");
            controller.SetTranslation("TomeHotkeyShift",
                "Shift",
                "Shift",
                "Shift");
            controller.SetTranslation("TomeHotkeyShift_tooltip",
                "Require Shift as part of the servant UI hotkey.",
                "従者UIホットキーに Shift を含めます。",
                "将 Shift 包含在仆从UI快捷键中。");
            controller.SetTranslation("TomeHotkeyCtrl",
                "Ctrl",
                "Ctrl",
                "Ctrl");
            controller.SetTranslation("TomeHotkeyCtrl_tooltip",
                "Require Ctrl as part of the servant UI hotkey.",
                "従者UIホットキーに Ctrl を含めます。",
                "将 Ctrl 包含在仆从UI快捷键中。");
            controller.SetTranslation("TomeHotkeyAlt",
                "Alt",
                "Alt",
                "Alt");
            controller.SetTranslation("TomeHotkeyAlt_tooltip",
                "Require Alt as part of the servant UI hotkey.",
                "従者UIホットキーに Alt を含めます。",
                "将 Alt 包含在仆从UI快捷键中。");
            controller.SetTranslation("TomeHotkeyModifiers",
                "Modifiers",
                "修飾キー",
                "修饰键");

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
