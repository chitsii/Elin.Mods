using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsDoorFacingResolverTests
    {
        [Theory]
        [InlineData(0, false, 0)]
        [InlineData(0, true, 1)]
        [InlineData(1, false, 1)]
        [InlineData(1, true, 0)]
        [InlineData(2, false, 0)]
        [InlineData(2, true, 1)]
        [InlineData(-1, false, 0)]
        [InlineData(-1, true, 1)]
        public void ResolveFaceDir_UsesWallAxisAndOpenState(int blockDir, bool isOpen, int expected)
        {
            Assert.Equal(expected, FpsDoorFacingResolver.ResolveFaceDir(blockDir, isOpen));
        }
    }
}
