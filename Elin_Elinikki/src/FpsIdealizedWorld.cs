using System.Collections.Generic;
using UnityEngine;

namespace Elin_Elinikki
{
    internal sealed class FpsIdealizedWorld
    {
        private const float SpritePixelsPerTile = 64f;
        private const float LooseItemBaseHeight = 0.18f;
        private const float CharaBaseHeight = 1.05f;
        private const float InstalledBaseHeight = 0.9f;
        private const float TallObjectBaseHeight = 1.3f;
        private readonly FpsLightingResolver _lightingResolver = new FpsLightingResolver();
        private static bool _loggedCellObjectSpriteFailure;
        private static bool _loggedCellEffectSpriteFailure;
        private int _loggedWallFaceObjects;
        private int _loggedDoorRejections;
        private int _loggedWallMountedCandidates;
        private int _loggedCellObjectSurvey;

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
                TryAddGroundSprite(card, origin, FpsSpriteVisibilityResolver.ResolveCardVisibilityDistance(kind, maxDistance), lighting, groundOutput);
                return;
            }

            TryAddUprightSprite(card, origin, FpsSpriteVisibilityResolver.ResolveCardVisibilityDistance(kind, maxDistance), kind, lighting, uprightOutput);
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

            float groundHeightWorld = GetCellSurfaceHeight(card.pos.cell) + FpsStackSupportResolver.ResolveBillboardElevation(card, kind);
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
                VisibilityDistance = FpsSpriteVisibilityResolver.ResolveCardVisibilityDistance(kind, maxDistance),
                VisibilityConeDot = FpsSpriteVisibilityResolver.ResolveCardVisibilityConeDot(kind),
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
            float groundHeightWorld = GetCellSurfaceHeight(card.pos.cell) + FpsStackSupportResolver.ResolveGroundElevation(card);
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
            float visibilityDistance = FpsSpriteVisibilityResolver.ResolveCellObjectVisibilityDistance(uprightObject, maxDistance);
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
                    VisibilityConeDot = FpsSpriteVisibilityResolver.ResolveCellObjectVisibilityConeDot(true),
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
                    VisibilityConeDot = FpsSpriteVisibilityResolver.ResolveCellObjectVisibilityConeDot(false),
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
            float visibilityDistance = FpsSpriteVisibilityResolver.ResolveEffectVisibilityDistance(maxDistance);
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
                VisibilityConeDot = FpsSpriteVisibilityResolver.GameplayConeDotValue,
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

    internal enum BillboardFacingRule
    {
        CameraFacing
    }
}
