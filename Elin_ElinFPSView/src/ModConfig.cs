using BepInEx.Configuration;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class ModConfig
    {
        public ConfigEntry<KeyCode> ToggleKey { get; private set; }

        public ConfigEntry<int> RenderWidth { get; private set; }

        public ConfigEntry<int> RenderHeight { get; private set; }

        public ConfigEntry<float> FieldOfViewDegrees { get; private set; }

        public ConfigEntry<float> EyeHeight { get; private set; }

        public ConfigEntry<float> MaxDistance { get; private set; }

        public ConfigEntry<bool> EnableDistanceShading { get; private set; }

        public ConfigEntry<float> LookSensitivity { get; private set; }

        public ConfigEntry<float> PitchSensitivity { get; private set; }

        public static ModConfig Bind(ConfigFile configFile)
        {
            return new ModConfig
            {
                ToggleKey = configFile.Bind("General", "ToggleKey", KeyCode.F9, "Toggle the FPS overlay."),
                RenderWidth = configFile.Bind("Rendering", "RenderWidth", 320, "Internal render width."),
                RenderHeight = configFile.Bind("Rendering", "RenderHeight", 200, "Internal render height."),
                FieldOfViewDegrees = configFile.Bind("Rendering", "FieldOfViewDegrees", 75f, "Horizontal field of view."),
                EyeHeight = configFile.Bind("Rendering", "EyeHeight", 0.5f, "Camera eye height within the current tile."),
                MaxDistance = configFile.Bind("Rendering", "MaxDistance", 20f, "Maximum raycast distance in tiles."),
                EnableDistanceShading = configFile.Bind("Rendering", "EnableDistanceShading", true, "Darken distant walls."),
                LookSensitivity = configFile.Bind("Input", "LookSensitivity", 0.025f, "Horizontal mouse-look sensitivity in radians per mouse delta."),
                PitchSensitivity = configFile.Bind("Input", "PitchSensitivity", 0.015f, "Vertical mouse-look sensitivity in normalized screen offset per mouse delta.")
            };
        }
    }
}
