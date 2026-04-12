using UnityEngine;

namespace Elin_Elinikki
{
    internal enum FpsTerrainTemplateKind
    {
        Top,
        Bottom,
        NorthSide,
        EastSide,
        SouthSide,
        WestSide
    }

    internal readonly struct FpsTerrainMeshTemplate
    {
        public FpsTerrainMeshTemplate(Vector3[] vertices, int[] triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }

        public int[] Triangles { get; }
    }

    internal static class FpsTerrainTemplateLibrary
    {
        private static readonly FpsTerrainMeshTemplate TopTemplate = new FpsTerrainMeshTemplate(
            new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f)
            },
            new[] { 0, 2, 1, 2, 3, 1 });

        private static readonly FpsTerrainMeshTemplate BottomTemplate = new FpsTerrainMeshTemplate(
            new[]
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 1f),
                new Vector3(0f, 0f, 1f)
            },
            new[] { 0, 2, 1, 2, 3, 1 });

        private static readonly FpsTerrainMeshTemplate NorthSideTemplate = new FpsTerrainMeshTemplate(
            new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 1f, 0f)
            },
            new[] { 0, 2, 1, 2, 3, 1 });

        private static readonly FpsTerrainMeshTemplate EastSideTemplate = new FpsTerrainMeshTemplate(
            new[]
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(1f, 0f, 1f),
                new Vector3(1f, 1f, 0f),
                new Vector3(1f, 1f, 1f)
            },
            new[] { 0, 2, 1, 2, 3, 1 });

        private static readonly FpsTerrainMeshTemplate SouthSideTemplate = new FpsTerrainMeshTemplate(
            new[]
            {
                new Vector3(1f, 0f, 1f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 1f, 1f),
                new Vector3(0f, 1f, 1f)
            },
            new[] { 0, 2, 1, 2, 3, 1 });

        private static readonly FpsTerrainMeshTemplate WestSideTemplate = new FpsTerrainMeshTemplate(
            new[]
            {
                new Vector3(0f, 0f, 1f),
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 1f, 1f),
                new Vector3(0f, 1f, 0f)
            },
            new[] { 0, 2, 1, 2, 3, 1 });

        public static FpsTerrainMeshTemplate Get(FpsTerrainTemplateKind kind)
        {
            return kind switch
            {
                FpsTerrainTemplateKind.Top => TopTemplate,
                FpsTerrainTemplateKind.Bottom => BottomTemplate,
                FpsTerrainTemplateKind.NorthSide => NorthSideTemplate,
                FpsTerrainTemplateKind.EastSide => EastSideTemplate,
                FpsTerrainTemplateKind.SouthSide => SouthSideTemplate,
                FpsTerrainTemplateKind.WestSide => WestSideTemplate,
                _ => TopTemplate
            };
        }
    }
}
