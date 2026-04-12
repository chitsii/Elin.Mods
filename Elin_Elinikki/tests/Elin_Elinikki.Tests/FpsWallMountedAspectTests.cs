using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsWallMountedAspectTests
    {
        [Fact]
        public void ApplyHorizontalScale_CompressesWidth()
        {
            float width = FpsWallMountedAspect.ApplyHorizontalScale(0.5f, 0.72f);

            Assert.Equal(0.36f, width, 3);
        }

        [Fact]
        public void ApplyHorizontalScale_ClampsToMinimumVisibleWidth()
        {
            float width = FpsWallMountedAspect.ApplyHorizontalScale(0.1f, 0.2f);

            Assert.Equal(0.05f, width, 3);
        }
    }
}
