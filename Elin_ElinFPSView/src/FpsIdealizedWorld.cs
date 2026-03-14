using System.Collections.Generic;
using UnityEngine;

namespace Elin_ElinFPSView
{
    internal sealed class FpsIdealizedWorld
    {
        private const float SpritePixelsPerTile = 64f;
        private const float LargeObjectDistanceMultiplier = 1.0f;
        private const float GameplayDistanceMultiplier = 1.0f;
        private const float ItemDistanceMultiplier = 0.85f;
        private const float EffectDistanceMultiplier = 0.9f;
        private const float LargeObjectConeDot = 0.17364818f; // ~160 degrees
        private const float SmallObjectConeDot = 0.259f; // ~150 degrees
        private const float GameplayConeDot = 0.0f; // ~180 degrees, frustum still applies
        private const float LooseItemBaseHeight = 0.18f;
        private const float CharaBaseHeight = 1.05f;
        private const float InstalledBaseHeight = 0.9f;
        private const float TallObjectBaseHeight = 1.3f;
        private readonly FpsLightingResolver _lightingResolver = new FpsLightingResolver();
        private static bool _loggedCellObjectSpriteFailure;
        private static bool _loggedCellEffectSpriteFailure;

        public void PrepareFrame()
        {
            BaseTileMap tileMap = EClass.scene?.screenElin?.tileMap ?? EClass.screen?.tileMap;
            _lightingResolver.CaptureFrame(tileMap, Plugin.Settings.EnableWorldLighting.Value);
        }

        public static float GetCellSurfaceHeight(Cell cell)
        {
            if (cell == null)
            {
                return 0f;
            }

            float baseHeight = (cell.bridgeHeight == 0 ? cell.height : cell.bridgeHeight) * GetTerrainHeightScale();
            SourceFloor.Row floor = cell.HasBridge ? cell.sourceBridge : cell.sourceFloor;
            if (floor != null)
            {
                baseHeight += floor.tileType.FloorHeight;
            }

            return baseHeight;
        }

        public static float GetTerrainHeightScale()
        {
            if (EClass.screen?.tileMap != null)
            {
                float referenceHeight = Mathf.Abs(EClass.screen.tileWorldSize.y);
                if (referenceHeight <= 0.0001f)
                {
                    referenceHeight = Mathf.Abs(EClass.screen.tileAlign.y);
                }

                if (referenceHeight > 0.0001f)
                {
                    return Mathf.Abs(EClass.screen.tileMap._heightMod.y / referenceHeight) * 0.5f;
                }
            }

            return 0.04f;
        }

        public static float GetSurfaceHeightAt(Vector2 position)
        {
            if (EClass._map == null || EClass._map.Size <= 0)
            {
                return 0f;
            }

            float sampleX = Mathf.Clamp(position.x - 0.5f, 0f, EClass._map.Size - 1f);
            float sampleZ = Mathf.Clamp(position.y - 0.5f, 0f, EClass._map.Size - 1f);
            int x0 = Mathf.FloorToInt(sampleX);
            int z0 = Mathf.FloorToInt(sampleZ);
            int x1 = Mathf.Min(x0 + 1, EClass._map.Size - 1);
            int z1 = Mathf.Min(z0 + 1, EClass._map.Size - 1);
            float tx = sampleX - x0;
            float tz = sampleZ - z0;

            float h00 = GetCellSurfaceHeight(EClass._map.cells[x0, z0]);
            float h10 = GetCellSurfaceHeight(EClass._map.cells[x1, z0]);
            float h01 = GetCellSurfaceHeight(EClass._map.cells[x0, z1]);
            float h11 = GetCellSurfaceHeight(EClass._map.cells[x1, z1]);
            float hx0 = Mathf.Lerp(h00, h10, tx);
            float hx1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(hx0, hx1, tz);
        }

