using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsVisualSupportResolverTests
    {
        [Fact]
        public void Resolve_ReturnsGameplaySupportWhenDisplayDoesNotScale()
        {
            float resolved = FpsVisualSupportResolver.Resolve(0.9f, 1.5f, 1.5f);

            Assert.Equal(0.9f, resolved, 3);
        }

        [Fact]
        public void Resolve_ScalesSupportByDisplayHeightRatio()
        {
            float resolved = FpsVisualSupportResolver.Resolve(0.9f, 1.5f, 3f);

            Assert.Equal(1.8f, resolved, 3);
        }

        [Fact]
        public void Resolve_FallsBackToGameplaySupportWhenDisplayHeightsAreInvalid()
        {
            float resolved = FpsVisualSupportResolver.Resolve(0.9f, 0f, 0f);

            Assert.Equal(0.9f, resolved, 3);
        }

        [Fact]
        public void Resolve_ClampsSupportRatioToDisplayHeight()
        {
            float resolved = FpsVisualSupportResolver.Resolve(2f, 1.5f, 3f);

            Assert.Equal(3f, resolved, 3);
        }
    }
}
