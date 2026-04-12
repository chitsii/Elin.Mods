using UnityEngine;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsGpuFaceGeometryBuildersTests
    {
        [Fact]
        public void BuildSideQuad_NorthFace_HasForwardNormalTowardNegativeZ()
        {
            FpsGpuFaceQuad quad = FpsGpuBlockGeometryBuilder.BuildSideQuad(0, 0, 0f, 1f, 0);
            Vector3 normal = ComputeNormal(quad);
            Assert.True(normal.z < -0.99f);
        }

        [Fact]
        public void BuildSideQuad_EastFace_HasForwardNormalTowardPositiveX()
        {
            FpsGpuFaceQuad quad = FpsGpuBlockGeometryBuilder.BuildSideQuad(0, 0, 0f, 1f, 1);
            Vector3 normal = ComputeNormal(quad);
            Assert.True(normal.x > 0.99f);
        }

        [Fact]
        public void BuildEdgeQuad_SouthFace_HasForwardNormalTowardPositiveZ()
        {
            FpsGpuFaceQuad quad = FpsGpuRiserGeometryBuilder.BuildEdgeQuad(0, 0, 1f, 0f, FpsGpuTerrainEdge.South);
            Vector3 normal = ComputeNormal(quad);
            Assert.True(normal.z > 0.99f);
        }

        private static Vector3 ComputeNormal(FpsGpuFaceQuad quad)
        {
            return Vector3.Cross(quad.TopLeft - quad.BottomLeft, quad.BottomRight - quad.BottomLeft).normalized;
        }
    }
}
