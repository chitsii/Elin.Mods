using BepInEx.Configuration;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class ModConfig
    {
        private const int LegacyRenderWidth = 320;
        private const int LegacyRenderHeight = 200;
        private const int DefaultRenderWidth = 640;
        private const int DefaultRenderHeight = 400;

        public ConfigEntry<KeyCode> ToggleKey { get; private set; }

        public ConfigEntry<FpsRenderBackend> RenderBackend { get; private set; }

        public ConfigEntry<int> RenderWidth { get; private set; }

        public ConfigEntry<int> RenderHeight { get; private set; }

        public ConfigEntry<float> FieldOfViewDegrees { get; private set; }

        public ConfigEntry<float> EyeHeight { get; private set; }

        public ConfigEntry<float> MaxDistance { get; private set; }

        public ConfigEntry<bool> EnableDistanceShading { get; private set; }

        public ConfigEntry<bool> EnableWorldLighting { get; private set; }

        public ConfigEntry<float> LookSensitivity { get; private set; }

        public ConfigEntry<float> PitchSensitivity { get; private set; }

        public ConfigEntry<bool> EnableGpuDiagnostics { get; private set; }

        public ConfigEntry<bool> GpuDebugSolidFaces { get; private set; }

        public static ModConfig Bind(ConfigFile configFile)
        {
            ModConfig settings = new ModConfig
            {
                ToggleKey = configFile.Bind("General", "ToggleKey", KeyCode.F9, "Toggle the FPS overlay."),
                RenderBackend = configFile.Bind("General", "RenderBackend", FpsRenderBackend.Software, "Rendering backend. Software keeps the current CPU renderer; GpuPreview enables the offscreen Unity camera preview path."),
                RenderWidth = configFile.Bind("Rendering", "RenderWidth", DefaultRenderWidth, "Internal render width."),
                RenderHeight = configFile.Bind("Rendering", "RenderHeight", DefaultRenderHeight, "Internal render height."),
                FieldOfViewDegrees = configFile.Bind("Rendering", "FieldOfViewDegrees", 75f, "Horizontal field of view."),
                EyeHeight = configFile.Bind("Rendering", "EyeHeight", 0.5f, "Camera eye height within the current tile."),
                MaxDistance = configFile.Bind("Rendering", "MaxDistance", 20f, "Maximum raycast distance in tiles."),
                EnableDistanceShading = configFile.Bind("Rendering", "EnableDistanceShading", true, "Darken distant walls."),
                EnableWorldLighting = configFile.Bind("Rendering", "EnableWorldLighting", true, "Apply Elin cell lighting, fire light, and shadow colors."),
                LookSensitivity = configFile.Bind("Input", "LookSensitivity", 0.025f, "Horizontal mouse-look sensitivity in radians per mouse delta."),
                PitchSensitivity = configFile.Bind("Input", "PitchSensitivity", 0.015f, "Vertical mouse-look sensitivity in normalized screen offset per mouse delta."),
                EnableGpuDiagnostics = configFile.Bind("Debug", "EnableGpuDiagnostics", true, "Log GPU wall/block/riser diagnostics to Player.log."),
                GpuDebugSolidFaces = configFile.Bind("Debug", "GpuDebugSolidFaces", false, "Render GPU wall/block/riser faces with solid colors instead of textures.")
            };

            UpgradeLegacyRenderResolution(configFile, settings);
            return settings;
        }

        private static void UpgradeLegacyRenderResolution(ConfigFile configFile, ModConfig settings)
        {
            if (settings.RenderWidth.Value != LegacyRenderWidth || settings.RenderHeight.Value != LegacyRenderHeight)
            {
                return;
            }

            settings.RenderWidth.Value = DefaultRenderWidth;
            settings.RenderHeight.Value = DefaultRenderHeight;
            configFile.Save();
        }
    }
}
