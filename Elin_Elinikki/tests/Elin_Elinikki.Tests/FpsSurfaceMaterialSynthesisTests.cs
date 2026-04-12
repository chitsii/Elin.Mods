using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsSurfaceMaterialSynthesisTests
    {
        [Fact]
        public void BuildFrontMaterial_SynthesizesOpaqueTileableTextureFromProjectedSource()
        {
            FpsSurfaceColor32[] source =
            {
                new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(0, 0, 0, 0),
                new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(140, 90, 50, 255), new FpsSurfaceColor32(90, 60, 35, 255), new FpsSurfaceColor32(0, 0, 0, 0),
                new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(150, 100, 55, 255), new FpsSurfaceColor32(100, 70, 40, 255), new FpsSurfaceColor32(0, 0, 0, 0),
                new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(0, 0, 0, 0)
            };

            FpsSurfaceColor32[] front = FpsSurfaceMaterialSynthesis.BuildFrontMaterial(source, 4, 4, 8, 8);

            Assert.Equal(64, front.Length);
            Assert.All(front, color => Assert.Equal((byte)255, color.A));
            Assert.Equal(front[0].R, front[7].R);
            Assert.Equal(front[0].G, front[56].G);
        }

        [Fact]
        public void BuildSideMaterial_PreservesAlphaAndDarkensOpaquePixels()
        {
            FpsSurfaceColor32[] source =
            {
                new FpsSurfaceColor32(200, 100, 50, 255), new FpsSurfaceColor32(0, 0, 0, 0),
                new FpsSurfaceColor32(120, 80, 40, 255), new FpsSurfaceColor32(60, 40, 20, 255)
            };

            FpsSurfaceColor32[] side = FpsSurfaceMaterialSynthesis.BuildSideMaterial(source, 2, 2);

            Assert.Equal((byte)255, side[0].A);
            Assert.Equal((byte)255, side[1].A);
            Assert.True(side[0].R <= source[0].R);
            Assert.Equal(side[0].R, side[1].R);
        }

        [Fact]
        public void BuildTopMaterial_UsesAverageOfTopOpaqueBand()
        {
            FpsSurfaceColor32[] source =
            {
                new FpsSurfaceColor32(240, 40, 40, 255), new FpsSurfaceColor32(240, 40, 40, 255),
                new FpsSurfaceColor32(20, 20, 220, 255), new FpsSurfaceColor32(20, 20, 220, 255),
                new FpsSurfaceColor32(10, 10, 10, 0),    new FpsSurfaceColor32(10, 10, 10, 0)
            };

            FpsSurfaceColor32[] top = FpsSurfaceMaterialSynthesis.BuildTopMaterial(source, 2, 3);

            Assert.Equal((byte)255, top[0].A);
            Assert.True(top[0].R > top[0].B);
            Assert.True(top[1].R > top[1].B);
            Assert.Equal(top[0].R, top[1].R);
        }

        [Fact]
        public void BuildTopMaterial_FallsBackToAverageOpaqueColorWhenNoTopBandExists()
        {
            FpsSurfaceColor32[] source =
            {
                new FpsSurfaceColor32(0, 0, 0, 0), new FpsSurfaceColor32(0, 0, 0, 0),
                new FpsSurfaceColor32(50, 100, 150, 255), new FpsSurfaceColor32(50, 100, 150, 255)
            };

            FpsSurfaceColor32[] top = FpsSurfaceMaterialSynthesis.BuildTopMaterial(source, 2, 2);

            Assert.InRange(top[0].R, (byte)48, (byte)52);
            Assert.InRange(top[0].G, (byte)96, (byte)104);
            Assert.InRange(top[0].B, (byte)144, (byte)156);
            Assert.Equal((byte)255, top[0].A);
        }
    }
}
