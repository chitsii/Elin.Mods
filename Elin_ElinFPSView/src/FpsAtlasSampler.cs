using System.Collections.Generic;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsAtlasSampler
    {
        private readonly Dictionary<int, TextureData> _textureCache = new Dictionary<int, TextureData>();
        private readonly Dictionary<int, SpriteMetrics> _spriteMetricsCache = new Dictionary<int, SpriteMetrics>();
        private AtlasData _blockAtlas;
        private AtlasData _blockSnowAtlas;
        private AtlasData _floorAtlas;
        private AtlasData _floorSnowAtlas;
        private AtlasData _autoTileAtlas;
        private AtlasData _autoTileWaterAtlas;

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

            Vector2 uv = MapFloorUv(Mathf.Repeat(worldX, 1f), Mathf.Repeat(worldZ, 1f));
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

            Vector2 uv = MapFloorUv(Mathf.Repeat(worldX, 1f), Mathf.Repeat(worldZ, 1f));
            return atlas.TrySample(surface.BaseTile, uv.x, uv.y, out color);
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

            AtlasData atlas = AtlasData.Create(renderData.pass);
            if (!atlas.IsReady)
            {
                return false;
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

        public bool TrySampleAutoTile(bool water, int tile, float worldX, float worldZ, out Color32 color)
        {
            color = default;
            AtlasData atlas = water ? GetAutoTileWaterAtlas() : GetAutoTileAtlas();
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapFloorUv(Mathf.Repeat(worldX, 1f), Mathf.Repeat(worldZ, 1f));
            return atlas.TrySample(tile, uv.x, uv.y, out color);
        }

        private static Vector2 MapFloorUv(float u, float v)
        {
            float diamondX = (u - v + 1f) * 0.5f;
            float diamondY = (u + v) * 0.25f;
            return new Vector2(
                Mathf.Clamp01(diamondX),
                1f - Mathf.Clamp01(diamondY));
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
