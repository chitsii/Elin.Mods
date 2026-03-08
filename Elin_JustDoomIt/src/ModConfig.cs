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
        internal static DoomInputBindings InputBindings;

        private static ConfigEntry<string> MoveForwardBindings;
        private static ConfigEntry<string> MoveBackwardBindings;
        private static ConfigEntry<string> TurnLeftBindings;
        private static ConfigEntry<string> TurnRightBindings;
        private static ConfigEntry<string> StrafeLeftBindings;
        private static ConfigEntry<string> StrafeRightBindings;
        private static ConfigEntry<string> FireBindings;
        private static ConfigEntry<string> UseBindings;
        private static ConfigEntry<string> RunBindings;
        private static ConfigEntry<string> Weapon1Bindings;
        private static ConfigEntry<string> Weapon2Bindings;
        private static ConfigEntry<string> Weapon3Bindings;
        private static ConfigEntry<string> Weapon4Bindings;
        private static ConfigEntry<string> Weapon5Bindings;
        private static ConfigEntry<string> Weapon6Bindings;
        private static ConfigEntry<string> Weapon7Bindings;
        private static ConfigEntry<string> NextWeaponBindings;
        private static ConfigEntry<string> PreviousWeaponBindings;
        private static ConfigEntry<string> KeyCodeReferenceUrl;

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
            KeyCodeReferenceUrl = config.Bind(
                "Input",
                "KeyCodeReferenceUrl",
                "https://docs.unity3d.com/ScriptReference/KeyCode.html",
                "Reference: official Unity KeyCode names used by Input.* settings.");
            MoveForwardBindings = BindInput(config, "MoveForward", DoomInputBindingDefaults.MoveForward, "Comma-separated bindings. Examples: W,UpArrow,Mouse0. See Input.KeyCodeReferenceUrl.");
            MoveBackwardBindings = BindInput(config, "MoveBackward", DoomInputBindingDefaults.MoveBackward, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            TurnLeftBindings = BindInput(config, "TurnLeft", DoomInputBindingDefaults.TurnLeft, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            TurnRightBindings = BindInput(config, "TurnRight", DoomInputBindingDefaults.TurnRight, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            StrafeLeftBindings = BindInput(config, "StrafeLeft", DoomInputBindingDefaults.StrafeLeft, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            StrafeRightBindings = BindInput(config, "StrafeRight", DoomInputBindingDefaults.StrafeRight, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            FireBindings = BindInput(config, "Fire", DoomInputBindingDefaults.Fire, "Comma-separated bindings. Mouse buttons use Mouse0, Mouse1, Mouse2. See Input.KeyCodeReferenceUrl.");
            UseBindings = BindInput(config, "Use", DoomInputBindingDefaults.Use, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            RunBindings = BindInput(config, "Run", DoomInputBindingDefaults.Run, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            Weapon1Bindings = BindInput(config, "Weapon1", DoomInputBindingDefaults.Weapon1, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            Weapon2Bindings = BindInput(config, "Weapon2", DoomInputBindingDefaults.Weapon2, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            Weapon3Bindings = BindInput(config, "Weapon3", DoomInputBindingDefaults.Weapon3, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            Weapon4Bindings = BindInput(config, "Weapon4", DoomInputBindingDefaults.Weapon4, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            Weapon5Bindings = BindInput(config, "Weapon5", DoomInputBindingDefaults.Weapon5, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            Weapon6Bindings = BindInput(config, "Weapon6", DoomInputBindingDefaults.Weapon6, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            Weapon7Bindings = BindInput(config, "Weapon7", DoomInputBindingDefaults.Weapon7, "Comma-separated bindings. See Input.KeyCodeReferenceUrl.");
            NextWeaponBindings = BindInput(config, "NextWeapon", DoomInputBindingDefaults.NextWeapon, "Comma-separated bindings. Supports WheelUp, keys, and mouse buttons. See Input.KeyCodeReferenceUrl.");
            PreviousWeaponBindings = BindInput(config, "PreviousWeapon", DoomInputBindingDefaults.PreviousWeapon, "Comma-separated bindings. Supports WheelDown, keys, and mouse buttons. See Input.KeyCodeReferenceUrl.");
            ReloadInputBindings();
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

        public static void ReloadInputBindings()
        {
            InputBindings = DoomInputBindings.FromConfig(
                MoveForwardBindings?.Value ?? DoomInputBindingDefaults.MoveForward,
                MoveBackwardBindings?.Value ?? DoomInputBindingDefaults.MoveBackward,
                TurnLeftBindings?.Value ?? DoomInputBindingDefaults.TurnLeft,
                TurnRightBindings?.Value ?? DoomInputBindingDefaults.TurnRight,
                StrafeLeftBindings?.Value ?? DoomInputBindingDefaults.StrafeLeft,
                StrafeRightBindings?.Value ?? DoomInputBindingDefaults.StrafeRight,
                FireBindings?.Value ?? DoomInputBindingDefaults.Fire,
                UseBindings?.Value ?? DoomInputBindingDefaults.Use,
                RunBindings?.Value ?? DoomInputBindingDefaults.Run,
                Weapon1Bindings?.Value ?? DoomInputBindingDefaults.Weapon1,
                Weapon2Bindings?.Value ?? DoomInputBindingDefaults.Weapon2,
                Weapon3Bindings?.Value ?? DoomInputBindingDefaults.Weapon3,
                Weapon4Bindings?.Value ?? DoomInputBindingDefaults.Weapon4,
                Weapon5Bindings?.Value ?? DoomInputBindingDefaults.Weapon5,
                Weapon6Bindings?.Value ?? DoomInputBindingDefaults.Weapon6,
                Weapon7Bindings?.Value ?? DoomInputBindingDefaults.Weapon7,
                NextWeaponBindings?.Value ?? DoomInputBindingDefaults.NextWeapon,
                PreviousWeaponBindings?.Value ?? DoomInputBindingDefaults.PreviousWeapon);
        }

        public static void ReloadRuntimeConfig()
        {
            if (_config == null)
            {
                ReloadInputBindings();
                return;
            }

            try
            {
                _config.Reload();
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Failed to reload config file. Using current values. " + ex.Message);
            }

            ReloadInputBindings();
        }

        private static ConfigEntry<string> BindInput(ConfigFile config, string key, string defaultValue, string description)
        {
            return config.Bind("Input", key, defaultValue, description);
        }
    }
}
