using BepInEx.Configuration;

namespace Elin_ModTemplate
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<int> DoomWidth;
        public static ConfigEntry<int> DoomHeight;
        public static ConfigEntry<int> DoomSfxVolume;
        public static ConfigEntry<float> MouseTurnSensitivity;
        public static ConfigEntry<bool> InvincibleMode;
        public static ConfigEntry<float> OverlayScale;
        public static ConfigEntry<float> BackdropAlpha;

        // TODO: Add your config entries here
        // Example:
        // public static ConfigEntry<float> MyValue;

        public static void LoadConfig(ConfigFile config)
        {
            EnableMod = config.Bind("General", "EnableMod", true, "Enable the mod.");
            DoomWidth = config.Bind("DOOM", "ScreenWidth", 320, "Internal DOOM render width.");
            DoomHeight = config.Bind("DOOM", "ScreenHeight", 200, "Internal DOOM render height.");
            DoomSfxVolume = config.Bind(
                "DOOM",
                "SfxVolume",
                6,
                new ConfigDescription("DOOM sound effect volume (0-15).", new AcceptableValueRange<int>(0, 15)));
            MouseTurnSensitivity = config.Bind(
                "DOOM",
                "MouseTurnSensitivity",
                5f,
                new ConfigDescription(
                    "Horizontal mouse turn sensitivity.",
                    new AcceptableValueRange<float>(1f, 40f)));
            InvincibleMode = config.Bind(
                "DOOM",
                "InvincibleMode",
                false,
                "Enable DOOM invincibility (God Mode).");
            OverlayScale = config.Bind(
                "Overlay",
                "Scale",
                0.60f,
                new ConfigDescription(
                    "Overlay size ratio against the screen. 0.5 = 50%, 0.8 = 80%.",
                    new AcceptableValueRange<float>(0.30f, 0.95f)));
            BackdropAlpha = config.Bind(
                "Overlay",
                "BackdropAlpha",
                0.80f,
                new ConfigDescription(
                    "DOOM frame shadow intensity. 0 = no shadow, 1 = strongest shadow.",
                    new AcceptableValueRange<float>(0.0f, 1.0f)));

            // TODO: Bind your config entries here
            // Example:
            // MyValue = config.Bind("General", "MyValue", 50f,
            //     new ConfigDescription("Description of MyValue.", new AcceptableValueRange<float>(0f, 100f)));
        }
    }
}
