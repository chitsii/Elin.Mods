using UnityEngine;

namespace Elin_ElinFPSView
{
    internal static class FpsGpuRoofGeometryBuilder
    {
        public static FpsGpuFaceQuad BuildHorizontalQuad(float minX, float maxX, float minZ, float maxZ, float y)
        {
            return new FpsGpuFaceQuad(
                new Vector3(minX, y, minZ),
                new Vector3(maxX, y, minZ),
                new Vector3(minX, y, maxZ),
                new Vector3(maxX, y, maxZ));
        }

        public static FpsGpuFaceQuad BuildSlopeAlongZ(float minX, float maxX, float minZ, float maxZ, float yAtMinZ, float yAtMaxZ)
        {
            return new FpsGpuFaceQuad(
                new Vector3(minX, yAtMinZ, minZ),
                new Vector3(maxX, yAtMinZ, minZ),
                new Vector3(minX, yAtMaxZ, maxZ),
                new Vector3(maxX, yAtMaxZ, maxZ));
        }

        public static FpsGpuFaceQuad BuildSlopeAlongX(float minX, float maxX, float minZ, float maxZ, float yAtMinX, float yAtMaxX)
        {
            return new FpsGpuFaceQuad(
                new Vector3(minX, yAtMinX, minZ),
                new Vector3(maxX, yAtMaxX, minZ),
                new Vector3(minX, yAtMinX, maxZ),
                new Vector3(maxX, yAtMaxX, maxZ));
        }

        public static FpsGpuFaceQuad BuildVerticalEdgeAtX(float x, float minZ, float maxZ, float bottom, float top, bool facePositiveX)
        {
            return facePositiveX
                ? new FpsGpuFaceQuad(
                    new Vector3(x, bottom, minZ),
                    new Vector3(x, bottom, maxZ),
                    new Vector3(x, top, minZ),
                    new Vector3(x, top, maxZ))
                : new FpsGpuFaceQuad(
                    new Vector3(x, bottom, maxZ),
                    new Vector3(x, bottom, minZ),
                    new Vector3(x, top, maxZ),
                    new Vector3(x, top, minZ));
        }

        public static FpsGpuFaceQuad BuildVerticalEdgeAtZ(float z, float minX, float maxX, float bottom, float top, bool facePositiveZ)
        {
            return facePositiveZ
                ? new FpsGpuFaceQuad(
                    new Vector3(maxX, bottom, z),
                    new Vector3(minX, bottom, z),
                    new Vector3(maxX, top, z),
                    new Vector3(minX, top, z))
                : new FpsGpuFaceQuad(
                    new Vector3(minX, bottom, z),
                    new Vector3(maxX, bottom, z),
                    new Vector3(minX, top, z),
                    new Vector3(maxX, top, z));
        }

        public static FpsGpuFaceQuad BuildGableFaceAtX(float x, float minZ, float maxZ, float ridgeStart, float ridgeEnd, float bottom, float top, bool facePositiveX)
        {
            return facePositiveX
                ? new FpsGpuFaceQuad(
                    new Vector3(x, bottom, minZ),
                    new Vector3(x, bottom, maxZ),
                    new Vector3(x, top, ridgeStart),
                    new Vector3(x, top, ridgeEnd))
                : new FpsGpuFaceQuad(
                    new Vector3(x, bottom, maxZ),
                    new Vector3(x, bottom, minZ),
                    new Vector3(x, top, ridgeEnd),
                    new Vector3(x, top, ridgeStart));
        }

        public static FpsGpuFaceQuad BuildGableFaceAtZ(float z, float minX, float maxX, float ridgeStart, float ridgeEnd, float bottom, float top, bool facePositiveZ)
        {
            return facePositiveZ
                ? new FpsGpuFaceQuad(
                    new Vector3(maxX, bottom, z),
                    new Vector3(minX, bottom, z),
                    new Vector3(ridgeEnd, top, z),
                    new Vector3(ridgeStart, top, z))
                : new FpsGpuFaceQuad(
                    new Vector3(minX, bottom, z),
                    new Vector3(maxX, bottom, z),
                    new Vector3(ridgeStart, top, z),
                    new Vector3(ridgeEnd, top, z));
        }
    }
}
