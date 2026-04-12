using UnityEngine;

namespace Elin_Elinikki
{
    internal readonly struct FpsGpuFaceQuad
    {
        public readonly Vector3 BottomLeft;
        public readonly Vector3 BottomRight;
        public readonly Vector3 TopLeft;
        public readonly Vector3 TopRight;

        public FpsGpuFaceQuad(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight)
        {
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            TopLeft = topLeft;
            TopRight = topRight;
        }

        public Vector3 Center => (BottomLeft + BottomRight + TopLeft + TopRight) * 0.25f;
    }

    internal enum FpsGpuTerrainEdge
    {
        East,
        South,
        West,
        North
    }

    internal static class FpsGpuBlockGeometryBuilder
    {
        public static FpsGpuFaceQuad BuildSideQuad(int cellX, int cellZ, float bottom, float top, int dir)
        {
            switch (NormalizeDir(dir))
            {
                case 0:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX, bottom, cellZ),
                        new Vector3(cellX + 1f, bottom, cellZ),
                        new Vector3(cellX, top, cellZ),
                        new Vector3(cellX + 1f, top, cellZ));
                case 1:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + 1f, bottom, cellZ),
                        new Vector3(cellX + 1f, bottom, cellZ + 1f),
                        new Vector3(cellX + 1f, top, cellZ),
                        new Vector3(cellX + 1f, top, cellZ + 1f));
                case 2:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + 1f, bottom, cellZ + 1f),
                        new Vector3(cellX, bottom, cellZ + 1f),
                        new Vector3(cellX + 1f, top, cellZ + 1f),
                        new Vector3(cellX, top, cellZ + 1f));
                default:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX, bottom, cellZ + 1f),
                        new Vector3(cellX, bottom, cellZ),
                        new Vector3(cellX, top, cellZ + 1f),
                        new Vector3(cellX, top, cellZ));
            }
        }

        private static int NormalizeDir(int dir)
        {
            int normalized = dir % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }
    }

    internal static class FpsGpuRiserGeometryBuilder
    {
        public static FpsGpuFaceQuad BuildEdgeQuad(int cellX, int cellZ, float topHeight, float bottomHeight, FpsGpuTerrainEdge edge)
        {
            switch (edge)
            {
                case FpsGpuTerrainEdge.East:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + 1f, bottomHeight, cellZ),
                        new Vector3(cellX + 1f, bottomHeight, cellZ + 1f),
                        new Vector3(cellX + 1f, topHeight, cellZ),
                        new Vector3(cellX + 1f, topHeight, cellZ + 1f));
                case FpsGpuTerrainEdge.South:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX + 1f, bottomHeight, cellZ + 1f),
                        new Vector3(cellX, bottomHeight, cellZ + 1f),
                        new Vector3(cellX + 1f, topHeight, cellZ + 1f),
                        new Vector3(cellX, topHeight, cellZ + 1f));
                case FpsGpuTerrainEdge.West:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX, bottomHeight, cellZ + 1f),
                        new Vector3(cellX, bottomHeight, cellZ),
                        new Vector3(cellX, topHeight, cellZ + 1f),
                        new Vector3(cellX, topHeight, cellZ));
                default:
                    return new FpsGpuFaceQuad(
                        new Vector3(cellX, bottomHeight, cellZ),
                        new Vector3(cellX + 1f, bottomHeight, cellZ),
                        new Vector3(cellX, topHeight, cellZ),
                        new Vector3(cellX + 1f, topHeight, cellZ));
            }
        }
    }
}
