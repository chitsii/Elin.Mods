using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsGpuSpriteTextureCache
    {
        private readonly Dictionary<int, Texture2D> _spriteCache = new Dictionary<int, Texture2D>();
        private readonly Dictionary<long, Texture2D> _renderTileCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _tintedRenderTileCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _wallTileCache = new Dictionary<long, Texture2D>();
        private readonly HashSet<long> _dumpedDebugTextures = new HashSet<long>();
        private readonly FpsAtlasSampler _atlasSampler = new FpsAtlasSampler();
        private const float MinimumBlockFaceOpaqueRatio = 0.08f;

        public void Dispose()
        {
            foreach (Texture2D texture in _spriteCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            foreach (Texture2D texture in _renderTileCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            foreach (Texture2D texture in _tintedRenderTileCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            foreach (Texture2D texture in _wallTileCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            _spriteCache.Clear();
            _renderTileCache.Clear();
            _tintedRenderTileCache.Clear();
            _wallTileCache.Clear();
            _dumpedDebugTextures.Clear();
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

        public bool TryGetTintedRenderTileTexture(RenderData renderData, int tile, bool flipX, int materialColor, FpsResolvedLightSample light, out Texture texture)
        {
            texture = null;
            if (renderData?.pass == null)
            {
                return false;
            }

            long cacheKey = (((long)renderData.pass.GetInstanceID()) << 32)
                ^ (uint)tile
                ^ (flipX ? (1L << 62) : 0L)
                ^ (renderData.multiSize ? (1L << 63) : 0L)
                ^ ((long)materialColor << 5)
                ^ ((long)light.PackedLight << 17);
            if (_tintedRenderTileCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            int width = 64;
            int height = renderData.multiSize ? 128 : 64;
            Color32[] bakedPixels = new Color32[width * height];
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
                        color = FpsIdealizedWorld.ApplyMatTint(color, materialColor);
                        color = FpsLightApplicator.ApplySample(color, light);
                        hasOpaque = true;
                    }

                    bakedPixels[y * width + x] = color;
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
                name = $"FpsGpuTintedRenderTile_{cacheKey}"
            };
            texture2D.SetPixels32(bakedPixels);
            texture2D.Apply(false, false);

            _tintedRenderTileCache[cacheKey] = texture2D;
            texture = texture2D;
            return true;
        }

        public bool TryGetWallTexture(FpsResolvedWallSurface surface, bool hitVertical, bool flipX, out Texture texture)
        {
            return TryGetBlockFaceTexture(surface, hitVertical ? FpsAtlasSampler.BlockFaceKind.Right : FpsAtlasSampler.BlockFaceKind.Left, flipX, out texture);
        }

        public bool TryGetBlockFaceTexture(FpsResolvedWallSurface surface, bool hitVertical, out Texture texture)
        {
            return TryGetBlockFaceTexture(surface, hitVertical ? FpsAtlasSampler.BlockFaceKind.Right : FpsAtlasSampler.BlockFaceKind.Left, false, out texture);
        }

        public bool TryGetBlockFaceTexture(FpsResolvedWallSurface surface, FpsAtlasSampler.BlockFaceKind face, bool flipX, out Texture texture)
        {
            texture = null;
            long cacheKey = ((long)surface.Tile << 2)
                ^ (surface.UseSnowAtlas ? 1L : 0L)
                ^ ((long)face << 2)
                ^ (flipX ? 16L : 0L)
                ^ ((long)surface.MaterialColor << 5)
                ^ ((long)surface.Light.PackedLight << 17);
            if (_wallTileCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            const int size = 64;
            Color32[] pixels = new Color32[size * size];
            bool hasOpaque = false;
            int opaqueCount = 0;
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
                        color = FpsIdealizedWorld.ApplyMatTint(color, surface.MaterialColor);
                        color = FpsLightApplicator.ApplySample(color, surface.Light);
                    }
                    if (color.a > 8)
                    {
                        hasOpaque = true;
                        opaqueCount++;
                    }

                    pixels[y * size + x] = color;
                }
            }

            if (!hasOpaque || opaqueCount < size * size * MinimumBlockFaceOpaqueRatio)
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
            MaybeDumpBlockDebugTexture(cacheKey, texture2D, surface, face, flipX, opaqueCount / (float)(size * size));
            texture = texture2D;
            return true;
        }

        private void MaybeDumpBlockDebugTexture(long cacheKey, Texture2D texture, FpsResolvedWallSurface surface, FpsAtlasSampler.BlockFaceKind face, bool flipX, float coverage)
        {
            if (Plugin.Settings?.EnableGpuDiagnostics?.Value != true || texture == null || _dumpedDebugTextures.Contains(cacheKey))
            {
                return;
            }

            _dumpedDebugTextures.Add(cacheKey);
            try
            {
                string directory = Path.Combine(Environment.CurrentDirectory, "_tmp_gpu_block_debug");
                Directory.CreateDirectory(directory);
                string fileName = $"tile_{surface.Tile}_face_{face}_flip_{(flipX ? 1 : 0)}_cov_{coverage:0.00}.png";
                string path = Path.Combine(directory, fileName);
                File.WriteAllBytes(path, texture.EncodeToPNG());

                if (surface.RenderData != null && TryGetTexture(surface.RenderData, surface.Tile, out Texture sourceTexture) && sourceTexture is Texture2D sourceTexture2D)
                {
                    string sourcePath = Path.Combine(directory, $"tile_{surface.Tile}_source.png");
                    if (!File.Exists(sourcePath))
                    {
                        File.WriteAllBytes(sourcePath, sourceTexture2D.EncodeToPNG());
                    }
                }

                Plugin.Log?.LogInfo($"Dumped GPU block debug texture: {path}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to dump GPU block debug texture: {ex.Message}");
            }
        }
    }
}
