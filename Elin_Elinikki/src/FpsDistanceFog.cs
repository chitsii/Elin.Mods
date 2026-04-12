using UnityEngine;

namespace Elin_Elinikki
{
    internal static class FpsDistanceFog
    {
        public static float ComputeFogFactor(float radialDistance, float maxDistance, float startRatio, float endRatio, float density)
        {
            if (maxDistance <= 0.01f)
            {
                return 0f;
            }

            float clampedStartRatio = Mathf.Clamp01(startRatio);
            float clampedEndRatio = Mathf.Clamp(endRatio, clampedStartRatio + 0.01f, 1f);
            float fogStart = Mathf.Max(0.1f, maxDistance * clampedStartRatio);
            float fogEnd = Mathf.Max(fogStart + 0.1f, maxDistance * clampedEndRatio);
            if (radialDistance <= fogStart)
            {
                return 0f;
            }

            float fogFactor = Mathf.InverseLerp(fogStart, fogEnd, radialDistance);
            fogFactor = Mathf.SmoothStep(0f, 1f, fogFactor);
            float shapedDensity = Mathf.Max(0.1f, density);
            return 1f - Mathf.Pow(1f - fogFactor, shapedDensity);
        }

        public static Color BlendTowardClearColor(Color hazeColor, Color clearColor, float fogFactor, float clearBlend)
        {
            float blend = Mathf.Lerp(Mathf.Clamp01(clearBlend), 1f, Mathf.Clamp01(fogFactor));
            Color result = Color.Lerp(hazeColor, clearColor, blend);
            result.a = hazeColor.a;
            return result;
        }
    }
}