        public bool TryResolveFloor(Cell cell, int index, out FpsResolvedFloorSurface surface)
        {
            surface = default;
            if (cell == null)
            {
                return false;
            }

            FpsResolvedCellLighting lighting = _lightingResolver.ResolveCellLighting(cell);

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
                    UseWaterAutoTileAtlas = floor.tileType.IsWater,
                    Light = lighting.FloorLight
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
                    if (cell.sourceObj != null && cell.sourceObj.snowTile > 0)
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
                UseWaterAutoTileAtlas = sourceFloor.tileType.IsWater,
                Light = lighting.FloorLight
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

            FpsResolvedCellLighting lighting = _lightingResolver.ResolveCellLighting(cell);
            int dirIndex = Mathf.Abs(cell.blockDir) % cell.sourceBlock._tiles.Length;
            int tile = cell.sourceBlock.tileType.IsFullBlock
                ? Mathf.Abs(cell.sourceBlock._tiles[dirIndex])
                : cell.sourceBlock.tileType.IsWallOrFence
                ? cell.sourceBlock._tiles[0]
                : cell.sourceBlock.GetTile(cell.matBlock, cell.blockDir % cell.sourceBlock._tiles.Length);
            surface = new FpsResolvedWallSurface
            {
                Cell = cell,
                Tile = tile,
                RenderData = cell.sourceBlock.renderData,
                MaterialColor = cell.sourceBlock.GetColorInt(cell.matBlock),
                UseSnowAtlas = cell.IsSnowTile,
                Light = lighting.BlockLight
            };
            return true;
        }

        public bool TryResolveTerrainRiser(Cell cell, out FpsResolvedWallSurface surface)
        {
            surface = default;
            if (cell == null)
            {
                return false;
            }

            SourceBlock.Row block = null;
            int materialColor = 104025;
            if (cell.HasBridge && cell.sourceBridge?._bridgeBlock != null)
            {
                block = cell.sourceBridge._bridgeBlock;
                if (block.colorMod != 0 && cell.matBridge != null)
                {
                    materialColor = block.GetColorInt(cell.matBridge);
                }
            }
            else if (cell.sourceBlock?.tileType?.IsFullBlock == true && cell.sourceBlock._tiles != null && cell.sourceBlock._tiles.Length > 0)
            {
                block = cell.sourceBlock;
                materialColor = cell.sourceBlock.GetColorInt(cell.matBlock);
            }
            else if (cell.sourceFloor?._defBlock != null)
            {
                block = cell.sourceFloor._defBlock;
                if (block.id != 1 && cell.sourceFloor.colorMod != 0 && cell.matFloor != null)
                {
                    materialColor = cell.sourceFloor.GetColorInt(cell.matFloor);
                }
            }

            if (block == null || block._tiles == null || block._tiles.Length == 0)
            {
                return false;
            }

            FpsResolvedCellLighting lighting = _lightingResolver.ResolveCellLighting(cell);
            int dirIndex = Mathf.Abs(cell.blockDir) % block._tiles.Length;
            surface = new FpsResolvedWallSurface
            {
                Cell = cell,
                Tile = Mathf.Abs(block._tiles[dirIndex]),
                RenderData = block.renderData,
                MaterialColor = materialColor,
                UseSnowAtlas = cell.IsSnowTile,
                Light = lighting.BlockLight
            };
            return true;
        }

