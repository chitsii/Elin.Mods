using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsIndoorCeilingResolverTests
    {
        [Fact]
        public void Resolve_StaysJustBelowWallTopWhenWallsExist()
        {
            float result = FpsIndoorCeilingResolver.Resolve(2f, true, 3f, 1.1f);

            Assert.Equal(2.98f, result, 3);
        }

        [Fact]
        public void Resolve_RespectsMinimumGapAboveSurfaceWhenWallTopIsTooLow()
        {
            float result = FpsIndoorCeilingResolver.Resolve(2f, true, 2.1f, 1.1f);

            Assert.Equal(2.4f, result, 3);
        }

        [Fact]
        public void Resolve_UsesConfiguredFallbackWhenNoWallTopExists()
        {
            float result = FpsIndoorCeilingResolver.Resolve(2f, false, 0f, 1.1f);

            Assert.Equal(3.1f, result, 3);
        }
    }
}
