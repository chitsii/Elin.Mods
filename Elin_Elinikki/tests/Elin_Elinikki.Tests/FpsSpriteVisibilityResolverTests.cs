using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsSpriteVisibilityResolverTests
    {
        [Fact]
        public void ResolveCardVisibilityDistance_GivesInstalledObjectsLongerRangeThanLooseItems()
        {
            float installed = FpsSpriteVisibilityResolver.ResolveCardVisibilityDistance(BillboardKind.InstalledObject, 10f);
            float loose = FpsSpriteVisibilityResolver.ResolveCardVisibilityDistance(BillboardKind.LooseItem, 10f);

            Assert.True(installed > loose);
        }

        [Fact]
        public void ResolveCardVisibilityConeDot_GivesGameplaySpritesWiderConeThanLooseItems()
        {
            float gameplay = FpsSpriteVisibilityResolver.ResolveCardVisibilityConeDot(BillboardKind.Chara);
            float loose = FpsSpriteVisibilityResolver.ResolveCardVisibilityConeDot(BillboardKind.LooseItem);

            Assert.True(gameplay < loose);
        }

        [Fact]
        public void ResolveCellObjectVisibilityDistance_PrefersUprightObjects()
        {
            float upright = FpsSpriteVisibilityResolver.ResolveCellObjectVisibilityDistance(true, 10f);
            float ground = FpsSpriteVisibilityResolver.ResolveCellObjectVisibilityDistance(false, 10f);

            Assert.True(upright > ground);
        }

        [Fact]
        public void ResolveEffectVisibilityDistance_StaysBelowBaseDistance()
        {
            float effect = FpsSpriteVisibilityResolver.ResolveEffectVisibilityDistance(10f);

            Assert.True(effect < 10f);
        }
    }
}
