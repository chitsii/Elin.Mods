using UnityEngine;
using System.Collections.Generic;

namespace Elin_ElinFPSView
{
    internal sealed class FpsGpuFloorAtlasBaker
    {
        private const int CompositeTileSize = 64;
        private readonly FpsAtlasSampler _sampler = new FpsAtlasSampler();
        private readonly Dictionary<CompositeSurfaceKey, Texture2D> _compositeCache = new Dictionary<CompositeSurfaceKey, Texture2D>();

        private BakedFloorAtlas _baseAtlas;
        private BakedFloorAtlas _snowAtlas;
        private BakedFloorAtlas _autoTileAtlas;
        private BakedFloorAtlas _autoTileWaterAtlas;
        private int _compositeFloorPassId;
        private int _compositeFloorSnowPassId;
        private int _compositeAutoTilePassId;
        private int _compositeAutoTileWaterPassId;

        public bool TryGetAtlas(bool snow, out Texture texture)
        {
            texture = null;
            BakedFloorAtlas atlas = GetOrBuildAtlas(snow);
            if (!atlas.IsReady)
            {
                return false;
            }

            texture = atlas.Texture;
            return texture != null;
        }

        public bool TryGetTileRect(bool snow, int tile, out Rect uvRect)
        {
            uvRect = default;
            BakedFloorAtlas atlas = GetOrBuildAtlas(snow);
            if (!atlas.IsReady)
            {
                return false;
            }

            return atlas.TryGetTileRect(tile, out uvRect);
        }

        public bool TryGetAutoTileAtlas(bool water, out Texture texture)
        {
            texture = null;
            BakedFloorAtlas atlas = GetOrBuildAutoTileAtlas(water);
            if (!atlas.IsReady)
            {
                return false;
            }

            texture = atlas.Texture;
            return texture != null;
        }

        public bool TryGetAutoTileRect(bool water, int tile, out Rect uvRect)
        {
            uvRect = default;
            BakedFloorAtlas atlas = GetOrBuildAutoTileAtlas(water);
            if (!atlas.IsReady)
            {
                return false;
            }

            return atlas.TryGetTileRect(tile, out uvRect);
        }

        public bool TryGetCompositeTexture(FpsResolvedFloorSurface surface, out Texture texture)
        {
            texture = null;
            RefreshCompositeCacheState();

            CompositeSurfaceKey key = new CompositeSurfaceKey
            {
                BaseTile = surface.BaseTile,
                AutoTileOverlay = surface.AutoTileOverlay,
                UseSnowAtlas = surface.UseSnowAtlas,
                UseWaterAutoTileAtlas = surface.UseWaterAutoTileAtlas
            };

            if (_compositeCache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                texture = cached;
                return true;
            }

            Texture2D baked = BakeCompositeTexture(surface);
            if (baked == null)
            {
                return false;
            }

            _compositeCache[key] = baked;
            texture = baked;
            return true;
        }

        private BakedFloorAtlas GetOrBuildAtlas(bool snow)
        {
            ref BakedFloorAtlas atlas = ref (snow ? ref _snowAtlas : ref _baseAtlas);
            MeshPass pass = snow
                ? EClass.scene?.screenElin?.tileMap?.passFloor?.snowPass
                : EClass.scene?.screenElin?.tileMap?.passFloor;

            int passId = pass != null ? pass.GetInstanceID() : 0;
            if (atlas.IsReady && atlas.PassId == passId)
            {
                return atlas;
            }

            atlas.Dispose();
            atlas = BakedFloorAtlas.Create(pass, _sampler, snow);
            return atlas;
        }

        private BakedFloorAtlas GetOrBuildAutoTileAtlas(bool water)
        {
            ref BakedFloorAtlas atlas = ref (water ? ref _autoTileWaterAtlas : ref _autoTileAtlas);
            MeshPass pass = water
                ? EClass.scene?.screenElin?.tileMap?.passAutoTileWater
                : EClass.scene?.screenElin?.tileMap?.passAutoTile;

            int passId = pass != null ? pass.GetInstanceID() : 0;
            if (atlas.IsReady && atlas.PassId == passId)
            {
                return atlas;
            }

            atlas.Dispose();
            atlas = BakedFloorAtlas.CreateAutoTile(pass, _sampler, water);
            return atlas;
        }

        private void RefreshCompositeCacheState()
        {
            int floorPassId = EClass.scene?.screenElin?.tileMap?.passFloor?.GetInstanceID() ?? 0;
            int floorSnowPassId = EClass.scene?.screenElin?.tileMap?.passFloor?.snowPass?.GetInstanceID() ?? 0;
            int autoTilePassId = EClass.scene?.screenElin?.tileMap?.passAutoTile?.GetInstanceID() ?? 0;
            int autoTileWaterPassId = EClass.scene?.screenElin?.tileMap?.passAutoTileWater?.GetInstanceID() ?? 0;
            if (_compositeFloorPassId == floorPassId
                && _compositeFloorSnowPassId == floorSnowPassId
                && _compositeAutoTilePassId == autoTilePassId
                && _compositeAutoTileWaterPassId == autoTileWaterPassId)
            {
                return;
            }

            foreach (Texture2D texture in _compositeCache.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }

            _compositeCache.Clear();
            _compositeFloorPassId = floorPassId;
            _compositeFloorSnowPassId = floorSnowPassId;
            _compositeAutoTilePassId = autoTilePassId;
            _compositeAutoTileWaterPassId = autoTileWaterPassId;
        }

