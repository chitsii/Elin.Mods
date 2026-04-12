using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_Elinikki
{
    internal readonly struct FpsTerrainChunkCell
    {
        public FpsTerrainChunkCell(int localX, int localZ, float height)
            : this(localX, localZ, height, new Color32(255, 255, 255, 255))
        {
        }

        public FpsTerrainChunkCell(int localX, int localZ, float height, Color32 tint)
            : this(localX, localZ, height, tint, false, 0f, false, 0f, false, 0f, false, 0f, true, false, 0, 0f, false, 0f, false, 0f)
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
            bool allowRisers)
            : this(localX, localZ, height, tint, hasNorthNeighbor, northNeighborHeight, hasEastNeighbor, eastNeighborHeight, hasSouthNeighbor, southNeighborHeight, hasWestNeighbor, westNeighborHeight, allowRisers, false, 0, 0f, false, 0f, false, 0f)
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
            bool allowRisers,
            bool hasRamp,
            int rampDir,
            float rampBaseHeight,
            bool hasBridgePillar,
            float bridgeBaseHeight,
            bool hasUndersideDeck,
            float undersideDeckBaseHeight)
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
            AllowRisers = allowRisers;
            HasRamp = hasRamp;
            RampDir = rampDir;
            RampBaseHeight = rampBaseHeight;
            HasBridgePillar = hasBridgePillar;
            BridgeBaseHeight = bridgeBaseHeight;
            HasUndersideDeck = hasUndersideDeck;
            UndersideDeckBaseHeight = undersideDeckBaseHeight;
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

        public bool AllowRisers { get; }

        public bool HasRamp { get; }

        public int RampDir { get; }

        public float RampBaseHeight { get; }

        public bool HasBridgePillar { get; }

        public float BridgeBaseHeight { get; }

        public bool HasUndersideDeck { get; }

        public float UndersideDeckBaseHeight { get; }
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
            float uvStepX = 1f / (chunkSize * 2f);
            float uvStepY = 1f / chunkSize;

            for (int i = 0; i < cells.Count; i++)
            {
                FpsTerrainChunkCell cell = cells[i];
                float worldX = chunkOriginX + cell.LocalX;
                float worldZ = chunkOriginZ + cell.LocalZ;
                float y = cell.Height;
                float topUMin = (cell.LocalX * 2) * uvStepX;
                float topUMax = (cell.LocalX * 2 + 1) * uvStepX;
                float sideUMin = (cell.LocalX * 2 + 1) * uvStepX;
                float sideUMax = (cell.LocalX * 2 + 2) * uvStepX;
                float vMin = cell.LocalZ * uvStepY;
                float vMax = (cell.LocalZ + 1) * uvStepY;

                if (cell.HasRamp)
                {
                    AddRampTop(vertices, uvs, triangles, colors, worldX, worldZ, cell.RampDir, cell.RampBaseHeight, y, topUMin, topUMax, vMin, vMax, cell.Tint);
                    AddRampVolume(vertices, uvs, triangles, colors, worldX, worldZ, cell.RampDir, cell.UndersideDeckBaseHeight, cell.RampBaseHeight, y, sideUMin, sideUMax, vMin, vMax, cell.Tint);
                }
                else
                {
                    AddQuad(
                        vertices,
                        uvs,
                        triangles,
                        colors,
                        new Vector3(worldX, y, worldZ),
                        new Vector3(worldX + 1f, y, worldZ),
                        new Vector3(worldX, y, worldZ + 1f),
                        new Vector3(worldX + 1f, y, worldZ + 1f),
                        topUMin,
                        topUMax,
                        vMin,
                        vMax,
                        cell.Tint);

                    if (cell.HasUndersideDeck && y > cell.UndersideDeckBaseHeight + HeightEpsilon)
                    {
                        AddQuad(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            new Vector3(worldX + 1f, cell.UndersideDeckBaseHeight, worldZ),
                            new Vector3(worldX, cell.UndersideDeckBaseHeight, worldZ),
                            new Vector3(worldX + 1f, cell.UndersideDeckBaseHeight, worldZ + 1f),
                            new Vector3(worldX, cell.UndersideDeckBaseHeight, worldZ + 1f),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }
                }

                if (cell.AllowRisers)
                {
                    if (cell.HasNorthNeighbor && cell.Height > cell.NorthNeighborHeight + HeightEpsilon)
                    {
                        AddQuad(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            new Vector3(worldX, cell.NorthNeighborHeight, worldZ),
                            new Vector3(worldX + 1f, cell.NorthNeighborHeight, worldZ),
                            new Vector3(worldX, y, worldZ),
                            new Vector3(worldX + 1f, y, worldZ),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }

                    if (cell.HasEastNeighbor && cell.Height > cell.EastNeighborHeight + HeightEpsilon)
                    {
                        AddQuad(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            new Vector3(worldX + 1f, cell.EastNeighborHeight, worldZ),
                            new Vector3(worldX + 1f, cell.EastNeighborHeight, worldZ + 1f),
                            new Vector3(worldX + 1f, y, worldZ),
                            new Vector3(worldX + 1f, y, worldZ + 1f),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }

                    if (cell.HasSouthNeighbor && cell.Height > cell.SouthNeighborHeight + HeightEpsilon)
                    {
                        AddQuad(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            new Vector3(worldX + 1f, cell.SouthNeighborHeight, worldZ + 1f),
                            new Vector3(worldX, cell.SouthNeighborHeight, worldZ + 1f),
                            new Vector3(worldX + 1f, y, worldZ + 1f),
                            new Vector3(worldX, y, worldZ + 1f),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }

                    if (cell.HasWestNeighbor && cell.Height > cell.WestNeighborHeight + HeightEpsilon)
                    {
                        AddQuad(
                            vertices,
                            uvs,
                            triangles,
                            colors,
                            new Vector3(worldX, cell.WestNeighborHeight, worldZ + 1f),
                            new Vector3(worldX, cell.WestNeighborHeight, worldZ),
                            new Vector3(worldX, y, worldZ + 1f),
                            new Vector3(worldX, y, worldZ),
                            sideUMin,
                            sideUMax,
                            vMin,
                            vMax,
                            cell.Tint);
                    }
                }

                if (cell.HasBridgePillar && y > cell.BridgeBaseHeight + HeightEpsilon)
                {
                    AddPillar(vertices, uvs, triangles, colors, worldX + 0.5f, worldZ + 0.5f, cell.BridgeBaseHeight, y, sideUMin, sideUMax, vMin, vMax, cell.Tint);
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

        private static void AddRampTop(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            List<Color32> colors,
            float worldX,
            float worldZ,
            int rampDir,
            float lowHeight,
            float highHeight,
            float uMin,
            float uMax,
            float vMin,
            float vMax,
            Color32 tint)
        {
            float northY = highHeight;
            float eastY = highHeight;
            float southY = highHeight;
            float westY = highHeight;
            switch ((rampDir % 4 + 4) % 4)
            {
                case 0:
                    northY = lowHeight;
                    break;
                case 1:
                    eastY = lowHeight;
                    break;
                case 2:
                    southY = lowHeight;
                    break;
                case 3:
                    westY = lowHeight;
                    break;
            }

            AddQuad(
                vertices,
                uvs,
                triangles,
                colors,
                new Vector3(worldX, northY, worldZ),
                new Vector3(worldX + 1f, eastY, worldZ),
                new Vector3(worldX, westY, worldZ + 1f),
                new Vector3(worldX + 1f, southY, worldZ + 1f),
                uMin,
                uMax,
                vMin,
                vMax,
                tint);
        }

        private static void AddRampVolume(
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
            AddQuad(
                vertices,
                uvs,
                triangles,
                colors,
                new Vector3(worldX + 1f, baseHeight, worldZ),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                new Vector3(worldX, baseHeight, worldZ + 1f),
                uMin,
                uMax,
                vMin,
                vMax,
                tint);

            float northLeft = highHeight;
            float northRight = highHeight;
            float southLeft = highHeight;
            float southRight = highHeight;
            switch ((rampDir % 4 + 4) % 4)
            {
                case 0:
                    northLeft = lowHeight;
                    northRight = lowHeight;
                    break;
                case 1:
                    northRight = lowHeight;
                    southRight = lowHeight;
                    break;
                case 2:
                    southLeft = lowHeight;
                    southRight = lowHeight;
                    break;
                case 3:
                    northLeft = lowHeight;
                    southLeft = lowHeight;
                    break;
            }

            AddQuad(
                vertices,
                uvs,
                triangles,
                colors,
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(worldX + 1f, baseHeight, worldZ),
                new Vector3(worldX, northLeft, worldZ),
                new Vector3(worldX + 1f, northRight, worldZ),
                uMin,
                uMax,
                vMin,
                vMax,
                tint);
            AddQuad(
                vertices,
                uvs,
                triangles,
                colors,
                new Vector3(worldX + 1f, baseHeight, worldZ),
                new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                new Vector3(worldX + 1f, northRight, worldZ),
                new Vector3(worldX + 1f, southRight, worldZ + 1f),
                uMin,
                uMax,
                vMin,
                vMax,
                tint);
            AddQuad(
                vertices,
                uvs,
                triangles,
                colors,
                new Vector3(worldX + 1f, baseHeight, worldZ + 1f),
                new Vector3(worldX, baseHeight, worldZ + 1f),
                new Vector3(worldX + 1f, southRight, worldZ + 1f),
                new Vector3(worldX, southLeft, worldZ + 1f),
                uMin,
                uMax,
                vMin,
                vMax,
                tint);
            AddQuad(
                vertices,
                uvs,
                triangles,
                colors,
                new Vector3(worldX, baseHeight, worldZ + 1f),
                new Vector3(worldX, baseHeight, worldZ),
                new Vector3(worldX, southLeft, worldZ + 1f),
                new Vector3(worldX, northLeft, worldZ),
                uMin,
                uMax,
                vMin,
                vMax,
                tint);
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
