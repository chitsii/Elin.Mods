using BepInEx.Configuration;

namespace Elin_ArsMoriendi
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> ShowServantWidget = null!;
        public static ConfigEntry<bool> DebugMode = null!;
        public static ConfigEntry<bool> EnableApotheosisStatBonuses = null!;
        public static ConfigEntry<bool> EnableUiCompatibilityMode = null!;
        public static ConfigEntry<bool> ShowServantAura = null!;
        public static ConfigEntry<int> WidgetFontScale = null!;

        public static void LoadConfig(ConfigFile config)
        {
            ShowServantWidget = config.Bind("General", "ShowServantWidget", true,
                "Show the servant status widget (HP/MP/SP bars).");
            EnableUiCompatibilityMode = config.Bind("General", "EnableUiCompatibilityMode", false,
                "Use compatibility UI rendering with light background (for older/integrated GPUs).");
            ShowServantAura = config.Bind("General", "ShowServantAura", true,
                "Show shadow aura VFX on undead servants.");
            EnableApotheosisStatBonuses = config.Bind("Balance", "EnableApotheosisStatBonuses", true,
                "Use Full Necro Divinity feat when enabled; use Lite feat when disabled.");
            WidgetFontScale = config.Bind("General", "WidgetFontScale", 0,
                "Widget font size preset (0=Small, 1=Medium, 2=Large).");
            DebugMode = config.Bind("Debug", "DebugMode", false,
                "Skip quest day cooldown and enable debug features.");
        }
    }
}
