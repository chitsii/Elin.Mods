using System;

namespace Elin_Elinikki
{
    internal static class FpsWallMountedAspect
    {
        public static float ApplyHorizontalScale(float widthWorld, float scale)
        {
            return Math.Max(0.05f, widthWorld * Clamp(scale, 0.2f, 1f));
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