        private Texture2D BakeCompositeTexture(FpsResolvedFloorSurface surface)
        {
            Texture2D texture = new Texture2D(CompositeTileSize, CompositeTileSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"FpsGpuFloorComposite_{surface.BaseTile}_{surface.AutoTileOverlay}"
            };

            Color32[] pixels = new Color32[CompositeTileSize * CompositeTileSize];
            bool anyOpaque = false;
            for (int y = 0; y < CompositeTileSize; y++)
            {
                float localV = (y + 0.5f) / CompositeTileSize;
                for (int x = 0; x < CompositeTileSize; x++)
                {
                    float localU = (x + 0.5f) / CompositeTileSize;
                    Color32 color = default;
                    bool hasColor = false;

                    if (_sampler.TrySampleFloorTileSurface(surface.UseSnowAtlas, surface.BaseTile, localU, localV, out Color32 baseColor))
                    {
                        color = baseColor;
                        hasColor = true;
                    }

                    if (surface.AutoTileOverlay >= 0
                        && _sampler.TrySampleAutoTileSurface(surface.UseWaterAutoTileAtlas, surface.AutoTileOverlay, localU, localV, out Color32 overlayColor))
                    {
                        color = hasColor ? AlphaBlend(color, overlayColor) : overlayColor;
                        hasColor = true;
                    }

                    if (!hasColor)
                    {
                        color = new Color32(255, 0, 255, 255);
                    }
                    else
                    {
                        color.a = 255;
                        anyOpaque = true;
                    }

                    int index = (CompositeTileSize - 1 - y) * CompositeTileSize + x;
                    pixels[index] = color;
                }
            }

            if (!anyOpaque)
            {
                Object.Destroy(texture);
                return null;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color32 AlphaBlend(Color32 under, Color32 over)
        {
            float alpha = over.a / 255f;
            float inverse = 1f - alpha;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(under.r * inverse + over.r * alpha), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(under.g * inverse + over.g * alpha), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(under.b * inverse + over.b * alpha), 0, 255),
                255);
        }

        private struct CompositeSurfaceKey
        {
            public int BaseTile;
            public int AutoTileOverlay;
            public bool UseSnowAtlas;
            public bool UseWaterAutoTileAtlas;
        }

        private struct BakedFloorAtlas
        {
            public int PassId;
            public Texture2D Texture;
            public int TilesPerRow;
            public int TilesPerColumn;
            public int TileSize;

            public bool IsReady => Texture != null && TileSize > 0 && TilesPerRow > 0 && TilesPerColumn > 0;

            public void Dispose()
            {
                if (Texture != null)
                {
                    Object.Destroy(Texture);
                    Texture = null;
                }

                PassId = 0;
                TilesPerRow = 0;
                TilesPerColumn = 0;
                TileSize = 0;
            }

            public bool TryGetTileRect(int tile, out Rect uvRect)
            {
                uvRect = default;
                if (!IsReady)
                {
                    return false;
                }

                int tileIndex = Mathf.Abs(tile);
                int tileX = tileIndex % TilesPerRow;
                int tileY = tileIndex / TilesPerRow;
                if (tileY < 0 || tileY >= TilesPerColumn)
                {
                    return false;
                }

                float width = 1f / TilesPerRow;
                float height = 1f / TilesPerColumn;
                uvRect = new Rect(tileX * width, tileY * height, width, height);
                return true;
            }

