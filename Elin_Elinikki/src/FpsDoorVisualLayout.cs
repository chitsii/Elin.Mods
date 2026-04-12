using System;

namespace Elin_Elinikki
{
    internal readonly struct FpsDoorPanelDimensions
    {
        public FpsDoorPanelDimensions(float widthWorld, float heightWorld)
        {
            WidthWorld = widthWorld;
            HeightWorld = heightWorld;
        }

        public float WidthWorld { get; }

        public float HeightWorld { get; }
    }

    internal readonly struct FpsDoorJambSpans
    {
        public FpsDoorJambSpans(float leftStart, float leftEnd, float rightStart, float rightEnd)
        {
            LeftStart = leftStart;
            LeftEnd = leftEnd;
            RightStart = rightStart;
            RightEnd = rightEnd;
        }

        public float LeftStart { get; }

        public float LeftEnd { get; }

        public float RightStart { get; }

        public float RightEnd { get; }
    }

    internal static class FpsDoorVisualLayout
    {
        public static float ResolveOpeningTop(float bottom, float top)
        {
            return ResolveOpeningTop(bottom, top, 0f);
        }

        public static float ResolveOpeningTop(float bottom, float top, float preferredDoorHeightWorld)
        {
            float wallHeight = Math.Max(0f, top - bottom);
            if (wallHeight <= 0.01f)
            {
                return top;
            }

            float maxOpeningHeight = Math.Max(0.05f, wallHeight - 0.18f);
            float minOpeningHeight = Math.Min(0.72f, maxOpeningHeight);
            float openingHeight = preferredDoorHeightWorld > 0.05f
                ? Clamp(preferredDoorHeightWorld + 0.06f, minOpeningHeight, Math.Min(0.98f, maxOpeningHeight))
                : Clamp(0.78f, minOpeningHeight, Math.Min(0.92f, maxOpeningHeight));
            if (openingHeight >= wallHeight)
            {
                openingHeight = maxOpeningHeight;
            }

            return Math.Min(top, bottom + openingHeight);
        }

        public static FpsDoorPanelDimensions ResolvePanelDimensions(float textureWidthPixels, float textureHeightPixels, float bottom, float top)
        {
            float openingTop = ResolveOpeningTop(bottom, top);
            float heightWorld = Math.Max(0.05f, openingTop - bottom);
            float aspect = textureHeightPixels > 1f
                ? textureWidthPixels / textureHeightPixels
                : 1f;
            float widthWorld = Clamp(heightWorld * aspect, 0.36f, 0.92f);
            return new FpsDoorPanelDimensions(widthWorld, heightWorld);
        }

        public static FpsDoorPanelDimensions ResolvePanelDimensions(float nativeWidthWorld, float nativeHeightWorld, float bottom, float top, float fallbackAspect)
        {
            float openingTop = ResolveOpeningTop(bottom, top, nativeHeightWorld);
            float openingHeight = Math.Max(0.05f, openingTop - bottom);
            float baseHeight = openingHeight;
            float aspect = nativeWidthWorld > 0.01f && nativeHeightWorld > 0.01f
                ? nativeWidthWorld / nativeHeightWorld
                : fallbackAspect > 0.01f
                    ? fallbackAspect
                    : 0.5f;
            float widthWorld = Clamp(baseHeight * aspect, 0.36f, 0.92f);
            return new FpsDoorPanelDimensions(widthWorld, baseHeight);
        }

        public static float ResolveOpeningWidth(float horizontalScale)
        {
            return Clamp(horizontalScale, 0.35f, 0.92f);
        }

        public static FpsDoorJambSpans ResolveJambSpans(float cellStart, float horizontalScale)
        {
            float width = ResolveOpeningWidth(horizontalScale);
            float sideWidth = Math.Max(0.04f, (1f - width) * 0.5f);
            return new FpsDoorJambSpans(
                cellStart,
                cellStart + sideWidth,
                cellStart + 1f - sideWidth,
                cellStart + 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