        public void GatherSprites(
            Vector2 origin,
            float maxDistance,
            bool includePlayerSelf,
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
                    FpsResolvedCellLighting lighting = _lightingResolver.ResolveCellLighting(cell);
                    CellDetail detail = cell.detail;
                    if (detail != null)
                    {
                        for (int i = 0; i < detail.things.Count; i++)
                        {
                            TryAddCardSprite(detail.things[i], origin, maxDistance, includePlayerSelf, lighting, uprightOutput, groundOutput);
                        }

                        for (int i = 0; i < detail.charas.Count; i++)
                        {
                            TryAddCardSprite(detail.charas[i], origin, maxDistance, includePlayerSelf, lighting, uprightOutput, groundOutput);
                        }
                    }

                    try
                    {
                        TryAddCellObjectSprite(cell, x, z, origin, maxDistance, lighting, uprightOutput, groundOutput);
                    }
                    catch (System.Exception ex)
                    {
                        if (!_loggedCellObjectSpriteFailure)
                        {
                            _loggedCellObjectSpriteFailure = true;
                            Plugin.Log?.LogWarning($"Skipping broken cell object sprites. First failure at {x},{z}: {ex}");
                        }
                    }

                    try
                    {
                        TryAddEffectSprite(cell, x, z, origin, maxDistance, lighting, effectOutput);
                    }
                    catch (System.Exception ex)
                    {
                        if (!_loggedCellEffectSpriteFailure)
                        {
                            _loggedCellEffectSpriteFailure = true;
                            Plugin.Log?.LogWarning($"Skipping broken cell effect sprites. First failure at {x},{z}: {ex}");
                        }
                    }
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

        private void TryAddCardSprite(
            Card card,
            Vector2 origin,
            float maxDistance,
            bool includePlayerSelf,
            FpsResolvedCellLighting lighting,
            List<FpsResolvedUprightSprite> uprightOutput,
            List<FpsResolvedGroundSprite> groundOutput)
        {
            if (card == null || !card.ExistsOnMap || card.isHidden || card.isRoofItem)
            {
                return;
            }

            if (card == EClass.pc && !includePlayerSelf)
            {
                return;
            }

            if (EClass.pc != null && card != EClass.pc && !EClass.pc.CanSee(card))
            {
                return;
            }

            BillboardKind kind = ResolveBillboardKind(card);
            TryAddUprightSprite(card, origin, ResolveCardVisibilityDistance(kind, maxDistance), kind, lighting, uprightOutput);
        }

        private void TryAddUprightSprite(
            Card card,
            Vector2 origin,
            float maxDistance,
            BillboardKind kind,
            FpsResolvedCellLighting lighting,
            List<FpsResolvedUprightSprite> output)
        {
            Vector2 position = GetCardTileCenter(card);
            float distance = Vector2.Distance(position, origin);
            if (distance <= 0.1f || distance > maxDistance + 1f)
            {
                return;
            }

            Sprite sprite = ResolveCardSprite(card);
            if (sprite == null)
            {
                return;
            }

            RenderData renderData = card.renderer?.data ?? card.sourceRenderCard?.renderData;
            Vector2 spriteSize = ResolveCardSpriteWorldSize(sprite, renderData, kind);
            float spriteWidth = spriteSize.x;
            float spriteHeight = spriteSize.y;
            float pivotX = 0.5f;
            float pivotY = ResolvePivotY(kind);
            if (renderData != null)
            {
                pivotX = Mathf.Clamp01(0.5f - 0.005f * (card.Pref?.pivotX ?? 0));
            }

            float groundHeightWorld = GetCellSurfaceHeight(card.pos.cell) + ResolveBillboardElevation(card, kind);
            position = ApplyScatterOffset(position, card, kind);
            FpsResolvedLightSample lightSample = ResolveUprightCardLight(kind, lighting);

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
                FacingRule = BillboardFacingRule.CameraFacing,
                VisibilityDistance = ResolveCardVisibilityDistance(kind, maxDistance),
                VisibilityConeDot = ResolveCardVisibilityConeDot(kind),
                Light = lightSample,
                    DiagnosticCategory = kind == BillboardKind.Chara ? "npc" : "item",
                    DiagnosticLabel = card.id.ToString(),
                    SourcePixelSize = new Vector2(sprite.textureRect.width, sprite.textureRect.height),
                    RenderDataSize = renderData != null ? renderData.size : Vector2.zero,
                    ImageScale = renderData != null ? renderData.imageScale : Vector2.one
                });
        }

