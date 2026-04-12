using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsWallTextureUvTests
    {
        [Fact]
        public void ComputeUvRect_FrontFaceRepeatsAcrossRunLengthAndHeight()
        {
            FpsWallStructureQuad quad = new FpsWallStructureQuad(
                FpsWallStructureQuadKind.SideFace,
                new FpsStructurePoint3(2f, 1f, 10f),
                new FpsStructurePoint3(6f, 1f, 10f),
                new FpsStructurePoint3(2f, 4f, 10f),
                new FpsStructurePoint3(6f, 4f, 10f));

            var rect = FpsWallTextureUv.ComputeUvRect(quad);

            Assert.Equal(4f, rect.Width, 3);
            Assert.Equal(3f, rect.Height, 3);
        }

        [Fact]
        public void ComputeUvRect_EndCapKeepsThinFacesAtSingleRepeat()
        {
            FpsWallStructureQuad quad = new FpsWallStructureQuad(
                FpsWallStructureQuadKind.EndCap,
                new FpsStructurePoint3(2f, 1f, 9.85f),
                new FpsStructurePoint3(2f, 1f, 10.15f),
                new FpsStructurePoint3(2f, 4f, 9.85f),
                new FpsStructurePoint3(2f, 4f, 10.15f));

            var rect = FpsWallTextureUv.ComputeUvRect(quad);

            Assert.Equal(1f, rect.Width, 3);
            Assert.Equal(3f, rect.Height, 3);
        }

        [Fact]
        public void ComputeUvRect_TopCapRepeatsAcrossRunButNotAcrossThinThickness()
        {
            FpsWallStructureQuad quad = new FpsWallStructureQuad(
                FpsWallStructureQuadKind.TopCap,
                new FpsStructurePoint3(2f, 4f, 9.85f),
                new FpsStructurePoint3(6f, 4f, 9.85f),
                new FpsStructurePoint3(2f, 4f, 10.15f),
                new FpsStructurePoint3(6f, 4f, 10.15f));

            var rect = FpsWallTextureUv.ComputeUvRect(quad);

            Assert.Equal(4f, rect.Width, 3);
            Assert.Equal(1f, rect.Height, 3);
        }
    }
}
