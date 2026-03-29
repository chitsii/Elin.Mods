using BepInEx.Configuration;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> ShowServantWidget = null!;
        public static ConfigEntry<bool> DebugMode = null!;
        public static ConfigEntry<bool> EnableApotheosisStatBonuses = null!;
        public static ConfigEntry<bool> EnableUiCompatibilityMode = null!;
        public static ConfigEntry<bool> ShowServantAura = null!;
        public static ConfigEntry<bool> EnableStashedServantHomeContribution = null!;
        public static ConfigEntry<KeyCode> TomeHotkeyKey = null!;
        public static ConfigEntry<bool> TomeHotkeyShift = null!;
        public static ConfigEntry<bool> TomeHotkeyCtrl = null!;
        public static ConfigEntry<bool> TomeHotkeyAlt = null!;
        public static ConfigEntry<int> WidgetFontScale = null!;

        public static void LoadConfig(ConfigFile config)
        {
            ShowServantWidget = config.Bind("General", "ShowServantWidget", true,
                "Show the servant status widget (HP/MP/SP bars).");
            EnableUiCompatibilityMode = config.Bind("General", "EnableUiCompatibilityMode", false,
                "Use compatibility UI rendering with light background (for older/integrated GPUs).");
            ShowServantAura = config.Bind("General", "ShowServantAura", true,
                "Show shadow aura VFX on undead servants.");
            EnableStashedServantHomeContribution = config.Bind("General", "EnableStashedServantHomeContribution", false,
                "Allow stashed servants to move, work, and fight in the home zone while remaining excluded from active servant squad handling.");
            TomeHotkeyKey = config.Bind("General", "TomeHotkeyKey", KeyCode.N,
                "Main key for opening the Ars Moriendi servant UI.");
            TomeHotkeyShift = config.Bind("General", "TomeHotkeyShift", true,
                "Require Shift for the Ars Moriendi servant UI hotkey.");
            TomeHotkeyCtrl = config.Bind("General", "TomeHotkeyCtrl", false,
                "Require Ctrl for the Ars Moriendi servant UI hotkey.");
            TomeHotkeyAlt = config.Bind("General", "TomeHotkeyAlt", false,
                "Require Alt for the Ars Moriendi servant UI hotkey.");
            EnableApotheosisStatBonuses = config.Bind("Balance", "EnableApotheosisStatBonuses", true,
                "Use Full Necro Divinity feat when enabled; use Lite feat when disabled.");
            WidgetFontScale = config.Bind("General", "WidgetFontScale", 0,
                "Widget font size preset (0=Small, 1=Medium, 2=Large).");
            DebugMode = config.Bind("Debug", "DebugMode", false,
                "Skip quest day cooldown and enable debug features.");
        }
    }
}
