using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_Elinikki
{
    internal enum FpsTerrainChunkArchetype
    {
        Flat,
        RaisedPlatform,
        Stair,
        Bridge
    }

    internal readonly struct FpsTerrainChunkCell
    {
        public FpsTerrainChunkCell(int localX, int localZ, float height)
            : this(localX, localZ, height, new Color32(255, 255, 255, 255))
        {
        }

        public FpsTerrainChunkCell(int localX, int localZ, float height, Color32 tint)
            : this(localX, localZ, height, tint, false, 0f, false, 0f, false, 0f, false, 0f, true, FpsTerrainChunkArchetype.Flat, 0f, 0, 0, false, 0f)
        {
        }

        public FpsTerrainChunkCell(
            int localX,
            int localZ,
            float height,
            Color32 tint,
            bool hasNorthNeighbor,
            float northNeighborHeight,
            bool hasEastNeighbor,
            float eastNeighborHeight,
            bool hasSouthNeighbor,
            float southNeighborHeight,
            bool hasWestNeighbor,
            float westNeighborHeight,
            bool emitFlatCliffSides)
            : this(localX, localZ, height, tint, hasNorthNeighbor, northNeighborHeight, hasEastNeighbor, eastNeighborHeight, hasSouthNeighbor, southNeighborHeight, hasWestNeighbor, westNeighborHeight, emitFlatCliffSides, FpsTerrainChunkArchetype.Flat, 0f, 0, 0, false, 0f)
        {
        }

        public FpsTerrainChunkCell(
            int localX,
            int localZ,
            float height,
            Color32 tint,
            bool hasNorthNeighbor,
            float northNeighborHeight,
            bool hasEastNeighbor,
            float eastNeighborHeight,
            bool hasSouthNeighbor,
            float southNeighborHeight,
            bool hasWestNeighbor,
            float westNeighborHeight,
            bool emitFlatCliffSides,
            FpsTerrainChunkArchetype archetype,
            float baseHeight,
            int rampDir,
            int rampStepCount,
            bool hasBridgePillar,
            float bridgeBaseHeight)
        {
            LocalX = localX;
            LocalZ = localZ;
            Height = height;
            Tint = tint;
            HasNorthNeighbor = hasNorthNeighbor;
            NorthNeighborHeight = northNeighborHeight;
            HasEastNeighbor = hasEastNeighbor;
            EastNeighborHeight = eastNeighborHeight;
            HasSouthNeighbor = hasSouthNeighbor;
            SouthNeighborHeight = southNeighborHeight;
            HasWestNeighbor = hasWestNeighbor;
            WestNeighborHeight = westNeighborHeight;
            EmitFlatCliffSides = emitFlatCliffSides;
            Archetype = archetype;
            BaseHeight = baseHeight;
            RampDir = rampDir;
            RampStepCount = rampStepCount;
            HasBridgePillar = hasBridgePillar;
            BridgeBaseHeight = bridgeBaseHeight;
        }

        public int LocalX { get; }

        public int LocalZ { get; }

        public float Height { get; }

        public Color32 Tint { get; }

        public bool HasNorthNeighbor { get; }

        public float NorthNeighborHeight { get; }

        public bool HasEastNeighbor { get; }

        public float EastNeighborHeight { get; }

        public bool HasSouthNeighbor { get; }

        public float SouthNeighborHeight { get; }

        public bool HasWestNeighbor { get; }

        public float WestNeighborHeight { get; }

        public bool EmitFlatCliffSides { get; }

        public FpsTerrainChunkArchetype Archetype { get; }

        public float BaseHeight { get; }

        public int RampDir { get; }

        public int RampStepCount { get; }

        public bool HasBridgePillar { get; }

        public float BridgeBaseHeight { get; }
    }

    internal sealed class FpsTerrainChunkMeshData
    {
        public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();

        public Vector2[] Uvs { get; set; } = Array.Empty<Vector2>();

        public int[] Triangles { get; set; } = Array.Empty<int>();

        public Color32[] Colors { get; set; } = Array.Empty<Color32>();
    }

    internal static class FpsTerrainChunkMeshBuilder
    {
        private const float HeightEpsilon = 0.02f;
        private const int TextureSlotCount = 2;

        public static FpsTerrainChunkMeshData Build(int chunkOriginX, int chunkOriginZ, int chunkSize, IReadOnlyList<FpsTerrainChunkCell> cells)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            }

            if (cells == null || cells.Count == 0)
            {
                return new FpsTerrainChunkMeshData();
            }

            List<Vector3> vertices = new List<Vector3>(cells.Count * 8);
            List<Vector2> uvs = new List<Vector2>(cells.Count * 8);
            List<int> triangles = new List<int>(cells.Count * 12);
            List<Color32> colors = new List<Color32>(cells.Count * 8);
            float uvStepX = 1f / (chunkSize * TextureSlotCount);
            float uvStepY = 1f / chunkSize;

            for (int i = 0; i < cells.Count; i++)
            {
                FpsTerrainChunkCell cell = cells[i];
                float worldX = chunkOriginX + cell.LocalX;
                float worldZ = chunkOriginZ + cell.LocalZ;
                float y = cell.Height;
                float topUMin = (cell.LocalX * TextureSlotCount) * uvStepX;
                float topUMax = (cell.LocalX * TextureSlotCount + 1) * uvStepX;
                float sideUMin = (cell.LocalX * TextureSlotCount + 1) * uvStepX;
                float sideUMax = (cell.LocalX * TextureSlotCount + 2) * uvStepX;
                float vMin = cell.LocalZ * uvStepY;
                float vMax = (cell.LocalZ + 1) * uvStepY;

                if (cell.Archetype == FpsTerrainChunkArchetype.Stair)
                {
                    AddStairVolume(
                        vertices,
                        uvs,
                        triangles,
                        colors,
                        worldX,
                        worldZ,
                        cell.RampDir,
                        cell.RampStepCount,
                        cell.BaseHeight,
                        cell.BaseHeight,
                        y,
                        topUMin,
                        topUMax,
                        sideUMin,
                        sideUMax,
                        vMin,
                        vMax,
                        cell.Tint);
                }
                else
                {
                    if ((cell.Archetype == FpsTerrainChunkArchetype.Bridge || cell.Archetype == FpsTerrainChunkArchetype.RaisedPlatform)
                        && cell.BridgeBaseHeight < cell.BaseHeight - HeightEpsilon)
                    {
                        AddTemplate(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.Top),
                            new Vector3(worldX, cell.BridgeBaseHeight, worldZ),
                            new Vector3(1f, 1f, 1f),
                            topUMin,
                            topUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }

                    AddTemplate(
                        vertices,
                        uvs,
                        triangles,
                        colors,
                        FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.Top),
                        new Vector3(worldX, y, worldZ),
                        new Vector3(1f, 1f, 1f),
                        topUMin,
                        topUMax,
                        vMin,
                        vMax,
                        cell.Tint);

                    if (cell.Archetype != FpsTerrainChunkArchetype.Flat && y > cell.BaseHeight + HeightEpsilon)
                    {
                        AddClosedPlatformVolume(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            worldX,
                            worldZ,
                            cell.BaseHeight,
                            y,
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }
                }

                if (cell.Archetype == FpsTerrainChunkArchetype.Flat && cell.EmitFlatCliffSides)
                {
                    if (cell.HasNorthNeighbor && cell.Height > cell.NorthNeighborHeight + HeightEpsilon)
                    {
                        AddTemplate(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.NorthSide),
                            new Vector3(worldX, cell.NorthNeighborHeight, worldZ),
                            new Vector3(1f, y - cell.NorthNeighborHeight, 1f),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }

                    if (cell.HasEastNeighbor && cell.Height > cell.EastNeighborHeight + HeightEpsilon)
                    {
                        AddTemplate(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.EastSide),
                            new Vector3(worldX, cell.EastNeighborHeight, worldZ),
                            new Vector3(1f, y - cell.EastNeighborHeight, 1f),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }

                    if (cell.HasSouthNeighbor && cell.Height > cell.SouthNeighborHeight + HeightEpsilon)
                    {
                        AddTemplate(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.SouthSide),
                            new Vector3(worldX, cell.SouthNeighborHeight, worldZ),
                            new Vector3(1f, y - cell.SouthNeighborHeight, 1f),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }

                    if (cell.HasWestNeighbor && cell.Height > cell.WestNeighborHeight + HeightEpsilon)
                    {
                        AddTemplate(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.WestSide),
                            new Vector3(worldX, cell.WestNeighborHeight, worldZ),
                            new Vector3(1f, y - cell.WestNeighborHeight, 1f),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }
                }

                if (cell.HasBridgePillar && y > cell.BridgeBaseHeight + HeightEpsilon)
                {
                    float pillarTop = cell.Archetype == FpsTerrainChunkArchetype.Bridge || cell.Archetype == FpsTerrainChunkArchetype.RaisedPlatform
                        ? cell.BaseHeight
                        : y;
                    if (pillarTop > cell.BridgeBaseHeight + HeightEpsilon)
                    {
                        AddPillar(vertices, uvs, triangles, colors, worldX + 0.5f, worldZ + 0.5f, cell.BridgeBaseHeight, pillarTop, sideUMin, sideUMax, vMin, vMax, cell.Tint);
                    }
                }
            }

            return new FpsTerrainChunkMeshData
            {
                Vertices = vertices.ToArray(),
                Uvs = uvs.ToArray(),
                Triangles = triangles.ToArray(),
                Colors = colors.ToArray()
            };
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            float uMin,
            float uMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            int vertexStart = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            uvs.Add(new Vector2(uMin, vMin));
            uvs.Add(new Vector2(uMax, vMin));
            uvs.Add(new Vector2(uMin, vMax));
            uvs.Add(new Vector2(uMax, vMax));
            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);
            triangles.Add(vertexStart + 0);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 1);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 3);
            triangles.Add(vertexStart + 1);
        }

        private static void AddTemplate(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            FpsTerrainMeshTemplate template,
            Vector3 origin,
            Vector3 scale,
            float uMin,
            float uMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            int vertexStart = vertices.Count;
            Vector3[] sourceVertices = template.Vertices;
            for (int i = 0; i < sourceVertices.Length; i++)
            {
                Vector3 v = sourceVertices[i];
                vertices.Add(new Vector3(
                    origin.x + v.x * scale.x,
                    origin.y + v.y * scale.y,
                    origin.z + v.z * scale.z));
            }

            uvs.Add(new Vector2(uMin, vMin));
            uvs.Add(new Vector2(uMax, vMin));
            uvs.Add(new Vector2(uMin, vMax));
            uvs.Add(new Vector2(uMax, vMax));
            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);
            colors.Add(tint);

            int[] sourceTriangles = template.Triangles;
            for (int i = 0; i < sourceTriangles.Length; i++)
            {
                triangles.Add(vertexStart + sourceTriangles[i]);
            }
        }

        private static void AddClosedPlatformVolume(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            float worldX,
            float worldZ,
            float baseHeight,
            float topHeight,
            float sideUMin,
            float sideUMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            AddTemplate(
                vertices,
                uvs,
                triangles,
                colors,
                FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.Bottom),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(1f, 1f, 1f),
                sideUMin,
                sideUMax,
                vMin,
                vMax,
                tint);
            AddTemplate(
                vertices,
                uvs,
                triangles,
                colors,
                FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.NorthSide),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(1f, topHeight - baseHeight, 1f),
                sideUMin,
                sideUMax,
                vMin,
                vMax,
                tint);
            AddTemplate(
                vertices,
                uvs,
                triangles,
                colors,
                FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.EastSide),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(1f, topHeight - baseHeight, 1f),
                sideUMin,
                sideUMax,
                vMin,
                vMax,
                tint);
            AddTemplate(
                vertices,
                uvs,
                triangles,
                colors,
                FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.SouthSide),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(1f, topHeight - baseHeight, 1f),
                sideUMin,
                sideUMax,
                vMin,
                vMax,
                tint);
            AddTemplate(
                vertices,
                uvs,
                triangles,
                colors,
                FpsTerrainTemplateLibrary.Get(FpsTerrainTemplateKind.WestSide),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(1f, topHeight - baseHeight, 1f),
                sideUMin,
                sideUMax,
                vMin,
                vMax,
                tint);
        }

        private static void AddStairVolume(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            float worldX,
            float worldZ,
            int rampDir,
            int rampStepCount,
            float baseHeight,
            float lowHeight,
            float highHeight,
            float topUMin,
            float topUMax,
            float sideUMin,
            float sideUMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            int normalizedDir = (rampDir % 4 + 4) % 4;
            int stepCount = Mathf.Max(1, rampStepCount);
            float totalRise = Mathf.Max(0f, highHeight - lowHeight);
            float treadRise = stepCount > 1 ? totalRise / (stepCount - 1) : 0f;
            float treadDepth = 1f / stepCount;

            AddQuad(
                vertices,
                uvs,
                triangles,
                colors,
                new Vector3(worldX + 1f, baseHeight, worldZ),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                new Vector3(worldX, baseHeight, worldZ + 1f),
                sideUMin,
                sideUMax,
                vMin,
                vMax,
                tint);

            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                float start = treadDepth * stepIndex;
                float end = treadDepth * (stepIndex + 1);
                float treadHeight = stepCount > 1
                    ? lowHeight + treadRise * stepIndex
                    : highHeight;

                float stepUMin = Mathf.Lerp(topUMin, topUMax, start);
                float stepUMax = Mathf.Lerp(topUMin, topUMax, end);
                float stepVMin = Mathf.Lerp(vMin, vMax, start);
                float stepVMax = Mathf.Lerp(vMin, vMax, end);

                switch (normalizedDir)
                {
                    case 0:
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX, treadHeight, worldZ + start),
                            new Vector3(worldX + 1f, treadHeight, worldZ + start),
                            new Vector3(worldX, treadHeight, worldZ + end),
                            new Vector3(worldX + 1f, treadHeight, worldZ + end),
                            topUMin, topUMax, stepVMin, stepVMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX, baseHeight, worldZ + start),
                            new Vector3(worldX, baseHeight, worldZ + end),
                            new Vector3(worldX, treadHeight, worldZ + start),
                            new Vector3(worldX, treadHeight, worldZ + end),
                            sideUMin, sideUMax, stepVMin, stepVMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + 1f, baseHeight, worldZ + start),
                            new Vector3(worldX + 1f, treadHeight, worldZ + start),
                            new Vector3(worldX + 1f, baseHeight, worldZ + end),
                            new Vector3(worldX + 1f, treadHeight, worldZ + end),
                            sideUMin, sideUMax, stepVMin, stepVMax, tint);
                        break;
                    case 1:
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + start, treadHeight, worldZ),
                            new Vector3(worldX + end, treadHeight, worldZ),
                            new Vector3(worldX + start, treadHeight, worldZ + 1f),
                            new Vector3(worldX + end, treadHeight, worldZ + 1f),
                            stepUMin, stepUMax, vMin, vMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + start, baseHeight, worldZ),
                            new Vector3(worldX + end, baseHeight, worldZ),
                            new Vector3(worldX + start, treadHeight, worldZ),
                            new Vector3(worldX + end, treadHeight, worldZ),
                            stepUMin, stepUMax, vMin, vMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + end, baseHeight, worldZ + 1f),
                            new Vector3(worldX + start, baseHeight, worldZ + 1f),
                            new Vector3(worldX + end, treadHeight, worldZ + 1f),
                            new Vector3(worldX + start, treadHeight, worldZ + 1f),
                            stepUMin, stepUMax, vMin, vMax, tint);
                        break;
                    case 2:
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + 1f, treadHeight, worldZ + 1f - start),
                            new Vector3(worldX, treadHeight, worldZ + 1f - start),
                            new Vector3(worldX + 1f, treadHeight, worldZ + 1f - end),
                            new Vector3(worldX, treadHeight, worldZ + 1f - end),
                            topUMin, topUMax, stepVMin, stepVMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + 1f, baseHeight, worldZ + 1f - start),
                            new Vector3(worldX + 1f, treadHeight, worldZ + 1f - start),
                            new Vector3(worldX + 1f, baseHeight, worldZ + 1f - end),
                            new Vector3(worldX + 1f, treadHeight, worldZ + 1f - end),
                            sideUMin, sideUMax, stepVMin, stepVMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX, baseHeight, worldZ + 1f - end),
                            new Vector3(worldX, baseHeight, worldZ + 1f - start),
                            new Vector3(worldX, treadHeight, worldZ + 1f - end),
                            new Vector3(worldX, treadHeight, worldZ + 1f - start),
                            sideUMin, sideUMax, stepVMin, stepVMax, tint);
                        break;
                    default:
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + 1f - start, treadHeight, worldZ + 1f),
                            new Vector3(worldX + 1f - end, treadHeight, worldZ + 1f),
                            new Vector3(worldX + 1f - start, treadHeight, worldZ),
                            new Vector3(worldX + 1f - end, treadHeight, worldZ),
                            stepUMin, stepUMax, vMin, vMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + 1f - start, baseHeight, worldZ + 1f),
                            new Vector3(worldX + 1f - start, treadHeight, worldZ + 1f),
                            new Vector3(worldX + 1f - end, baseHeight, worldZ + 1f),
                            new Vector3(worldX + 1f - end, treadHeight, worldZ + 1f),
                            stepUMin, stepUMax, vMin, vMax, tint);
                        AddQuad(
                            vertices, uvs, triangles, colors,
                            new Vector3(worldX + 1f - end, baseHeight, worldZ),
                            new Vector3(worldX + 1f - start, baseHeight, worldZ),
                            new Vector3(worldX + 1f - end, treadHeight, worldZ),
                            new Vector3(worldX + 1f - start, treadHeight, worldZ),
                            stepUMin, stepUMax, vMin, vMax, tint);
                        break;
                }

                float previousHeight = stepIndex == 0
                    ? lowHeight
                    : lowHeight + treadRise * (stepIndex - 1);
                if (stepIndex > 0 && treadHeight > previousHeight + HeightEpsilon)
                {
                    AddStairRiser(
                        vertices,
                        uvs,
                        triangles,
                        colors,
                        worldX,
                        worldZ,
                        normalizedDir,
                        start,
                        previousHeight,
                        treadHeight,
                        sideUMin,
                        sideUMax,
                        vMin,
                        vMax,
                        tint);
                }
            }

            AddStairBoundary(
                vertices, uvs, triangles, colors,
                worldX, worldZ, normalizedDir, baseHeight, lowHeight, highHeight,
                sideUMin, sideUMax, vMin, vMax, tint);
        }

        private static void AddStairRiser(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            float worldX,
            float worldZ,
            int rampDir,
            float boundary,
            float lowerHeight,
            float upperHeight,
            float uMin,
            float uMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            switch (rampDir)
            {
                case 0:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX, lowerHeight, worldZ + boundary),
                        new Vector3(worldX + 1f, lowerHeight, worldZ + boundary),
                        new Vector3(worldX, upperHeight, worldZ + boundary),
                        new Vector3(worldX + 1f, upperHeight, worldZ + boundary),
                        uMin, uMax, vMin, vMax, tint);
                    break;
                case 1:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX + boundary, lowerHeight, worldZ + 1f),
                        new Vector3(worldX + boundary, lowerHeight, worldZ),
                        new Vector3(worldX + boundary, upperHeight, worldZ + 1f),
                        new Vector3(worldX + boundary, upperHeight, worldZ),
                        uMin, uMax, vMin, vMax, tint);
                    break;
                case 2:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX + 1f, lowerHeight, worldZ + 1f - boundary),
                        new Vector3(worldX, lowerHeight, worldZ + 1f - boundary),
                        new Vector3(worldX + 1f, upperHeight, worldZ + 1f - boundary),
                        new Vector3(worldX, upperHeight, worldZ + 1f - boundary),
                        uMin, uMax, vMin, vMax, tint);
                    break;
                default:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX + 1f - boundary, lowerHeight, worldZ),
                        new Vector3(worldX + 1f - boundary, lowerHeight, worldZ + 1f),
                        new Vector3(worldX + 1f - boundary, upperHeight, worldZ),
                        new Vector3(worldX + 1f - boundary, upperHeight, worldZ + 1f),
                        uMin, uMax, vMin, vMax, tint);
                    break;
            }
        }

        private static void AddStairBoundary(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            float worldX,
            float worldZ,
            int rampDir,
            float baseHeight,
            float lowHeight,
            float highHeight,
            float uMin,
            float uMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            switch (rampDir)
            {
                case 0:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX, baseHeight, worldZ),
                        new Vector3(worldX + 1f, baseHeight, worldZ),
                        new Vector3(worldX, lowHeight, worldZ),
                        new Vector3(worldX + 1f, lowHeight, worldZ),
                        uMin, uMax, vMin, vMax, tint);
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                        new Vector3(worldX, baseHeight, worldZ + 1f),
                        new Vector3(worldX + 1f, highHeight, worldZ + 1f),
                        new Vector3(worldX, highHeight, worldZ + 1f),
                        uMin, uMax, vMin, vMax, tint);
                    break;
                case 1:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX + 1f, baseHeight, worldZ),
                        new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                        new Vector3(worldX + 1f, lowHeight, worldZ),
                        new Vector3(worldX + 1f, lowHeight, worldZ + 1f),
                        uMin, uMax, vMin, vMax, tint);
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX, baseHeight, worldZ + 1f),
                        new Vector3(worldX, baseHeight, worldZ),
                        new Vector3(worldX, highHeight, worldZ + 1f),
                        new Vector3(worldX, highHeight, worldZ),
                        uMin, uMax, vMin, vMax, tint);
                    break;
                case 2:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                        new Vector3(worldX, baseHeight, worldZ + 1f),
                        new Vector3(worldX + 1f, lowHeight, worldZ + 1f),
                        new Vector3(worldX, lowHeight, worldZ + 1f),
                        uMin, uMax, vMin, vMax, tint);
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX, baseHeight, worldZ),
                        new Vector3(worldX + 1f, baseHeight, worldZ),
                        new Vector3(worldX, highHeight, worldZ),
                        new Vector3(worldX + 1f, highHeight, worldZ),
                        uMin, uMax, vMin, vMax, tint);
                    break;
                default:
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX, baseHeight, worldZ + 1f),
                        new Vector3(worldX, baseHeight, worldZ),
                        new Vector3(worldX, lowHeight, worldZ + 1f),
                        new Vector3(worldX, lowHeight, worldZ),
                        uMin, uMax, vMin, vMax, tint);
                    AddQuad(vertices, uvs, triangles, colors,
                        new Vector3(worldX + 1f, baseHeight, worldZ),
                        new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                        new Vector3(worldX + 1f, highHeight, worldZ),
                        new Vector3(worldX + 1f, highHeight, worldZ + 1f),
                        uMin, uMax, vMin, vMax, tint);
                    break;
            }
        }

        private static void AddPillar(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            float centerX,
            float centerZ,
            float baseHeight,
            float topHeight,
            float uMin,
            float uMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            const float halfSize = 0.09f;
            float minX = centerX - halfSize;
            float maxX = centerX + halfSize;
            float minZ = centerZ - halfSize;
            float maxZ = centerZ + halfSize;

            AddQuad(vertices, uvs, triangles, colors, new Vector3(minX, baseHeight, minZ), new Vector3(maxX, baseHeight, minZ), new Vector3(minX, topHeight, minZ), new Vector3(maxX, topHeight, minZ), uMin, uMax, vMin, vMax, tint);
            AddQuad(vertices, uvs, triangles, colors, new Vector3(maxX, baseHeight, minZ), new Vector3(maxX, baseHeight, maxZ), new Vector3(maxX, topHeight, minZ), new Vector3(maxX, topHeight, maxZ), uMin, uMax, vMin, vMax, tint);
            AddQuad(vertices, uvs, triangles, colors, new Vector3(maxX, baseHeight, maxZ), new Vector3(minX, baseHeight, maxZ), new Vector3(maxX, topHeight, maxZ), new Vector3(minX, topHeight, maxZ), uMin, uMax, vMin, vMax, tint);
            AddQuad(vertices, uvs, triangles, colors, new Vector3(minX, baseHeight, maxZ), new Vector3(minX, baseHeight, minZ), new Vector3(minX, topHeight, maxZ), new Vector3(minX, topHeight, minZ), uMin, uMax, vMin, vMax, tint);
        }
    }
}
