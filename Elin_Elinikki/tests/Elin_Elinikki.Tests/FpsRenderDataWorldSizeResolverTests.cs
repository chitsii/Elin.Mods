using UnityEngine;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsRenderDataWorldSizeResolverTests
    {
        [Fact]
        public void Resolve_UsesMeshFootprintWhenItExceedsPixelDerivedSize()
        {
            Vector2 size = FpsRenderDataWorldSizeResolver.Resolve(
                new Vector2(32f, 32f),
                Vector2.one,
                new Vector2(2f, 1f),
                new Vector3(2f, 1f, 0f),
                false,
                100f);

            Assert.Equal(2f, size.x, 3);
            Assert.Equal(1f, size.y, 3);
        }

        [Fact]
        public void Resolve_ExpandsMultiSizeHeightFromProceduralMesh()
        {
            Vector2 size = FpsRenderDataWorldSizeResolver.Resolve(
                new Vector2(32f, 32f),
                Vector2.one,
                new Vector2(1f, 1f),
                new Vector3(1f, 1.25f, 0f),
                true,
                100f);

            Assert.Equal(1f, size.x, 3);
            Assert.Equal(2.5f, size.y, 3);
        }

        [Fact]
        public void Resolve_KeepsPixelDerivedSizeForSmallProps()
        {
            Vector2 size = FpsRenderDataWorldSizeResolver.Resolve(
                new Vector2(64f, 96f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector3.zero,
                false,
                100f);

            Assert.Equal(0.64f, size.x, 3);
            Assert.Equal(0.96f, size.y, 3);
        }

        [Fact]
        public void Resolve_ExpandsInstalledSpriteByFootprintScale()
        {
            Vector2 size = FpsRenderDataWorldSizeResolver.Resolve(
                new Vector2(64f, 64f),
                Vector2.one,
                Vector2.zero,
                Vector3.zero,
                false,
                64f,
                2,
                2);

            Assert.Equal(2f, size.x, 3);
            Assert.Equal(2f, size.y, 3);
        }

        [Fact]
        public void ApplyFootprintScale_PreservesAspectWhileGrowingToFootprint()
        {
            Vector2 size = FpsRenderDataWorldSizeResolver.ApplyFootprintScale(
                new Vector2(0.8f, 1.6f),
                1,
                2);

            Assert.Equal(1.414f, size.x, 3);
            Assert.Equal(2.828f, size.y, 3);
        }
    }
}
