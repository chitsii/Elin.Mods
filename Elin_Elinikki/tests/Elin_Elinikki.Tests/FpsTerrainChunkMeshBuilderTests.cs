using UnityEngine;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsTerrainChunkMeshBuilderTests
    {
        [Fact]
        public void Build_ProducesOneQuadPerCell()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                32,
                48,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(0, 0, 0.1f),
                    new FpsTerrainChunkCell(1, 0, 0.2f)
                });

            Assert.Equal(8, mesh.Vertices.Length);
            Assert.Equal(8, mesh.Uvs.Length);
            Assert.Equal(12, mesh.Triangles.Length);
        }

        [Fact]
        public void Build_PlacesVerticesAtWorldCellBounds()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                10,
                20,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(2, 3, 1.25f)
                });

            Assert.Equal(new Vector3(12f, 1.25f, 23f), mesh.Vertices[0]);
            Assert.Equal(new Vector3(13f, 1.25f, 24f), mesh.Vertices[3]);
        }

        [Fact]
        public void Build_MapsUvsToChunkTileRect()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(2, 5, 0f)
                });

            Assert.True(Mathf.Approximately(mesh.Uvs[0].x, 0.25f));
            Assert.True(Mathf.Approximately(mesh.Uvs[0].y, 0.625f));
            Assert.True(Mathf.Approximately(mesh.Uvs[3].x, 0.29166666f));
            Assert.True(Mathf.Approximately(mesh.Uvs[3].y, 0.75f));
        }

        [Fact]
        public void Build_CopiesTintToAllVerticesOfTheCell()
        {
            Color32 tint = new Color32(10, 20, 30, 255);
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(0, 0, 0f, tint)
                });

            Assert.Equal(4, mesh.Colors.Length);
            Assert.Equal(tint, mesh.Colors[0]);
            Assert.Equal(tint, mesh.Colors[3]);
        }

        [Fact]
        public void Build_AddsRiserQuadWhenNeighborIsLower()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        true, 0f,
                        false, 0f,
                        false, 0f,
                        true)
                });

            Assert.Equal(8, mesh.Vertices.Length);
            Assert.Equal(12, mesh.Triangles.Length);
            Assert.Contains(mesh.Vertices, v => v == new Vector3(1f, 0f, 0f));
            Assert.Contains(mesh.Vertices, v => v == new Vector3(1f, 1f, 1f));
        }

        [Fact]
        public void Build_DoesNotAddRiserQuadWhenNeighborHeightMatches()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        true, 1f,
                        false, 0f,
                        false, 0f,
                        true)
                });

            Assert.Equal(4, mesh.Vertices.Length);
            Assert.Equal(6, mesh.Triangles.Length);
        }

        [Fact]
        public void Build_UsesSteppedTopForRampCells()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        true,
                        true,
                        0,
                        3,
                        0.6f,
                        false,
                        0f,
                        true,
                        0.2f)
                });

            Assert.Contains(mesh.Vertices, v => Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0.6f) && Mathf.Approximately(v.z, 0f));
            Assert.Contains(mesh.Vertices, v => Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0.8f) && Mathf.Approximately(v.z, 0.33333334f));
            Assert.Contains(mesh.Vertices, v => Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 1f) && Mathf.Approximately(v.z, 0.6666667f));
        }

        [Fact]
        public void Build_AddsBridgePillarColumn()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        true,
                        false,
                        0,
                        0,
                        0f,
                        true,
                        0.1f,
                        false,
                        0f)
                });

            Assert.True(mesh.Vertices.Length > 4);
            Assert.Contains(mesh.Vertices, v => Mathf.Approximately(v.x, 0.41f) && Mathf.Approximately(v.y, 0.1f));
            Assert.Contains(mesh.Vertices, v => Mathf.Approximately(v.x, 0.59f) && Mathf.Approximately(v.y, 1f));
        }

        [Fact]
        public void Build_TopQuadUsesUpwardFacingWinding()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(0, 0, 1f)
                });

            Vector3 normal = ComputeTriangleNormal(mesh, 0);
            Assert.True(normal.y > 0.99f);
        }

        [Fact]
        public void Build_EastRiserUsesOutwardFacingWinding()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        true, 0f,
                        false, 0f,
                        false, 0f,
                        true)
                });

            Vector3 normal = ComputeTriangleNormal(mesh, 1);
            Assert.True(normal.x > 0.99f);
        }

        [Fact]
        public void Build_AddsUndersideDeckForElevatedCells()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        true,
                        false,
                        0,
                        0,
                        0f,
                        false,
                        0f,
                        true,
                        0.25f)
                });

            Assert.Contains(mesh.Vertices, v => v == new Vector3(0f, 0.25f, 0f));
            Assert.Contains(mesh.Vertices, v => v == new Vector3(1f, 0.25f, 1f));
        }

        [Fact]
        public void Build_UsesBaseHeightForRampUnderside()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        true,
                        true,
                        1,
                        3,
                        0.6f,
                        false,
                        0f,
                        true,
                        0.15f)
                });

            Assert.Contains(mesh.Vertices, v => v == new Vector3(0f, 0.15f, 0f));
            Assert.Contains(mesh.Vertices, v => v == new Vector3(1f, 0.15f, 1f));
        }

        [Fact]
        public void Build_AddsClosedStepSidesForRampCells()
        {
            FpsTerrainChunkMeshData mesh = FpsTerrainChunkMeshBuilder.Build(
                0,
                0,
                8,
                new[]
                {
                    new FpsTerrainChunkCell(
                        0,
                        0,
                        1f,
                        new Color32(255, 255, 255, 255),
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        false, 0f,
                        true,
                        true,
                        0,
                        3,
                        0.6f,
                        false,
                        0f,
                        true,
                        0.15f)
                });

            Assert.Contains(mesh.Vertices, v => Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0.15f) && Mathf.Approximately(v.z, 0.33333334f));
            Assert.Contains(mesh.Vertices, v => Mathf.Approximately(v.x, 0f) && Mathf.Approximately(v.y, 0.8f) && Mathf.Approximately(v.z, 0.33333334f));
        }

        private static Vector3 ComputeTriangleNormal(FpsTerrainChunkMeshData mesh, int quadIndex)
        {
            int triangleStart = quadIndex * 6;
            Vector3 v0 = mesh.Vertices[mesh.Triangles[triangleStart + 0]];
            Vector3 v1 = mesh.Vertices[mesh.Triangles[triangleStart + 1]];
            Vector3 v2 = mesh.Vertices[mesh.Triangles[triangleStart + 2]];
            return Vector3.Cross(v1 - v0, v2 - v0).normalized;
        }
    }
}
