using UnityEngine;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsSpritePlacementResolverTests
    {
        [Fact]
        public void ResolveBaseHeight_GivesTallObjectsHigherFallbackThanLooseItems()
        {
            Assert.True(
                FpsSpritePlacementResolver.ResolveBaseHeight(BillboardKind.TallObject)
                > FpsSpritePlacementResolver.ResolveBaseHeight(BillboardKind.LooseItem));
        }

        [Fact]
        public void ResolvePivotY_GivesCharactersHigherPivotThanTallObjects()
        {
            Assert.True(
                FpsSpritePlacementResolver.ResolvePivotY(BillboardKind.Chara)
                > FpsSpritePlacementResolver.ResolvePivotY(BillboardKind.TallObject));
        }

        [Fact]
        public void ApplyScatterOffset_ShiftsOnlyNonLooseFreePosSprites()
        {
            Vector2 shifted = FpsSpritePlacementResolver.ApplyScatterOffset(new Vector2(10f, 20f), true, 2f, -1f, BillboardKind.InstalledObject);
            Vector2 unchanged = FpsSpritePlacementResolver.ApplyScatterOffset(new Vector2(10f, 20f), true, 2f, -1f, BillboardKind.LooseItem);

            Assert.NotEqual(new Vector2(10f, 20f), shifted);
            Assert.Equal(new Vector2(10f, 20f), unchanged);
        }

        [Fact]
        public void ResolveGroundScatter_UsesFreePosOffsetsWhenPresent()
        {
            Vector2 scatter = FpsSpritePlacementResolver.ResolveGroundScatter(true, 4f, -5f, 0);

            Assert.Equal(0.08f, scatter.x, 3);
            Assert.Equal(-0.08f, scatter.y, 3);
        }

        [Fact]
        public void ResolveGroundScatter_IsDeterministicForStableHash()
        {
            Vector2 a = FpsSpritePlacementResolver.ResolveGroundScatter(false, 0f, 0f, 12345);
            Vector2 b = FpsSpritePlacementResolver.ResolveGroundScatter(false, 0f, 0f, 12345);

            Assert.Equal(a, b);
        }
    }
}
