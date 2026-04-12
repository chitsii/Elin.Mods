using UnityEngine;

namespace Elin_Elinikki
{
    internal static class FpsRenderDataWorldSizeResolver
    {
        public static Vector2 Resolve(
            Vector2 sourcePixelSize,
            Vector2 imageScale,
            Vector2 renderDataSize,
            Vector3 pmeshSize,
            bool multiSize,
            float spritePixelsPerTile,
            int footprintWidth = 1,
            int footprintHeight = 1)
        {
            float safePixelsPerTile = Mathf.Max(1f, spritePixelsPerTile);
            float scaleX = Mathf.Max(0.1f, imageScale.x);
            float scaleY = Mathf.Max(0.1f, imageScale.y);

            float pixelWidthWorld = Mathf.Max(0.02f, sourcePixelSize.x * scaleX / safePixelsPerTile);
            float pixelHeightWorld = Mathf.Max(0.02f, sourcePixelSize.y * scaleY / safePixelsPerTile);

            float meshWidthWorld = Mathf.Max(renderDataSize.x, pmeshSize.x);
            float meshHeightWorld = Mathf.Max(renderDataSize.y, pmeshSize.y);
            if (multiSize)
            {
                meshHeightWorld = Mathf.Max(meshHeightWorld, pmeshSize.y * 2f);
            }

            float widthWorld = Mathf.Max(pixelWidthWorld, Mathf.Max(0.02f, meshWidthWorld));
            float heightWorld = Mathf.Max(pixelHeightWorld, Mathf.Max(0.02f, meshHeightWorld));
            Vector2 size = new Vector2(widthWorld, heightWorld);
            return ApplyFootprintScale(size, footprintWidth, footprintHeight);
        }

        public static Vector2 ApplyFootprintScale(Vector2 size, int footprintWidth, int footprintHeight)
        {
            float safeWidth = Mathf.Max(0.02f, size.x);
            float footprintScale = ResolveFootprintScale(footprintWidth, footprintHeight);
            if (footprintScale <= 1f)
            {
                return size;
            }

            float targetWidth = Mathf.Max(size.x, footprintScale);
            float scale = targetWidth / safeWidth;
            return new Vector2(targetWidth, Mathf.Max(0.02f, size.y * scale));
        }

        public static float ResolveFootprintScale(int footprintWidth, int footprintHeight)
        {
            int clampedFootprintWidth = Mathf.Max(1, footprintWidth);
            int clampedFootprintHeight = Mathf.Max(1, footprintHeight);
            return Mathf.Sqrt(clampedFootprintWidth * clampedFootprintHeight);
        }
    }
}
