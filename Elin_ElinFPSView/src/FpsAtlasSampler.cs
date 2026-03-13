using System.Collections.Generic;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsAtlasSampler
    {
        private readonly Dictionary<int, TextureData> _textureCache = new Dictionary<int, TextureData>();
        private readonly Dictionary<int, SpriteMetrics> _spriteMetricsCache = new Dictionary<int, SpriteMetrics>();
        private readonly Dictionary<MeshPass, AtlasData> _renderPassAtlasCache = new Dictionary<MeshPass, AtlasData>();
        private readonly Dictionary<long, SpriteMetrics> _renderTileMetricsCache = new Dictionary<long, SpriteMetrics>();
        private AtlasData _blockAtlas;
        private AtlasData _blockSnowAtlas;
        private AtlasData _floorAtlas;
        private AtlasData _floorSnowAtlas;
        private AtlasData _autoTileAtlas;
        private AtlasData _autoTileWaterAtlas;
        private const float TerrainFloorInset = 0.22f;
        private const int TerrainOpaqueSearchSteps = 6;

        public bool TrySampleBlock(FpsResolvedWallSurface surface, float u, float v, bool hitVertical, out Color32 color)
        {
            color = default;
            if (surface.Cell == null)
            {
                return false;
            }

            AtlasData atlas = surface.UseSnowAtlas ? GetBlockSnowAtlas() : GetBlockAtlas();
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapWallUv(u, v, hitVertical);
            return atlas.TrySample(surface.Tile, uv.x, uv.y, out color);
        }

        public bool TrySampleFloor(Cell cell, float worldX, float worldZ, out Color32 color)
        {
            color = default;
            if (cell == null)
            {
                return false;
            }

            SourceFloor.Row floor = cell.HasBridge ? cell.sourceBridge : cell.sourceFloor;
            SourceMaterial.Row mat = cell.HasBridge ? cell.matBridge : cell.matFloor;
            if (floor == null || floor._tiles == null || floor._tiles.Length == 0)
            {
                return false;
            }

            int dir = cell.floorDir % floor._tiles.Length;
            int tile = floor.GetTile(mat, dir);
            AtlasData atlas = cell.IsSnowTile ? GetFloorSnowAtlas() : GetFloorAtlas();
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapFloorSurfaceUv(Mathf.Repeat(worldX, 1f), Mathf.Repeat(worldZ, 1f));
            return atlas.TrySample(tile, uv.x, uv.y, out color);
        }

        public bool TrySampleFloorState(FpsResolvedFloorSurface surface, float worldX, float worldZ, out Color32 color)
        {
            color = default;
            if (surface.Cell == null || surface.Floor == null || surface.Material == null)
            {
                return false;
            }

            AtlasData atlas = surface.UseSnowAtlas ? GetFloorSnowAtlas() : GetFloorAtlas();
            if (surface.Floor == FLOOR.sourceIce)
            {
                atlas = GetFloorAtlas();
            }
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapFloorSurfaceUv(Mathf.Repeat(worldX, 1f), Mathf.Repeat(worldZ, 1f));
            return atlas.TrySample(surface.BaseTile, uv.x, uv.y, out color);
        }

        public bool TrySampleFloorSurface(FpsResolvedFloorSurface surface, float localU, float localV, out Color32 color)
        {
            color = default;
            if (surface.Cell == null || surface.Floor == null || surface.Material == null)
            {
                return false;
            }

            AtlasData atlas = surface.UseSnowAtlas ? GetFloorSnowAtlas() : GetFloorAtlas();
            if (surface.Floor == FLOOR.sourceIce)
            {
                atlas = GetFloorAtlas();
            }
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapFloorSurfaceUv(
                InsetTerrainCoordinate(localU),
                InsetTerrainCoordinate(localV));
            return TrySampleTerrainAtlas(atlas, surface.BaseTile, uv.x, uv.y, out color);
        }

        public bool TrySampleFloorTileSurface(bool snow, int tile, float localU, float localV, out Color32 color)
        {
            color = default;
            AtlasData atlas = snow ? GetFloorSnowAtlas() : GetFloorAtlas();
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapFloorSurfaceUv(
                InsetTerrainCoordinate(localU),
                InsetTerrainCoordinate(localV));
            return TrySampleTerrainAtlas(atlas, tile, uv.x, uv.y, out color);
        }

        public bool TrySampleSprite(Sprite sprite, float u, float v, out Color32 color)
        {
            color = default;
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            int textureId = sprite.texture.GetInstanceID();
            if (!_textureCache.TryGetValue(textureId, out TextureData textureData))
            {
                textureData = new TextureData
                {
                    Pixels = sprite.texture.GetPixels32(),
                    Width = sprite.texture.width,
                    Height = sprite.texture.height
                };
                _textureCache[textureId] = textureData;
            }

            Rect rect = sprite.textureRect;
            int x = Mathf.Clamp(Mathf.FloorToInt(rect.x + Mathf.Clamp01(u) * (rect.width - 1f)), 0, textureData.Width - 1);
            int yFromBottom = Mathf.Clamp(Mathf.FloorToInt(rect.y + Mathf.Clamp01(v) * (rect.height - 1f)), 0, textureData.Height - 1);
            int index = yFromBottom * textureData.Width + x;
            if (index < 0 || index >= textureData.Pixels.Length)
            {
                return false;
            }

            color = textureData.Pixels[index];
            return color.a > 0;
        }

        public bool TrySampleRenderTile(RenderData renderData, int tile, float u, float v, out Color32 color)
        {
            color = default;
            if (renderData?.pass == null)
            {
                return false;
            }

            AtlasData atlas = GetRenderPassAtlas(renderData.pass);
            if (!atlas.IsReady)
            {
                return false;
            }

            if (renderData.multiSize && tile >= atlas.TilesPerRow)
            {
                if (v < 0.5f)
                {
                    return atlas.TrySample(tile - atlas.TilesPerRow, u, Mathf.Clamp01(v * 2f), out color);
                }

                return atlas.TrySample(tile, u, Mathf.Clamp01((v - 0.5f) * 2f), out color);
            }

            return atlas.TrySample(tile, u, v, out color);
        }

        public bool TryGetSpriteMetrics(Sprite sprite, out SpriteMetrics metrics)
        {
            metrics = default;
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            int spriteId = sprite.GetInstanceID();
            if (_spriteMetricsCache.TryGetValue(spriteId, out metrics))
            {
                return metrics.HasOpaquePixels;
            }

            int textureId = sprite.texture.GetInstanceID();
            if (!_textureCache.TryGetValue(textureId, out TextureData textureData))
            {
                textureData = new TextureData
                {
                    Pixels = sprite.texture.GetPixels32(),
                    Width = sprite.texture.width,
                    Height = sprite.texture.height
                };
                _textureCache[textureId] = textureData;
            }

            Rect rect = sprite.textureRect;
            int minX = Mathf.FloorToInt(rect.xMin);
            int minY = Mathf.FloorToInt(rect.yMin);
            int maxX = Mathf.CeilToInt(rect.xMax) - 1;
            int maxY = Mathf.CeilToInt(rect.yMax) - 1;

            int opaqueMinX = maxX;
            int opaqueMaxX = minX;
            int opaqueMinY = maxY;
            int opaqueMaxY = minY;
            bool hasOpaque = false;
            for (int y = minY; y <= maxY; y++)
            {
                int row = y * textureData.Width;
                for (int x = minX; x <= maxX; x++)
                {
                    Color32 pixel = textureData.Pixels[row + x];
                    if (pixel.a <= 8)
                    {
                        continue;
                    }

                    hasOpaque = true;
                    if (x < opaqueMinX)
                    {
                        opaqueMinX = x;
                    }
                    if (x > opaqueMaxX)
                    {
                        opaqueMaxX = x;
                    }
                    if (y < opaqueMinY)
                    {
                        opaqueMinY = y;
                    }
                    if (y > opaqueMaxY)
                    {
                        opaqueMaxY = y;
                    }
                }
            }

            if (!hasOpaque)
            {
                metrics = new SpriteMetrics
                {
                    HasOpaquePixels = false
                };
                _spriteMetricsCache[spriteId] = metrics;
                return false;
            }

            float width = Mathf.Max(1f, rect.width - 1f);
            float height = Mathf.Max(1f, rect.height - 1f);
            metrics = new SpriteMetrics
            {
                MinU = Mathf.Clamp01((opaqueMinX - rect.xMin) / width),
                MaxU = Mathf.Clamp01((opaqueMaxX - rect.xMin) / width),
                BottomV = Mathf.Clamp01((opaqueMinY - rect.yMin) / height),
                TopV = Mathf.Clamp01((opaqueMaxY - rect.yMin) / height),
                HasOpaquePixels = true
            };
            _spriteMetricsCache[spriteId] = metrics;
            return true;
        }

        public bool TryGetRenderTileMetrics(RenderData renderData, int tile, out SpriteMetrics metrics)
        {
            metrics = default;
            if (renderData?.pass == null)
            {
                return false;
            }

            AtlasData atlas = GetRenderPassAtlas(renderData.pass);
            if (!atlas.IsReady)
            {
                return false;
            }

            long cacheKey = (((long)renderData.pass.GetInstanceID()) << 32)
                ^ (uint)tile
                ^ (renderData.multiSize ? (1L << 63) : 0L);
            if (_renderTileMetricsCache.TryGetValue(cacheKey, out metrics))
            {
                return metrics.HasOpaquePixels;
            }

            bool hasBounds;
            int minX;
            int maxX;
            int minYFromTop;
            int maxYFromTop;

            if (renderData.multiSize && tile >= atlas.TilesPerRow)
            {
                bool hasUpper = atlas.TryGetOpaqueBounds(tile - atlas.TilesPerRow, out int upperMinX, out int upperMaxX, out int upperMinY, out int upperMaxY);
                bool hasLower = atlas.TryGetOpaqueBounds(tile, out int lowerMinX, out int lowerMaxX, out int lowerMinY, out int lowerMaxY);
                hasBounds = hasUpper || hasLower;
                if (!hasBounds)
                {
                    metrics = new SpriteMetrics
                    {
                        HasOpaquePixels = false
                    };
                    _renderTileMetricsCache[cacheKey] = metrics;
                    return false;
                }

                if (hasUpper)
                {
                    minX = upperMinX;
                    maxX = upperMaxX;
                    minYFromTop = upperMinY;
                    maxYFromTop = upperMaxY;
                }
                else
                {
                    minX = lowerMinX;
                    maxX = lowerMaxX;
                    minYFromTop = lowerMinY + atlas.TileHeight;
                    maxYFromTop = lowerMaxY + atlas.TileHeight;
                }

                if (hasLower)
                {
                    minX = Mathf.Min(minX, lowerMinX);
                    maxX = Mathf.Max(maxX, lowerMaxX);
                    minYFromTop = Mathf.Min(minYFromTop, lowerMinY + atlas.TileHeight);
                    maxYFromTop = Mathf.Max(maxYFromTop, lowerMaxY + atlas.TileHeight);
                }

                metrics = CreateMetrics(minX, maxX, minYFromTop, maxYFromTop, atlas.TileWidth, atlas.TileHeight * 2);
                _renderTileMetricsCache[cacheKey] = metrics;
                return true;
            }

            if (!atlas.TryGetOpaqueBounds(tile, out minX, out maxX, out minYFromTop, out maxYFromTop))
            {
                metrics = new SpriteMetrics
                {
                    HasOpaquePixels = false
                };
                _renderTileMetricsCache[cacheKey] = metrics;
                return false;
            }

            metrics = CreateMetrics(minX, maxX, minYFromTop, maxYFromTop, atlas.TileWidth, atlas.TileHeight);
            _renderTileMetricsCache[cacheKey] = metrics;
            return true;
        }

        public bool TrySampleAutoTile(bool water, int tile, float worldX, float worldZ, out Color32 color)
        {
            color = default;
            AtlasData atlas = water ? GetAutoTileWaterAtlas() : GetAutoTileAtlas();
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapFloorSurfaceUv(Mathf.Repeat(worldX, 1f), Mathf.Repeat(worldZ, 1f));
            return atlas.TrySample(tile, uv.x, uv.y, out color);
        }

        public bool TrySampleAutoTileSurface(bool water, int tile, float localU, float localV, out Color32 color)
        {
            color = default;
            AtlasData atlas = water ? GetAutoTileWaterAtlas() : GetAutoTileAtlas();
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapFloorSurfaceUv(
                InsetTerrainCoordinate(localU),
                InsetTerrainCoordinate(localV));
            return TrySampleTerrainAtlas(atlas, tile, uv.x, uv.y, out color);
        }

        private static bool TrySampleTerrainAtlas(AtlasData atlas, int tile, float u, float v, out Color32 color)
        {
            if (atlas.TrySample(tile, u, v, out color))
            {
                return true;
            }

            const float centerU = 0.5f;
            const float centerV = 0.5f;
            for (int step = 1; step <= TerrainOpaqueSearchSteps; step++)
            {
                float t = step / (float)(TerrainOpaqueSearchSteps + 1);
                float sampleU = Mathf.Lerp(u, centerU, t);
                float sampleV = Mathf.Lerp(v, centerV, t);
                if (atlas.TrySample(tile, sampleU, sampleV, out color))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 MapFloorSurfaceUv(float u, float v)
        {
            float diamondX = (u - v + 1f) * 0.5f;
            float diamondY = (u + v) * 0.5f;
            return new Vector2(
                Mathf.Clamp01(diamondX),
                Mathf.Clamp01(diamondY));
        }

        private static float InsetTerrainCoordinate(float value)
        {
            return Mathf.Lerp(TerrainFloorInset, 1f - TerrainFloorInset, Mathf.Clamp01(value));
        }

        private static Vector2 MapWallUv(float u, float v, bool hitVertical)
        {
            Vector2 topLeft;
            Vector2 topRight;
            Vector2 bottomLeft;
            Vector2 bottomRight;

            if (hitVertical)
            {
                topLeft = new Vector2(0.50f, 0.25f);
                topRight = new Vector2(1.00f, 0.50f);
                bottomLeft = new Vector2(0.50f, 0.98f);
                bottomRight = new Vector2(1.00f, 0.75f);
            }
            else
            {
                topLeft = new Vector2(0.00f, 0.50f);
                topRight = new Vector2(0.50f, 0.25f);
                bottomLeft = new Vector2(0.00f, 0.75f);
                bottomRight = new Vector2(0.50f, 0.98f);
            }

            Vector2 top = Vector2.Lerp(topLeft, topRight, Mathf.Clamp01(u));
            Vector2 bottom = Vector2.Lerp(bottomLeft, bottomRight, Mathf.Clamp01(u));
            return Vector2.Lerp(top, bottom, Mathf.Clamp01(v));
        }

        private AtlasData GetBlockAtlas()
        {
            if (!_blockAtlas.IsReady)
            {
                _blockAtlas = AtlasData.Create(EClass.scene?.screenElin?.tileMap?.passBlock);
            }

            return _blockAtlas;
        }

        private AtlasData GetBlockSnowAtlas()
        {
            if (!_blockSnowAtlas.IsReady)
            {
                _blockSnowAtlas = AtlasData.Create(EClass.scene?.screenElin?.tileMap?.passBlock?.snowPass);
            }

            return _blockSnowAtlas;
        }

        private AtlasData GetFloorAtlas()
        {
            if (!_floorAtlas.IsReady)
            {
                _floorAtlas = AtlasData.Create(EClass.scene?.screenElin?.tileMap?.passFloor);
            }

            return _floorAtlas;
        }

        private AtlasData GetFloorSnowAtlas()
        {
            if (!_floorSnowAtlas.IsReady)
            {
                _floorSnowAtlas = AtlasData.Create(EClass.scene?.screenElin?.tileMap?.passFloor?.snowPass);
            }

            return _floorSnowAtlas;
        }

        private AtlasData GetAutoTileAtlas()
        {
            if (!_autoTileAtlas.IsReady)
            {
                _autoTileAtlas = AtlasData.Create(EClass.scene?.screenElin?.tileMap?.passAutoTile);
            }

            return _autoTileAtlas;
        }

        private AtlasData GetAutoTileWaterAtlas()
        {
            if (!_autoTileWaterAtlas.IsReady)
            {
                _autoTileWaterAtlas = AtlasData.Create(EClass.scene?.screenElin?.tileMap?.passAutoTileWater);
            }

            return _autoTileWaterAtlas;
        }

        private AtlasData GetRenderPassAtlas(MeshPass pass)
        {
            if (pass == null)
            {
                return default;
            }

            if (_renderPassAtlasCache.TryGetValue(pass, out AtlasData atlas) && atlas.IsReady)
            {
                return atlas;
            }

            atlas = AtlasData.Create(pass);
            if (atlas.IsReady)
            {
                _renderPassAtlasCache[pass] = atlas;
            }

            return atlas;
        }

        private static SpriteMetrics CreateMetrics(int minX, int maxX, int minYFromTop, int maxYFromTop, int width, int height)
        {
            float widthScale = Mathf.Max(1f, width - 1f);
            float heightScale = Mathf.Max(1f, height - 1f);
            return new SpriteMetrics
            {
                MinU = Mathf.Clamp01(minX / widthScale),
                MaxU = Mathf.Clamp01(maxX / widthScale),
                BottomV = Mathf.Clamp01((height - 1 - maxYFromTop) / heightScale),
                TopV = Mathf.Clamp01((height - 1 - minYFromTop) / heightScale),
                HasOpaquePixels = true
            };
        }

        private struct AtlasData
        {
            public Color32[] Pixels;
            public int TextureWidth;
            public int TextureHeight;
            public int TileWidth;
            public int TileHeight;
            public int TilesPerRow;
            public int TilesPerColumn;

            public bool IsReady => Pixels != null && Pixels.Length > 0 && TileWidth > 0 && TileHeight > 0 && TilesPerRow > 0 && TilesPerColumn > 0;

            public static AtlasData Create(MeshPass pass)
            {
                if (pass == null || pass.mat == null || pass.pmesh == null)
                {
                    return default;
                }

                Texture2D texture = pass.mat.GetTexture("_MainTex") as Texture2D;
                if (texture == null)
                {
                    return default;
                }

                int tilesPerRow = Mathf.RoundToInt(pass.pmesh.tiling.x);
                int tilesPerColumn = Mathf.RoundToInt(pass.pmesh.tiling.y);
                if (tilesPerRow <= 0 || tilesPerColumn <= 0)
                {
                    return default;
                }

                return new AtlasData
                {
                    Pixels = texture.GetPixels32(),
                    TextureWidth = texture.width,
                    TextureHeight = texture.height,
                    TileWidth = texture.width / tilesPerRow,
                    TileHeight = texture.height / tilesPerColumn,
                    TilesPerRow = tilesPerRow,
                    TilesPerColumn = tilesPerColumn
                };
            }

            public bool TrySample(int tile, float u, float v, out Color32 color)
            {
                color = default;
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

                int sampleX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(u) * (TileWidth - 1)), 0, TileWidth - 1);
                int sampleY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(v) * (TileHeight - 1)), 0, TileHeight - 1);
                int atlasX = tileX * TileWidth + sampleX;
                int atlasYFromTop = tileY * TileHeight + sampleY;
                int atlasY = TextureHeight - 1 - atlasYFromTop;
                int index = atlasY * TextureWidth + atlasX;
                if (index < 0 || index >= Pixels.Length)
                {
                    return false;
                }

                color = Pixels[index];
                return color.a > 0;
            }

            public bool TryGetOpaqueBounds(int tile, out int minX, out int maxX, out int minYFromTop, out int maxYFromTop)
            {
                minX = 0;
                maxX = 0;
                minYFromTop = 0;
                maxYFromTop = 0;
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

                int startX = tileX * TileWidth;
                int startYFromTop = tileY * TileHeight;
                bool hasOpaque = false;
                int foundMinX = TileWidth - 1;
                int foundMaxX = 0;
                int foundMinY = TileHeight - 1;
                int foundMaxY = 0;

                for (int localY = 0; localY < TileHeight; localY++)
                {
                    int atlasYFromTop = startYFromTop + localY;
                    int atlasY = TextureHeight - 1 - atlasYFromTop;
                    int row = atlasY * TextureWidth;
                    for (int localX = 0; localX < TileWidth; localX++)
                    {
                        Color32 pixel = Pixels[row + startX + localX];
                        if (pixel.a <= 8)
                        {
                            continue;
                        }

                        hasOpaque = true;
                        if (localX < foundMinX)
                        {
                            foundMinX = localX;
                        }

                        if (localX > foundMaxX)
                        {
                            foundMaxX = localX;
                        }

                        if (localY < foundMinY)
                        {
                            foundMinY = localY;
                        }

                        if (localY > foundMaxY)
                        {
                            foundMaxY = localY;
                        }
                    }
                }

                if (!hasOpaque)
                {
                    return false;
                }

                minX = foundMinX;
                maxX = foundMaxX;
                minYFromTop = foundMinY;
                maxYFromTop = foundMaxY;
                return true;
            }
        }

        private struct TextureData
        {
            public Color32[] Pixels;
            public int Width;
            public int Height;
        }

        public struct SpriteMetrics
        {
            public float MinU;
            public float MaxU;
            public float BottomV;
            public float TopV;
            public bool HasOpaquePixels;
        }
    }
}
