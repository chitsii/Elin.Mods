using UnityEngine;

namespace Elin_Elinikki
{
    internal static class FpsIndoorCeilingResolver
    {
        public static float Resolve(float maxSurface, bool hasWallTop, float maxWallTop, float configuredFallbackHeight)
        {
            if (hasWallTop)
            {
                return Mathf.Max(maxSurface + 0.4f, maxWallTop - 0.02f);
            }

            return maxSurface + Mathf.Max(0.4f, configuredFallbackHeight);
        }
    }
}
