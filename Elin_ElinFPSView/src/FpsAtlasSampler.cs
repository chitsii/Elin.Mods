using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsAtlasSampler
    {
        private AtlasData _blockAtlas;
        private AtlasData _blockSnowAtlas;
        private AtlasData _floorAtlas;
        private AtlasData _floorSnowAtlas;

        public bool TrySampleBlock(Cell cell, float u, float v, bool hitVertical, out Color32 color)
        {
            color = default;
            if (cell == null || cell.sourceBlock == null || cell.sourceBlock._tiles == null || cell.sourceBlock._tiles.Length == 0)
            {
                return false;
            }

            int dir = cell.blockDir % cell.sourceBlock._tiles.Length;
            int tile = cell.sourceBlock.GetTile(cell.matBlock, dir);
            AtlasData atlas = cell.IsSnowTile ? GetBlockSnowAtlas() : GetBlockAtlas();
            if (!atlas.IsReady)
            {
                return false;
            }

            Vector2 uv = MapWallUv(u, v, hitVertical);
            return atlas.TrySample(tile, uv.x, uv.y, out color);
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
    }
}
