using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Estimates a sprite's visible vertical span (alpha bounds) and returns
    /// a head-near world-space offset from card center.
    /// </summary>
    internal static class SpriteHeadAnchorUtil
    {
        private const int CastGap = 4;
        private const int CastCount = 5;
        private const float HeadRatioFromCenter = 0.40f;

        private static readonly Dictionary<int, float> VisibleHeightPixelsCache = new Dictionary<int, float>();

        public static bool TryGetHeadOffsetWorld(Card card, out float yOffset)
        {
            yOffset = 0f;
            if (card == null) return false;

            var renderer = card.renderer;
            if (renderer == null || !renderer.hasActor || renderer.actor == null) return false;

            var spriteRenderer = renderer.actor.sr;
            var sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            if (sprite == null) return false;

            float visiblePixels = GetVisibleHeightPixels(sprite);
            if (visiblePixels <= 0f) return false;

            float pixelsPerUnit = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
            float scaleY = Mathf.Abs(renderer.actor.transform.lossyScale.y);
            float visibleWorldHeight = visiblePixels / pixelsPerUnit * scaleY;
            if (visibleWorldHeight <= 0f) return false;

            yOffset = visibleWorldHeight * HeadRatioFromCenter;
            return true;
        }

        private static float GetVisibleHeightPixels(Sprite sprite)
        {
            int key = sprite.GetInstanceID();
            if (VisibleHeightPixelsCache.TryGetValue(key, out float cached))
                return cached;

            float fallback = sprite.rect.height;
            float up = RaycastAverageDistance(sprite, downward: false);
            float down = RaycastAverageDistance(sprite, downward: true);

            float visible = fallback;
            if (up >= 0f && down >= 0f && down > up)
            {
                visible = down - up;
            }

            VisibleHeightPixelsCache[key] = visible;
            return visible;
        }

        private static float RaycastAverageDistance(Sprite sprite, bool downward)
        {
            try
            {
                var rect = sprite.rect;
                int width = Mathf.Max(1, (int)rect.width);
                int height = Mathf.Max(1, (int)rect.height);
                int spread = (CastCount - 1) * CastGap / 2;
                int baseX = Mathf.Max(0, width / 2 - spread);
                int startY = downward ? height - 1 : 0;
                int dirY = downward ? -1 : 1;

                float sum = 0f;
                int hits = 0;
                for (int i = 0; i < CastCount; i++)
                {
                    int localX = Mathf.Clamp(baseX + CastGap * i, 0, width - 1);
                    int dist = RaycastOne(sprite, localX, startY, dirY);
                    if (dist < 0) continue;
                    sum += dist;
                    hits++;
                }

                return hits > 0 ? sum / hits : -1f;
            }
            catch
            {
                return -1f;
            }
        }

        private static int RaycastOne(Sprite sprite, int localX, int localStartY, int dirY)
        {
            var rect = sprite.rect;
            var tex = sprite.texture;
            if (tex == null) return -1;

            int texX = (int)rect.xMin + localX;
            int y = localStartY;
            int h = Mathf.Max(1, (int)rect.height);

            for (int steps = 0; steps < h; steps++)
            {
                int texY = (int)rect.yMin + y;
                if (texX >= 0 && texX < tex.width && texY >= 0 && texY < tex.height)
                {
                    Color c = tex.GetPixel(texX, texY);
                    if (!Mathf.Approximately(c.a, 0f))
                        return Math.Abs(localStartY - y);
                }

                y += dirY;
                if (y < 0 || y >= h)
                    break;
            }

            return -1;
        }
    }
}
