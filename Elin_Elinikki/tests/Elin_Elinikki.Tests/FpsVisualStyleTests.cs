using UnityEngine;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsVisualStyleTests
    {
        [Fact]
        public void ApplySurfaceShading_DarkensTerrainSidesRelativeToTerrainTop()
        {
            Color baseColor = new Color(0.8f, 0.7f, 0.6f, 1f);

            Color top = FpsVisualStyle.ApplySurfaceShading(baseColor, FpsVisualSurfaceKind.TerrainTop, 0, false, 0.12f, 0.16f);
            Color side = FpsVisualStyle.ApplySurfaceShading(baseColor, FpsVisualSurfaceKind.TerrainSide, 0, false, 0.12f, 0.16f);

            Assert.True(side.grayscale < top.grayscale);
        }

        [Fact]
        public void ApplySurfaceShading_DarkensIndoorCeiling()
        {
            Color baseColor = new Color(0.75f, 0.72f, 0.68f, 1f);

            Color outdoor = FpsVisualStyle.ApplySurfaceShading(baseColor, FpsVisualSurfaceKind.Ceiling, 0, false, 0.12f, 0.16f);
            Color indoor = FpsVisualStyle.ApplySurfaceShading(baseColor, FpsVisualSurfaceKind.Ceiling, 0, true, 0.12f, 0.16f);

            Assert.True(indoor.grayscale < outdoor.grayscale);
        }

        [Fact]
        public void GradeFogColor_ShiftsTerrainFogTowardCoolerPalette()
        {
            Color fog = new Color(0.6f, 0.6f, 0.6f, 1f);

            Color graded = FpsVisualStyle.GradeFogColor(fog, "terrain-floor", 0.2f);

            Assert.True(graded.b > graded.r);
        }
    }
}
