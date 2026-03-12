using System.Collections.Generic;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsIdealizedWorld
    {
        private const float LooseItemBaseHeight = 0.28f;
        private const float CharaBaseHeight = 1.05f;
        private const float InstalledBaseHeight = 0.9f;
        private const float TallObjectBaseHeight = 1.3f;

        public static float GetCellSurfaceHeight(Cell cell)
        {
            if (cell == null)
            {
                return 0f;
            }

            float baseHeight = cell.bridgeHeight == 0 ? cell.height : cell.bridgeHeight;
            SourceFloor.Row floor = cell.HasBridge ? cell.sourceBridge : cell.sourceFloor;
            if (floor != null)
            {
                baseHeight += floor.tileType.FloorHeight;
            }

            return baseHeight;
        }

        public bool TryResolveFloor(Cell cell, int index, out FpsResolvedFloorSurface surface)
        {
            surface = default;
            if (cell == null)
            {
                return false;
            }

            if (cell.HasBridge)
            {
                SourceFloor.Row floor = cell.sourceBridge;
                SourceMaterial.Row material = cell.matBridge;
                bool useSnowAtlas = cell.IsSnowTile && !cell.sourceBridge.ignoreSnow;

                if (useSnowAtlas)
                {
                    if (cell.IsBridgeWater)
                    {
                        floor = FLOOR.sourceIce;
                        useSnowAtlas = false;
                    }
                    else
                    {
                        floor = FLOOR.sourceSnow;
                        material = MATERIAL.sourceSnow;
                    }
                }

                surface = new FpsResolvedFloorSurface
                {
                    Cell = cell,
                    Floor = floor,
                    Material = material,
                    BaseTile = floor._tiles[cell.floorDir % floor._tiles.Length],
                    MaterialColor = floor.GetColorInt(material),
                    AutoTileOverlay = cell.autotileBridge != 0 && floor.autotile != 0
                        ? (26 + floor.autotile / 2) * 32 + floor.autotile % 2 * 16 + cell.autotileBridge
                        : -1,
                    UseSnowAtlas = useSnowAtlas,
                    UseWaterAutoTileAtlas = floor.tileType.IsWater
                };
                return true;
            }

            SourceFloor.Row sourceFloor = cell.sourceFloor;
            SourceMaterial.Row matFloor = cell.matFloor;
            int floorDir = cell.floorDir;
            bool floorSnowAtlas = cell.IsSnowTile && !cell.sourceFloor.ignoreSnow;
            if (floorSnowAtlas)
            {
                if (cell.IsFloorWater)
                {
                    sourceFloor = FLOOR.sourceIce;
                    floorSnowAtlas = false;
                }
                else
                {
                    if (cell.sourceObj.snowTile > 0)
                    {
                        sourceFloor = FLOOR.sourceSnow2;
                        floorDir = cell.sourceObj.snowTile - 1;
                    }
                    else if (index % 3 == 0 && Rand.bytes[index % Rand.MaxBytes] < 8 && !cell.HasObj && cell.FirstThing == null)
                    {
                        sourceFloor = FLOOR.sourceSnow2;
                        floorDir = Rand.bytes[index % Rand.MaxBytes];
                    }
                    else
                    {
                        sourceFloor = FLOOR.sourceSnow;
                    }

                    matFloor = MATERIAL.sourceSnow;
                }
            }

            surface = new FpsResolvedFloorSurface
            {
                Cell = cell,
                Floor = sourceFloor,
                Material = matFloor,
                BaseTile = sourceFloor._tiles[floorDir % sourceFloor._tiles.Length],
                MaterialColor = sourceFloor.GetColorInt(matFloor),
                AutoTileOverlay = cell.autotile != 0 && sourceFloor.autotile != 0
                    ? (26 + sourceFloor.autotile / 2) * 32 + sourceFloor.autotile % 2 * 16 + cell.autotile
                    : -1,
                UseSnowAtlas = floorSnowAtlas,
                UseWaterAutoTileAtlas = sourceFloor.tileType.IsWater
            };
            return true;
        }

        public bool TryResolveWall(Cell cell, out FpsResolvedWallSurface surface)
        {
            surface = default;
            if (cell == null || cell.sourceBlock == null || cell.sourceBlock._tiles == null || cell.sourceBlock._tiles.Length == 0)
            {
                return false;
            }

            int dir = cell.blockDir % cell.sourceBlock._tiles.Length;
            surface = new FpsResolvedWallSurface
            {
                Cell = cell,
                Tile = cell.sourceBlock.GetTile(cell.matBlock, dir),
                MaterialColor = cell.sourceBlock.GetColorInt(cell.matBlock),
                UseSnowAtlas = cell.IsSnowTile
            };
            return true;
        }

        public void GatherSprites(
            Vector2 origin,
            float maxDistance,
            List<FpsResolvedUprightSprite> uprightOutput,
            List<FpsResolvedGroundSprite> groundOutput,
            List<FpsResolvedEffectSprite> effectOutput)
        {
            uprightOutput.Clear();
            groundOutput.Clear();
            effectOutput.Clear();

            int radius = Mathf.CeilToInt(maxDistance) + 1;
            int centerX = Mathf.FloorToInt(origin.x);
            int centerZ = Mathf.FloorToInt(origin.y);

            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || z < 0 || x >= EClass._map.Size || z >= EClass._map.Size)
                    {
                        continue;
                    }

                    Cell cell = EClass._map.cells[x, z];
                    CellDetail detail = cell.detail;
                    if (detail != null)
                    {
                        for (int i = 0; i < detail.things.Count; i++)
                        {
                            TryAddCardSprite(detail.things[i], origin, maxDistance, uprightOutput, groundOutput);
                        }

                        for (int i = 0; i < detail.charas.Count; i++)
                        {
                            TryAddCardSprite(detail.charas[i], origin, maxDistance, uprightOutput, groundOutput);
                        }
                    }

                    TryAddEffectSprite(cell, x, z, origin, maxDistance, effectOutput);
                }
            }
        }

        public static Vector2 GetCardTileCenter(Card card)
        {
            if (card == null)
            {
                return default;
            }

            return new Vector2(card.pos.x + 0.5f, card.pos.z + 0.5f);
        }

        public static Color32 ApplyMatTint(Color32 color, int matColor)
        {
            int num = matColor == 0 ? 104025 : matColor;
            float intensity = (num / 262144) * 0.01f;
            float scale = 0.02f;
            float baseLevel = 0.3f;
            if (intensity != 0f)
            {
                scale *= intensity;
            }

            float tintR = scale * ((num % 262144) / 4096) + baseLevel;
            float tintG = scale * ((num % 4096) / 64) + baseLevel;
            float tintB = scale * (num % 64) + baseLevel;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * tintR), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * tintG), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * tintB), 0, 255),
                color.a);
        }

        private static void TryAddCardSprite(
            Card card,
            Vector2 origin,
            float maxDistance,
            List<FpsResolvedUprightSprite> uprightOutput,
            List<FpsResolvedGroundSprite> groundOutput)
        {
            if (card == null || card == EClass.pc || !card.ExistsOnMap || card.isHidden || card.isRoofItem)
            {
                return;
            }

            BillboardKind kind = ResolveBillboardKind(card);
            if (kind == BillboardKind.LooseItem)
            {
                TryAddGroundSprite(card, origin, maxDistance, groundOutput);
                return;
            }

            TryAddUprightSprite(card, origin, maxDistance, kind, uprightOutput);
        }

        private static void TryAddUprightSprite(
            Card card,
            Vector2 origin,
            float maxDistance,
            BillboardKind kind,
            List<FpsResolvedUprightSprite> output)
        {
            Vector2 position = GetCardTileCenter(card);
            float distance = Vector2.Distance(position, origin);
            if (distance <= 0.1f || distance > maxDistance + 1f)
            {
                return;
            }

            Sprite sprite = card.GetSprite();
            if (sprite == null)
            {
                return;
            }

            RenderData renderData = card.renderer?.data ?? card.sourceRenderCard?.renderData;
            float aspect = Mathf.Max(0.2f, sprite.rect.width) / Mathf.Max(1f, sprite.rect.height);
            float spriteHeight = ResolveBaseHeight(card, kind);
            float spriteWidth = Mathf.Clamp(spriteHeight * aspect, 0.2f, 1.8f);
            float pivotX = 0.5f;
            float pivotY = ResolvePivotY(kind);
            if (renderData != null)
            {
                pivotX = Mathf.Clamp01(0.5f - 0.005f * (card.Pref?.pivotX ?? 0));
            }

            float groundHeightWorld = GetCellSurfaceHeight(card.pos.cell) + ResolveBillboardElevation(card, kind);
            position = ApplyScatterOffset(position, card, kind);

            output.Add(new FpsResolvedUprightSprite
            {
                AnchorWorld = new Vector3(position.x, groundHeightWorld, position.y),
                Sprite = sprite,
                RenderData = renderData,
                Tile = 0,
                Distance = distance,
                WidthWorld = spriteWidth,
                HeightWorld = spriteHeight,
                PivotX = pivotX,
                PivotY = pivotY,
                ShadowSizeWorld = Mathf.Max(0.12f, spriteWidth * 0.25f),
                CastsShadow = true,
                FacingRule = BillboardFacingRule.CameraFacing
            });
        }

        private static void TryAddGroundSprite(
            Card card,
            Vector2 origin,
            float maxDistance,
            List<FpsResolvedGroundSprite> output)
        {
            Vector2 position = GetCardTileCenter(card);
            float distance = Vector2.Distance(position, origin);
            if (distance <= 0.1f || distance > maxDistance + 1f)
            {
                return;
            }

            Sprite sprite = card.GetSprite();
            if (sprite == null)
            {
                return;
            }

            position += ResolveGroundScatter(card);
            float groundHeightWorld = GetCellSurfaceHeight(card.pos.cell) + ResolveGroundElevation(card);
            float aspect = Mathf.Max(0.2f, sprite.rect.width) / Mathf.Max(1f, sprite.rect.height);
            float width = Mathf.Clamp(0.18f + aspect * 0.14f, 0.18f, 0.48f);
            float depth = Mathf.Clamp(0.16f + aspect * 0.08f, 0.16f, 0.34f);

            output.Add(new FpsResolvedGroundSprite
            {
                CenterWorld = new Vector3(position.x, groundHeightWorld + 0.01f, position.y),
                Sprite = sprite,
                Distance = distance,
                SizeWorld = new Vector2(width, depth)
            });
        }

        private static BillboardKind ResolveBillboardKind(Card card)
        {
            if (card == null)
            {
                return BillboardKind.LooseItem;
            }

            if (card.isChara)
            {
                return BillboardKind.Chara;
            }

            if (card.IsInstalled)
            {
                return card.sourceCard?.multisize == true ? BillboardKind.TallObject : BillboardKind.InstalledObject;
            }

            return BillboardKind.LooseItem;
        }

        private static float ResolveBaseHeight(Card card, BillboardKind kind)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return CharaBaseHeight;
                case BillboardKind.InstalledObject:
                    return InstalledBaseHeight;
                case BillboardKind.TallObject:
                    return TallObjectBaseHeight;
                default:
                    return LooseItemBaseHeight;
            }
        }

        private static float ResolvePivotY(BillboardKind kind)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return 0.04f;
                case BillboardKind.InstalledObject:
                    return 0.03f;
                case BillboardKind.TallObject:
                    return 0.02f;
                default:
                    return 0.01f;
            }
        }

        private static Vector2 ApplyScatterOffset(Vector2 position, Card card, BillboardKind kind)
        {
            if (card == null)
            {
                return position;
            }

            if (card.freePos && kind != BillboardKind.LooseItem)
            {
                float fx = Mathf.Clamp(card.fx * 0.05f, -0.12f, 0.12f);
                float fy = Mathf.Clamp(card.fy * 0.05f, -0.12f, 0.12f);
                return new Vector2(position.x + fx, position.y + fy);
            }

            return position;
        }

        private static Vector2 ResolveGroundScatter(Card card)
        {
            if (card == null)
            {
                return default;
            }

            if (card.freePos)
            {
                return new Vector2(
                    Mathf.Clamp(card.fx * 0.03f, -0.08f, 0.08f),
                    Mathf.Clamp(card.fy * 0.03f, -0.08f, 0.08f));
            }

            int hash = card.GetHashCode();
            float offsetX = (((hash >> 1) & 3) - 1.5f) * 0.025f;
            float offsetZ = (((hash >> 3) & 3) - 1.5f) * 0.025f;
            return new Vector2(offsetX, offsetZ);
        }

        private static float ResolveGroundElevation(Card card)
        {
            if (card == null)
            {
                return 0f;
            }

            float elevation = Mathf.Clamp(card.altitude, 0, 4) * 0.06f;
            if (card is Thing thing && !card.ignoreStackHeight)
            {
                elevation += Mathf.Clamp(thing.stackOrder, 0, 6) * 0.025f;
            }

            return Mathf.Clamp(elevation, 0f, 0.25f);
        }

        private static float ResolveBillboardElevation(Card card, BillboardKind kind)
        {
            if (card == null)
            {
                return 0f;
            }

            float elevation = 0f;
            BaseTileMap tileMap = EClass.screen?.tileMap;
            if (tileMap != null)
            {
                if (card.altitude != 0)
                {
                    elevation += card.altitude * 0.12f;
                }
            }

            if (card is Thing thing && !card.ignoreStackHeight)
            {
                elevation += Mathf.Clamp(thing.stackOrder, 0, 6) * 0.05f;
            }

            if (kind == BillboardKind.InstalledObject || kind == BillboardKind.TallObject)
            {
                SourcePref pref = card.Pref;
                if (pref != null)
                {
                    elevation += Mathf.Clamp(pref.height * 0.08f, 0f, 0.25f);
                }
            }
            else if (kind == BillboardKind.Chara)
            {
                elevation += 0.02f;
            }

            return Mathf.Clamp(elevation, 0f, 0.6f);
        }

        private static void TryAddEffectSprite(
            Cell cell,
            int cellX,
            int cellZ,
            Vector2 origin,
            float maxDistance,
            List<FpsResolvedEffectSprite> output)
        {
            if (cell?.effect == null || cell.effect.IsLiquid)
            {
                return;
            }

            RenderData renderData = cell.effect.IsFire
                ? EClass.screen?.tileMap?.rendererEffect
                : cell.sourceEffect?.renderData;
            if (renderData == null)
            {
                return;
            }

            Vector2 position = new Vector2(cellX + 0.5f, cellZ + 0.5f);
            float distance = Vector2.Distance(position, origin);
            if (distance <= 0.1f || distance > maxDistance + 1f)
            {
                return;
            }

            int tile = cell.effect.source._tiles[0];
            SourceCellEffect.Row sourceEffect = cell.sourceEffect;
            if (sourceEffect?.anime != null && sourceEffect.anime.Length != 0)
            {
                if (sourceEffect.anime.Length > 2)
                {
                    float frame = Time.realtimeSinceStartup * 1000f / sourceEffect.anime[1] % sourceEffect.anime[2];
                    if (frame < sourceEffect.anime[0])
                    {
                        tile += Mathf.FloorToInt(frame);
                    }
                }
                else
                {
                    float frame = Time.realtimeSinceStartup * 1000f / sourceEffect.anime[1] % sourceEffect.anime[0];
                    tile += Mathf.FloorToInt(frame);
                }
            }
            else if (renderData is RenderDataEffect effectData)
            {
                tile += Mathf.FloorToInt((Time.realtimeSinceStartup * effectData.speed + cellX + cellZ) % 5f);
            }

            output.Add(new FpsResolvedEffectSprite
            {
                AnchorWorld = new Vector3(position.x, GetCellSurfaceHeight(cell) + 0.05f, position.y),
                Distance = distance,
                WidthWorld = Mathf.Max(0.4f, renderData.size.x * Mathf.Max(0.1f, renderData.imageScale.x)),
                HeightWorld = Mathf.Max(0.6f, renderData.size.y * Mathf.Max(0.1f, renderData.imageScale.y)),
                PivotX = 0.5f,
                PivotY = 0.05f,
                RenderData = renderData,
                Tile = tile
            });
        }
    }

    internal struct FpsResolvedFloorSurface
    {
        public Cell Cell;
        public SourceFloor.Row Floor;
        public SourceMaterial.Row Material;
        public int BaseTile;
        public int MaterialColor;
        public int AutoTileOverlay;
        public bool UseSnowAtlas;
        public bool UseWaterAutoTileAtlas;
    }

    internal struct FpsResolvedWallSurface
    {
        public Cell Cell;
        public int Tile;
        public int MaterialColor;
        public bool UseSnowAtlas;
    }

    internal struct FpsResolvedUprightSprite
    {
        public Vector3 AnchorWorld;
        public Sprite Sprite;
        public RenderData RenderData;
        public int Tile;
        public float Distance;
        public float WidthWorld;
        public float HeightWorld;
        public float PivotX;
        public float PivotY;
        public float ShadowSizeWorld;
        public bool CastsShadow;
        public BillboardFacingRule FacingRule;
    }

    internal struct FpsResolvedGroundSprite
    {
        public Vector3 CenterWorld;
        public Sprite Sprite;
        public float Distance;
        public Vector2 SizeWorld;
    }

    internal struct FpsResolvedEffectSprite
    {
        public Vector3 AnchorWorld;
        public RenderData RenderData;
        public int Tile;
        public float Distance;
        public float WidthWorld;
        public float HeightWorld;
        public float PivotX;
        public float PivotY;
    }

    internal enum BillboardKind
    {
        LooseItem,
        Chara,
        InstalledObject,
        TallObject
    }

    internal enum BillboardFacingRule
    {
        CameraFacing
    }
}
