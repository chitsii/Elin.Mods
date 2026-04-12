using System.Collections.Generic;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class FpsIdealizedWorld
    {
        private const float SpritePixelsPerTile = 64f;
        private const float LargeObjectDistanceMultiplier = 1.45f;
        private const float GameplayDistanceMultiplier = 1.35f;
        private const float ItemDistanceMultiplier = 1.1f;
        private const float EffectDistanceMultiplier = 0.9f;
        private const float LargeObjectConeDot = 0.17364818f; // ~160 degrees
        private const float SmallObjectConeDot = 0.259f; // ~150 degrees
        private const float GameplayConeDot = 0.0f; // ~180 degrees, frustum still applies
        private const float RoofHeightOffset = 0.01f;
        private const float HybridRoofEaveDepth = 0.16f;
        private const float HybridRoofThickness = 0.18f;
        private const float HybridRoofRidgeCapWidth = 0.18f;
        private const float LooseItemBaseHeight = 0.18f;
        private const float CharaBaseHeight = 1.05f;
        private const float InstalledBaseHeight = 0.9f;
        private const float TallObjectBaseHeight = 1.3f;
        private readonly FpsLightingResolver _lightingResolver = new FpsLightingResolver();
        private static bool _loggedCellObjectSpriteFailure;
        private static bool _loggedCellEffectSpriteFailure;
        private readonly HashSet<int> _visitedRoofLots = new HashSet<int>();
        private readonly List<FpsResolvedRoofPlane> _cachedRoofPlanes = new List<FpsResolvedRoofPlane>(128);
        private readonly List<FpsResolvedRoofStructure> _cachedRoofStructures = new List<FpsResolvedRoofStructure>(64);
        private int _loggedWallFaceObjects;
        private int _loggedDoorRejections;
        private int _loggedWallMountedCandidates;
        private int _loggedCellObjectSurvey;
        private int _loggedRoofLots;
        private int _loggedRoofPlanes;
        private int _cachedRoofMapId;
        private int _cachedRoofOriginX = int.MinValue;
        private int _cachedRoofOriginZ = int.MinValue;
        private int _cachedRoofRadius = int.MinValue;
        private int _cachedRoofCurrentLotId = int.MinValue;
        private bool _cachedRoofShowRoof;
        private bool _cachedRoofHideRoomFog;
        private bool _cachedRoofNoRoofMode;
        private bool _hasCachedRoofPlanes;
        private bool _hasCachedRoofStructures;

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

        public bool TryResolveBaseFloor(Cell cell, int index, out FpsResolvedFloorSurface surface)
        {
            surface = default;
            if (cell == null || cell.sourceFloor == null)
            {
                return false;
            }

            FpsResolvedCellLighting lighting = _lightingResolver.ResolveCellLighting(cell);
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
                        floorDir = Rand.bytes[index % Rand.MaxBytes] % 3 + 10;
                    }
                    else
                    {
                        sourceFloor = FLOOR.sourceSnow;
                        matFloor = MATERIAL.sourceSnow;
                    }
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
            List<FpsResolvedWallMountedSprite> wallMountedOutput,
            List<FpsResolvedUprightSprite> uprightOutput,
            List<FpsResolvedGroundSprite> groundOutput,
            List<FpsResolvedEffectSprite> effectOutput)
        {
            _loggedWallFaceObjects = 0;
            _loggedDoorRejections = 0;
            _loggedWallMountedCandidates = 0;
            _loggedCellObjectSurvey = 0;
            wallMountedOutput.Clear();
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
                            TryAddCardSprite(detail.things[i], origin, maxDistance, includePlayerSelf, lighting, wallMountedOutput, uprightOutput, groundOutput);
                        }

                        for (int i = 0; i < detail.charas.Count; i++)
                        {
                            TryAddCardSprite(detail.charas[i], origin, maxDistance, includePlayerSelf, lighting, wallMountedOutput, uprightOutput, groundOutput);
                        }
                    }

                    try
                    {
                        TryAddCellObjectSprite(cell, x, z, origin, maxDistance, lighting, wallMountedOutput, uprightOutput, groundOutput);
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

        public void GatherRoofPlanes(Vector2 origin, float maxDistance, List<FpsResolvedRoofPlane> output)
        {
            output.Clear();

            BaseTileMap tileMap = EClass.scene?.screenElin?.tileMap ?? EClass.screen?.tileMap;
            if (tileMap?.roofStyles == null || EClass._map == null)
            {
                InvalidateRoofPlaneCache();
                return;
            }

            Room currentRoom = _lightingResolver.GetCurrentRoom() ?? EClass.pc?.pos?.cell?.room;
            Lot currentLot = _lightingResolver.GetCurrentLot() ?? currentRoom?.lot;
            bool showRoof = _lightingResolver.GetShowRoof();
            bool hideRoomFog = _lightingResolver.GetHideRoomFog();
            bool noRoofMode = _lightingResolver.GetNoRoofMode();

            int radius = Mathf.Min(EClass._map.Size - 1, Mathf.CeilToInt(maxDistance) + 8);
            int centerX = Mathf.FloorToInt(origin.x);
            int centerZ = Mathf.FloorToInt(origin.y);
            int mapId = EClass._map.GetHashCode();
            int currentLotId = currentLot?.id ?? 0;
            if (_hasCachedRoofPlanes
                && _cachedRoofMapId == mapId
                && _cachedRoofOriginX == centerX
                && _cachedRoofOriginZ == centerZ
                && _cachedRoofRadius == radius
                && _cachedRoofCurrentLotId == currentLotId
                && _cachedRoofShowRoof == showRoof
                && _cachedRoofHideRoomFog == hideRoomFog
                && _cachedRoofNoRoofMode == noRoofMode)
            {
                output.AddRange(_cachedRoofPlanes);
                return;
            }

            _visitedRoofLots.Clear();
            _loggedRoofLots = 0;
            _loggedRoofPlanes = 0;
            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || z < 0 || x >= EClass._map.Size || z >= EClass._map.Size)
                    {
                        continue;
                    }

                    Lot lot = EClass._map.cells[x, z]?.room?.lot;
                    TryAddRoofLot(lot, tileMap, origin, maxDistance, output, currentLot, showRoof, hideRoomFog, noRoofMode, addInteriorCeiling: false);
                }
            }

            if (currentRoom?.lot != null && currentRoom.HasRoof && !(currentRoom.data?.atrium ?? false))
            {
                TryAddRoofLot(currentRoom.lot, tileMap, origin, maxDistance, output, currentLot, showRoof, hideRoomFog, noRoofMode, addInteriorCeiling: true);
            }

            _cachedRoofPlanes.Clear();
            _cachedRoofPlanes.AddRange(output);
            _cachedRoofMapId = mapId;
            _cachedRoofOriginX = centerX;
            _cachedRoofOriginZ = centerZ;
            _cachedRoofRadius = radius;
            _cachedRoofCurrentLotId = currentLotId;
            _cachedRoofShowRoof = showRoof;
            _cachedRoofHideRoomFog = hideRoomFog;
            _cachedRoofNoRoofMode = noRoofMode;
            _hasCachedRoofPlanes = true;
        }

        public void GatherRoofStructures(Vector2 origin, float maxDistance, List<FpsResolvedRoofStructure> output)
        {
            output.Clear();

            BaseTileMap tileMap = EClass.scene?.screenElin?.tileMap ?? EClass.screen?.tileMap;
            if (tileMap?.roofStyles == null || EClass._map == null)
            {
                InvalidateRoofPlaneCache();
                return;
            }

            Room currentRoom = _lightingResolver.GetCurrentRoom() ?? EClass.pc?.pos?.cell?.room;
            Lot currentLot = _lightingResolver.GetCurrentLot() ?? currentRoom?.lot;
            bool showRoof = _lightingResolver.GetShowRoof();
            bool hideRoomFog = _lightingResolver.GetHideRoomFog();
            bool noRoofMode = _lightingResolver.GetNoRoofMode();

            int radius = Mathf.Min(EClass._map.Size - 1, Mathf.CeilToInt(maxDistance) + 8);
            int centerX = Mathf.FloorToInt(origin.x);
            int centerZ = Mathf.FloorToInt(origin.y);
            int mapId = EClass._map.GetHashCode();
            int currentLotId = currentLot?.id ?? 0;
            if (_hasCachedRoofStructures
                && _cachedRoofMapId == mapId
                && _cachedRoofOriginX == centerX
                && _cachedRoofOriginZ == centerZ
                && _cachedRoofRadius == radius
                && _cachedRoofCurrentLotId == currentLotId
                && _cachedRoofShowRoof == showRoof
                && _cachedRoofHideRoomFog == hideRoomFog
                && _cachedRoofNoRoofMode == noRoofMode)
            {
                output.AddRange(_cachedRoofStructures);
                return;
            }

            _visitedRoofLots.Clear();
            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || z < 0 || x >= EClass._map.Size || z >= EClass._map.Size)
                    {
                        continue;
                    }

                    Lot lot = EClass._map.cells[x, z]?.room?.lot;
                    TryAddRoofStructureLot(lot, tileMap, origin, maxDistance, output, currentLot, showRoof, hideRoomFog, noRoofMode);
                }
            }

            _cachedRoofStructures.Clear();
            _cachedRoofStructures.AddRange(output);
            _cachedRoofMapId = mapId;
            _cachedRoofOriginX = centerX;
            _cachedRoofOriginZ = centerZ;
            _cachedRoofRadius = radius;
            _cachedRoofCurrentLotId = currentLotId;
            _cachedRoofShowRoof = showRoof;
            _cachedRoofHideRoomFog = hideRoomFog;
            _cachedRoofNoRoofMode = noRoofMode;
            _hasCachedRoofStructures = true;
        }

        private void InvalidateRoofPlaneCache()
        {
            _cachedRoofPlanes.Clear();
            _cachedRoofStructures.Clear();
            _cachedRoofMapId = 0;
            _cachedRoofOriginX = int.MinValue;
            _cachedRoofOriginZ = int.MinValue;
            _cachedRoofRadius = int.MinValue;
            _cachedRoofCurrentLotId = int.MinValue;
            _cachedRoofShowRoof = false;
            _cachedRoofHideRoomFog = false;
            _cachedRoofNoRoofMode = false;
            _hasCachedRoofPlanes = false;
            _hasCachedRoofStructures = false;
        }

        private void TryAddRoofLot(
            Lot lot,
            BaseTileMap tileMap,
            Vector2 origin,
            float maxDistance,
            List<FpsResolvedRoofPlane> output,
            Lot currentLot,
            bool showRoof,
            bool hideRoomFog,
            bool noRoofMode,
            bool addInteriorCeiling)
        {
            if (lot == null
                || !TryResolveRoofStyle(tileMap, lot, out RoofStyle roofStyle)
                || !TryBuildRoofLayout(lot, roofStyle, tileMap, out FpsRoofLayout layout))
            {
                return;
            }

            int lotKey = lot.id != 0 ? lot.id : lot.GetHashCode();
            bool isFirstVisit = _visitedRoofLots.Add(lotKey);
            if (!isFirstVisit && !addInteriorCeiling)
            {
                return;
            }

            bool nearPlayer = IsLotNearOrigin(lot, origin, maxDistance + 6f);
            if (isFirstVisit && nearPlayer)
            {
                bool suppressExteriorForCurrentLot = currentLot != null
                    && currentLot == lot
                    && (!showRoof || hideRoomFog);
                bool suppressExteriorForGlobalNoRoof = !showRoof && currentLot == null && noRoofMode;
                if (!suppressExteriorForCurrentLot && !suppressExteriorForGlobalNoRoof)
                {
                    AddExteriorRoofPlanes(layout, roofStyle, output);
                    LogRoofLot(layout, roofStyle, exteriorOnly: false);
                }
            }

            if (addInteriorCeiling)
            {
                AddInteriorCeilingPlane(layout, roofStyle, output);
            }
        }

        private void TryAddRoofStructureLot(
            Lot lot,
            BaseTileMap tileMap,
            Vector2 origin,
            float maxDistance,
            List<FpsResolvedRoofStructure> output,
            Lot currentLot,
            bool showRoof,
            bool hideRoomFog,
            bool noRoofMode)
        {
            if (lot == null
                || !TryResolveRoofStyle(tileMap, lot, out RoofStyle roofStyle)
                || !TryBuildRoofLayout(lot, roofStyle, tileMap, out FpsRoofLayout layout))
            {
                return;
            }

            int lotKey = lot.id != 0 ? lot.id : lot.GetHashCode();
            if (!_visitedRoofLots.Add(lotKey))
            {
                return;
            }

            if (!IsLotNearOrigin(lot, origin, maxDistance + 6f))
            {
                return;
            }

            bool suppressExteriorForCurrentLot = currentLot != null
                && currentLot == lot
                && (!showRoof || hideRoomFog);
            bool suppressExteriorForGlobalNoRoof = !showRoof && currentLot == null && noRoofMode;
            if (suppressExteriorForCurrentLot || suppressExteriorForGlobalNoRoof)
            {
                return;
            }

            if (!TryBuildRoofStructure(layout, roofStyle, out FpsResolvedRoofStructure structure))
            {
                return;
            }

            output.Add(structure);
        }

        private static bool TryResolveRoofStyle(BaseTileMap tileMap, Lot lot, out RoofStyle roofStyle)
        {
            roofStyle = null;
            if (tileMap?.roofStyles == null || lot == null || lot.idRoofStyle <= 0 || lot.idRoofStyle >= tileMap.roofStyles.Length)
            {
                return false;
            }

            roofStyle = tileMap.roofStyles[lot.idRoofStyle];
            return roofStyle != null && roofStyle.type != RoofStyle.Type.None;
        }

        private bool TryBuildRoofStructure(FpsRoofLayout layout, RoofStyle roofStyle, out FpsResolvedRoofStructure structure)
        {
            structure = default;
            FpsRoofStructureLayoutKind kind;
            switch (roofStyle.type)
            {
                case RoofStyle.Type.Flat:
                case RoofStyle.Type.FlatFloor:
                    kind = FpsRoofStructureLayoutKind.Flat;
                    break;
                case RoofStyle.Type.Default:
                case RoofStyle.Type.DefaultNoTop:
                    kind = FpsRoofStructureLayoutKind.Ridge;
                    break;
                default:
                    return false;
            }

            FpsRoofStructureLayoutInput layoutInput = new FpsRoofStructureLayoutInput(
                kind,
                layout.Reverse,
                layout.MinX,
                layout.MaxX,
                layout.MinZ,
                layout.MaxZ,
                layout.BaseY,
                kind == FpsRoofStructureLayoutKind.Flat ? layout.BaseY + layout.StepY * 0.42f : layout.TopY,
                layout.MinorStart,
                layout.FlatStart,
                layout.FlatEnd,
                layout.HasFlatBand);
            FpsRoofStructureSpec spec = FpsRoofStructureSpecFactory.Create(layoutInput, HybridRoofEaveDepth, HybridRoofThickness, HybridRoofRidgeCapWidth);
            FpsRoofStructurePlan plan = FpsRoofStructurePlanner.Build(spec);

            bool hasPrimarySource = TryResolveRoofPrimarySource(layout.Lot, layout.Reverse, layout.Light, out FpsResolvedRoofPrimarySource primarySource);
            bool hasFallbackSurface = TryResolveRoofTopSurface(layout.Lot, layout.Reverse, layout.Light, out FpsResolvedWallSurface fallbackSurface);
            if (!hasPrimarySource && !hasFallbackSurface)
            {
                return false;
            }

            structure = new FpsResolvedRoofStructure
            {
                Lot = layout.Lot,
                Plan = plan,
                HasPrimarySource = hasPrimarySource,
                PrimarySource = primarySource,
                HasFallbackSurface = hasFallbackSurface,
                FallbackSurface = fallbackSurface,
                Light = layout.Light,
                DiagnosticLabel = $"roof-structure:lot={layout.Lot?.id ?? 0}:type={roofStyle.type}"
            };
            return true;
        }

        private static bool IsLotNearOrigin(Lot lot, Vector2 origin, float maxDistance)
        {
            float closestX = Mathf.Clamp(origin.x, lot.x, lot.mx + 1f);
            float closestZ = Mathf.Clamp(origin.y, lot.z, lot.mz + 1f);
            float sqrDistance = (new Vector2(closestX, closestZ) - origin).sqrMagnitude;
            return sqrDistance <= maxDistance * maxDistance;
        }

        private void AddExteriorRoofPlanes(FpsRoofLayout layout, RoofStyle roofStyle, List<FpsResolvedRoofPlane> output)
        {
            switch (roofStyle.type)
            {
                case RoofStyle.Type.Flat:
                case RoofStyle.Type.FlatFloor:
                    AddFlatRoofPlanes(layout, output);
                    break;
                case RoofStyle.Type.Default:
                case RoofStyle.Type.DefaultNoTop:
                    AddRidgeRoofPlanes(layout, roofStyle, output);
                    break;
            }
        }

        private void AddFlatRoofPlanes(FpsRoofLayout layout, List<FpsResolvedRoofPlane> output)
        {
            float topY = layout.BaseY + layout.StepY * 0.42f;
            FpsResolvedWallSurface topSurface = default;
            bool hasTopSurface = TryResolveRoofTopSurface(layout.Lot, layout.Reverse, layout.Light, out topSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.Top,
                FpsRoofTextureProjectionKind.RawTile,
                FpsGpuRoofGeometryBuilder.BuildHorizontalQuad(layout.MinX, layout.MaxX, layout.MinZ, layout.MaxZ, topY + RoofHeightOffset),
                hasTopSurface,
                topSurface,
                FpsAtlasSampler.BlockFaceKind.Top,
                false,
                false,
                "roof-flat-top");

            FpsResolvedWallSurface westSurface = default;
            bool hasWestSurface = TryResolveRoofEdgeSurface(layout.Lot, 3, layout.Light, out westSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.Edge,
                FpsRoofTextureProjectionKind.RectSlice,
                FpsGpuRoofGeometryBuilder.BuildVerticalEdgeAtX(layout.MinX, layout.MinZ, layout.MaxZ, layout.BaseY, topY, false),
                hasWestSurface,
                westSurface,
                FpsAtlasSampler.BlockFaceKind.Left,
                false,
                false,
                "roof-flat-west");

            FpsResolvedWallSurface eastSurface = default;
            bool hasEastSurface = TryResolveRoofEdgeSurface(layout.Lot, 1, layout.Light, out eastSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.Edge,
                FpsRoofTextureProjectionKind.RectSlice,
                FpsGpuRoofGeometryBuilder.BuildVerticalEdgeAtX(layout.MaxX, layout.MinZ, layout.MaxZ, layout.BaseY, topY, true),
                hasEastSurface,
                eastSurface,
                FpsAtlasSampler.BlockFaceKind.Right,
                false,
                false,
                "roof-flat-east");

            FpsResolvedWallSurface northSurface = default;
            bool hasNorthSurface = TryResolveRoofEdgeSurface(layout.Lot, 0, layout.Light, out northSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.Edge,
                FpsRoofTextureProjectionKind.RectSlice,
                FpsGpuRoofGeometryBuilder.BuildVerticalEdgeAtZ(layout.MinZ, layout.MinX, layout.MaxX, layout.BaseY, topY, false),
                hasNorthSurface,
                northSurface,
                FpsAtlasSampler.BlockFaceKind.Left,
                false,
                false,
                "roof-flat-north");

            FpsResolvedWallSurface southSurface = default;
            bool hasSouthSurface = TryResolveRoofEdgeSurface(layout.Lot, 2, layout.Light, out southSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.Edge,
                FpsRoofTextureProjectionKind.RectSlice,
                FpsGpuRoofGeometryBuilder.BuildVerticalEdgeAtZ(layout.MaxZ, layout.MinX, layout.MaxX, layout.BaseY, topY, true),
                hasSouthSurface,
                southSurface,
                FpsAtlasSampler.BlockFaceKind.Right,
                false,
                false,
                "roof-flat-south");
        }

        private void AddRidgeRoofPlanes(FpsRoofLayout layout, RoofStyle roofStyle, List<FpsResolvedRoofPlane> output)
        {
            bool hasRidgeCap = roofStyle.type == RoofStyle.Type.Default && roofStyle.flatW > 0 && layout.HasFlatBand;
            float rawRidgeStart = layout.MinorStart + layout.FlatStart;
            float rawRidgeEnd = layout.MinorStart + layout.FlatEnd;
            float ridgeCenter = (rawRidgeStart + rawRidgeEnd) * 0.5f;
            float ridgeStart = hasRidgeCap ? rawRidgeStart : ridgeCenter;
            float ridgeEnd = hasRidgeCap ? rawRidgeEnd : ridgeCenter;

            int lowDir = layout.Reverse ? 3 : 0;
            int highDir = layout.Reverse ? 1 : 2;

            float topY = layout.TopY;

            if (roofStyle.type == RoofStyle.Type.DefaultNoTop && layout.RowCount <= 1)
            {
                FpsResolvedWallSurface fallbackTopSurface = default;
                bool hasFallbackTopSurface = TryResolveRoofTopSurface(layout.Lot, layout.Reverse, layout.Light, out fallbackTopSurface);
                AddRoofPrimaryOrFallbackPlane(
                    output,
                    layout,
                    FpsRoofPlaneKind.Top,
                    FpsRoofTextureProjectionKind.RawTile,
                    FpsGpuRoofGeometryBuilder.BuildHorizontalQuad(layout.MinX, layout.MaxX, layout.MinZ, layout.MaxZ, topY + RoofHeightOffset),
                    hasFallbackTopSurface,
                    fallbackTopSurface,
                    FpsAtlasSampler.BlockFaceKind.Top,
                    false,
                    false,
                    "roof-defaultnotop-top-fallback");

                AddRidgeRoofEndPlanes(layout, ridgeStart, ridgeEnd, topY, output);
                return;
            }

            if (roofStyle.type == RoofStyle.Type.DefaultNoTop)
            {
                float leftEnd = ridgeCenter;
                float rightStart = ridgeCenter;

                FpsResolvedWallSurface leftSlopeSurface = default;
                bool hasLeftSlopeSurface = TryResolveRoofSlopeSurface(layout.Lot, lowDir, layout.Light, out leftSlopeSurface);
                if (leftEnd > layout.MinorStart + 0.01f
                    && hasLeftSlopeSurface)
                {
                    AddRoofPrimaryOrFallbackPlane(
                        output,
                        layout,
                        FpsRoofPlaneKind.SlopeLeft,
                        FpsRoofTextureProjectionKind.RectSlice,
                        BuildRoofSlopeFace(layout, layout.MinorStart, leftEnd, layout.BaseY, topY),
                        hasLeftSlopeSurface,
                        leftSlopeSurface,
                        lowDir == 1 || lowDir == 2 ? FpsAtlasSampler.BlockFaceKind.Right : FpsAtlasSampler.BlockFaceKind.Left,
                        false,
                        false,
                        "roof-defaultnotop-slope-a");
                }

                FpsResolvedWallSurface rightSlopeSurface = default;
                bool hasRightSlopeSurface = TryResolveRoofSlopeSurface(layout.Lot, highDir, layout.Light, out rightSlopeSurface);
                if (layout.MinorEnd > rightStart + 0.01f
                    && hasRightSlopeSurface)
                {
                    AddRoofPrimaryOrFallbackPlane(
                        output,
                        layout,
                        FpsRoofPlaneKind.SlopeRight,
                        FpsRoofTextureProjectionKind.RectSlice,
                        BuildRoofSlopeFace(layout, rightStart, layout.MinorEnd, topY, layout.BaseY),
                        hasRightSlopeSurface,
                        rightSlopeSurface,
                        highDir == 1 || highDir == 2 ? FpsAtlasSampler.BlockFaceKind.Right : FpsAtlasSampler.BlockFaceKind.Left,
                        false,
                        false,
                        "roof-defaultnotop-slope-b");
                }

                AddRidgeRoofEndPlanes(layout, ridgeStart, ridgeEnd, topY, output);
                return;
            }

            FpsResolvedWallSurface risingSlopeSurface = default;
            bool hasRisingSlopeSurface = TryResolveRoofSlopeSurface(layout.Lot, lowDir, layout.Light, out risingSlopeSurface);
            if (ridgeStart > layout.MinorStart + 0.01f
                && hasRisingSlopeSurface)
            {
                AddRoofPrimaryOrFallbackPlane(
                    output,
                    layout,
                    FpsRoofPlaneKind.SlopeLeft,
                    FpsRoofTextureProjectionKind.RectSlice,
                    BuildRoofSlopeFace(layout, layout.MinorStart, ridgeStart, layout.BaseY, topY),
                    hasRisingSlopeSurface,
                    risingSlopeSurface,
                    lowDir == 1 || lowDir == 2 ? FpsAtlasSampler.BlockFaceKind.Right : FpsAtlasSampler.BlockFaceKind.Left,
                    false,
                    false,
                    "roof-default-slope-a");
            }

            FpsResolvedWallSurface ridgeSurface = default;
            bool hasRidgeSurface = TryResolveRoofTopSurface(layout.Lot, layout.Reverse, layout.Light, out ridgeSurface);
            if (hasRidgeCap
                && hasRidgeSurface)
            {
                AddRoofPrimaryOrFallbackPlane(
                    output,
                    layout,
                    FpsRoofPlaneKind.Top,
                    FpsRoofTextureProjectionKind.RawTile,
                    BuildRoofTopFace(layout, ridgeStart, ridgeEnd, topY + RoofHeightOffset),
                    hasRidgeSurface,
                    ridgeSurface,
                    FpsAtlasSampler.BlockFaceKind.Top,
                    false,
                    false,
                    "roof-default-ridge");
            }

            FpsResolvedWallSurface fallingSlopeSurface = default;
            bool hasFallingSlopeSurface = TryResolveRoofSlopeSurface(layout.Lot, highDir, layout.Light, out fallingSlopeSurface);
            if (layout.MinorEnd > ridgeEnd + 0.01f
                && hasFallingSlopeSurface)
            {
                AddRoofPrimaryOrFallbackPlane(
                    output,
                    layout,
                    FpsRoofPlaneKind.SlopeRight,
                    FpsRoofTextureProjectionKind.RectSlice,
                    BuildRoofSlopeFace(layout, ridgeEnd, layout.MinorEnd, topY, layout.BaseY),
                    hasFallingSlopeSurface,
                    fallingSlopeSurface,
                    highDir == 1 || highDir == 2 ? FpsAtlasSampler.BlockFaceKind.Right : FpsAtlasSampler.BlockFaceKind.Left,
                    false,
                    false,
                    "roof-default-slope-b");
            }

            AddRidgeRoofEndPlanes(layout, ridgeStart, ridgeEnd, topY, output);
        }

        private void AddInteriorCeilingPlane(FpsRoofLayout layout, RoofStyle roofStyle, List<FpsResolvedRoofPlane> output)
        {
            float inset = 0.05f;
            float minX = layout.Lot.x + inset;
            float maxX = layout.Lot.mx + 1f - inset;
            float minZ = layout.Lot.z + inset;
            float maxZ = layout.Lot.mz + 1f - inset;
            if (maxX <= minX || maxZ <= minZ)
            {
                return;
            }

            float ceilingY = layout.BaseY - 0.04f;
            FpsResolvedWallSurface ceilingSurface = default;
            bool hasCeilingSurface = TryResolveRoofTopSurface(layout.Lot, layout.Reverse, layout.Light, out ceilingSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.InteriorCeiling,
                FpsRoofTextureProjectionKind.RectSlice,
                FpsGpuRoofGeometryBuilder.BuildHorizontalQuad(minX, maxX, minZ, maxZ, ceilingY),
                hasCeilingSurface,
                ceilingSurface,
                FpsAtlasSampler.BlockFaceKind.Top,
                true,
                true,
                "roof-interior-ceiling");
        }

        private void AddRidgeRoofEndPlanes(FpsRoofLayout layout, float ridgeStart, float ridgeEnd, float topY, List<FpsResolvedRoofPlane> output)
        {
            if (layout.Reverse)
            {
                FpsResolvedWallSurface northSurface = default;
                bool hasNorthSurface = TryResolveRoofEdgeSurface(layout.Lot, 0, layout.Light, out northSurface);
                AddRoofPrimaryOrFallbackPlane(
                    output,
                    layout,
                    FpsRoofPlaneKind.Edge,
                    FpsRoofTextureProjectionKind.TriSlice,
                    FpsGpuRoofGeometryBuilder.BuildGableFaceAtZ(layout.MinZ, layout.MinX, layout.MaxX, ridgeStart, ridgeEnd, layout.BaseY, topY, false),
                    hasNorthSurface,
                    northSurface,
                    FpsAtlasSampler.BlockFaceKind.Left,
                    false,
                    false,
                    "roof-ridge-gable-north");

                FpsResolvedWallSurface southSurface = default;
                bool hasSouthSurface = TryResolveRoofEdgeSurface(layout.Lot, 2, layout.Light, out southSurface);
                AddRoofPrimaryOrFallbackPlane(
                    output,
                    layout,
                    FpsRoofPlaneKind.Edge,
                    FpsRoofTextureProjectionKind.TriSlice,
                    FpsGpuRoofGeometryBuilder.BuildGableFaceAtZ(layout.MaxZ, layout.MinX, layout.MaxX, ridgeStart, ridgeEnd, layout.BaseY, topY, true),
                    hasSouthSurface,
                    southSurface,
                    FpsAtlasSampler.BlockFaceKind.Right,
                    false,
                    false,
                    "roof-ridge-gable-south");

                return;
            }

            FpsResolvedWallSurface westSurface = default;
            bool hasWestSurface = TryResolveRoofEdgeSurface(layout.Lot, 3, layout.Light, out westSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.Edge,
                FpsRoofTextureProjectionKind.TriSlice,
                FpsGpuRoofGeometryBuilder.BuildGableFaceAtX(layout.MinX, layout.MinZ, layout.MaxZ, ridgeStart, ridgeEnd, layout.BaseY, topY, false),
                hasWestSurface,
                westSurface,
                FpsAtlasSampler.BlockFaceKind.Left,
                false,
                false,
                "roof-ridge-gable-west");

            FpsResolvedWallSurface eastSurface = default;
            bool hasEastSurface = TryResolveRoofEdgeSurface(layout.Lot, 1, layout.Light, out eastSurface);
            AddRoofPrimaryOrFallbackPlane(
                output,
                layout,
                FpsRoofPlaneKind.Edge,
                FpsRoofTextureProjectionKind.TriSlice,
                FpsGpuRoofGeometryBuilder.BuildGableFaceAtX(layout.MaxX, layout.MinZ, layout.MaxZ, ridgeStart, ridgeEnd, layout.BaseY, topY, true),
                hasEastSurface,
                eastSurface,
                FpsAtlasSampler.BlockFaceKind.Right,
                false,
                false,
                "roof-ridge-gable-east");
        }

        private static FpsGpuFaceQuad BuildRoofTopFace(FpsRoofLayout layout, float minorStart, float minorEnd, float y)
        {
            if (layout.Reverse)
            {
                return FpsGpuRoofGeometryBuilder.BuildHorizontalQuad(minorStart, minorEnd, layout.MinZ, layout.MaxZ, y);
            }

            return FpsGpuRoofGeometryBuilder.BuildHorizontalQuad(layout.MinX, layout.MaxX, minorStart, minorEnd, y);
        }

        private static FpsGpuFaceQuad BuildRoofSlopeFace(FpsRoofLayout layout, float minorStart, float minorEnd, float lowY, float highY)
        {
            if (layout.Reverse)
            {
                return FpsGpuRoofGeometryBuilder.BuildSlopeAlongX(minorStart, minorEnd, layout.MinZ, layout.MaxZ, lowY, highY);
            }

            return FpsGpuRoofGeometryBuilder.BuildSlopeAlongZ(layout.MinX, layout.MaxX, minorStart, minorEnd, lowY, highY);
        }

        private bool TryBuildRoofLayout(Lot lot, RoofStyle roofStyle, BaseTileMap tileMap, out FpsRoofLayout layout)
        {
            layout = default;
            if (lot == null || roofStyle == null || tileMap == null)
            {
                return false;
            }

            ResolveRoofBounds(lot, roofStyle, out int minX, out int minZ, out int maxX, out int maxZ);
            int rowCount = lot.reverse ? maxX - minX : maxZ - minZ;
            if (rowCount <= 0)
            {
                return false;
            }

            int flatStart;
            int flatEnd;
            switch (roofStyle.type)
            {
                case RoofStyle.Type.Flat:
                case RoofStyle.Type.FlatFloor:
                    flatStart = Mathf.Clamp(roofStyle.flatW, 0, rowCount);
                    flatEnd = Mathf.Clamp(rowCount - roofStyle.flatW, flatStart, rowCount);
                    break;
                case RoofStyle.Type.Default:
                case RoofStyle.Type.DefaultNoTop:
                    flatStart = Mathf.Clamp(rowCount / 2 - roofStyle.flatW, 0, rowCount);
                    flatEnd = Mathf.Clamp(rowCount / 2 + roofStyle.flatW + ((rowCount % 2 != 0) ? 1 : 0), flatStart, rowCount);
                    break;
                default:
                    return false;
            }

            int ridgeSteps = Mathf.Max(1, Mathf.Max(flatStart, rowCount - flatEnd));
            float baseY = ResolveRoofBaseHeight(tileMap, lot, roofStyle);
            float stepY = ResolveRoofStep(tileMap);
            FpsResolvedLightSample light = _lightingResolver.ResolveRoofLight(lot);
            bool hasPrimarySource = TryResolveRoofPrimarySource(lot, lot.reverse, light, out FpsResolvedRoofPrimarySource primarySource);

            layout = new FpsRoofLayout
            {
                Lot = lot,
                Type = roofStyle.type,
                Reverse = lot.reverse,
                MinX = minX,
                MinZ = minZ,
                MaxX = maxX,
                MaxZ = maxZ,
                RowCount = rowCount,
                FlatStart = flatStart,
                FlatEnd = flatEnd,
                BaseY = baseY,
                StepY = stepY,
                TopY = baseY + ridgeSteps * stepY,
                Light = light,
                HasPrimarySource = hasPrimarySource,
                PrimarySource = primarySource
            };
            return true;
        }

        private static void ResolveRoofBounds(Lot lot, RoofStyle roofStyle, out int minX, out int minZ, out int maxX, out int maxZ)
        {
            if (lot.reverse)
            {
                minZ = lot.z - roofStyle.h;
                minX = lot.x - roofStyle.w;
                maxZ = lot.mz + 1 + roofStyle.h;
                maxX = lot.mx + 1 + roofStyle.w;
                if (minX > 1 && minZ > 0 && EClass._map.cells[minX - 1, minZ].HasFullBlock)
                {
                    minX--;
                }

                if (maxZ < EClass._map.Size && maxX < EClass._map.Size && EClass._map.cells[maxX - 1, maxZ].HasFullBlock)
                {
                    maxZ++;
                }
            }
            else
            {
                minX = lot.x - roofStyle.w;
                minZ = lot.z - roofStyle.h;
                maxX = lot.mx + 1 + roofStyle.w;
                maxZ = lot.mz + 1 + roofStyle.h;
                if (minZ > 0 && minX > 1 && EClass._map.cells[minX - 1, minZ].HasFullBlock)
                {
                    minX--;
                }

                if (maxX < EClass._map.Size && maxZ < EClass._map.Size && EClass._map.cells[maxX - 1, maxZ].HasFullBlock)
                {
                    maxZ++;
                }
            }

            minX = Mathf.Clamp(minX, 0, EClass._map.Size);
            minZ = Mathf.Clamp(minZ, 0, EClass._map.Size);
            maxX = Mathf.Clamp(maxX, 0, EClass._map.Size);
            maxZ = Mathf.Clamp(maxZ, 0, EClass._map.Size);
        }

        private static float ResolveRoofStep(BaseTileMap tileMap)
        {
            float baseStep = Mathf.Abs(tileMap.roofFix2.y) * GetTerrainHeightScale();
            return Mathf.Max(0.6f, baseStep * 3.5f);
        }

        private static float ResolveRoofBaseYOffset(Lot lot, RoofStyle roofStyle)
        {
            float scale = GetTerrainHeightScale();
            Vector3 offset = lot.fullblock ? roofStyle.posFixBlock : roofStyle.posFix;
            float y = offset.y * scale;
            if (lot.height == 1 && lot.heightFix < 20)
            {
                y += roofStyle.lowRoofFix.y * scale;
            }

            return y;
        }

        private static float ResolveRoofBaseHeight(BaseTileMap tileMap, Lot lot, RoofStyle roofStyle)
        {
            if (tileMap == null || lot == null)
            {
                return 0f;
            }

            float scale = GetTerrainHeightScale();
            float roofFixY = tileMap.roofFix.y * scale;
            float lotHeight = lot.mh * scale + Mathf.Max(0.5f, lot.realHeight);
            return lotHeight + roofFixY + ResolveRoofBaseYOffset(lot, roofStyle);
        }

        private bool TryResolveRoofPrimarySource(Lot lot, bool reverse, FpsResolvedLightSample light, out FpsResolvedRoofPrimarySource source)
        {
            source = default;
            if (lot == null)
            {
                return false;
            }

            if (TryResolveRoofTopPrimarySourceSurface(lot, reverse, light, out FpsResolvedWallSurface topSurface, out int tileDirection))
            {
                source = new FpsResolvedRoofPrimarySource
                {
                    RoofTileId = lot.idRoofTile,
                    TileDirection = tileDirection,
                    RenderData = topSurface.RenderData,
                    Tile = topSurface.Tile,
                    MaterialColor = topSurface.MaterialColor == 0 ? 104025 : topSurface.MaterialColor,
                    Light = light,
                    Origin = FpsRoofSourceOrigin.RoofTopPrimary,
                    DiagnosticLabel = $"roof-top-primary:roofTile={lot.idRoofTile}:dir={tileDirection}:tile={topSurface.Tile}:source=block-top"
                };
                return true;
            }

            return false;
        }

        private bool TryResolveRoofTopPrimarySourceSurface(Lot lot, bool reverse, FpsResolvedLightSample light, out FpsResolvedWallSurface surface, out int tileDirection)
        {
            surface = default;
            tileDirection = 0;
            if (lot == null || lot.idRoofTile <= 0 || EMono.sources?.objs?.rows == null || lot.idRoofTile >= EMono.sources.objs.rows.Count)
            {
                return false;
            }

            SourceObj.Row roofRow = EMono.sources.objs.rows[lot.idRoofTile];
            if (roofRow?.renderData == null || roofRow.idRoof <= 0 || EMono.sources.blocks?.rows == null || roofRow.idRoof >= EMono.sources.blocks.rows.Count)
            {
                return false;
            }

            tileDirection = ResolveRoofTopPrimaryTileDirection(reverse);
            SourceBlock.Row blockRow = EMono.sources.blocks.rows[roofRow.idRoof];
            return TryCreateRoofBlockSurface(blockRow, tileDirection, lot.colRoof, light, out surface);
        }

        private static int ResolveRoofTopPrimaryTileDirection(bool reverse)
        {
            return 0;
        }

        private bool TryResolveRoofSlopeSurface(Lot lot, int dir, FpsResolvedLightSample light, out FpsResolvedWallSurface surface)
        {
            surface = default;
            if (lot == null || EMono.sources?.blocks?.rows == null)
            {
                return false;
            }

            SourceBlock.Row blockRow = null;
            if (lot.idRamp > 0 && lot.idRamp < EMono.sources.blocks.rows.Count)
            {
                blockRow = EMono.sources.blocks.rows[lot.idRamp];
            }

            if (blockRow == null && lot.idBlock > 0 && lot.idBlock < EMono.sources.blocks.rows.Count)
            {
                blockRow = EMono.sources.blocks.rows[lot.idBlock];
            }

            return TryCreateRoofBlockSurface(blockRow, dir, lot.colBlock, light, out surface);
        }

        private bool TryResolveRoofTopSurface(Lot lot, bool reverse, FpsResolvedLightSample light, out FpsResolvedWallSurface surface)
        {
            surface = default;
            SourceBlock.Row blockRow = null;
            if (lot != null && lot.idRoofTile > 0 && EMono.sources?.objs?.rows != null && lot.idRoofTile < EMono.sources.objs.rows.Count)
            {
                SourceObj.Row roofRow = EMono.sources.objs.rows[lot.idRoofTile];
                if (roofRow != null && roofRow.idRoof > 0 && EMono.sources.blocks?.rows != null && roofRow.idRoof < EMono.sources.blocks.rows.Count)
                {
                    blockRow = EMono.sources.blocks.rows[roofRow.idRoof];
                }
            }

            if (blockRow == null && lot != null && EMono.sources?.blocks?.rows != null && lot.idBlock > 0 && lot.idBlock < EMono.sources.blocks.rows.Count)
            {
                blockRow = EMono.sources.blocks.rows[lot.idBlock];
            }

            return TryCreateRoofBlockSurface(blockRow, reverse ? 1 : 0, lot?.colRoof ?? 104025, light, out surface);
        }

        private bool TryResolveRoofEdgeSurface(Lot lot, int dir, FpsResolvedLightSample light, out FpsResolvedWallSurface surface)
        {
            surface = default;
            if (lot == null || EMono.sources?.blocks?.rows == null)
            {
                return false;
            }

            SourceBlock.Row blockRow = null;
            if (lot.idRamp > 0 && lot.idRamp < EMono.sources.blocks.rows.Count)
            {
                blockRow = EMono.sources.blocks.rows[lot.idRamp];
            }

            if (blockRow == null && lot.idBlock > 0 && lot.idBlock < EMono.sources.blocks.rows.Count)
            {
                blockRow = EMono.sources.blocks.rows[lot.idBlock];
            }

            return TryCreateRoofBlockSurface(blockRow, dir, lot.colBlock, light, out surface);
        }

        private static bool TryCreateRoofBlockSurface(SourceBlock.Row blockRow, int dir, int materialColor, FpsResolvedLightSample light, out FpsResolvedWallSurface surface)
        {
            surface = default;
            if (blockRow?.renderData == null)
            {
                return false;
            }

            surface = new FpsResolvedWallSurface
            {
                Cell = null,
                Tile = blockRow.GetTile(MATERIAL.sourceGold, dir),
                RenderData = blockRow.renderData,
                MaterialColor = materialColor == 0 ? 104025 : materialColor,
                UseSnowAtlas = false,
                Light = light
            };
            return true;
        }

        private void AddRoofPrimaryOrFallbackPlane(
            List<FpsResolvedRoofPlane> output,
            FpsRoofLayout layout,
            FpsRoofPlaneKind kind,
            FpsRoofTextureProjectionKind projectionKind,
            FpsGpuFaceQuad face,
            bool hasFallbackSurface,
            FpsResolvedWallSurface fallbackSurface,
            FpsAtlasSampler.BlockFaceKind fallbackFaceKind,
            bool doubleSided,
            bool usePanelPlacement,
            string label)
        {
            if (layout.HasPrimarySource)
            {
                AddRoofPlane(
                    output,
                    CreateRoofPrimaryPlane(
                        layout.Lot,
                        kind,
                        face,
                        layout.PrimarySource,
                        projectionKind,
                        doubleSided,
                        usePanelPlacement,
                        label));
                return;
            }

            if (!hasFallbackSurface)
            {
                return;
            }

            FpsResolvedRoofPlane plane = CreateRoofBlockPlane(
                layout.Lot,
                kind,
                face,
                projectionKind,
                fallbackSurface,
                fallbackFaceKind,
                doubleSided,
                usePanelPlacement,
                label);
            AddRoofPlane(output, plane);
        }

        private static FpsResolvedRoofPlane CreateRoofPrimaryPlane(
            Lot lot,
            FpsRoofPlaneKind kind,
            FpsGpuFaceQuad face,
            FpsResolvedRoofPrimarySource source,
            FpsRoofTextureProjectionKind projectionKind,
            bool doubleSided,
            bool usePanelPlacement,
            string label)
        {
            return new FpsResolvedRoofPlane
            {
                Lot = lot,
                Kind = kind,
                Face = face,
                TextureKind = FpsRoofTextureKind.PrimarySource,
                ProjectionKind = projectionKind,
                SourceOrigin = source.Origin,
                RenderData = source.RenderData,
                Tile = source.Tile,
                MaterialColor = source.MaterialColor == 0 ? 104025 : source.MaterialColor,
                HasMaterialTint = true,
                UseSelectiveMaterialTint = false,
                FlipX = false,
                TrimTransparent = false,
                BlockSurface = new FpsResolvedWallSurface
                {
                    Cell = null,
                    Tile = 0,
                    RenderData = null,
                    MaterialColor = source.MaterialColor == 0 ? 104025 : source.MaterialColor,
                    UseSnowAtlas = false,
                    Light = source.Light
                },
                BlockFaceKind = FpsAtlasSampler.BlockFaceKind.Top,
                DoubleSided = doubleSided,
                UsePanelPlacement = usePanelPlacement,
                HasLotPrimarySource = true,
                LotPrimarySource = source,
                DiagnosticLabel = $"{label}:{source.DiagnosticLabel}"
            };
        }

        private static FpsResolvedRoofPlane CreateRoofBlockPlane(
            Lot lot,
            FpsRoofPlaneKind kind,
            FpsGpuFaceQuad face,
            FpsRoofTextureProjectionKind projectionKind,
            FpsResolvedWallSurface blockSurface,
            FpsAtlasSampler.BlockFaceKind blockFaceKind,
            bool doubleSided,
            bool usePanelPlacement,
            string label)
        {
            return new FpsResolvedRoofPlane
            {
                Lot = lot,
                Kind = kind,
                Face = face,
                TextureKind = FpsRoofTextureKind.BlockFace,
                ProjectionKind = projectionKind,
                SourceOrigin = FpsRoofSourceOrigin.None,
                RenderData = null,
                Tile = 0,
                MaterialColor = 104025,
                HasMaterialTint = false,
                UseSelectiveMaterialTint = false,
                FlipX = false,
                TrimTransparent = false,
                BlockSurface = blockSurface,
                BlockFaceKind = blockFaceKind,
                DoubleSided = doubleSided,
                UsePanelPlacement = usePanelPlacement,
                HasLotPrimarySource = false,
                LotPrimarySource = default,
                DiagnosticLabel = label
            };
        }

        private void AddRoofPlane(List<FpsResolvedRoofPlane> output, FpsResolvedRoofPlane plane)
        {
            output.Add(plane);
            if (_loggedRoofPlanes >= 24)
            {
                return;
            }

            ModLog.Developer(
                "RoofPlane",
                $"GPU roof plane[{_loggedRoofPlanes}]: lot={plane.Lot?.id ?? 0} kind={plane.Kind} tex={plane.TextureKind} label={plane.DiagnosticLabel} " +
                $"bl={plane.Face.BottomLeft:F3} br={plane.Face.BottomRight:F3} tl={plane.Face.TopLeft:F3} tr={plane.Face.TopRight:F3}");
            _loggedRoofPlanes++;
        }

        private void LogRoofLot(FpsRoofLayout layout, RoofStyle roofStyle, bool exteriorOnly)
        {
            Lot lot = layout.Lot;
            if (_loggedRoofLots >= 12 || lot == null || roofStyle == null)
            {
                return;
            }

            string primary = layout.HasPrimarySource
                ? $"primary=roofTile:{layout.PrimarySource.RoofTileId} dir:{layout.PrimarySource.TileDirection} tile:{layout.PrimarySource.Tile}"
                : "primary=none";
            ModLog.Developer(
                "RoofLot",
                $"GPU roof lot[{_loggedRoofLots}]: id={lot.id} bounds=({lot.x},{lot.z})-({lot.mx},{lot.mz}) style={roofStyle.type} reverse={lot.reverse} " +
                $"flatW={roofStyle.flatW} wing={roofStyle.wing} fullblock={lot.fullblock} height={lot.height} roofTile={lot.idRoofTile} block={lot.idBlock} ramp={lot.idRamp} {primary} exteriorOnly={exteriorOnly}");
            _loggedRoofLots++;
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
            List<FpsResolvedWallMountedSprite> wallMountedOutput,
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

            if (card != EClass.pc && card.pos?.cell != null && !card.pos.cell.isSeen)
            {
                return;
            }

            bool wallMountedInstalledCard = IsWallMountedInstalledCard(card);
            LogWallMountedCandidate(
                "card",
                card?.id.ToString(),
                card?.pos?.cell,
                card?.TileType,
                card?.trait?.IsDoor == true || card?.TileType?.IsDoor == true,
                wallMountedInstalledCard,
                wallMountedInstalledCard);

            if (wallMountedInstalledCard)
            {
                TryAddWallMountedInstalledCard(card, origin, maxDistance, lighting, wallMountedOutput);
                return;
            }

            BillboardKind kind = ResolveBillboardKind(card);
            if (card.trait?.IsGround == true)
            {
                TryAddGroundSprite(card, origin, ResolveCardVisibilityDistance(kind, maxDistance), lighting, groundOutput);
                return;
            }

            TryAddUprightSprite(card, origin, ResolveCardVisibilityDistance(kind, maxDistance), kind, lighting, uprightOutput);
        }

        private void TryAddWallMountedInstalledCard(
            Card card,
            Vector2 origin,
            float maxDistance,
            FpsResolvedCellLighting lighting,
            List<FpsResolvedWallMountedSprite> output)
        {
            Vector2 position = GetCardTileCenter(card);
            float distance = Vector2.Distance(position, origin);
            if (distance <= 0.1f || distance > maxDistance + 1f)
            {
                return;
            }

            RenderData renderData = card.renderer?.data ?? card.sourceRenderCard?.renderData;
            if (renderData == null || card.Tiles == null || card.Tiles.Length == 0)
            {
                return;
            }

            TileType tileType = card.TileType;
            bool isDoor = card.trait?.IsDoor == true || tileType?.IsDoor == true;
            bool flipX = card.dir % 2 != 0;
            int tile = Mathf.Abs(card.Tiles[card.refVal % card.Tiles.Length]);
            int materialColor = ResolveCardMaterialColor(card);

            output.Add(new FpsResolvedWallMountedSprite
            {
                Cell = card.pos?.cell,
                CellCenter = new Vector3(position.x, GetCellSurfaceHeight(card.pos?.cell), position.y),
                MountWorld = isDoor ? Vector3.zero : ResolveWallMountedCardAnchor(card, ResolveWallMountedCardFaceDir(card)),
                HasCustomAnchor = !isDoor,
                Sprite = null,
                RenderData = renderData,
                Tile = tile,
                FlipX = flipX,
                Distance = distance,
                WallDir = card.pos?.cell?.blockDir ?? 0,
                FaceDir = isDoor
                    ? FpsDoorFacingResolver.ResolveFaceDir(card.pos?.cell?.blockDir ?? 0, card.trait is TraitDoor traitDoor && traitDoor.IsOpen())
                    : ResolveWallMountedCardFaceDir(card),
                IsDoor = isDoor,
                UseWallPanelPlacement = true,
                TrimTransparent = isDoor,
                RenderPriority = isDoor ? 0 : 1,
                MaterialColor = materialColor,
                HasMaterialTint = true,
                UseSelectiveMaterialTint = false,
                Light = lighting.BlockLight,
                DiagnosticLabel = card.id.ToString()
            });
        }

        private static int ResolveWallMountedCardFaceDir(Card card)
        {
            if (card == null)
            {
                return 0;
            }

            TileType tileType = card.TileType;
            int desiredDir = tileType?.GetDesiredDir(card.pos, card.dir) ?? card.dir;
            if (desiredDir < 0)
            {
                desiredDir = card.dir;
            }

            return ((desiredDir % 4) + 4) % 4;
        }

        private static Vector3 ResolveWallMountedCardAnchor(Card card, int desiredDir)
        {
            if (card?.pos == null || card.TileType == null)
            {
                return Vector3.zero;
            }

            Vector3 mount = card.pos.Position();
            card.TileType.GetMountHeight(ref mount, card.pos, desiredDir, card);
            return mount;
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
            bool isPlayerSelf = card == EClass.pc;
            if ((!isPlayerSelf && distance <= 0.1f) || distance > maxDistance + 1f)
            {
                return;
            }

            Sprite sprite = ResolveCardSprite(card);
            if (sprite == null)
            {
                return;
            }

            RenderData renderData = card.renderer?.data ?? card.sourceRenderCard?.renderData;
            Vector2 spriteSize = ResolveCardSpriteWorldSize(card, sprite, renderData, kind);
            float spriteWidth = spriteSize.x;
            float spriteHeight = spriteSize.y;
            float pivotX = 0.5f;
            float pivotY = ResolvePivotY(kind);
            int materialColor = ResolveCardMaterialColor(card);
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
                RenderPriority = ResolveCardRenderPriority(kind),
                MaterialColor = materialColor,
                HasMaterialTint = true,
                UseSelectiveMaterialTint = false,
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
            int materialColor = ResolveCardMaterialColor(card);

            output.Add(new FpsResolvedGroundSprite
            {
                CenterWorld = new Vector3(position.x, groundHeightWorld + 0.01f, position.y),
                Sprite = sprite,
                RenderData = null,
                Tile = 0,
                Distance = distance,
                SizeWorld = new Vector2(width, depth),
                VisibilityDistance = maxDistance,
                VisibilityConeDot = 0.17364818f,
                RenderPriority = 3,
                MaterialColor = materialColor,
                HasMaterialTint = true,
                UseSelectiveMaterialTint = false,
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
            List<FpsResolvedWallMountedSprite> wallMountedOutput,
            List<FpsResolvedUprightSprite> uprightOutput,
            List<FpsResolvedGroundSprite> groundOutput)
        {
            void LogDoorRejection(SourceObj.Row row, RenderData data, string reason)
            {
                if (_loggedDoorRejections >= 12 || row?.tileType?.IsDoor != true)
                {
                    return;
                }

                ModLog.Developer(
                    "DoorReject",
                    $"GPU door reject[{_loggedDoorRejections}]: label={row.alias ?? row.id.ToString()} cell=({cellX},{cellZ}) " +
                    $"reason={reason} seen={cell?.isSeen} hasBlock={cell?.HasBlock} wallOrFence={cell?.HasWallOrFence} fullBlock={cell?.HasFullBlock} " +
                    $"blockDir={cell?.blockDir} renderData={(data != null ? data.GetType().Name : "null")}");
                _loggedDoorRejections++;
            }

            if (cell == null || cell.obj == 0)
            {
                return;
            }

            LogCellObjectSurvey(cell, cellX, cellZ);

            if (!cell.isSeen)
            {
                return;
            }

            SourceObj.Row sourceObj = cell.sourceObj;
            RenderData renderData = ResolveCellObjectRenderData(cell);
            if (sourceObj == null || renderData == null || renderData.SkipOnMap)
            {
                LogDoorRejection(sourceObj, renderData, sourceObj == null ? "no-sourceObj" : renderData == null ? "no-renderData" : "skip-on-map");
                return;
            }

            Vector2 position = new Vector2(cellX + 0.5f, cellZ + 0.5f);
            float distance = Vector2.Distance(position, origin);
            bool uprightObject = IsUprightCellObject(cell, sourceObj, renderData);
            float visibilityDistance = ResolveCellObjectVisibilityDistance(sourceObj, renderData, uprightObject, maxDistance);
            if (distance <= 0.1f || distance > visibilityDistance + 1f)
            {
                LogDoorRejection(sourceObj, renderData, distance <= 0.1f ? "too-close" : $"distance>{visibilityDistance + 1f:0.00}");
                return;
            }

            if (!TryResolveCellObjectTile(cell, out int tile))
            {
                LogDoorRejection(sourceObj, renderData, "no-tile");
                return;
            }

            int materialColor = ResolveCellObjectMaterialColor(cell, sourceObj);
            bool hasMaterialTint = sourceObj.colorMod != 0 || sourceObj.useAltColor;
            bool useSelectiveMaterialTint = sourceObj.HasGrowth && hasMaterialTint;
            bool wallMountedCellObject = IsWallMountedCellObject(cell, sourceObj, renderData);
            LogWallMountedCandidate(
                "cell",
                sourceObj.alias ?? sourceObj.id.ToString(),
                cell,
                sourceObj.tileType,
                sourceObj.tileType?.IsDoor == true,
                sourceObj.tileType?.IsDoor == true || sourceObj.tileType?.IsBlockMount == true,
                wallMountedCellObject);

            if (wallMountedCellObject)
            {
                bool isDoor = sourceObj.tileType?.IsDoor == true;
                wallMountedOutput.Add(new FpsResolvedWallMountedSprite
                {
                    Cell = cell,
                    CellCenter = new Vector3(position.x, GetCellSurfaceHeight(cell), position.y),
                    Sprite = null,
                    RenderData = renderData,
                    Tile = tile,
                    FlipX = false,
                    Distance = distance,
                    WallDir = cell.blockDir,
                    FaceDir = isDoor
                        ? FpsDoorFacingResolver.ResolveFaceDir(cell.blockDir, (cell.objDir & 1) != 0)
                        : ((cell.objDir % 4) + 4) % 4,
                    IsDoor = isDoor,
                    UseWallPanelPlacement = true,
                    TrimTransparent = isDoor || ShouldTrimWallMountedTransparent(cell, sourceObj, renderData),
                    RenderPriority = isDoor ? 0 : 1,
                    MaterialColor = hasMaterialTint ? materialColor : 0,
                    HasMaterialTint = hasMaterialTint,
                    UseSelectiveMaterialTint = useSelectiveMaterialTint,
                    Light = lighting.BlockLight,
                    DiagnosticLabel = sourceObj.alias ?? sourceObj.id.ToString()
                });
                if (_loggedWallFaceObjects < 8)
                {
                    ModLog.Developer(
                        "WallFaceObject",
                        $"GPU wall-face object[{_loggedWallFaceObjects}]: label={sourceObj.alias ?? sourceObj.id.ToString()} " +
                        $"cell=({cellX},{cellZ}) tileType={sourceObj.tileType?.GetType().Name} blockMount={sourceObj.tileType?.IsBlockMount} door={sourceObj.tileType?.IsDoor} " +
                        $"wallOrFence={cell.HasWallOrFence} fullBlock={cell.HasFullBlock} blockDir={cell.blockDir}");
                    _loggedWallFaceObjects++;
                }
                return;
            }

            if (sourceObj.tileType?.IsDoor == true)
            {
                LogDoorRejection(sourceObj, renderData, "door-fell-through-to-non-wall-mounted");
            }

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
                    RenderPriority = sourceObj.HasGrowth ? 1 : 2,
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
                    RenderPriority = sourceObj.HasGrowth ? 1 : 3,
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

        private static bool IsWallMountedInstalledCard(Card card)
        {
            if (card == null || !card.IsInstalled || card.pos?.cell == null)
            {
                return false;
            }

            TileType tileType = card.TileType;
            if (tileType == null)
            {
                return false;
            }

            if (tileType.IsDoor || card.trait?.IsDoor == true)
            {
                return card.pos.cell.HasBlock;
            }

            if (tileType.IsBlockMount)
            {
                return true;
            }

            return false;
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

        private static Vector2 ResolveCardSpriteWorldSize(Card card, Sprite sprite, RenderData renderData, BillboardKind kind)
        {
            if (sprite == null && renderData == null)
            {
                float fallbackHeight = ResolveBaseHeight(null, kind);
                return new Vector2(fallbackHeight, fallbackHeight);
            }

            Vector2 sourcePixelSize = sprite != null
                ? new Vector2(sprite.textureRect.width, sprite.textureRect.height)
                : ResolveRenderDataSourcePixelSize(renderData);

            if (renderData != null)
            {
                Vector3 pmeshSize = renderData.pass?.pmesh != null ? renderData.pass.pmesh.size : Vector3.zero;
                return FpsRenderDataWorldSizeResolver.Resolve(
                    sourcePixelSize,
                    renderData.imageScale,
                    renderData.size,
                    pmeshSize,
                    renderData.multiSize,
                    SpritePixelsPerTile,
                    card?.sourceCard?.W ?? 1,
                    card?.sourceCard?.H ?? 1);
            }

            return FpsRenderDataWorldSizeResolver.ApplyFootprintScale(
                new Vector2(
                Mathf.Max(0.02f, sourcePixelSize.x / SpritePixelsPerTile),
                Mathf.Max(0.02f, sourcePixelSize.y / SpritePixelsPerTile)),
                card?.sourceCard?.W ?? 1,
                card?.sourceCard?.H ?? 1);
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

            float elevation = ResolveVisualSupportHeight(card);
            elevation += Mathf.Clamp(card.altitude, 0, 4) * 0.06f;
            if (card is Thing thing && !card.ignoreStackHeight)
            {
                elevation += Mathf.Clamp(thing.stackOrder, 0, 6) * 0.025f;
            }

            return Mathf.Clamp(elevation, 0f, 3f);
        }

        private static float ResolveBillboardElevation(Card card, BillboardKind kind)
        {
            if (card == null)
            {
                return 0f;
            }

            float elevation = ResolveVisualSupportHeight(card);
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

            return Mathf.Clamp(elevation, 0f, 3f);
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

        private static int ResolveCardMaterialColor(Card card)
        {
            if (card == null)
            {
                return 104025;
            }

            int color = card.colorInt;
            return color == 0 ? 104025 : color;
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

        private static bool IsWallMountedCellObject(Cell cell, SourceObj.Row sourceObj, RenderData renderData)
        {
            if (cell == null || sourceObj == null || renderData == null)
            {
                return false;
            }

            TileType tileType = sourceObj.tileType;
            if (tileType == null)
            {
                return false;
            }

            if (tileType.IsDoor)
            {
                return cell.HasBlock;
            }

            if (tileType.IsBlockMount)
            {
                return cell.HasWallOrFence || cell.HasFullBlock || cell.HasBlock;
            }

            if (!cell.HasWallOrFence && !cell.HasFullBlock)
            {
                return false;
            }

            if (sourceObj.HasGrowth)
            {
                return false;
            }

            return true;
        }

        private void LogWallMountedCandidate(
            string source,
            string label,
            Cell cell,
            TileType tileType,
            bool door,
            bool candidate,
            bool routedToWallMounted)
        {
            if (_loggedWallMountedCandidates >= 20 || !candidate)
            {
                return;
            }

            ModLog.Developer(
                "WallMountedCandidate",
                $"GPU wall-mounted candidate[{_loggedWallMountedCandidates}]: source={source} label={label} cell=({cell?.x},{cell?.z}) " +
                $"tileType={tileType?.GetType().Name} blockMount={tileType?.IsBlockMount} door={door} routed={routedToWallMounted} " +
                $"wallOrFence={cell?.HasWallOrFence} fullBlock={cell?.HasFullBlock} hasBlock={cell?.HasBlock} objDir={cell?.objDir} blockDir={cell?.blockDir}");
            _loggedWallMountedCandidates++;
        }

        private void LogCellObjectSurvey(Cell cell, int cellX, int cellZ)
        {
            if (_loggedCellObjectSurvey >= 24 || cell == null || cell.obj == 0 || cell.sourceObj == null)
            {
                return;
            }

            TileType tileType = cell.sourceObj.tileType;
            ModLog.Developer(
                "CellObjectSurvey",
                $"GPU cell-obj survey[{_loggedCellObjectSurvey}]: id={cell.sourceObj.id} alias={cell.sourceObj.alias} cell=({cellX},{cellZ}) " +
                $"tileType={tileType?.GetType().Name} blockMount={tileType?.IsBlockMount} door={tileType?.IsDoor} " +
                $"wallOrFence={cell.HasWallOrFence} fullBlock={cell.HasFullBlock} hasBlock={cell.HasBlock} objDir={cell.objDir} blockDir={cell.blockDir}");
            _loggedCellObjectSurvey++;
        }

        private static bool LooksLikeWallMountedItem(SourceObj.Row sourceObj, RenderData renderData)
        {
            if (sourceObj == null || renderData == null)
            {
                return false;
            }

            Vector2 sourcePixelSize = ResolveRenderDataSourcePixelSize(renderData);
            return sourcePixelSize.x <= 40f && sourcePixelSize.y <= 40f;
        }

        private static bool ShouldTrimWallMountedTransparent(Cell cell, SourceObj.Row sourceObj, RenderData renderData)
        {
            if (sourceObj?.tileType?.IsDoor == true)
            {
                return true;
            }

            return false;
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
            Vector3 pmeshSize = renderData.pass?.pmesh != null ? renderData.pass.pmesh.size : Vector3.zero;
            return FpsRenderDataWorldSizeResolver.Resolve(
                sourcePixelSize,
                renderData.imageScale,
                renderData.size,
                pmeshSize,
                renderData.multiSize,
                SpritePixelsPerTile);
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
                float stackHeight = ResolveInstalledSupportHeight(thing, tileType, pref);

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

            return Mathf.Clamp(supportHeight, 0f, 3f);
        }

        private static float ResolveVisualSupportHeight(Card card)
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

                float stackHeight = ResolveInstalledVisualSupportHeight(thing, tileType, thing.Pref);
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

            return Mathf.Clamp(supportHeight, 0f, 3f);
        }

        private static float ResolveInstalledSupportHeight(Thing thing, TileType tileType, SourcePref pref)
        {
            if (thing == null || tileType == null)
            {
                return 0f;
            }

            float stackHeight = tileType.UseMountHeight
                ? 0f
                : ((pref == null || pref.height < 0f) ? 0f : ((Mathf.Abs(pref.height) <= 0.0001f) ? 0.1f : pref.height));

            if (stackHeight <= 0f)
            {
                return stackHeight;
            }

            return stackHeight;
        }

        private static float ResolveInstalledVisualSupportHeight(Thing thing, TileType tileType, SourcePref pref)
        {
            float stackHeight = ResolveInstalledSupportHeight(thing, tileType, pref);
            if (thing == null || tileType == null)
            {
                return stackHeight;
            }

            if (pref != null && pref.Surface)
            {
                float surfaceHeight = pref.height + Mathf.Clamp(thing.altitude, 0, 6) * 0.1f;
                return Mathf.Max(stackHeight, surfaceHeight);
            }

            return stackHeight;
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
                RenderPriority = 0,
                RenderData = renderData,
                Tile = tile,
                Light = lighting.ApproxBlockLight
            });
        }

        private static int ResolveCardRenderPriority(BillboardKind kind)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return 0;
                case BillboardKind.InstalledObject:
                case BillboardKind.TallObject:
                    return 1;
                default:
                    return 3;
            }
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
        public int RenderPriority;
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
        public int RenderPriority;
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
        public int RenderPriority;
        public FpsResolvedLightSample Light;
    }

    internal struct FpsResolvedWallMountedSprite
    {
        public Cell Cell;
        public Vector3 CellCenter;
        public Vector3 MountWorld;
        public bool HasCustomAnchor;
        public Sprite Sprite;
        public RenderData RenderData;
        public int Tile;
        public bool FlipX;
        public float Distance;
        public int WallDir;
        public int FaceDir;
        public bool IsDoor;
        public bool UseWallPanelPlacement;
        public bool TrimTransparent;
        public int RenderPriority;
        public int MaterialColor;
        public bool HasMaterialTint;
        public bool UseSelectiveMaterialTint;
        public FpsResolvedLightSample Light;
        public string DiagnosticLabel;
    }

    internal enum FpsRoofTextureKind
    {
        None,
        PrimarySource,
        BlockFace
    }

    internal enum FpsRoofTextureProjectionKind
    {
        RawTile,
        RectSlice,
        TriSlice
    }

    internal enum FpsRoofSourceOrigin
    {
        None,
        RoofTopPrimary
    }

    internal enum FpsRoofPlaneKind
    {
        Top,
        SlopeLeft,
        SlopeRight,
        Edge,
        Ramp,
        InteriorCeiling
    }

    internal struct FpsResolvedRoofPlane
    {
        public Lot Lot;
        public FpsRoofPlaneKind Kind;
        public FpsGpuFaceQuad Face;
        public FpsRoofTextureKind TextureKind;
        public FpsRoofTextureProjectionKind ProjectionKind;
        public FpsRoofSourceOrigin SourceOrigin;
        public RenderData RenderData;
        public int Tile;
        public int MaterialColor;
        public bool HasMaterialTint;
        public bool UseSelectiveMaterialTint;
        public bool FlipX;
        public bool TrimTransparent;
        public FpsResolvedWallSurface BlockSurface;
        public FpsAtlasSampler.BlockFaceKind BlockFaceKind;
        public bool DoubleSided;
        public bool UsePanelPlacement;
        public bool HasLotPrimarySource;
        public FpsResolvedRoofPrimarySource LotPrimarySource;
        public string DiagnosticLabel;
    }

    internal struct FpsResolvedRoofStructure
    {
        public Lot Lot;
        public FpsRoofStructurePlan Plan;
        public bool HasPrimarySource;
        public FpsResolvedRoofPrimarySource PrimarySource;
        public bool HasFallbackSurface;
        public FpsResolvedWallSurface FallbackSurface;
        public FpsResolvedLightSample Light;
        public string DiagnosticLabel;
    }

    internal struct FpsRoofLayout
    {
        public Lot Lot;
        public RoofStyle.Type Type;
        public bool Reverse;
        public int MinX;
        public int MinZ;
        public int MaxX;
        public int MaxZ;
        public int RowCount;
        public int FlatStart;
        public int FlatEnd;
        public float BaseY;
        public float StepY;
        public float TopY;
        public FpsResolvedLightSample Light;
        public bool HasPrimarySource;
        public FpsResolvedRoofPrimarySource PrimarySource;

        public float MinorStart => Reverse ? MinX : MinZ;

        public float MinorEnd => Reverse ? MaxX : MaxZ;

        public bool HasFlatBand => FlatEnd > FlatStart;
    }

    internal struct FpsResolvedRoofPrimarySource
    {
        public int RoofTileId;
        public int TileDirection;
        public RenderData RenderData;
        public int Tile;
        public int MaterialColor;
        public FpsResolvedLightSample Light;
        public FpsRoofSourceOrigin Origin;
        public string DiagnosticLabel;
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
