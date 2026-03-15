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
        private readonly Dictionary<long, Texture2D> _wallMountedCache = new Dictionary<long, Texture2D>();
        private readonly HashSet<long> _dumpedDebugTextures = new HashSet<long>();
        private readonly HashSet<long> _dumpedWallMountedDebugSheets = new HashSet<long>();
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

            foreach (Texture2D texture in _wallMountedCache.Values)
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
            _wallMountedCache.Clear();
            _dumpedDebugTextures.Clear();
            _dumpedWallMountedDebugSheets.Clear();
        }

        public bool TryGetWallMountedTexture(RenderData renderData, int tile, bool flipX, bool trimTransparent, int materialColor, bool hasMaterialTint, bool useSelectiveMaterialTint, FpsResolvedLightSample light, out Texture texture)
        {
            texture = null;
            if (renderData?.pass == null)
            {
                return false;
            }

            long cacheKey = (((long)renderData.pass.GetInstanceID()) << 32)
                ^ (uint)tile
                ^ (flipX ? (1L << 62) : 0L)
                ^ (trimTransparent ? (1L << 61) : 0L)
                ^ ((long)materialColor << 5)
                ^ ((long)light.PackedLight << 17)
                ^ (useSelectiveMaterialTint ? (1L << 60) : 0L)
                ^ (hasMaterialTint ? (1L << 59) : 0L);
            if (_wallMountedCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            if (!TryResolveRenderDataTextureDimensions(renderData, out int width, out int height))
            {
                return false;
            }

            Color32[] bakedPixels = new Color32[width * height];
            bool hasOpaque = false;
            int minX = width;
            int maxX = -1;
            int minY = height;
            int maxY = -1;

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
                    Vector2 sourceUv = MapWallMountedProjectionUv(u, v);
                    Color32 color = _atlasSampler.TrySampleRenderTile(renderData, tile, sourceUv.x, sourceUv.y, out Color32 sampled)
                        ? sampled
                        : new Color32(0, 0, 0, 0);

                    if (color.a > 8)
                    {
                        hasOpaque = true;
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                        if (hasMaterialTint)
                        {
                            if (!useSelectiveMaterialTint || ShouldApplySelectiveMatTint(color))
                            {
                                color = FpsIdealizedWorld.ApplyMatTint(color, materialColor);
                            }
                        }

                        color = FpsLightApplicator.ApplySample(color, light);
                    }

                    bakedPixels[y * width + x] = color;
                }
            }

            if (!hasOpaque)
            {
                return false;
            }

            Texture2D texture2D;
            if (trimTransparent)
            {
                int trimTop = 0;
                int trimmedWidth = Mathf.Max(1, maxX - minX + 1);
                int trimmedHeight = Mathf.Max(1, maxY - trimTop + 1);
                Color32[] trimmed = new Color32[trimmedWidth * trimmedHeight];
                for (int y = 0; y < trimmedHeight; y++)
                {
                    Array.Copy(bakedPixels, (trimTop + y) * width + minX, trimmed, y * trimmedWidth, trimmedWidth);
                }

                texture2D = new Texture2D(trimmedWidth, trimmedHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"FpsGpuWallMountedTrimmed_{cacheKey}"
                };
                texture2D.SetPixels32(trimmed);
            }
            else
            {
                texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"FpsGpuWallMounted_{cacheKey}"
                };
                texture2D.SetPixels32(bakedPixels);
            }

            texture2D.Apply(false, false);
            _wallMountedCache[cacheKey] = texture2D;
            MaybeDumpWallMountedProjectionDebug(renderData, tile, flipX, width, height);
            texture = texture2D;
            return true;
        }

        public bool TryGetWallMountedTexture(Sprite sprite, bool flipX, bool trimTransparent, FpsResolvedLightSample light, out Texture texture)
        {
            texture = null;
            if (sprite?.texture == null)
            {
                return false;
            }

            int sourceId = sprite.GetInstanceID();
            long cacheKey = (((long)sourceId) << 32)
                ^ (flipX ? (1L << 62) : 0L)
                ^ (trimTransparent ? (1L << 61) : 0L)
                ^ ((long)light.PackedLight << 17)
                ^ (1L << 58);
            if (_wallMountedCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            Rect rect = sprite.textureRect;
            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            Color32[] sourcePixels = sprite.texture.GetPixels32();
            int sourceWidth = sprite.texture.width;
            int sourceHeight = sprite.texture.height;
            int startX = Mathf.RoundToInt(rect.xMin);
            int startY = Mathf.RoundToInt(rect.yMin);

            Color32[] bakedPixels = new Color32[width * height];
            bool hasOpaque = false;
            int minX = width;
            int maxX = -1;
            int maxY = -1;

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

                    Vector2 sourceUv = MapWallMountedProjectionUv(u, v);
                    int sourcePixelX = Mathf.Clamp(startX + Mathf.FloorToInt(sourceUv.x * width), 0, sourceWidth - 1);
                    int sourcePixelY = Mathf.Clamp(startY + Mathf.FloorToInt(sourceUv.y * height), 0, sourceHeight - 1);
                    Color32 color = sourcePixels[sourcePixelY * sourceWidth + sourcePixelX];
                    if (color.a > 8)
                    {
                        hasOpaque = true;
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                        color = FpsLightApplicator.ApplySample(color, light);
                    }

                    bakedPixels[y * width + x] = color;
                }
            }

            if (!hasOpaque)
            {
                return false;
            }

            Texture2D texture2D;
            if (trimTransparent)
            {
                int trimmedWidth = Mathf.Max(1, maxX - minX + 1);
                int trimmedHeight = Mathf.Max(1, maxY + 1);
                Color32[] trimmed = new Color32[trimmedWidth * trimmedHeight];
                for (int y = 0; y < trimmedHeight; y++)
                {
                    System.Array.Copy(bakedPixels, y * width + minX, trimmed, y * trimmedWidth, trimmedWidth);
                }

                texture2D = new Texture2D(trimmedWidth, trimmedHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"FpsGpuWallMountedSpriteTrimmed_{cacheKey}"
                };
                texture2D.SetPixels32(trimmed);
            }
            else
            {
                texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"FpsGpuWallMountedSprite_{cacheKey}"
                };
                texture2D.SetPixels32(bakedPixels);
            }

            texture2D.Apply(false, false);
            _wallMountedCache[cacheKey] = texture2D;
            texture = texture2D;
            return true;
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

            if (!TryResolveRenderDataTextureDimensions(renderData, out int width, out int height))
            {
                return false;
            }

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

            if (!TryResolveRenderDataTextureDimensions(renderData, out int width, out int height))
            {
                return false;
            }

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

        public bool TryGetSelectiveTintRenderTileTexture(RenderData renderData, int tile, bool flipX, int materialColor, FpsResolvedLightSample light, out Texture texture)
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
                ^ ((long)light.PackedLight << 17)
                ^ (1L << 61);
            if (_tintedRenderTileCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            if (!TryResolveRenderDataTextureDimensions(renderData, out int width, out int height))
            {
                return false;
            }

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
                        if (ShouldApplySelectiveMatTint(color))
                        {
                            color = FpsIdealizedWorld.ApplyMatTint(color, materialColor);
                        }

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
                name = $"FpsGpuSelectiveTintRenderTile_{cacheKey}"
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

        private static bool TryResolveRenderDataTextureDimensions(RenderData renderData, out int width, out int height)
        {
            width = 0;
            height = 0;

            Texture texture = renderData?.pass?.mat?.GetTexture("_MainTex");
            ProceduralMesh pmesh = renderData?.pass?.pmesh;
            if (texture == null || pmesh == null || pmesh.tiling.x <= 0f || pmesh.tiling.y <= 0f)
            {
                return false;
            }

            width = Mathf.Max(1, Mathf.RoundToInt(texture.width / pmesh.tiling.x));
            height = Mathf.Max(1, Mathf.RoundToInt(texture.height / pmesh.tiling.y));
            if (renderData.multiSize)
            {
                height *= 2;
            }

            return true;
        }

        private static bool ShouldApplySelectiveMatTint(Color32 color)
        {
            int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            int range = max - min;
            int luminance = (color.r + color.g + color.b) / 3;

            if (range > 8 || luminance < 48)
            {
                return false;
            }

            return true;
        }

        private static Vector2 MapWallMountedProjectionUv(float u, float v)
        {
            return MapWallMountedProjectionUv(u, v, 0.48f);
        }

        private static Vector2 MapWallMountedProjectionUv(float u, float v, float shear)
        {
            float clampedU = Mathf.Clamp01(u);
            float clampedV = Mathf.Clamp01(v);
            float shiftedV = clampedV + (clampedU - 0.5f) * shear;
            return new Vector2(clampedU, Mathf.Clamp01(shiftedV));
        }

        private void MaybeDumpWallMountedProjectionDebug(RenderData renderData, int tile, bool flipX, int width, int height)
        {
            if (Plugin.Settings?.EnableGpuDiagnostics?.Value != true || renderData?.pass == null)
            {
                return;
            }

            long debugKey = (((long)renderData.pass.GetInstanceID()) << 32) ^ (uint)tile ^ (flipX ? (1L << 62) : 0L);
            if (_dumpedWallMountedDebugSheets.Contains(debugKey))
            {
                return;
            }

            _dumpedWallMountedDebugSheets.Add(debugKey);
            try
            {
                float[] candidateShears =
                {
                    0.22f,
                    0.28f,
                    0.38f,
                    0.48f
                };

                int columns = 5;
                int spacing = 4;
                int sheetWidth = columns * width + (columns - 1) * spacing;
                int sheetHeight = height;
                Color32[] sheet = new Color32[sheetWidth * sheetHeight];
                for (int i = 0; i < sheet.Length; i++)
                {
                    sheet[i] = new Color32(20, 20, 20, 255);
                }

                BlitWallMountedCandidate(sheet, sheetWidth, 0, 0, renderData, tile, flipX, width, height, false, 0f);
                for (int i = 0; i < 4; i++)
                {
                    BlitWallMountedCandidate(
                        sheet,
                        sheetWidth,
                        (i + 1) * (width + spacing),
                        0,
                        renderData,
                        tile,
                        flipX,
                        width,
                        height,
                        true,
                        candidateShears[i]);
                }

                Texture2D contactSheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"FpsGpuWallProjectionDebug_{debugKey}"
                };
                contactSheet.SetPixels32(sheet);
                contactSheet.Apply(false, false);

                string directory = Path.Combine(Environment.CurrentDirectory, "_tmp_gpu_wall_projection_debug");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"tile_{tile}_wall_projection_sheet.png");
                File.WriteAllBytes(path, contactSheet.EncodeToPNG());
                UnityEngine.Object.Destroy(contactSheet);
                Plugin.Log?.LogInfo($"Wrote GPU wall projection debug sheet: {path} (columns: source, shear 0.22, 0.28, 0.38, 0.48)");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to dump wall projection debug sheet: {ex.Message}");
            }
        }

        private void BlitWallMountedCandidate(
            Color32[] destination,
            int destinationWidth,
            int offsetX,
            int offsetY,
            RenderData renderData,
            int tile,
            bool flipX,
            int width,
            int height,
            bool useProjection,
            float shear)
        {
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

                    Vector2 sampleUv = useProjection
                        ? MapWallMountedProjectionUv(u, v, shear)
                        : new Vector2(u, v);
                    Color32 color = _atlasSampler.TrySampleRenderTile(renderData, tile, sampleUv.x, sampleUv.y, out Color32 sampled)
                        ? sampled
                        : new Color32(0, 0, 0, 0);
                    destination[(offsetY + y) * destinationWidth + offsetX + x] = color.a > 0 ? color : new Color32(0, 0, 0, 0);
                }
            }
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