            public static BakedFloorAtlas Create(MeshPass pass, FpsAtlasSampler sampler, bool snow)
            {
                if (pass?.mat == null || pass.pmesh == null || sampler == null)
                {
                    return default;
                }

                Texture2D source = pass.mat.GetTexture("_MainTex") as Texture2D;
                if (source == null)
                {
                    return default;
                }

                int tilesPerRow = Mathf.RoundToInt(pass.pmesh.tiling.x);
                int tilesPerColumn = Mathf.RoundToInt(pass.pmesh.tiling.y);
                if (tilesPerRow <= 0 || tilesPerColumn <= 0)
                {
                    return default;
                }

                int sourceTileWidth = source.width / tilesPerRow;
                int sourceTileHeight = source.height / tilesPerColumn;
                int tileSize = Mathf.Max(8, Mathf.Min(sourceTileWidth, sourceTileHeight));
                Color32[] sourcePixels = source.GetPixels32();
                Color32[] bakedPixels = new Color32[tileSize * tileSize * tilesPerRow * tilesPerColumn];
                int bakedWidth = tileSize * tilesPerRow;
                int bakedHeight = tileSize * tilesPerColumn;

                for (int tileY = 0; tileY < tilesPerColumn; tileY++)
                {
                    for (int tileX = 0; tileX < tilesPerRow; tileX++)
                    {
                        BakeTile(
                            sampler,
                            snow,
                            tileY * tilesPerRow + tileX,
                            tileX,
                            tileY,
                            bakedPixels,
                            bakedWidth,
                            bakedHeight,
                            tileSize);
                    }
                }

                Texture2D texture = new Texture2D(bakedWidth, bakedHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"FpsGpuFloorAtlas_{pass.GetInstanceID()}"
                };
                texture.SetPixels32(bakedPixels);
                texture.Apply(false, false);

                return new BakedFloorAtlas
                {
                    PassId = pass.GetInstanceID(),
                    Texture = texture,
                    TilesPerRow = tilesPerRow,
                    TilesPerColumn = tilesPerColumn,
                    TileSize = tileSize
                };
            }

            public static BakedFloorAtlas CreateAutoTile(MeshPass pass, FpsAtlasSampler sampler, bool water)
            {
                if (pass?.mat == null || pass.pmesh == null || sampler == null)
                {
                    return default;
                }

                Texture2D source = pass.mat.GetTexture("_MainTex") as Texture2D;
                if (source == null)
                {
                    return default;
                }

                int tilesPerRow = Mathf.RoundToInt(pass.pmesh.tiling.x);
                int tilesPerColumn = Mathf.RoundToInt(pass.pmesh.tiling.y);
                if (tilesPerRow <= 0 || tilesPerColumn <= 0)
                {
                    return default;
                }

                int sourceTileWidth = source.width / tilesPerRow;
                int sourceTileHeight = source.height / tilesPerColumn;
                int tileSize = Mathf.Max(8, Mathf.Min(sourceTileWidth, sourceTileHeight));
                Color32[] bakedPixels = new Color32[tileSize * tileSize * tilesPerRow * tilesPerColumn];
                int bakedWidth = tileSize * tilesPerRow;
                int bakedHeight = tileSize * tilesPerColumn;

                for (int tileY = 0; tileY < tilesPerColumn; tileY++)
                {
                    for (int tileX = 0; tileX < tilesPerRow; tileX++)
                    {
                        int tileIndex = tileY * tilesPerRow + tileX;
                        int bakedStartX = tileX * tileSize;
                        int bakedStartY = tileY * tileSize;
                        for (int y = 0; y < tileSize; y++)
                        {
                            float localV = (y + 0.5f) / tileSize;
                            for (int x = 0; x < tileSize; x++)
                            {
                                float localU = (x + 0.5f) / tileSize;
                                Color32 color = sampler.TrySampleAutoTileSurface(water, tileIndex, localU, localV, out Color32 sampled)
                                    ? sampled
                                    : new Color32(0, 0, 0, 0);
                                int bakedX = bakedStartX + x;
                                int bakedYFromTop = bakedStartY + y;
                                int bakedY = bakedHeight - 1 - bakedYFromTop;
                                bakedPixels[bakedY * bakedWidth + bakedX] = color;
                            }
                        }
                    }
                }

                Texture2D texture = new Texture2D(bakedWidth, bakedHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"FpsGpuAutoTileAtlas_{pass.GetInstanceID()}"
                };
                texture.SetPixels32(bakedPixels);
                texture.Apply(false, false);

                return new BakedFloorAtlas
                {
                    PassId = pass.GetInstanceID(),
                    Texture = texture,
                    TilesPerRow = tilesPerRow,
                    TilesPerColumn = tilesPerColumn,
                    TileSize = tileSize
                };
            }

            private static void BakeTile(
                FpsAtlasSampler sampler,
                bool snow,
                int tileIndex,
                int tileX,
                int tileY,
                Color32[] bakedPixels,
                int bakedWidth,
                int bakedHeight,
                int tileSize)
            {
                int bakedStartX = tileX * tileSize;
                int bakedStartY = tileY * tileSize;

                for (int y = 0; y < tileSize; y++)
                {
                    float localV = (y + 0.5f) / tileSize;
                    for (int x = 0; x < tileSize; x++)
                    {
                        float localU = (x + 0.5f) / tileSize;
                        Color32 color = sampler.TrySampleFloorTileSurface(snow, tileIndex, localU, localV, out Color32 sampled)
                            ? sampled
                            : new Color32(255, 0, 255, 255);
                        color.a = 255;

                        int bakedX = bakedStartX + x;
                        int bakedYFromTop = bakedStartY + y;
                        int bakedY = bakedHeight - 1 - bakedYFromTop;
                        bakedPixels[bakedY * bakedWidth + bakedX] = color;
                    }
                }
            }

        }
    }
}
