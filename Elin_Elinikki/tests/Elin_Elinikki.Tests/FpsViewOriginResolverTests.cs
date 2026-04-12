using UnityEngine;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsViewOriginResolverTests
    {
        [Fact]
        public void Resolve_ReturnsLogicalOriginWhenLocked()
        {
            Vector2 resolved = FpsViewOriginResolver.Resolve(
                new Vector2(10.5f, 20.5f),
                new Vector2(11.1f, 19.8f),
                true,
                0.35f);

            Assert.Equal(new Vector2(10.5f, 20.5f), resolved);
        }

        [Fact]
        public void Resolve_FallsBackToLogicalWhenProjectedOriginDriftsTooFar()
        {
            Vector2 resolved = FpsViewOriginResolver.Resolve(
                new Vector2(10.5f, 20.5f),
                new Vector2(11.0f, 20.5f),
                false,
                0.35f);

            Assert.Equal(new Vector2(10.5f, 20.5f), resolved);
        }

        [Fact]
        public void Resolve_UsesProjectedOriginWhenDeviationIsSmall()
        {
            Vector2 resolved = FpsViewOriginResolver.Resolve(
                new Vector2(10.5f, 20.5f),
                new Vector2(10.65f, 20.55f),
                false,
                0.35f);

            Assert.Equal(new Vector2(10.65f, 20.55f), resolved);
        }

        [Fact]
        public void ResolveAxisLocked_ForNorthSouthUsesProjectedZOnly()
        {
            Vector2 resolved = FpsViewOriginResolver.ResolveAxisLocked(
                new Vector2(10.5f, 20.5f),
                new Vector2(10.8f, 20.85f),
                0,
                0.75f);

            Assert.Equal(new Vector2(10.5f, 20.85f), resolved);
        }

        [Fact]
        public void ResolveAxisLocked_ForEastWestUsesProjectedXOnly()
        {
            Vector2 resolved = FpsViewOriginResolver.ResolveAxisLocked(
                new Vector2(10.5f, 20.5f),
                new Vector2(10.85f, 20.8f),
                1,
                0.75f);

            Assert.Equal(new Vector2(10.85f, 20.5f), resolved);
        }

        [Fact]
        public void ResolveAxisLocked_FallsBackWhenAxisDeviationIsTooLarge()
        {
            Vector2 resolved = FpsViewOriginResolver.ResolveAxisLocked(
                new Vector2(10.5f, 20.5f),
                new Vector2(10.5f, 21.5f),
                0,
                0.75f);

            Assert.Equal(new Vector2(10.5f, 20.5f), resolved);
        }

        [Fact]
        public void IsAxisSettled_UsesForwardAxisForNorthSouth()
        {
            Assert.True(FpsViewOriginResolver.IsAxisSettled(
                new Vector2(10.5f, 20.5f),
                new Vector2(11.2f, 20.56f),
                0,
                0.1f));
        }

        [Fact]
        public void IsAxisSettled_ReturnsFalseWhenForwardAxisIsStillMoving()
        {
            Assert.False(FpsViewOriginResolver.IsAxisSettled(
                new Vector2(10.5f, 20.5f),
                new Vector2(10.5f, 20.72f),
                0,
                0.1f));
        }
    }
}
