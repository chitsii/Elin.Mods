using System;
using BepInEx.Configuration;

namespace Elin_JustDoomIt
{
    public static class ModConfig
    {
        private static ConfigFile _config;

        public static ConfigEntry<int> DoomWidth;
        public static ConfigEntry<int> DoomHeight;
        public static ConfigEntry<int> DoomBrightness;
        public static ConfigEntry<int> DoomSfxVolume;
        public static ConfigEntry<float> MouseTurnSensitivity;
        public static ConfigEntry<bool> InvincibleMode;
        public static ConfigEntry<float> OverlayScale;
        public static ConfigEntry<float> BackdropAlpha;

        public static void LoadConfig(ConfigFile config)
        {
            _config = config;
            DoomWidth = config.Bind(
                "DOOM",
                "ScreenWidth",
                320,
                new ConfigDescription(
                    "Internal DOOM render width.",
                    new AcceptableValueRange<int>(160, 1280)));
            DoomHeight = config.Bind(
                "DOOM",
                "ScreenHeight",
                200,
                new ConfigDescription(
                    "Internal DOOM render height.",
                    new AcceptableValueRange<int>(100, 720)));
            DoomBrightness = config.Bind(
                "DOOM",
                "Brightness",
                5,
                new ConfigDescription(
                    "DOOM gamma/brightness level (0-10). Higher is brighter.",
                    new AcceptableValueRange<int>(0, 10)));
            DoomSfxVolume = config.Bind(
                "DOOM",
                "SfxVolume",
                3,
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
        }

        public static void SetDoomResolution(int width, int height)
        {
            DoomWidth.Value = width;
            DoomHeight.Value = height;
            Save();
        }

        public static void SetDoomBrightness(int brightness)
        {
            DoomBrightness.Value = brightness < 0 ? 0 : (brightness > 10 ? 10 : brightness);
            Save();
        }

        public static void Save()
        {
            _config?.Save();
        }
    }
}
