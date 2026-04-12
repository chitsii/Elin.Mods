using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class FpsGpuSpriteTextureCache
    {
        private const int SynthesizedSurfaceTextureSize = 32;
        private readonly Dictionary<int, Texture2D> _spriteCache = new Dictionary<int, Texture2D>();
        private readonly Dictionary<long, Texture2D> _renderTileCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _tintedRenderTileCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _wallTileCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _wallMountedCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _surfaceMaterialCache = new Dictionary<long, Texture2D>();
        private readonly Dictionary<long, Texture2D> _roofRenderTileCache = new Dictionary<long, Texture2D>();
        private readonly HashSet<long> _dumpedDebugTextures = new HashSet<long>();
        private readonly HashSet<long> _dumpedWallMountedDebugSheets = new HashSet<long>();
        private readonly HashSet<long> _dumpedRoofDebugTextures = new HashSet<long>();
        private readonly HashSet<long> _dumpedRoofSliceDebugTextures = new HashSet<long>();
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

            foreach (Texture2D texture in _surfaceMaterialCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            foreach (Texture2D texture in _roofRenderTileCache.Values)
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
            _surfaceMaterialCache.Clear();
            _roofRenderTileCache.Clear();
            _dumpedDebugTextures.Clear();
            _dumpedWallMountedDebugSheets.Clear();
            _dumpedRoofDebugTextures.Clear();
            _dumpedRoofSliceDebugTextures.Clear();
        }

        public void MaybeDumpRoofSourceTexture(FpsResolvedRoofPlane plane)
        {
            long debugKey = (((long)(plane.Lot?.id ?? 0)) << 32)
                ^ ((long)plane.Kind << 24)
                ^ ((long)plane.TextureKind << 20)
                ^ (uint)plane.Tile
                ^ (plane.FlipX ? (1L << 62) : 0L);
            if (_dumpedRoofDebugTextures.Contains(debugKey))
            {
                return;
            }

            try
            {
                Texture texture = null;
                string subDirectory = "unknown";
                string baseName = $"lot_{plane.Lot?.id ?? 0}_kind_{plane.Kind}_unresolved";
                string failureReason = null;
                switch (plane.TextureKind)
                {
                    case FpsRoofTextureKind.PrimarySource:
                        if (plane.RenderData == null)
                        {
                            failureReason = "renderData-null";
                            break;
                        }

                        if (!TryGetTexture(plane.RenderData, plane.Tile, plane.FlipX, out texture))
                        {
                            failureReason = "primary-source-unresolved";
                        }
                        if (plane.SourceOrigin == FpsRoofSourceOrigin.RoofTopPrimary)
                        {
                            subDirectory = "block_tiles";
                            baseName = $"lot_{plane.Lot?.id ?? 0}_kind_Top_tile_{plane.Tile}_face_Top_origin_{plane.SourceOrigin}";
                        }
                        else
                        {
                            subDirectory = "render_tiles";
                            baseName = $"lot_{plane.Lot?.id ?? 0}_kind_{plane.Kind}_tile_{plane.Tile}_origin_{plane.SourceOrigin}_flip_{(plane.FlipX ? 1 : 0)}";
                        }
                        break;
                    case FpsRoofTextureKind.BlockFace:
                        if (plane.BlockSurface.RenderData == null)
                        {
                            failureReason = "block-renderData-null";
                            break;
                        }

                        if (!TryGetTexture(plane.BlockSurface.RenderData, plane.BlockSurface.Tile, false, out texture))
                        {
                            failureReason = "block-tile-unresolved";
                        }

                        subDirectory = "block_tiles";
                        baseName = $"lot_{plane.Lot?.id ?? 0}_kind_{plane.Kind}_tile_{plane.BlockSurface.Tile}_face_{plane.BlockFaceKind}";
                        break;
                    default:
                        return;
                }

                string root = Path.Combine(Paths.GameRootPath, "_tmp_gpu_roof_debug", subDirectory);
                Directory.CreateDirectory(root);
                string infoPath = Path.Combine(root, $"{baseName}.txt");
                File.WriteAllText(
                    infoPath,
                    $"lot={plane.Lot?.id ?? 0}\n" +
                    $"kind={plane.Kind}\n" +
                    $"textureKind={plane.TextureKind}\n" +
                    $"tile={plane.Tile}\n" +
                    $"blockTile={plane.BlockSurface.Tile}\n" +
                    $"blockFace={plane.BlockFaceKind}\n" +
                    $"flipX={plane.FlipX}\n" +
                    $"label={plane.DiagnosticLabel}\n" +
                    $"failure={failureReason ?? "none"}\n");

                if (!(texture is Texture2D texture2D))
                {
                    Plugin.Log?.LogWarning($"Failed to resolve GPU roof source texture: {infoPath} reason={failureReason ?? "not-texture2d"}");
                    _dumpedRoofDebugTextures.Add(debugKey);
                    return;
                }

                string imagePath = Path.Combine(root, $"{baseName}.png");
                File.WriteAllBytes(imagePath, texture2D.EncodeToPNG());
                Plugin.Log?.LogInfo($"Dumped GPU roof source texture: {imagePath}");
                _dumpedRoofDebugTextures.Add(debugKey);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to dump roof source texture: {ex.Message}");
            }
        }

        public void MaybeDumpRoofSliceTextures(FpsResolvedRoofPlane plane)
        {
            if (!plane.HasLotPrimarySource)
            {
                return;
            }

            FpsResolvedRoofPrimarySource source = plane.LotPrimarySource;
            long debugKey = (((long)(plane.Lot?.id ?? 0)) << 32)
                ^ ((long)source.RoofTileId << 12)
                ^ ((long)source.TileDirection << 8)
                ^ (uint)source.Tile;
            if (_dumpedRoofSliceDebugTextures.Contains(debugKey))
            {
                return;
            }

            try
            {
                if (!TryGetTexture(source.RenderData, source.Tile, false, out Texture texture) || !(texture is Texture2D sourceTexture))
                {
                    Plugin.Log?.LogWarning(
                        $"Failed to resolve GPU roof slice source texture: lot={plane.Lot?.id ?? 0} roofTile={source.RoofTileId} dir={source.TileDirection} tile={source.Tile}");
                    _dumpedRoofSliceDebugTextures.Add(debugKey);
                    return;
                }

                RoofSliceGuide guide = ResolveRoofSliceGuide();
                RoofRectSlice rectSliceDefinition = guide.RectSlice;
                RoofTriSlice triSliceDefinition = guide.TriSlice;
                string root = Path.Combine(Paths.GameRootPath, "_tmp_gpu_roof_debug", "roof_top_primary_slices");
                Directory.CreateDirectory(root);
                string baseName = $"lot_{plane.Lot?.id ?? 0}_roofTile_{source.RoofTileId}_dir_{source.TileDirection}_kind_Top_tile_{source.Tile}_face_Top";

                string sourcePath = Path.Combine(root, $"{baseName}_source.png");
                File.WriteAllBytes(sourcePath, sourceTexture.EncodeToPNG());

                Texture2D annotated = null;
                Texture2D rectSlice = null;
                Texture2D triSlice = null;
                Texture2D triProjection = null;
                try
                {
                    annotated = CreateRoofSliceAnnotatedTexture(sourceTexture, guide);
                    File.WriteAllBytes(Path.Combine(root, $"{baseName}_source_points.png"), annotated.EncodeToPNG());

                    rectSlice = CreateRoofSliceTexture(sourceTexture, rectSliceDefinition.Polygon);
                    File.WriteAllBytes(Path.Combine(root, $"{baseName}_rect_slice.png"), rectSlice.EncodeToPNG());

                    Texture2D rectProjection = null;
                    try
                    {
                        rectProjection = CreateRectProjectionTexture(sourceTexture, rectSliceDefinition);
                        File.WriteAllBytes(Path.Combine(root, $"{baseName}_rect_projection.png"), rectProjection.EncodeToPNG());
                    }
                    finally
                    {
                        if (rectProjection != null)
                        {
                            UnityEngine.Object.Destroy(rectProjection);
                        }
                    }

                    triSlice = CreateRoofSliceTexture(sourceTexture, triSliceDefinition.Polygon);
                    File.WriteAllBytes(Path.Combine(root, $"{baseName}_tri_slice.png"), triSlice.EncodeToPNG());

                    triProjection = CreateTriProjectionTexture(sourceTexture, triSliceDefinition);
                    File.WriteAllBytes(Path.Combine(root, $"{baseName}_tri_projection.png"), triProjection.EncodeToPNG());
                }
                finally
                {
                    if (annotated != null)
                    {
                        UnityEngine.Object.Destroy(annotated);
                    }

                    if (rectSlice != null)
                    {
                        UnityEngine.Object.Destroy(rectSlice);
                    }

                    if (triSlice != null)
                    {
                        UnityEngine.Object.Destroy(triSlice);
                    }

                    if (triProjection != null)
                    {
                        UnityEngine.Object.Destroy(triProjection);
                    }
                }

                File.WriteAllText(
                    Path.Combine(root, $"{baseName}_slice_points.txt"),
                    $"lot={plane.Lot?.id ?? 0}\n" +
                    $"roofTileId={source.RoofTileId}\n" +
                    $"tileDirection={source.TileDirection}\n" +
                    $"tile={source.Tile}\n" +
                    $"origin={source.Origin}\n" +
                    $"referenceCanvas=96x80\n" +
                    $"A={FormatVector(guide.A)}\n" +
                    $"B={FormatVector(guide.B)}\n" +
                    $"C={FormatVector(guide.C)}\n" +
                    $"D={FormatVector(guide.D)}\n" +
                    $"E={FormatVector(guide.E)}\n" +
                    $"rectSlice=A,B,C,D\n" +
                    $"triSlice=B,C,E\n" +
                    $"triProjection=top:B,bottomLeft:C,bottomRight:E\n" +
                    $"rectProjectionCanvas={sourceTexture.width}x{sourceTexture.height}\n" +
                    $"triProjectionCanvas={sourceTexture.width}x{sourceTexture.height}\n" +
                    $"triProjectionDestination=top:(0.5,0.0),bottomLeft:(0.0,1.0),bottomRight:(1.0,1.0)\n");

                Plugin.Log?.LogInfo($"Dumped GPU roof slice textures: {Path.Combine(root, baseName)}_*");
                _dumpedRoofSliceDebugTextures.Add(debugKey);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to dump roof slice textures: {ex.Message}");
            }
        }

        public bool TryGetRoofRenderTileTexture(FpsResolvedRoofPlane plane, FpsRoofTextureProjectionMode mode, out Texture texture)
        {
            texture = null;
            FpsRoofTextureProjectionMode canonicalMode = ResolveCanonicalRoofTextureProjectionMode(mode);
            if (plane.RenderData?.pass == null || canonicalMode == FpsRoofTextureProjectionMode.SolidGray)
            {
                return false;
            }

            long cacheKey = (((long)plane.RenderData.pass.GetInstanceID()) << 32)
                ^ (uint)plane.Tile
                ^ (plane.FlipX ? (1L << 62) : 0L)
                ^ ((long)canonicalMode << 52)
                ^ ((long)plane.ProjectionKind << 48)
                ^ ((long)plane.Kind << 45);
            if (_roofRenderTileCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            if (!TryGetTexture(plane.RenderData, plane.Tile, plane.FlipX, out Texture sourceTexture)
                || !(sourceTexture is Texture2D sourceTexture2D))
            {
                return false;
            }

            Texture2D projectedTexture = null;
            try
            {
                if (!TryCreateRoofProjectionTexture(sourceTexture2D, plane.ProjectionKind, canonicalMode, out projectedTexture))
                {
                    return false;
                }

                _roofRenderTileCache[cacheKey] = projectedTexture;
                texture = projectedTexture;
                projectedTexture = null;
                return true;
            }
            finally
            {
                if (projectedTexture != null)
                {
                    UnityEngine.Object.Destroy(projectedTexture);
                }
            }
        }

        private static FpsRoofTextureProjectionMode ResolveCanonicalRoofTextureProjectionMode(FpsRoofTextureProjectionMode mode)
        {
            switch (mode)
            {
                case FpsRoofTextureProjectionMode.SolidGray:
                    return FpsRoofTextureProjectionMode.SolidGray;
                case FpsRoofTextureProjectionMode.RawTile:
                    return FpsRoofTextureProjectionMode.RawTile;
                default:
                    return FpsRoofTextureProjectionMode.Projected;
            }
        }

        private static bool TryCreateRoofProjectionTexture(
            Texture2D sourceTexture,
            FpsRoofTextureProjectionKind projectionKind,
            FpsRoofTextureProjectionMode mode,
            out Texture2D texture)
        {
            texture = null;
            if (sourceTexture == null)
            {
                return false;
            }

            if (mode == FpsRoofTextureProjectionMode.RawTile || projectionKind == FpsRoofTextureProjectionKind.RawTile)
            {
                texture = CreateTextureCopy(sourceTexture, "FpsGpuRoofRawTile");
                return texture != null;
            }

            RoofSliceGuide guide = ResolveRoofSliceGuide();
            Texture2D sliceTexture = null;
            try
            {
                switch (projectionKind)
                {
                    case FpsRoofTextureProjectionKind.RectSlice:
                        sliceTexture = CreateRoofSliceTexture(sourceTexture, guide.RectSlice.Polygon);
                        texture = CreateRectProjectionTexture(sliceTexture, guide.RectSlice);
                        return texture != null;
                    case FpsRoofTextureProjectionKind.TriSlice:
                        sliceTexture = CreateRoofSliceTexture(sourceTexture, guide.TriSlice.Polygon);
                        texture = CreateTriProjectionTexture(sliceTexture, guide.TriSlice);
                        return texture != null;
                    default:
                        texture = CreateTextureCopy(sourceTexture, "FpsGpuRoofRawTile");
                        return texture != null;
                }
            }
            finally
            {
                if (sliceTexture != null)
                {
                    UnityEngine.Object.Destroy(sliceTexture);
                }
            }
        }

        private static Texture2D CreateTextureCopy(Texture2D sourceTexture, string textureName)
        {
            if (sourceTexture == null)
            {
                return null;
            }

            Color32[] pixels = sourceTexture.GetPixels32();
            Texture2D texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = textureName
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private bool TrySynthesizeSurfaceTexture(Texture2D sourceTexture, FpsAutoSurfaceMaterialKind kind, out Texture texture)
        {
            texture = null;
            if (sourceTexture == null)
            {
                return false;
            }

            long cacheKey = (((long)sourceTexture.GetInstanceID()) << 8) ^ (long)kind;
            if (_surfaceMaterialCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            Color32[] sourcePixels = sourceTexture.GetPixels32();
            FpsSurfaceColor32[] source = new FpsSurfaceColor32[sourcePixels.Length];
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color32 color = sourcePixels[i];
                source[i] = new FpsSurfaceColor32(color.r, color.g, color.b, color.a);
            }

            FpsSurfaceColor32[] synthesized = kind switch
            {
                FpsAutoSurfaceMaterialKind.Front => FpsSurfaceMaterialSynthesis.BuildFrontMaterial(
                    source,
                    sourceTexture.width,
                    sourceTexture.height,
                    SynthesizedSurfaceTextureSize,
                    SynthesizedSurfaceTextureSize),
                FpsAutoSurfaceMaterialKind.Side => FpsSurfaceMaterialSynthesis.BuildSideMaterial(
                    source,
                    sourceTexture.width,
                    sourceTexture.height,
                    SynthesizedSurfaceTextureSize,
                    SynthesizedSurfaceTextureSize),
                _ => FpsSurfaceMaterialSynthesis.BuildTopMaterial(
                    source,
                    sourceTexture.width,
                    sourceTexture.height,
                    SynthesizedSurfaceTextureSize,
                    SynthesizedSurfaceTextureSize)
            };

            Color32[] pixels = new Color32[synthesized.Length];
            for (int i = 0; i < synthesized.Length; i++)
            {
                FpsSurfaceColor32 color = synthesized[i];
                pixels[i] = new Color32(color.R, color.G, color.B, color.A);
            }

            Texture2D built = new Texture2D(SynthesizedSurfaceTextureSize, SynthesizedSurfaceTextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                name = $"FpsGpuSurface_{kind}_{cacheKey}"
            };
            built.SetPixels32(pixels);
            built.Apply(false, false);
            _surfaceMaterialCache[cacheKey] = built;
            texture = built;
            return true;
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
                int trimTop = Mathf.Max(0, minY);
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
                    int sourcePixelX = Mathf.Clamp(startX + Mathf.FloorToInt(sourceUv.x * width), 0, sourceWidth - 1);
                    int sourcePixelY = Mathf.Clamp(startY + Mathf.FloorToInt(sourceUv.y * height), 0, sourceHeight - 1);
                    Color32 color = sourcePixels[sourcePixelY * sourceWidth + sourcePixelX];
                    if (color.a > 8)
                    {
                        hasOpaque = true;
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
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
                int trimTop = minY;
                int trimmedWidth = Mathf.Max(1, maxX - minX + 1);
                int trimmedHeight = Mathf.Max(1, maxY - trimTop + 1);
                Color32[] trimmed = new Color32[trimmedWidth * trimmedHeight];
                for (int y = 0; y < trimmedHeight; y++)
                {
                    System.Array.Copy(bakedPixels, (trimTop + y) * width + minX, trimmed, y * trimmedWidth, trimmedWidth);
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

        public bool TryGetHybridWallTexture(
            RenderData renderData,
            int tile,
            bool flipX,
            int materialColor,
            bool hasMaterialTint,
            bool useSelectiveMaterialTint,
            FpsResolvedLightSample light,
            FpsAutoSurfaceMaterialKind kind,
            out Texture texture)
        {
            texture = null;
            Texture baseTexture;
            if (useSelectiveMaterialTint && hasMaterialTint)
            {
                if (!TryGetSelectiveTintRenderTileTexture(renderData, tile, flipX, materialColor, light, out baseTexture))
                {
                    return false;
                }
            }
            else if (hasMaterialTint)
            {
                if (!TryGetTintedRenderTileTexture(renderData, tile, flipX, materialColor, light, out baseTexture))
                {
                    return false;
                }
            }
            else if (!TryGetTexture(renderData, tile, flipX, out baseTexture))
            {
                return false;
            }

            return TrySynthesizeSurfaceTexture(baseTexture as Texture2D, kind, out texture);
        }

        public bool TryGetHybridBlockTexture(
            FpsResolvedWallSurface surface,
            bool hitVertical,
            FpsAutoSurfaceMaterialKind kind,
            out Texture texture)
        {
            texture = null;
            if (!TryGetBlockFaceTexture(surface, hitVertical, out Texture baseTexture))
            {
                return false;
            }

            return TrySynthesizeSurfaceTexture(baseTexture as Texture2D, kind, out texture);
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

        private static RoofSliceGuide ResolveRoofSliceGuide()
        {
            // 96x80 source coordinates provided directly from Paint verification.
            return new RoofSliceGuide(
                FromPaintPixels(29f, 5f),
                FromPaintPixels(66f, 24f),
                FromPaintPixels(46f, 61f),
                FromPaintPixels(4f, 41f),
                FromPaintPixels(91f, 36f));
        }

        private static Vector2 FromPaintPixels(float xFromLeft, float yFromTop)
        {
            const float annotatedWidth = 96f;
            const float annotatedHeight = 80f;
            return new Vector2(
                Mathf.Clamp01(xFromLeft / (annotatedWidth - 1f)),
                Mathf.Clamp01(yFromTop / (annotatedHeight - 1f)));
        }

        private static string FormatVector(Vector2 value)
        {
            return $"{value.x:F4},{value.y:F4}";
        }

        private static Texture2D CreateRoofSliceAnnotatedTexture(Texture2D sourceTexture, RoofSliceGuide guide)
        {
            int width = sourceTexture.width;
            int height = sourceTexture.height;
            Color32[] pixels = sourceTexture.GetPixels32();
            Color32[] annotated = new Color32[pixels.Length];
            Array.Copy(pixels, annotated, pixels.Length);

            DrawMarker(annotated, width, height, guide.A, new Color32(255, 32, 32, 255));
            DrawMarker(annotated, width, height, guide.B, new Color32(255, 32, 32, 255));
            DrawMarker(annotated, width, height, guide.C, new Color32(255, 32, 32, 255));
            DrawMarker(annotated, width, height, guide.D, new Color32(255, 32, 32, 255));
            DrawMarker(annotated, width, height, guide.E, new Color32(255, 32, 32, 255));

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FpsGpuRoofSliceAnnotated"
            };
            texture.SetPixels32(annotated);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateRoofSliceTexture(Texture2D sourceTexture, Vector2[] polygon)
        {
            int width = sourceTexture.width;
            int height = sourceTexture.height;
            Color32[] sourcePixels = sourceTexture.GetPixels32();
            Color32[] output = new Color32[sourcePixels.Length];

            for (int y = 0; y < height; y++)
            {
                float vFromTop = 1f - (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    Vector2 sample = new Vector2(u, vFromTop);
                    if (!IsPointInsidePolygon(sample, polygon))
                    {
                        output[y * width + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    output[y * width + x] = sourcePixels[y * width + x];
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FpsGpuRoofSlice"
            };
            texture.SetPixels32(output);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateRectProjectionTexture(Texture2D sourceTexture, RoofRectSlice rectSlice)
        {
            int width = sourceTexture.width;
            int height = sourceTexture.height;
            Color32[] sourcePixels = sourceTexture.GetPixels32();
            Color32[] output = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float vFromTop = 1f - (y + 0.5f) / height;
                Vector2 sourceLeft = Vector2.Lerp(rectSlice.TopLeft, rectSlice.BottomLeft, vFromTop);
                Vector2 sourceRight = Vector2.Lerp(rectSlice.TopRight, rectSlice.BottomRight, vFromTop);
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    Vector2 sourceUvTop = Vector2.Lerp(sourceLeft, sourceRight, u);
                    output[y * width + x] = SampleTextureNearest(sourcePixels, width, height, sourceUvTop.x, sourceUvTop.y);
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FpsGpuRoofRectProjection"
            };
            texture.SetPixels32(output);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateTriProjectionTexture(Texture2D sourceTexture, RoofTriSlice triSlice)
        {
            int width = sourceTexture.width;
            int height = sourceTexture.height;
            Color32[] sourcePixels = sourceTexture.GetPixels32();
            Color32[] output = new Color32[width * height];
            Vector2 destinationTop = new Vector2(0.5f, 0f);
            Vector2 destinationBottomLeft = new Vector2(0f, 1f);
            Vector2 destinationBottomRight = new Vector2(1f, 1f);

            for (int y = 0; y < height; y++)
            {
                float vFromTop = 1f - (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    Vector2 destinationPoint = new Vector2(u, vFromTop);
                    if (!TryGetTriangleBarycentricWeights(
                        destinationPoint,
                        destinationTop,
                        destinationBottomLeft,
                        destinationBottomRight,
                        out float weightTop,
                        out float weightBottomLeft,
                        out float weightBottomRight))
                    {
                        output[y * width + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    Vector2 sourceUvTop = triSlice.Top * weightTop
                        + triSlice.BottomLeft * weightBottomLeft
                        + triSlice.BottomRight * weightBottomRight;
                    output[y * width + x] = SampleTextureNearest(sourcePixels, width, height, sourceUvTop.x, sourceUvTop.y);
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FpsGpuRoofTriProjection"
            };
            texture.SetPixels32(output);
            texture.Apply(false, false);
            return texture;
        }

        private static Color32 SampleTextureNearest(Color32[] pixels, int width, int height, float u, float vTop)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt(u * width), 0, width - 1);
            int yFromTop = Mathf.Clamp(Mathf.FloorToInt(vTop * height), 0, height - 1);
            int y = height - 1 - yFromTop;
            return pixels[y * width + x];
        }

        private static bool TryGetTriangleBarycentricWeights(
            Vector2 point,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            out float weightA,
            out float weightB,
            out float weightC)
        {
            weightA = 0f;
            weightB = 0f;
            weightC = 0f;

            float area = Cross(b - a, c - a);
            if (Mathf.Abs(area) <= 0.000001f)
            {
                return false;
            }

            weightA = Cross(b - point, c - point) / area;
            weightB = Cross(c - point, a - point) / area;
            weightC = 1f - weightA - weightB;
            const float epsilon = -0.000001f;
            return weightA >= epsilon && weightB >= epsilon && weightC >= epsilon;
        }

        private static void DrawMarker(Color32[] pixels, int width, int height, Vector2 point, Color32 color)
        {
            int centerX = Mathf.Clamp(Mathf.RoundToInt(point.x * (width - 1)), 0, width - 1);
            int centerY = Mathf.Clamp(Mathf.RoundToInt((1f - point.y) * (height - 1)), 0, height - 1);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x < 0 || y < 0 || x >= width || y >= height)
                    {
                        continue;
                    }

                    pixels[y * width + x] = color;
                }
            }
        }

        private static bool IsPointInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
            {
                return false;
            }

            if (polygon.Length == 3)
            {
                return IsPointInsideTriangle(point, polygon[0], polygon[1], polygon[2]);
            }

            if (polygon.Length == 4)
            {
                return IsPointInsideTriangle(point, polygon[0], polygon[1], polygon[2])
                    || IsPointInsideTriangle(point, polygon[0], polygon[2], polygon[3]);
            }

            return false;
        }

        private static bool IsPointInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = Cross(b - a, c - a);
            if (Mathf.Abs(area) <= 0.000001f)
            {
                return false;
            }

            float sign = Mathf.Sign(area);
            float edge0 = Cross(b - a, point - a) * sign;
            float edge1 = Cross(c - b, point - b) * sign;
            float edge2 = Cross(a - c, point - c) * sign;
            const float epsilon = -0.000001f;
            return edge0 >= epsilon && edge1 >= epsilon && edge2 >= epsilon;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private readonly struct RoofSliceGuide
        {
            public RoofSliceGuide(
                Vector2 a,
                Vector2 b,
                Vector2 c,
                Vector2 d,
                Vector2 e)
            {
                A = a;
                B = b;
                C = c;
                D = d;
                E = e;
                RectQuad = new[]
                {
                    a,
                    b,
                    c,
                    d
                };
                TriPolygon = new[]
                {
                    b,
                    c,
                    e
                };
            }

            public Vector2 A { get; }
            public Vector2 B { get; }
            public Vector2 C { get; }
            public Vector2 D { get; }
            public Vector2 E { get; }
            public Vector2[] RectQuad { get; }
            public Vector2[] TriPolygon { get; }
            public RoofRectSlice RectSlice => new RoofRectSlice(A, B, C, D);
            public RoofTriSlice TriSlice => new RoofTriSlice(B, C, E);
        }

        private readonly struct RoofRectSlice
        {
            public RoofRectSlice(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
            {
                TopLeft = topLeft;
                TopRight = topRight;
                BottomRight = bottomRight;
                BottomLeft = bottomLeft;
                Polygon = new[]
                {
                    topLeft,
                    topRight,
                    bottomRight,
                    bottomLeft
                };
            }

            public Vector2 TopLeft { get; }
            public Vector2 TopRight { get; }
            public Vector2 BottomRight { get; }
            public Vector2 BottomLeft { get; }
            public Vector2[] Polygon { get; }
        }

        private readonly struct RoofTriSlice
        {
            public RoofTriSlice(Vector2 top, Vector2 bottomLeft, Vector2 bottomRight)
            {
                Top = top;
                BottomLeft = bottomLeft;
                BottomRight = bottomRight;
                Polygon = new[]
                {
                    top,
                    bottomLeft,
                    bottomRight
                };
            }

            public Vector2 Top { get; }
            public Vector2 BottomLeft { get; }
            public Vector2 BottomRight { get; }
            public Vector2[] Polygon { get; }
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
