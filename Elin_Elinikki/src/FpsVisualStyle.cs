using System;
using UnityEngine;

namespace Elin_Elinikki
{
    internal enum FpsVisualSurfaceKind
    {
        TerrainTop,
        TerrainSide,
        WallFront,
        WallSide,
        WallTop,
        Ceiling,
        GroundSprite,
        UprightSprite
    }

    internal static class FpsVisualStyle
    {
        public static Color ApplySurfaceShading(
            Color baseColor,
            FpsVisualSurfaceKind kind,
            int dir,
            bool indoor,
            float contrastStrength,
            float indoorOcclusionStrength)
        {
            float brightness = 1f;
            float saturation = 1f;
            float contrast = 0f;

            switch (kind)
            {
                case FpsVisualSurfaceKind.TerrainTop:
                    brightness = 1.03f;
                    saturation = 0.98f;
                    contrast = 0.18f;
                    break;
                case FpsVisualSurfaceKind.TerrainSide:
                    brightness = 0.84f;
                    saturation = 0.92f;
                    contrast = 0.24f;
                    break;
                case FpsVisualSurfaceKind.WallFront:
                    brightness = 0.97f + ResolveDirectionalBias(dir, 0.07f);
                    saturation = 0.98f;
                    contrast = 0.22f;
                    break;
                case FpsVisualSurfaceKind.WallSide:
                    brightness = 0.86f + ResolveDirectionalBias(dir, 0.05f);
                    saturation = 0.94f;
                    contrast = 0.24f;
                    break;
                case FpsVisualSurfaceKind.WallTop:
                    brightness = 1.06f;
                    saturation = 0.96f;
                    contrast = 0.12f;
                    break;
                case FpsVisualSurfaceKind.Ceiling:
                    brightness = 0.82f;
                    saturation = 0.93f;
                    contrast = 0.16f;
                    break;
                case FpsVisualSurfaceKind.GroundSprite:
                    brightness = 1.00f;
                    saturation = 1.02f;
                    contrast = 0.10f;
                    break;
                case FpsVisualSurfaceKind.UprightSprite:
                    brightness = 0.99f;
                    saturation = 1.02f;
                    contrast = 0.12f;
                    break;
            }

            if (indoor)
            {
                switch (kind)
                {
                    case FpsVisualSurfaceKind.WallFront:
                    case FpsVisualSurfaceKind.WallSide:
                    case FpsVisualSurfaceKind.UprightSprite:
                        brightness -= indoorOcclusionStrength * 0.55f;
                        saturation *= 0.97f;
                        break;
                    case FpsVisualSurfaceKind.WallTop:
                    case FpsVisualSurfaceKind.Ceiling:
                        brightness -= indoorOcclusionStrength * 0.85f;
                        saturation *= 0.94f;
                        break;
                }
            }

            Color result = MultiplyRgb(baseColor, brightness);
            result = AdjustSaturation(result, saturation);
            result = AdjustContrast(result, 1f + (contrastStrength * contrast));
            result.a = baseColor.a;
            return Clamp01(result);
        }

        public static Color GradeFogColor(Color fogColor, string category, float strength)
        {
            if (strength <= 0.001f)
            {
                return fogColor;
            }

            Color target = fogColor;
            if (category.StartsWith("terrain", StringComparison.Ordinal))
            {
                target = new Color(0.72f, 0.79f, 0.83f, fogColor.a);
            }
            else if (string.Equals(category, "roof-interior", StringComparison.Ordinal))
            {
                target = new Color(0.70f, 0.67f, 0.62f, fogColor.a);
            }
            else if (category.StartsWith("ground", StringComparison.Ordinal) || category.StartsWith("upright", StringComparison.Ordinal))
            {
                target = new Color(0.76f, 0.74f, 0.70f, fogColor.a);
            }

            Color graded = Color.Lerp(fogColor, target, Mathf.Clamp01(strength));
            graded.a = fogColor.a;
            return Clamp01(graded);
        }

        private static float ResolveDirectionalBias(int dir, float amplitude)
        {
            switch (NormalizeDir(dir))
            {
                case 0:
                    return -amplitude * 0.40f;
                case 1:
                    return amplitude * 0.20f;
                case 2:
                    return amplitude * 0.55f;
                default:
                    return -amplitude * 0.10f;
            }
        }

        private static int NormalizeDir(int dir)
        {
            int normalized = dir % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }

        private static Color MultiplyRgb(Color color, float multiplier)
        {
            return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
        }

        private static Color AdjustSaturation(Color color, float saturation)
        {
            float gray = color.grayscale;
            return new Color(
                gray + ((color.r - gray) * saturation),
                gray + ((color.g - gray) * saturation),
                gray + ((color.b - gray) * saturation),
                color.a);
        }

        private static Color AdjustContrast(Color color, float contrast)
        {
            return new Color(
                (((color.r - 0.5f) * contrast) + 0.5f),
                (((color.g - 0.5f) * contrast) + 0.5f),
                (((color.b - 0.5f) * contrast) + 0.5f),
                color.a);
        }

        private static Color Clamp01(Color color)
        {
            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }
    }
}
