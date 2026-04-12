using System;
using System.Collections.Generic;
using UnityEngine;
namespace Elin_Elinikki
{
    internal sealed partial class FpsGpuPreviewRenderer
    {

        private void UpdateTerrainPreview(GpuViewPose pose)
        {
            float terrainMaxDistance = Mathf.Max(1f, Plugin.Settings.MaxDistance.Value * Plugin.Settings.TerrainDistanceMultiplier.Value);
            int radius = Mathf.Min(Mathf.CeilToInt(terrainMaxDistance), MaxPreviewRadius);
            int minX = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.x) - radius);
            int maxX = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.x) + radius);
            int minZ = Mathf.Max(0, Mathf.FloorToInt(pose.PlayerOrigin.y) - radius);
            int maxZ = Mathf.Min(EClass._map.Size - 1, Mathf.FloorToInt(pose.PlayerOrigin.y) + radius);

            int activeWallIndex = 0;
            bool useHybridWallGeometry = Plugin.Settings?.EnableHybridWallGeometry?.Value == true;
            _fullBlockWallSeeds.Clear();
            _fullBlockWallRuns.Clear();
            _fullBlockWallSurfaceLookup.Clear();
            _panelWallSeeds.Clear();
            _panelWallRuns.Clear();
            _panelDoorFrameRuns.Clear();
            _panelWallSurfaceLookup.Clear();
            _terrainChunkContexts.Clear();
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Cell cell = EClass._map.cells[x, z];
                    Vector3 cellCenter = new Vector3(x + 0.5f, FpsIdealizedWorld.GetCellSurfaceHeight(cell), z + 0.5f);
                    bool terrainVisible = IsTerrainVisibleToCamera(cellCenter, pose, terrainMaxDistance, 0f, 0.35f);
                    bool structureVisible = IsTerrainVisibleToCamera(cellCenter, pose, terrainMaxDistance + 0.25f, 0f, 0.45f);
                    if (!terrainVisible && !structureVisible)
                    {
                        continue;
                    }

                    bool hasSurface = _idealizedWorld.TryResolveFloor(cell, cell?.index ?? -1, out FpsResolvedFloorSurface surface);
                    bool isFullBlock = cell != null && cell.HasFullBlock;
                    FpsResolvedWallSurface blockTopSurface = default;
                    bool hasBlockSurface = isFullBlock && _idealizedWorld.TryResolveWall(cell, out blockTopSurface);

                    FpsTerrainArchetypeResolution terrainResolution = FpsTerrainArchetypeResolver.Resolve(
                        cell,
                        FpsIdealizedWorld.GetCellSurfaceHeight(cell) + RenderHeightOffset,
                        0f);
                    bool isSpecialTerrainCell = cell != null && terrainResolution.IsSpecialTerrainCell;

                    if (terrainVisible || (structureVisible && isSpecialTerrainCell))
                    {
                        float surfaceHeight = isFullBlock
                            ? FpsIdealizedWorld.GetCellSurfaceHeight(cell) + 1f + RenderHeightOffset
                            : FpsIdealizedWorld.GetCellSurfaceHeight(cell) + RenderHeightOffset;
                        CollectTerrainChunkCell(x, z, surfaceHeight, hasSurface, surface, hasBlockSurface, blockTopSurface);
                    }

                    if (structureVisible && IsSolidWall(cell) && _idealizedWorld.TryResolveWall(cell, out FpsResolvedWallSurface wallSurface))
                    {
                        if (useHybridWallGeometry && cell.HasFullBlock)
                        {
                            CollectFullBlockWallSeeds(x, z, wallSurface);
                        }
                        else if (useHybridWallGeometry && cell.HasWallOrFence)
                        {
                            CollectPanelWallSeeds(x, z, wallSurface);
                        }
                        else
                        {
                            activeWallIndex = AddWallQuads(activeWallIndex, x, z, wallSurface);
                        }
                    }
                }
            }

            if (useHybridWallGeometry && _fullBlockWallSeeds.Count > 0)
            {
                _fullBlockWallRuns.AddRange(FpsWallRunPlanner.BuildRuns(_fullBlockWallSeeds));
                activeWallIndex = RenderHybridFullBlockRuns(activeWallIndex);
            }

            if (useHybridWallGeometry && _panelWallSeeds.Count > 0)
            {
                _panelWallRuns.AddRange(FpsWallRunPlanner.BuildRuns(_panelWallSeeds));
                activeWallIndex = RenderHybridPanelRuns(activeWallIndex);
            }

            int activeTerrainChunks = RenderTerrainChunks(terrainMaxDistance);

            for (int i = activeWallIndex; i < _wallQuads.Count; i++)
            {
                _wallQuads[i].SetActive(false);
            }

            LogDebugFrame(activeTerrainChunks, pose);
        }

        private void CollectTerrainChunkCell(
            int cellX,
            int cellZ,
            float surfaceHeight,
            bool hasFloorSurface,
            FpsResolvedFloorSurface floorSurface,
            bool hasBlockSurface,
            FpsResolvedWallSurface blockTopSurface)
        {
            int chunkX = Mathf.FloorToInt(cellX / (float)TerrainChunkSize);
            int chunkZ = Mathf.FloorToInt(cellZ / (float)TerrainChunkSize);
            int key = GetTerrainChunkKey(chunkX, chunkZ);
            if (!_terrainChunkContexts.TryGetValue(key, out TerrainChunkBuildContext context))
            {
                context = new TerrainChunkBuildContext(chunkX, chunkZ);
                _terrainChunkContexts.Add(key, context);
            }

            Cell cell = EClass._map.cells[cellX, cellZ];
            bool allowRisers = cell != null && !cell.HasFullBlock;
            FpsTerrainArchetypeResolution resolution = FpsTerrainArchetypeResolver.Resolve(cell, surfaceHeight, RenderHeightOffset);
            bool hasNorthNeighbor = TryGetTerrainNeighborHeight(cellX, cellZ - 1, out float northNeighborHeight);
            bool hasEastNeighbor = TryGetTerrainNeighborHeight(cellX + 1, cellZ, out float eastNeighborHeight);
            bool hasSouthNeighbor = TryGetTerrainNeighborHeight(cellX, cellZ + 1, out float southNeighborHeight);
            bool hasWestNeighbor = TryGetTerrainNeighborHeight(cellX - 1, cellZ, out float westNeighborHeight);
            FpsResolvedWallSurface riserSurface = default;
            bool hasRiserSurface = allowRisers && _idealizedWorld.TryResolveTerrainRiser(EClass._map.cells[cellX, cellZ], out riserSurface);
            context.Cells.Add(new TerrainChunkSourceCell(
                cellX - chunkX * TerrainChunkSize,
                cellZ - chunkZ * TerrainChunkSize,
                surfaceHeight,
                hasFloorSurface,
                floorSurface,
                hasBlockSurface,
                blockTopSurface,
                allowRisers,
                resolution.Archetype,
                resolution.ShapeBaseHeight,
                hasNorthNeighbor,
                northNeighborHeight + RenderHeightOffset,
                hasEastNeighbor,
                eastNeighborHeight + RenderHeightOffset,
                hasSouthNeighbor,
                southNeighborHeight + RenderHeightOffset,
                hasWestNeighbor,
                westNeighborHeight + RenderHeightOffset,
                hasRiserSurface,
                riserSurface,
                resolution.RampDir,
                resolution.RampStepCount,
                resolution.HasBridgePillar,
                resolution.SupportBaseHeight));
        }

        private int RenderTerrainChunks(float terrainMaxDistance)
        {
            _orderedTerrainChunks.Clear();
            foreach (TerrainChunkBuildContext context in _terrainChunkContexts.Values)
            {
                if (context.Cells.Count > 0)
                {
                    _orderedTerrainChunks.Add(context);
                }
            }

            _orderedTerrainChunks.Sort(static (a, b) =>
            {
                int zCompare = a.ChunkZ.CompareTo(b.ChunkZ);
                return zCompare != 0 ? zCompare : a.ChunkX.CompareTo(b.ChunkX);
            });

            int activeIndex = 0;
            for (int i = 0; i < _orderedTerrainChunks.Count; i++)
            {
                TerrainChunkBuildContext chunk = _orderedTerrainChunks[i];
                EnsureTerrainPool(activeIndex + 1);
                EnsureTerrainChunkStateCount(activeIndex + 1);

                GameObject quad = _terrainQuads[activeIndex];
                MeshRenderer renderer = _terrainRenderers[activeIndex];
                MeshFilter filter = _terrainFilters[activeIndex];
                quad.SetActive(true);
                _terrainOverlayQuads[activeIndex].SetActive(false);
                quad.transform.localPosition = Vector3.zero;
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = Vector3.one;

                int signature = ComputeTerrainChunkSignature(chunk);
                int lightingSignature = ComputeTerrainChunkLightingSignature(chunk);
                TerrainChunkVisualState state = _terrainChunkStates[activeIndex];
                if (!state.Matches(chunk.ChunkX, chunk.ChunkZ, signature))
                {
                    RebuildTerrainChunkMesh(filter.sharedMesh, chunk);
                    Texture2D texture = BuildTerrainChunkTexture(chunk);
                    if (state.Texture != null && state.Texture != texture)
                    {
                        UnityEngine.Object.Destroy(state.Texture);
                    }

                    state = new TerrainChunkVisualState
                    {
                        HasData = true,
                        ChunkX = chunk.ChunkX,
                        ChunkZ = chunk.ChunkZ,
                        Signature = signature,
                        LightingSignature = lightingSignature,
                        Texture = texture
                    };
                    _terrainChunkStates[activeIndex] = state;
                }
                else if (state.LightingSignature != lightingSignature)
                {
                    RebuildTerrainChunkMesh(filter.sharedMesh, chunk);
                    state.LightingSignature = lightingSignature;
                    _terrainChunkStates[activeIndex] = state;
                }

                _propertyBlock.Clear();
                _propertyBlock.SetTexture("_MainTex", state.Texture != null ? state.Texture : Texture2D.whiteTexture);
                _propertyBlock.SetColor("_Color", ApplyAtmosphericFog(Color.white, chunk.CenterWorld, terrainMaxDistance, 0.18f, "terrain-floor"));
                renderer.SetPropertyBlock(_propertyBlock);
                activeIndex++;
            }

            for (int i = activeIndex; i < _terrainQuads.Count; i++)
            {
                _terrainQuads[i].SetActive(false);
                _terrainOverlayQuads[i].SetActive(false);
            }

            return activeIndex;
        }

        private void EnsureTerrainChunkStateCount(int count)
        {
            while (_terrainChunkStates.Count < count)
            {
                _terrainChunkStates.Add(default);
            }
        }

        private static int GetTerrainChunkKey(int chunkX, int chunkZ)
        {
            return (chunkX << 16) ^ (chunkZ & 0xFFFF);
        }

        private static int ComputeTerrainChunkSignature(TerrainChunkBuildContext chunk)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + chunk.ChunkX;
                hash = hash * 31 + chunk.ChunkZ;
                for (int i = 0; i < chunk.Cells.Count; i++)
                {
                    TerrainChunkSourceCell cell = chunk.Cells[i];
                    hash = hash * 31 + cell.LocalX;
                    hash = hash * 31 + cell.LocalZ;
                    hash = hash * 31 + cell.Height.GetHashCode();
                    hash = hash * 31 + (cell.HasFloorSurface ? 1 : 0);
                    hash = hash * 31 + (cell.HasBlockSurface ? 1 : 0);
                    hash = hash * 31 + (cell.AllowRisers ? 1 : 0);
                    hash = hash * 31 + (int)cell.Archetype;
                    hash = hash * 31 + cell.BaseHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasNorthNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.NorthNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasEastNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.EastNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasSouthNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.SouthNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasWestNeighbor ? 1 : 0);
                    hash = hash * 31 + cell.WestNeighborHeight.GetHashCode();
                    hash = hash * 31 + (cell.HasRiserSurface ? 1 : 0);
                    hash = hash * 31 + cell.RampDir;
                    hash = hash * 31 + cell.RampStepCount;
                    hash = hash * 31 + (cell.HasBridgePillar ? 1 : 0);
                    hash = hash * 31 + cell.BridgeBaseHeight.GetHashCode();
                    if (cell.HasBlockSurface)
                    {
                        hash = hash * 31 + cell.BlockTopSurface.Tile;
                        hash = hash * 31 + cell.BlockTopSurface.MaterialColor;
                    }
                    else
                    {
                        hash = hash * 31 + cell.FloorSurface.BaseTile;
                        hash = hash * 31 + cell.FloorSurface.AutoTileOverlay;
                        hash = hash * 31 + (cell.FloorSurface.UseSnowAtlas ? 1 : 0);
                        hash = hash * 31 + (cell.FloorSurface.UseWaterAutoTileAtlas ? 1 : 0);
                        hash = hash * 31 + cell.FloorSurface.MaterialColor;
                    }

                    if (cell.HasRiserSurface)
                    {
                        hash = hash * 31 + cell.RiserSurface.Tile;
                        hash = hash * 31 + cell.RiserSurface.MaterialColor;
                    }
                }

                return hash;
            }
        }

        private static int ComputeTerrainChunkLightingSignature(TerrainChunkBuildContext chunk)
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + chunk.ChunkX;
                hash = hash * 31 + chunk.ChunkZ;
                for (int i = 0; i < chunk.Cells.Count; i++)
                {
                    TerrainChunkSourceCell cell = chunk.Cells[i];
                    hash = hash * 31 + cell.LocalX;
                    hash = hash * 31 + cell.LocalZ;
                    hash = hash * 31 + (cell.HasBlockSurface
                        ? cell.BlockTopSurface.Light.PackedLight
                        : cell.HasFloorSurface
                            ? cell.FloorSurface.Light.PackedLight
                            : 0);
                }

                return hash;
            }
        }

        private static void RebuildTerrainChunkMesh(Mesh mesh, TerrainChunkBuildContext chunk)
        {
            if (mesh == null)
            {
                return;
            }

            List<FpsTerrainChunkCell> cells = new List<FpsTerrainChunkCell>(chunk.Cells.Count);
            for (int i = 0; i < chunk.Cells.Count; i++)
            {
                TerrainChunkSourceCell cell = chunk.Cells[i];
                cells.Add(new FpsTerrainChunkCell(
                    cell.LocalX,
                    cell.LocalZ,
                    cell.Height,
                    ResolveTerrainChunkTint(cell),
                    cell.HasNorthNeighbor,
                    cell.NorthNeighborHeight,
                    cell.HasEastNeighbor,
                    cell.EastNeighborHeight,
                    cell.HasSouthNeighbor,
                    cell.SouthNeighborHeight,
                    cell.HasWestNeighbor,
                    cell.WestNeighborHeight,
                    cell.AllowRisers && cell.HasRiserSurface,
                    cell.Archetype,
                    cell.BaseHeight,
                    cell.RampDir,
                    cell.RampStepCount,
                    cell.HasBridgePillar,
                    cell.BridgeBaseHeight));
            }

            FpsTerrainChunkMeshData data = FpsTerrainChunkMeshBuilder.Build(
                chunk.ChunkX * TerrainChunkSize,
                chunk.ChunkZ * TerrainChunkSize,
                TerrainChunkSize,
                cells);

            mesh.Clear();
            mesh.vertices = data.Vertices;
            mesh.uv = data.Uvs;
            mesh.triangles = data.Triangles;
            mesh.colors32 = data.Colors;
            if (data.Vertices.Length > 0)
            {
                Vector3[] normals = new Vector3[data.Vertices.Length];
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = Vector3.up;
                }

                mesh.normals = normals;
            }

            mesh.RecalculateBounds();
        }

        private Texture2D BuildTerrainChunkTexture(TerrainChunkBuildContext chunk)
        {
            int textureWidth = TerrainChunkSize * TerrainTileTextureSize * TerrainTextureSlotCount;
            int textureHeight = TerrainChunkSize * TerrainTileTextureSize;
            Color32[] pixels = new Color32[textureWidth * textureHeight];

            for (int i = 0; i < chunk.Cells.Count; i++)
            {
                TerrainChunkSourceCell cell = chunk.Cells[i];
                Color topTint = ApplySurfaceStyle(Color.white, FpsVisualSurfaceKind.TerrainTop, 0, false);
                if (!TryResolveTerrainChunkSource(cell, out Texture2D sourceTexture))
                {
                    FillTerrainChunkTile(
                        pixels,
                        textureWidth,
                        textureHeight,
                        cell.LocalX * TerrainTileTextureSize * TerrainTextureSlotCount,
                        cell.LocalZ * TerrainTileTextureSize,
                        topTint);
                }
                else
                {
                    BlitTerrainChunkTile(
                        pixels,
                        textureWidth,
                        textureHeight,
                        cell.LocalX * TerrainTileTextureSize * TerrainTextureSlotCount,
                        cell.LocalZ * TerrainTileTextureSize,
                        sourceTexture,
                        topTint);
                }

                Color sideTint = ApplySurfaceStyle(Color.white, FpsVisualSurfaceKind.TerrainSide, 0, false);
                if (TryResolveTerrainChunkSideSource(cell, out Texture2D riserTexture))
                {
                    BlitTerrainChunkTile(
                        pixels,
                        textureWidth,
                        textureHeight,
                        cell.LocalX * TerrainTileTextureSize * TerrainTextureSlotCount + TerrainTileTextureSize,
                        cell.LocalZ * TerrainTileTextureSize,
                        riserTexture,
                        sideTint);
                }
                else if (cell.Archetype != FpsTerrainChunkArchetype.Flat || (cell.AllowRisers && cell.HasRiserSurface))
                {
                    FillTerrainChunkTile(
                        pixels,
                        textureWidth,
                        textureHeight,
                        cell.LocalX * TerrainTileTextureSize * TerrainTextureSlotCount + TerrainTileTextureSize,
                        cell.LocalZ * TerrainTileTextureSize,
                        sideTint);
                }
            }

            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"FpsGpuTerrainChunk_{chunk.ChunkX}_{chunk.ChunkZ}"
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private bool TryResolveTerrainChunkSource(TerrainChunkSourceCell cell, out Texture2D texture)
        {
            texture = null;

            if (cell.HasBlockSurface)
            {
                if (_spriteTextureCache.TryGetBlockFaceTexture(cell.BlockTopSurface, FpsAtlasSampler.BlockFaceKind.Top, false, out Texture blockTexture)
                    && blockTexture is Texture2D blockTexture2D)
                {
                    texture = blockTexture2D;
                    return true;
                }

                return false;
            }

            if (!cell.HasFloorSurface)
            {
                return false;
            }

            if (_floorAtlasBaker.TryGetCompositeTexture(cell.FloorSurface, out Texture floorTexture)
                && floorTexture is Texture2D floorTexture2D)
            {
                texture = floorTexture2D;
                return true;
            }

            return false;
        }

        private bool TryResolveTerrainChunkSideSource(TerrainChunkSourceCell cell, out Texture2D texture)
        {
            texture = null;
            bool preferFloorSide = cell.Archetype != FpsTerrainChunkArchetype.Flat;

            if (preferFloorSide
                && cell.HasFloorSurface
                && _floorAtlasBaker.TryGetCompositeTexture(cell.FloorSurface, out Texture preferredFloorTexture)
                && preferredFloorTexture is Texture2D preferredFloorTexture2D)
            {
                texture = preferredFloorTexture2D;
                return true;
            }

            if (cell.HasRiserSurface
                && _spriteTextureCache.TryGetBlockFaceTexture(cell.RiserSurface, true, out Texture riserTexture)
                && riserTexture is Texture2D riserTexture2D)
            {
                texture = riserTexture2D;
                return true;
            }

            if (cell.HasBlockSurface
                && _spriteTextureCache.TryGetBlockFaceTexture(cell.BlockTopSurface, true, out Texture blockTexture)
                && blockTexture is Texture2D blockTexture2D)
            {
                texture = blockTexture2D;
                return true;
            }

            if (cell.HasFloorSurface
                && _floorAtlasBaker.TryGetCompositeTexture(cell.FloorSurface, out Texture floorTexture)
                && floorTexture is Texture2D floorTexture2D)
            {
                texture = floorTexture2D;
                return true;
            }

            return false;
        }

        private static void BlitTerrainChunkTile(Color32[] destination, int destinationWidth, int destinationHeight, int startX, int startY, Texture2D sourceTexture, Color tint)
        {
            Color32[] source = sourceTexture.GetPixels32();
            int sourceWidth = sourceTexture.width;
            int sourceHeight = sourceTexture.height;

            for (int y = 0; y < TerrainTileTextureSize; y++)
            {
                int destinationY = startY + y;
                if (destinationY < 0 || destinationY >= destinationHeight)
                {
                    continue;
                }

                int sampleY = Mathf.Clamp(Mathf.FloorToInt((y / (float)TerrainTileTextureSize) * sourceHeight), 0, sourceHeight - 1);
                for (int x = 0; x < TerrainTileTextureSize; x++)
                {
                    int destinationX = startX + x;
                    if (destinationX < 0 || destinationX >= destinationWidth)
                    {
                        continue;
                    }

                    int sampleX = Mathf.Clamp(Mathf.FloorToInt((x / (float)TerrainTileTextureSize) * sourceWidth), 0, sourceWidth - 1);
                    Color32 color = source[sampleY * sourceWidth + sampleX];
                    color.a = 255;
                    destination[destinationY * destinationWidth + destinationX] = MultiplyColor(color, tint);
                }
            }
        }

        private static void FillTerrainChunkTile(Color32[] destination, int destinationWidth, int destinationHeight, int startX, int startY, Color tint)
        {
            Color32 color = MultiplyColor(new Color32(255, 255, 255, 255), tint);
            color.a = 255;
            for (int y = 0; y < TerrainTileTextureSize; y++)
            {
                int destinationY = startY + y;
                if (destinationY < 0 || destinationY >= destinationHeight)
                {
                    continue;
                }

                for (int x = 0; x < TerrainTileTextureSize; x++)
                {
                    int destinationX = startX + x;
                    if (destinationX < 0 || destinationX >= destinationWidth)
                    {
                        continue;
                    }

                    destination[destinationY * destinationWidth + destinationX] = color;
                }
            }
        }

        private static bool TryGetTerrainNeighborHeight(int cellX, int cellZ, out float height)
        {
            height = 0f;
            if (EClass._map == null || cellX < 0 || cellZ < 0 || cellX >= EClass._map.Size || cellZ >= EClass._map.Size)
            {
                return false;
            }

            Cell neighbor = EClass._map.cells[cellX, cellZ];
            if (neighbor == null)
            {
                return false;
            }

            height = FpsIdealizedWorld.GetCellSurfaceHeight(neighbor);
            return true;
        }

        private static Color32 ResolveTerrainChunkTint(TerrainChunkSourceCell cell)
        {
            if (cell.HasBlockSurface)
            {
                return (Color32)ApplySurfaceStyle(
                    FpsLightApplicator.ApplySample(new Color32(255, 255, 255, 255), cell.BlockTopSurface.Light),
                    FpsVisualSurfaceKind.TerrainTop,
                    0,
                    false);
            }

            if (cell.HasFloorSurface)
            {
                return (Color32)ResolveTerrainChunkFloorTint(cell.FloorSurface);
            }

            return new Color32(255, 255, 255, 255);
        }

        private static Color ResolveTerrainChunkFloorTint(FpsResolvedFloorSurface surface)
        {
            Color32 color = new Color32(255, 255, 255, 255);
            return ApplySurfaceStyle(FpsLightApplicator.ApplySample(color, surface.Light), FpsVisualSurfaceKind.TerrainTop, 0, false);
        }

    }
}