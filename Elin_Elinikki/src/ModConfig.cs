using BepInEx.Configuration;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class ModConfig
    {
        private const int LegacyRenderWidth = 320;
        private const int LegacyRenderHeight = 200;
        private const int DefaultRenderWidth = 640;
        private const int DefaultRenderHeight = 400;

        public ConfigEntry<KeyCode> ToggleKey { get; private set; }

        public ConfigEntry<KeyCode> VisualDumpKey { get; private set; }

        public ConfigEntry<bool> EnableDungeonCrawlerControls { get; private set; }

        public ConfigEntry<bool> EnableIndoorCeiling { get; private set; }

        public ConfigEntry<float> IndoorCeilingHeight { get; private set; }

        public ConfigEntry<bool> EnableHybridWallGeometry { get; private set; }

        public ConfigEntry<bool> EnableWorldMeshBackfaceCulling { get; private set; }

        public ConfigEntry<float> WallMountedHorizontalScale { get; private set; }

        public ConfigEntry<int> RenderWidth { get; private set; }

        public ConfigEntry<int> RenderHeight { get; private set; }

        public ConfigEntry<float> FieldOfViewDegrees { get; private set; }

        public ConfigEntry<float> EyeHeight { get; private set; }

        public ConfigEntry<float> MaxDistance { get; private set; }

        public ConfigEntry<int> MaxWallMountedSprites { get; private set; }

        public ConfigEntry<int> MaxUprightSprites { get; private set; }

        public ConfigEntry<int> MaxGroundSprites { get; private set; }

        public ConfigEntry<int> MaxEffectSprites { get; private set; }

        public ConfigEntry<float> TerrainDistanceMultiplier { get; private set; }

        public ConfigEntry<bool> EnableDistanceShading { get; private set; }

        public ConfigEntry<bool> EnableWorldLighting { get; private set; }

        public ConfigEntry<bool> EnableDistanceFog { get; private set; }

        public ConfigEntry<float> VisualContrastStrength { get; private set; }

        public ConfigEntry<float> IndoorShadowStrength { get; private set; }

        public ConfigEntry<float> FogColorGradeStrength { get; private set; }

        public ConfigEntry<float> DistanceFogStartRatio { get; private set; }

        public ConfigEntry<float> DistanceFogEndRatio { get; private set; }

        public ConfigEntry<float> DistanceFogDensity { get; private set; }

        public ConfigEntry<float> DistanceFogClearBlend { get; private set; }

        public ConfigEntry<bool> EnablePreviewPostEffects { get; private set; }

        public ConfigEntry<float> PreviewBloomIntensity { get; private set; }

        public ConfigEntry<float> PreviewSharpenStrength { get; private set; }

        public ConfigEntry<float> PreviewSaturationBoost { get; private set; }

        public ConfigEntry<float> PreviewContrastBoost { get; private set; }

        public ConfigEntry<float> PreviewBrightnessBoost { get; private set; }

        public ConfigEntry<bool> EnablePreviewVignette { get; private set; }

        public ConfigEntry<float> LookSensitivity { get; private set; }

        public ConfigEntry<float> PitchSensitivity { get; private set; }

        public ConfigEntry<bool> EnableGpuDiagnostics { get; private set; }

        public ConfigEntry<bool> EnableDeveloperLogs { get; private set; }

        public ConfigEntry<bool> GpuDebugSolidFaces { get; private set; }

        public ConfigEntry<bool> EnableDreamTestSet { get; private set; }

        public ConfigEntry<bool> RunSelfTestOnStartup { get; private set; }

        public static ModConfig Bind(ConfigFile configFile)
        {
            ModConfig settings = new ModConfig
            {
                ToggleKey = configFile.Bind("General", "ToggleKey", KeyCode.F9, "Toggle the Elinikki fullscreen view."),
                VisualDumpKey = configFile.Bind("General", "VisualDumpKey", KeyCode.F10, "Capture a visual dump with normal-view and FPS-view screenshots."),
                EnableDungeonCrawlerControls = configFile.Bind("General", "EnableDungeonCrawlerControls", true, "Lock the fullscreen view to 90-degree dungeon-crawler controls while the Elinikki overlay is visible."),
                EnableIndoorCeiling = configFile.Bind("General", "EnableIndoorCeiling", true, "Render a simple flat indoor ceiling for the current room instead of full roof geometry."),
                IndoorCeilingHeight = configFile.Bind("General", "IndoorCeilingHeight", 1.1f, "Ceiling height added above the highest floor surface in the current room for the simplified FPS indoor ceiling."),
                EnableHybridWallGeometry = configFile.Bind("General", "EnableHybridWallGeometry", true, "Render full-block walls with merged thickness-aware structure geometry instead of per-cell paper-thin faces."),
                EnableWorldMeshBackfaceCulling = configFile.Bind("General", "EnableWorldMeshBackfaceCulling", false, "Cull backfaces only on audited single-sided world meshes. Terrain chunks remain double-sided because their top, riser, ramp, and pillar faces currently share one mesh."),
                WallMountedHorizontalScale = configFile.Bind("General", "WallMountedHorizontalScale", 0.72f, "Horizontal scale applied to wall-mounted sprites and doors to compensate for quarter-view projection width."),
                RenderWidth = configFile.Bind("Rendering", "RenderWidth", DefaultRenderWidth, "Internal render width."),
                RenderHeight = configFile.Bind("Rendering", "RenderHeight", DefaultRenderHeight, "Internal render height."),
                FieldOfViewDegrees = configFile.Bind("Rendering", "FieldOfViewDegrees", 75f, "Horizontal field of view."),
                EyeHeight = configFile.Bind("Rendering", "EyeHeight", 0.5f, "Camera eye height within the current tile."),
                MaxDistance = configFile.Bind("Rendering", "MaxDistance", 20f, "Maximum raycast distance in tiles."),
                MaxWallMountedSprites = configFile.Bind("Rendering", "MaxWallMountedSprites", 80, "Maximum number of wall-mounted sprites rendered in the FPS view after priority culling."),
                MaxUprightSprites = configFile.Bind("Rendering", "MaxUprightSprites", 96, "Maximum number of upright sprites rendered in the FPS view after priority culling."),
                MaxGroundSprites = configFile.Bind("Rendering", "MaxGroundSprites", 72, "Maximum number of ground sprites rendered in the FPS view after priority culling."),
                MaxEffectSprites = configFile.Bind("Rendering", "MaxEffectSprites", 24, "Maximum number of effect sprites rendered in the FPS view after priority culling."),
                TerrainDistanceMultiplier = configFile.Bind("Rendering", "TerrainDistanceMultiplier", 0.95f, "Terrain, wall, and riser draw distance multiplier relative to MaxDistance."),
                EnableDistanceShading = configFile.Bind("Rendering", "EnableDistanceShading", true, "Darken distant walls."),
                EnableWorldLighting = configFile.Bind("Rendering", "EnableWorldLighting", true, "Apply Elin cell lighting, fire light, and shadow colors."),
                EnableDistanceFog = configFile.Bind("Rendering", "EnableDistanceFog", true, "Fade distant terrain and sprites toward the camera background color."),
                VisualContrastStrength = configFile.Bind("Rendering", "VisualContrastStrength", 0.12f, "Global low-cost contrast and face-lighting strength for the FPS view."),
                IndoorShadowStrength = configFile.Bind("Rendering", "IndoorShadowStrength", 0.16f, "Extra ambient darkening applied to indoor walls and ceilings."),
                FogColorGradeStrength = configFile.Bind("Rendering", "FogColorGradeStrength", 0.18f, "How strongly fog color is graded toward the FPS view's stylized haze palette."),
                DistanceFogStartRatio = configFile.Bind("Rendering", "DistanceFogStartRatio", 0.14f, "Distance at which dense fog starts, as a ratio of the relevant draw distance."),
                DistanceFogEndRatio = configFile.Bind("Rendering", "DistanceFogEndRatio", 0.34f, "Distance at which dense fog is effectively opaque, as a ratio of the relevant draw distance."),
                DistanceFogDensity = configFile.Bind("Rendering", "DistanceFogDensity", 3.25f, "How aggressively fog ramps up between the start and end distances."),
                DistanceFogClearBlend = configFile.Bind("Rendering", "DistanceFogClearBlend", 0.92f, "How strongly dense fog converges toward the actual background clear color to hide far-object cutoffs."),
                EnablePreviewPostEffects = configFile.Bind("Rendering", "EnablePreviewPostEffects", false, "Enable lightweight post-processing on the FPS preview camera. Currently kept off by default because the offscreen preview path can black out with stock image effects."),
                PreviewBloomIntensity = configFile.Bind("Rendering", "PreviewBloomIntensity", 0.55f, "Bloom intensity for the FPS preview camera."),
                PreviewSharpenStrength = configFile.Bind("Rendering", "PreviewSharpenStrength", 0.45f, "Sharpen amount for the FPS preview camera."),
                PreviewSaturationBoost = configFile.Bind("Rendering", "PreviewSaturationBoost", 0.08f, "Additional saturation for the FPS preview camera."),
                PreviewContrastBoost = configFile.Bind("Rendering", "PreviewContrastBoost", 0.06f, "Additional contrast for the FPS preview camera."),
                PreviewBrightnessBoost = configFile.Bind("Rendering", "PreviewBrightnessBoost", 0.015f, "Additional brightness for the FPS preview camera."),
                EnablePreviewVignette = configFile.Bind("Rendering", "EnablePreviewVignette", true, "Enable a light vignette on the FPS preview camera."),
                LookSensitivity = configFile.Bind("Input", "LookSensitivity", 0.025f, "Horizontal mouse-look sensitivity in radians per mouse delta."),
                PitchSensitivity = configFile.Bind("Input", "PitchSensitivity", 0.015f, "Vertical mouse-look sensitivity in normalized screen offset per mouse delta."),
                EnableGpuDiagnostics = configFile.Bind("Debug", "EnableGpuDiagnostics", false, "Log GPU wall/block/riser diagnostics to Player.log."),
                EnableDeveloperLogs = configFile.Bind("Debug", "EnableDeveloperLogs", false, "Log high-volume developer-only survey/candidate traces to Player.log."),
                GpuDebugSolidFaces = configFile.Bind("Debug", "GpuDebugSolidFaces", false, "Render GPU wall/block/riser faces with solid colors instead of textures."),
                EnableDreamTestSet = configFile.Bind("Dreamscape", "EnableDreamTestSet", true, "Spawn a small surreal shared-object test set near the player on each map."),
                RunSelfTestOnStartup = configFile.Bind("Debug", "RunSelfTestOnStartup", false, "Run Elinikki self-tests once on startup for saves whose player name contains RUNTIME_TEST.")
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