        private void TryAddGroundSprite(
            Card card,
            Vector2 origin,
            float maxDistance,
            FpsResolvedCellLighting lighting,
            List<FpsResolvedGroundSprite> output)
        {
            Vector2 position = GetCardTileCenter(card);
            float distance = Vector2.Distance(position, origin);
            if (distance <= 0.1f || distance > maxDistance + 1f)
            {
                return;
            }

            Sprite sprite = ResolveCardSprite(card);
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
                RenderData = null,
                Tile = 0,
                Distance = distance,
                SizeWorld = new Vector2(width, depth),
                MaterialColor = 0,
                HasMaterialTint = false,
                Light = lighting.FloorLight,
                DiagnosticCategory = "item",
                DiagnosticLabel = card.id.ToString(),
                SourcePixelSize = new Vector2(sprite.rect.width, sprite.rect.height),
                RenderDataSize = Vector2.zero,
                ImageScale = Vector2.one
            });
        }

        private void TryAddCellObjectSprite(
            Cell cell,
            int cellX,
            int cellZ,
            Vector2 origin,
            float maxDistance,
            FpsResolvedCellLighting lighting,
            List<FpsResolvedUprightSprite> uprightOutput,
            List<FpsResolvedGroundSprite> groundOutput)
        {
            if (cell == null || cell.obj == 0)
            {
                return;
            }

            if (!cell.isSeen)
            {
                return;
            }

            SourceObj.Row sourceObj = cell.sourceObj;
            RenderData renderData = ResolveCellObjectRenderData(cell);
            if (sourceObj == null || renderData == null || renderData.SkipOnMap)
            {
                return;
            }

            Vector2 position = new Vector2(cellX + 0.5f, cellZ + 0.5f);
            float distance = Vector2.Distance(position, origin);
            bool uprightObject = IsUprightCellObject(cell, sourceObj, renderData);
            float visibilityDistance = ResolveCellObjectVisibilityDistance(sourceObj, renderData, uprightObject, maxDistance);
            if (distance <= 0.1f || distance > visibilityDistance + 1f)
            {
                return;
            }

            if (!TryResolveCellObjectTile(cell, out int tile))
            {
                return;
            }

            int materialColor = ResolveCellObjectMaterialColor(cell, sourceObj);
            bool hasMaterialTint = sourceObj.colorMod != 0 || sourceObj.useAltColor;
            bool useSelectiveMaterialTint = sourceObj.HasGrowth && hasMaterialTint;
            if (uprightObject)
            {
                Vector2 worldSize = ResolveCellObjectUprightWorldSize(sourceObj, renderData);
                float elevation = ResolveCellObjectElevation(cell, sourceObj);
                float pivotX = 0.5f;
                float pivotY = 0f;

                uprightOutput.Add(new FpsResolvedUprightSprite
                {
                    AnchorWorld = new Vector3(position.x, GetCellSurfaceHeight(cell) + elevation, position.y),
                    Sprite = null,
                    RenderData = renderData,
                    Tile = tile,
                    Distance = distance,
                    WidthWorld = worldSize.x,
                    HeightWorld = worldSize.y,
                    PivotX = pivotX,
                    PivotY = pivotY,
                    ShadowSizeWorld = Mathf.Max(0.16f, worldSize.x * 0.25f),
                    CastsShadow = sourceObj.pref.shadow > 1 && !cell.ignoreObjShadow,
                    FacingRule = BillboardFacingRule.CameraFacing,
                    VisibilityDistance = visibilityDistance,
                    VisibilityConeDot = ResolveCellObjectVisibilityConeDot(sourceObj, renderData, true),
                    MaterialColor = hasMaterialTint ? materialColor : 0,
                    HasMaterialTint = hasMaterialTint,
                    UseSelectiveMaterialTint = useSelectiveMaterialTint,
                    Light = lighting.BlockLight,
                    DiagnosticCategory = sourceObj.HasGrowth ? "tree" : "furniture",
                    DiagnosticLabel = sourceObj.alias ?? sourceObj.id.ToString(),
                    SourcePixelSize = ResolveRenderDataSourcePixelSize(renderData),
                    RenderDataSize = renderData.size,
                    ImageScale = renderData.imageScale
                });
            }
            else
            {
                Vector2 groundSize = ResolveCellObjectGroundWorldSize(renderData);
                groundOutput.Add(new FpsResolvedGroundSprite
                {
                    CenterWorld = new Vector3(position.x, GetCellSurfaceHeight(cell) + ResolveCellObjectElevation(cell, sourceObj) + 0.01f, position.y),
                    Sprite = null,
                    RenderData = renderData,
                    Tile = tile,
                    Distance = distance,
                    SizeWorld = groundSize,
                    VisibilityDistance = visibilityDistance,
                    VisibilityConeDot = ResolveCellObjectVisibilityConeDot(sourceObj, renderData, false),
                    MaterialColor = hasMaterialTint ? materialColor : 0,
                    HasMaterialTint = hasMaterialTint,
                    UseSelectiveMaterialTint = useSelectiveMaterialTint,
                    Light = lighting.FloorLight,
                    DiagnosticCategory = sourceObj.HasGrowth ? "tree" : "furniture",
                    DiagnosticLabel = sourceObj.alias ?? sourceObj.id.ToString(),
                    SourcePixelSize = ResolveRenderDataSourcePixelSize(renderData),
                    RenderDataSize = renderData.size,
                    ImageScale = renderData.imageScale
                });
            }
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

        private static Sprite ResolveCardSprite(Card card)
        {
            if (card?.renderer?.actor?.sr?.sprite != null)
            {
                return card.renderer.actor.sr.sprite;
            }

            return card?.GetSprite();
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

        private static Vector2 ResolveCardSpriteWorldSize(Sprite sprite, RenderData renderData, BillboardKind kind)
        {
            if (sprite == null)
            {
                float fallbackHeight = ResolveBaseHeight(null, kind);
                return new Vector2(fallbackHeight, fallbackHeight);
            }

            float scaleX = renderData != null ? Mathf.Max(0.1f, renderData.imageScale.x) : 1f;
            float scaleY = renderData != null ? Mathf.Max(0.1f, renderData.imageScale.y) : 1f;
            float width = sprite.textureRect.width * scaleX / SpritePixelsPerTile;
            float height = sprite.textureRect.height * scaleY / SpritePixelsPerTile;
            return new Vector2(
                Mathf.Max(0.02f, width),
                Mathf.Max(0.02f, height));
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

            float elevation = ResolveSupportHeight(card);
            elevation += Mathf.Clamp(card.altitude, 0, 4) * 0.06f;
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

            float elevation = ResolveSupportHeight(card);
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

        private static FpsResolvedLightSample ResolveUprightCardLight(BillboardKind kind, FpsResolvedCellLighting lighting)
        {
            switch (kind)
            {
                case BillboardKind.InstalledObject:
                case BillboardKind.TallObject:
                    return lighting.BlockLight;
                case BillboardKind.Chara:
                default:
                    return lighting.ApproxBlockLight;
            }
        }

        private static RenderData ResolveCellObjectRenderData(Cell cell)
        {
            if (cell?.sourceObj == null)
            {
                return null;
            }

            if (cell.sourceObj.HasGrowth && cell.growth?.stages != null && cell.growth.stages.Length > 0)
            {
                int stageIndex = Mathf.Clamp(cell.objVal / 30, 0, cell.growth.stages.Length - 1);
                return cell.growth.stages[stageIndex].renderData ?? cell.sourceObj.renderData;
            }

            return cell.sourceObj.renderData;
        }

        private static int ResolveCellObjectMaterialColor(Cell cell, SourceObj.Row sourceObj)
        {
            if (sourceObj == null)
            {
                return 104025;
            }

            SourceMaterial.Row material = cell?.matObj ?? sourceObj.DefaultMaterial;
            if (material == null)
            {
                return 104025;
            }

            return sourceObj.GetColorInt(material);
        }

        private static bool TryResolveCellObjectTile(Cell cell, out int tile)
        {
            tile = 0;
            if (cell?.sourceObj == null)
            {
                return false;
            }

            SourceObj.Row sourceObj = cell.sourceObj;
            if (sourceObj.HasGrowth && cell.growth?.stages != null && cell.growth.stages.Length > 0)
            {
                int stageIndex = Mathf.Clamp(cell.objVal / 30, 0, cell.growth.stages.Length - 1);
                GrowSystem.Stage stage = cell.growth.stages[stageIndex];
                if (stage?.tiles == null || stage.tiles.Length == 0)
                {
                    return false;
                }

                tile = stage.tiles[cell.objDir % stage.tiles.Length];
                return true;
            }

            if (sourceObj._tiles == null || sourceObj._tiles.Length == 0)
            {
                return false;
            }

            if (cell.autotileObj != 0)
            {
                tile = sourceObj._tiles[0] + cell.autotileObj;
            }
            else if (sourceObj.tileType.IsUseBlockDir)
            {
                tile = sourceObj._tiles[cell.blockDir % sourceObj._tiles.Length];
            }
            else
            {
                tile = sourceObj._tiles[cell.objDir % sourceObj._tiles.Length];
            }

            return true;
        }

        private static bool IsUprightCellObject(Cell cell, SourceObj.Row sourceObj, RenderData renderData)
        {
            if (sourceObj == null || renderData == null)
            {
                return false;
            }

            if (sourceObj.HasGrowth && cell?.growth != null)
            {
                return true;
            }

            if (renderData.multiSize || renderData is RenderDataObjV || sourceObj.pref.shadow > 1 || sourceObj.pref.height > 0.05f)
            {
                return true;
            }

            if (renderData.pass?.pmesh != null && renderData.pass.pmesh.top)
            {
                return false;
            }

            return renderData.size.y * Mathf.Max(0.1f, renderData.imageScale.y)
                >= renderData.size.x * Mathf.Max(0.1f, renderData.imageScale.x) * 0.9f;
        }

        private static Vector2 ResolveCellObjectUprightWorldSize(SourceObj.Row sourceObj, RenderData renderData)
        {
            return ResolveRenderDataWorldSize(renderData);
        }

        private static Vector2 ResolveCellObjectGroundWorldSize(RenderData renderData)
        {
            return ResolveRenderDataWorldSize(renderData);
        }

        private static Vector2 ResolveRenderDataWorldSize(RenderData renderData)
        {
            if (renderData == null)
            {
                return Vector2.one;
            }

            Vector2 sourcePixelSize = ResolveRenderDataSourcePixelSize(renderData);
            float scaleX = Mathf.Max(0.1f, renderData.imageScale.x);
            float scaleY = Mathf.Max(0.1f, renderData.imageScale.y);
            return new Vector2(
                Mathf.Max(0.02f, sourcePixelSize.x * scaleX / SpritePixelsPerTile),
                Mathf.Max(0.02f, sourcePixelSize.y * scaleY / SpritePixelsPerTile));
        }

        private static Vector2 ResolveRenderDataSourcePixelSize(RenderData renderData)
        {
            Texture texture = renderData?.pass?.mat?.GetTexture("_MainTex");
            ProceduralMesh pmesh = renderData?.pass?.pmesh;
            if (texture == null || pmesh == null || pmesh.tiling.x <= 0f || pmesh.tiling.y <= 0f)
            {
                return Vector2.zero;
            }

            float width = texture.width / pmesh.tiling.x;
            float height = texture.height / pmesh.tiling.y;
            if (renderData.multiSize)
            {
                height *= 2f;
            }

            return new Vector2(width, height);
        }


        private static float ResolveCellObjectElevation(Cell cell, SourceObj.Row sourceObj)
        {
            if (cell == null || sourceObj == null)
            {
                return 0f;
            }

            float elevation = 0f;
            if (sourceObj.pref.Float && cell.IsTopWater)
            {
                elevation += 0.08f;
            }

            return elevation;
        }

        private static float ResolveSupportHeight(Card card)
        {
            if (!(card is Thing target) || target.ignoreStackHeight || target.pos?.cell?.detail == null)
            {
                return 0f;
            }

            CellDetail detail = target.pos.cell.detail;
            float supportHeight = 0f;
            float lastStackHeight = 0f;
            Card lastInstalled = null;
            for (int i = 0; i < detail.things.Count; i++)
            {
                Thing thing = detail.things[i];
                if (thing == target)
                {
                    break;
                }

                if (!thing.IsInstalled)
                {
                    continue;
                }

                TileType tileType = thing.TileType;
                if (!tileType.CanStack)
                {
                    continue;
                }

                SourcePref pref = thing.Pref;
                float stackHeight = tileType.UseMountHeight
                    ? 0f
                    : ((pref == null || Mathf.Abs(pref.height) <= 0.0001f) ? 0.1f : pref.height);

                if (thing.ignoreStackHeight)
                {
                    supportHeight -= lastStackHeight;
                }

                supportHeight += stackHeight;
                if (!tileType.UseMountHeight && thing.altitude != 0)
                {
                    stackHeight += Mathf.Clamp(thing.altitude, 0, 6) * 0.06f;
                }

                if (thing.trait.IgnoreLastStackHeight && (lastInstalled == null || !lastInstalled.trait.IgnoreLastStackHeight))
                {
                    supportHeight -= lastStackHeight;
                }

                lastStackHeight = stackHeight;
                lastInstalled = thing;
            }

            if (target.ignoreStackHeight)
            {
                supportHeight -= lastStackHeight;
            }

            return Mathf.Clamp(supportHeight, 0f, 1.25f);
        }

        private static void TryAddEffectSprite(
            Cell cell,
            int cellX,
            int cellZ,
            Vector2 origin,
            float maxDistance,
            FpsResolvedCellLighting lighting,
            List<FpsResolvedEffectSprite> output)
        {
            if (cell?.effect == null || cell.effect.IsLiquid)
            {
                return;
            }

            if (!cell.isSeen)
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
            float visibilityDistance = maxDistance * EffectDistanceMultiplier;
            float distance = Vector2.Distance(position, origin);
            if (distance <= 0.1f || distance > visibilityDistance + 1f)
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
                VisibilityDistance = visibilityDistance,
                VisibilityConeDot = GameplayConeDot,
                RenderData = renderData,
                Tile = tile,
                Light = lighting.ApproxBlockLight
            });
        }

        private static float ResolveCardVisibilityDistance(BillboardKind kind, float maxDistance)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return maxDistance * GameplayDistanceMultiplier;
                case BillboardKind.InstalledObject:
                case BillboardKind.TallObject:
                    return maxDistance * LargeObjectDistanceMultiplier;
                default:
                    return maxDistance * ItemDistanceMultiplier;
            }
        }

        private static float ResolveCardVisibilityConeDot(BillboardKind kind)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return GameplayConeDot;
                case BillboardKind.InstalledObject:
                case BillboardKind.TallObject:
                    return LargeObjectConeDot;
                default:
                    return SmallObjectConeDot;
            }
        }

        private static float ResolveCellObjectVisibilityDistance(SourceObj.Row sourceObj, RenderData renderData, bool upright, float maxDistance)
        {
            if (upright)
            {
                return maxDistance * LargeObjectDistanceMultiplier;
            }

            return maxDistance * GameplayDistanceMultiplier;
        }

        private static float ResolveCellObjectVisibilityConeDot(SourceObj.Row sourceObj, RenderData renderData, bool upright)
        {
            return upright ? LargeObjectConeDot : SmallObjectConeDot;
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
        public FpsResolvedLightSample Light;
    }

    internal struct FpsResolvedWallSurface
    {
        public Cell Cell;
        public int Tile;
        public RenderData RenderData;
        public int MaterialColor;
        public bool UseSnowAtlas;
        public FpsResolvedLightSample Light;
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
        public float VisibilityDistance;
        public float VisibilityConeDot;
        public int MaterialColor;
        public bool HasMaterialTint;
        public bool UseSelectiveMaterialTint;
        public FpsResolvedLightSample Light;
        public string DiagnosticCategory;
        public string DiagnosticLabel;
        public Vector2 SourcePixelSize;
        public Vector2 RenderDataSize;
        public Vector2 ImageScale;
    }

    internal struct FpsResolvedGroundSprite
    {
        public Vector3 CenterWorld;
        public Sprite Sprite;
        public RenderData RenderData;
        public int Tile;
        public float Distance;
        public Vector2 SizeWorld;
        public float VisibilityDistance;
        public float VisibilityConeDot;
        public int MaterialColor;
        public bool HasMaterialTint;
        public bool UseSelectiveMaterialTint;
        public FpsResolvedLightSample Light;
        public string DiagnosticCategory;
        public string DiagnosticLabel;
        public Vector2 SourcePixelSize;
        public Vector2 RenderDataSize;
        public Vector2 ImageScale;
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
        public float VisibilityDistance;
        public float VisibilityConeDot;
        public FpsResolvedLightSample Light;
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
