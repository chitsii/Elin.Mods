using System;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed partial class FpsGpuPreviewRenderer
    {
        private Color ApplyAtmosphericFog(Color baseColor, Vector3 worldPosition, float maxDistance, float startRatio, string category)
        {
            if (Plugin.Settings.EnableDistanceFog.Value != true || _camera == null || maxDistance <= 0.01f)
            {
                return ApplySceneTone(baseColor);
            }

            Vector3 cameraSpace = _camera.transform.InverseTransformPoint(worldPosition);
            float depth = cameraSpace.z;
            if (depth <= 0f)
            {
                return baseColor;
            }

            float radialDistance = Vector3.Distance(_camera.transform.position, worldPosition);
            bool terrainLike = category.StartsWith("terrain", StringComparison.Ordinal);
            bool spriteLike = category.StartsWith("ground", StringComparison.Ordinal) || category.StartsWith("upright", StringComparison.Ordinal);
            float configuredStartRatio = Plugin.Settings?.DistanceFogStartRatio?.Value ?? 0.3f;
            float configuredEndRatio = Plugin.Settings?.DistanceFogEndRatio?.Value ?? 0.58f;
            float configuredDensity = Plugin.Settings?.DistanceFogDensity?.Value ?? 1.8f;
            float configuredClearBlend = Plugin.Settings?.DistanceFogClearBlend?.Value ?? 0.7f;
            float effectiveStartRatio = terrainLike
                ? Mathf.Max(startRatio, configuredStartRatio * 0.92f)
                : spriteLike
                    ? Mathf.Max(startRatio, configuredStartRatio * 0.88f)
                    : Mathf.Max(startRatio, configuredStartRatio);
            float effectiveEndRatio = terrainLike
                ? Mathf.Clamp(configuredEndRatio * 0.94f, effectiveStartRatio + 0.05f, 1f)
                : spriteLike
                    ? Mathf.Clamp(configuredEndRatio, effectiveStartRatio + 0.05f, 1f)
                    : Mathf.Clamp(configuredEndRatio * 0.97f, effectiveStartRatio + 0.05f, 1f);
            float fogStart = Mathf.Max(0.1f, maxDistance * effectiveStartRatio);
            float fogEnd = Mathf.Max(fogStart + 0.1f, maxDistance * effectiveEndRatio);
            if (radialDistance <= fogStart)
            {
                return baseColor;
            }

            float fogFactor = FpsDistanceFog.ComputeFogFactor(radialDistance, maxDistance, effectiveStartRatio, effectiveEndRatio, configuredDensity);
            Color desaturated = Desaturate(baseColor, fogFactor * 0.7f);
            Color fogColor = ResolveFogColor();
            fogColor = FpsVisualStyle.GradeFogColor(fogColor, category, (Plugin.Settings?.FogColorGradeStrength?.Value ?? 0.18f) * (0.55f + (fogFactor * 0.45f)));
            fogColor = FpsDistanceFog.BlendTowardClearColor(fogColor, ResolveClearColor(), fogFactor, configuredClearBlend);
            fogColor.a = desaturated.a;
            Color result = Color.Lerp(desaturated, fogColor, fogFactor);
            result = ApplySceneTone(result);
            MaybeRecordFogDiagnostic(category, radialDistance, fogStart, fogEnd, fogFactor, baseColor, result);
            return result;
        }

        private float ResolveSceneTimeRatio()
        {
            if (EMono.scene != null)
            {
                return EMono.scene.timeRatio;
            }

            return 0f;
        }

        private Color ResolveClearColor()
        {
            SceneProfile profile = EMono.scene?.profile;
            SceneColorProfile color = profile?.color;
            if (color == null)
            {
                return DefaultClearColor;
            }

            float timeRatio = ResolveSceneTimeRatio();
            Color sky = color.sky.Evaluate(timeRatio);
            Color skyBg = color.skyBG.Evaluate(timeRatio);
            Color clear = Color.Lerp(skyBg, sky, 0.35f);
            return ApplySceneTone(clear, includeNightBrightness: false);
        }

        private void MaybeRecordFogDiagnostic(string category, float depth, float fogStart, float fogEnd, float fogFactor, Color baseColor, Color result)
        {
            if (!IsGpuDiagnosticsEnabled() || _diagnosticFramesRemaining <= 0 || _fogDiagnosticSamples.Count >= 12)
            {
                return;
            }

            _fogDiagnosticSamples.Add(
                $"GPU fog sample[{_fogDiagnosticSamples.Count}]: kind={category} depth={depth:F3} start={fogStart:F3} end={fogEnd:F3} factor={fogFactor:F3} base=({baseColor.r:F3},{baseColor.g:F3},{baseColor.b:F3}) result=({result.r:F3},{result.g:F3},{result.b:F3})");
        }

        private Color ResolveFogColor()
        {
            SceneProfile profile = EMono.scene?.profile;
            SceneColorProfile color = profile?.color;
            if (color == null)
            {
                Color background = _camera != null ? _camera.backgroundColor : DefaultClearColor;
                Color haze = Color.Lerp(background, Color.white, 0.38f);
                return Color.Lerp(haze, new Color(0.76f, 0.82f, 0.88f, 1f), 0.24f);
            }

            float timeRatio = ResolveSceneTimeRatio();
            Color fog = color.fog.Evaluate(timeRatio);
            Color skyBg = color.skyBG.Evaluate(timeRatio);
            Color hazeColor = Color.Lerp(fog, skyBg, 0.5f);
            return ApplySceneTone(hazeColor, includeNightBrightness: false);
        }

        private Color ApplySceneTone(Color color)
        {
            return ApplySceneTone(color, includeNightBrightness: true);
        }

        private Color ApplySceneTone(Color color, bool includeNightBrightness)
        {
            Color result = color;

            if (EMono.scene?.camSupport?.beautify != null)
            {
                Color tint = EMono.scene.camSupport.beautify.tintColor;
                if (tint.a > 0.001f)
                {
                    Color tinted = new Color(result.r * tint.r, result.g * tint.g, result.b * tint.b, result.a);
                    result = Color.Lerp(result, tinted, Mathf.Clamp01(tint.a * 0.35f));
                }
            }

            SceneProfile profile = EMono.scene?.profile;
            SceneColorProfile colorProfile = profile?.color;
            SceneLightProfile lightProfile = profile?.light;
            if (colorProfile != null && lightProfile != null)
            {
                float timeRatio = ResolveSceneTimeRatio();
                float nightRate = lightProfile.nightRatioCurve.Evaluate(timeRatio);
                Color sky = colorProfile.sky.Evaluate(timeRatio);
                Color fog = colorProfile.fog.Evaluate(timeRatio);
                Color ambientTone = Color.Lerp(sky, fog, 0.45f);
                Color modulated = new Color(result.r * ambientTone.r, result.g * ambientTone.g, result.b * ambientTone.b, result.a);
                result = Color.Lerp(result, modulated, Mathf.Clamp01(nightRate * 0.18f));
            }

            if (includeNightBrightness && EMono.scene?.camSupport?.grading != null)
            {
                float nightBrightness = EMono.scene.camSupport.grading.nightBrightness;
                if (!Mathf.Approximately(nightBrightness, 0f))
                {
                    result.r = Mathf.Clamp01(result.r + nightBrightness);
                    result.g = Mathf.Clamp01(result.g + nightBrightness);
                    result.b = Mathf.Clamp01(result.b + nightBrightness);
                }
            }

            return result;
        }

        private static Color Desaturate(Color color, float amount)
        {
            float gray = color.grayscale;
            return Color.Lerp(color, new Color(gray, gray, gray, color.a), Mathf.Clamp01(amount));
        }

        private static Color ApplySurfaceStyle(Color baseColor, FpsVisualSurfaceKind kind, int dir, bool indoor)
        {
            return FpsVisualStyle.ApplySurfaceShading(
                baseColor,
                kind,
                dir,
                indoor,
                Plugin.Settings?.VisualContrastStrength?.Value ?? 0.12f,
                Plugin.Settings?.IndoorShadowStrength?.Value ?? 0.16f);
        }
    }
}
