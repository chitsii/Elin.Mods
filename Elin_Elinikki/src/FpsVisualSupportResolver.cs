using UnityEngine;

namespace Elin_Elinikki
{
    internal static class FpsVisualSupportResolver
    {
        public static float Resolve(float gameplaySupportHeight, float unscaledDisplayHeight, float scaledDisplayHeight)
        {
            if (gameplaySupportHeight <= 0f)
            {
                return 0f;
            }

            if (unscaledDisplayHeight <= 0.0001f || scaledDisplayHeight <= 0.0001f)
            {
                return gameplaySupportHeight;
            }

            float supportRatio = Mathf.Clamp01(gameplaySupportHeight / unscaledDisplayHeight);
            float scaledSupportHeight = scaledDisplayHeight * supportRatio;
            return Mathf.Max(gameplaySupportHeight, scaledSupportHeight);
        }
    }
}
