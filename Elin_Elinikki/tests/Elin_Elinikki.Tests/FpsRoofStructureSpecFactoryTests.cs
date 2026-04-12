using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsRoofStructureSpecFactoryTests
    {
        [Fact]
        public void CreateFlat_ExpandsByEaveDepthAndUsesSurfaceY()
        {
            FpsRoofStructureLayoutInput input = new FpsRoofStructureLayoutInput(
                FpsRoofStructureLayoutKind.Flat,
                reverse: false,
                minX: 1f,
                maxX: 5f,
                minZ: 10f,
                maxZ: 14f,
                baseY: 3f,
                topY: 6.5f,
                minorStart: 10f,
                flatStart: 0,
                flatEnd: 0,
                hasFlatBand: false);

            FpsRoofStructureSpec spec = FpsRoofStructureSpecFactory.Create(input, eaveDepth: 0.2f, thickness: 0.35f, ridgeCapWidth: 0.18f);

            Assert.Equal(FpsRoofStructureKind.Flat, spec.Kind);
            Assert.Equal(6.5f, spec.TopY, 3);
            Assert.Equal(6.15f, spec.BaseY, 3);
            Assert.Equal(0.2f, spec.EaveDepth, 3);
        }

        [Fact]
        public void CreateRidge_WithFlatBand_UsesBandExtentsAsRidgeSpan()
        {
            FpsRoofStructureLayoutInput input = new FpsRoofStructureLayoutInput(
                FpsRoofStructureLayoutKind.Ridge,
                reverse: false,
                minX: 0f,
                maxX: 6f,
                minZ: 4f,
                maxZ: 10f,
                baseY: 2f,
                topY: 5f,
                minorStart: 4f,
                flatStart: 2,
                flatEnd: 4,
                hasFlatBand: true);

            FpsRoofStructureSpec spec = FpsRoofStructureSpecFactory.Create(input, eaveDepth: 0.1f, thickness: 0.25f, ridgeCapWidth: 0.2f);

            Assert.Equal(FpsRoofStructureKind.Ridge, spec.Kind);
            Assert.Equal(6f, spec.RidgeStart, 3);
            Assert.Equal(8f, spec.RidgeEnd, 3);
            Assert.False(spec.Reverse);
        }

        [Fact]
        public void CreateRidge_WithoutFlatBand_CollapsesRidgeToCenterLine()
        {
            FpsRoofStructureLayoutInput input = new FpsRoofStructureLayoutInput(
                FpsRoofStructureLayoutKind.Ridge,
                reverse: true,
                minX: 2f,
                maxX: 8f,
                minZ: 5f,
                maxZ: 9f,
                baseY: 1f,
                topY: 4f,
                minorStart: 2f,
                flatStart: 3,
                flatEnd: 3,
                hasFlatBand: false);

            FpsRoofStructureSpec spec = FpsRoofStructureSpecFactory.Create(input, eaveDepth: 0.15f, thickness: 0.3f, ridgeCapWidth: 0.16f);

            Assert.True(spec.Reverse);
            Assert.Equal(5f, spec.RidgeStart, 3);
            Assert.Equal(5f, spec.RidgeEnd, 3);
        }
    }
}
