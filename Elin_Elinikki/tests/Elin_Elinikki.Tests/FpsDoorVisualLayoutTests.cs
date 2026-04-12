using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsDoorVisualLayoutTests
    {
        [Fact]
        public void ResolveOpeningTop_PreservesVisibleLintelForShortWalls()
        {
            float openingTop = FpsDoorVisualLayout.ResolveOpeningTop(0f, 1f);

            Assert.InRange(openingTop, 0.70f, 0.82f);
            Assert.True(1f - openingTop >= 0.18f);
        }

        [Fact]
        public void ResolveOpeningTop_CapsTallDoorOpening()
        {
            float openingTop = FpsDoorVisualLayout.ResolveOpeningTop(0f, 3.2f);

            Assert.InRange(openingTop, 0.77f, 0.79f);
        }

        [Fact]
        public void ResolveOpeningTop_FollowsPreferredDoorHeightOnTallWalls()
        {
            float openingTop = FpsDoorVisualLayout.ResolveOpeningTop(0f, 3.2f, 0.90f);

            Assert.InRange(openingTop, 0.95f, 0.97f);
        }

        [Fact]
        public void ResolvePanelDimensions_UsesOpeningHeightAndTextureAspect()
        {
            FpsDoorPanelDimensions dims = FpsDoorVisualLayout.ResolvePanelDimensions(40f, 80f, 0f, 1f);

            Assert.InRange(dims.HeightWorld, 0.70f, 0.82f);
            Assert.InRange(dims.WidthWorld, 0.36f, 0.50f);
        }

        [Fact]
        public void ResolvePanelDimensions_UsesNativeWorldSizeAsUpperBound()
        {
            FpsDoorPanelDimensions dims = FpsDoorVisualLayout.ResolvePanelDimensions(0.50f, 0.90f, 0f, 1f, 0.5f);

            Assert.InRange(dims.HeightWorld, 0.82f, 0.82f);
            Assert.InRange(dims.WidthWorld, 0.45f, 0.46f);
        }

        [Fact]
        public void ResolveJambSpans_CreatesSymmetricSideStrips()
        {
            FpsDoorJambSpans spans = FpsDoorVisualLayout.ResolveJambSpans(10f, 0.72f);

            Assert.Equal(10.00f, spans.LeftStart, 3);
            Assert.Equal(10.14f, spans.LeftEnd, 3);
            Assert.Equal(10.86f, spans.RightStart, 3);
            Assert.Equal(11.00f, spans.RightEnd, 3);
        }
    }
}
