using UnityEngine;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsDistanceFogTests
    {
        [Fact]
        public void ComputeFogFactor_ReturnsZeroBeforeStart()
        {
            float factor = FpsDistanceFog.ComputeFogFactor(5f, 20f, 0.3f, 0.6f, 1.8f);
            Assert.Equal(0f, factor, 3);
        }

        [Fact]
        public void ComputeFogFactor_ReachesNearOpaqueByEnd()
        {
            float factor = FpsDistanceFog.ComputeFogFactor(12f, 20f, 0.3f, 0.6f, 1.8f);
            Assert.True(factor > 0.98f);
        }

        [Fact]
        public void BlendTowardClearColor_ReachesClearerColorAsFogRises()
        {
            Color haze = new Color(0.3f, 0.4f, 0.5f, 1f);
            Color clear = new Color(0.8f, 0.85f, 0.9f, 1f);
            Color blended = FpsDistanceFog.BlendTowardClearColor(haze, clear, 1f, 0.6f);

            Assert.True(blended.r > 0.75f);
            Assert.True(blended.g > 0.8f);
            Assert.True(blended.b > 0.85f);
        }
    }
}
