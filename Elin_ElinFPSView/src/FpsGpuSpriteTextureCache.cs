using System.Collections.Generic;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsGpuSpriteTextureCache
    {
        private readonly Dictionary<int, Texture2D> _spriteCache = new Dictionary<int, Texture2D>();
        private readonly Dictionary<long, Texture2D> _renderTileCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _wallTileCache = new Dictionary<long, Texture2D>();
        private readonly FpsAtlasSampler _atlasSampler = new FpsAtlasSampler();

        public void Dispose()
        {
            foreach (Texture2D texture in _spriteCache.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }

            foreach (Texture2D texture in _renderTileCache.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }

            foreach (Texture2D texture in _wallTileCache.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }

            _spriteCache.Clear();
            _renderTileCache.Clear();
            _wallTileCache.Clear();
        }

        public bool TryGetTexture(Sprite sprite, out Texture texture)
        {
            texture = null;
            if (sprite?.texture == null)
            {
                return false;
            }

            int spriteId = sprite.GetInstanceID();
            if (_spriteCache.TryGetValue(spriteId, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            Rect rect = sprite.textureRect;
            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            Color32[] sourcePixels = sprite.texture.GetPixels32();
            Color32[] bakedPixels = new Color32[width * height];
            int sourceWidth = sprite.texture.width;
            int sourceHeight = sprite.texture.height;
            int startX = Mathf.RoundToInt(rect.xMin);
            int startY = Mathf.RoundToInt(rect.yMin);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceX = Mathf.Clamp(startX + x, 0, sourceWidth - 1);
                    int sourceY = Mathf.Clamp(startY + y, 0, sourceHeight - 1);
                    bakedPixels[y * width + x] = sourcePixels[sourceY * sourceWidth + sourceX];
                }
            }

            Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"FpsGpuSprite_{spriteId}"
            };
            texture2D.SetPixels32(bakedPixels);
            texture2D.Apply(false, false);

            _spriteCache[spriteId] = texture2D;
            texture = texture2D;
            return true;
        }

        public bool TryGetTexture(RenderData renderData, int tile, out Texture texture)
        {
            return TryGetTexture(renderData, tile, false, out texture);
        }

        public bool TryGetTexture(RenderData renderData, int tile, bool flipX, out Texture texture)
        {
            texture = null;
            if (renderData?.pass == null)
            {
                return false;
            }

            long cacheKey = (((long)renderData.pass.GetInstanceID()) << 32)
                ^ (uint)tile
                ^ (flipX ? (1L << 62) : 0L)
                ^ (renderData.multiSize ? (1L << 63) : 0L);
            if (_renderTileCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            int width = 64;
            int height = renderData.multiSize ? 128 : 64;
            Color32[] pixels = new Color32[width * height];
            bool hasOpaque = false;
            for (int y = 0; y < height; y++)
            {
                float v = 1f - (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    if (flipX)
                    {
                        u = 1f - u;
                    }
                    Color32 color = _atlasSampler.TrySampleRenderTile(renderData, tile, u, v, out Color32 sampled)
                        ? sampled
                        : new Color32(0, 0, 0, 0);
                    if (color.a > 8)
                    {
                        hasOpaque = true;
                    }

                    pixels[y * width + x] = color;
                }
            }

            if (!hasOpaque)
            {
                return false;
            }

            Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"FpsGpuRenderTile_{cacheKey}"
            };
            texture2D.SetPixels32(pixels);
            texture2D.Apply(false, false);

            _renderTileCache[cacheKey] = texture2D;
            texture = texture2D;
            return true;
        }

        public bool TryGetWallTexture(FpsResolvedWallSurface surface, bool hitVertical, bool flipX, out Texture texture)
        {
            return TryGetBlockFaceTexture(surface, hitVertical ? FpsAtlasSampler.BlockFaceKind.Right : FpsAtlasSampler.BlockFaceKind.Left, flipX, out texture);
        }

        public bool TryGetBlockFaceTexture(FpsResolvedWallSurface surface, FpsAtlasSampler.BlockFaceKind face, bool flipX, out Texture texture)
        {
            texture = null;
            long cacheKey = ((long)surface.Tile << 2)
                ^ (surface.UseSnowAtlas ? 1L : 0L)
                ^ ((long)face << 2)
                ^ (flipX ? 16L : 0L)
                ^ ((long)surface.MaterialColor << 5);
            if (_wallTileCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            const int size = 64;
            Color32[] pixels = new Color32[size * size];
            bool hasOpaque = false;
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    if (flipX)
                    {
                        u = 1f - u;
                    }
                    Color32 color = _atlasSampler.TrySampleBlockFace(surface, u, v, face, out Color32 sampled)
                        ? sampled
                        : new Color32(0, 0, 0, 0);
                    if (color.a > 8)
                    {
                        hasOpaque = true;
                    }

                    pixels[y * size + x] = color;
                }
            }

            if (!hasOpaque)
            {
                return false;
            }

            Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"FpsGpuWallTile_{cacheKey}"
            };
            texture2D.SetPixels32(pixels);
            texture2D.Apply(false, false);
            _wallTileCache[cacheKey] = texture2D;
            texture = texture2D;
            return true;
        }
    }
}
