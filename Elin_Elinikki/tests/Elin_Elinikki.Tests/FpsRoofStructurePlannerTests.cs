using System.Linq;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsRoofStructurePlannerTests
    {
        [Fact]
        public void BuildFlatRoof_ProducesTopBottomAndFourEdges()
        {
            FpsRoofStructureSpec spec = FpsRoofStructureSpec.CreateFlat(
                minX: 10f,
                maxX: 14f,
                minZ: 20f,
                maxZ: 23f,
                topY: 7f,
                eaveDepth: 0.25f,
                thickness: 0.3f);

            FpsRoofStructurePlan plan = FpsRoofStructurePlanner.Build(spec);

            Assert.Equal(6, plan.Quads.Count);
            Assert.Equal(1, plan.Quads.Count(q => q.Kind == FpsRoofStructureQuadKind.TopSurface));
            Assert.Equal(1, plan.Quads.Count(q => q.Kind == FpsRoofStructureQuadKind.BottomSurface));
            Assert.Equal(4, plan.Quads.Count(q => q.Kind == FpsRoofStructureQuadKind.EdgeBand));

            FpsRoofStructureQuad top = plan.Quads.Single(q => q.Kind == FpsRoofStructureQuadKind.TopSurface);
            Assert.Equal(9.75f, top.MinX, 3);
            Assert.Equal(14.25f, top.MaxX, 3);
            Assert.Equal(19.75f, top.MinZ, 3);
            Assert.Equal(23.25f, top.MaxZ, 3);
            Assert.Equal(7f, top.MinY, 3);
            Assert.Equal(7f, top.MaxY, 3);
        }

        [Fact]
        public void BuildRidgeRoof_ProducesTwoSlopesTwoGablesTwoEavesAndRidgeCap()
        {
            FpsRoofStructureSpec spec = FpsRoofStructureSpec.CreateRidge(
                minX: 0f,
                maxX: 4f,
                minZ: 0f,
                maxZ: 6f,
                baseY: 3f,
                topY: 5f,
                reverse: false,
                ridgeStart: 2.5f,
                ridgeEnd: 3.5f,
                eaveDepth: 0.2f,
                thickness: 0.25f,
                ridgeCapWidth: 0.18f);

            FpsRoofStructurePlan plan = FpsRoofStructurePlanner.Build(spec);

            Assert.Equal(7, plan.Quads.Count);
            Assert.Equal(2, plan.Quads.Count(q => q.Kind == FpsRoofStructureQuadKind.SlopeSurface));
            Assert.Equal(2, plan.Quads.Count(q => q.Kind == FpsRoofStructureQuadKind.GableFace));
            Assert.Equal(2, plan.Quads.Count(q => q.Kind == FpsRoofStructureQuadKind.EdgeBand));
            Assert.Equal(1, plan.Quads.Count(q => q.Kind == FpsRoofStructureQuadKind.RidgeCap));

            FpsRoofStructureQuad ridgeCap = plan.Quads.Single(q => q.Kind == FpsRoofStructureQuadKind.RidgeCap);
            Assert.Equal(5f, ridgeCap.MinY, 3);
            Assert.Equal(5f, ridgeCap.MaxY, 3);
            Assert.True(ridgeCap.MaxZ - ridgeCap.MinZ > 0.17f);

            FpsRoofStructureQuad lowSlope = plan.Quads
                .Where(q => q.Kind == FpsRoofStructureQuadKind.SlopeSurface)
                .OrderBy(q => q.MinZ)
                .First();
            Assert.Equal(2.3f, lowSlope.MaxZ, 3);
            Assert.Equal(3f, lowSlope.MinY, 3);
            Assert.Equal(5f, lowSlope.MaxY, 3);
        }

        [Fact]
        public void BuildReverseRidgeRoof_MovesRidgeAlongXInsteadOfZ()
        {
            FpsRoofStructureSpec spec = FpsRoofStructureSpec.CreateRidge(
                minX: 4f,
                maxX: 10f,
                minZ: 8f,
                maxZ: 12f,
                baseY: 2f,
                topY: 4.5f,
                reverse: true,
                ridgeStart: 6.25f,
                ridgeEnd: 7.75f,
                eaveDepth: 0.1f,
                thickness: 0.2f,
                ridgeCapWidth: 0.2f);

            FpsRoofStructurePlan plan = FpsRoofStructurePlanner.Build(spec);

            FpsRoofStructureQuad ridgeCap = plan.Quads.Single(q => q.Kind == FpsRoofStructureQuadKind.RidgeCap);
            Assert.Equal(6.15f, ridgeCap.MinX, 3);
            Assert.Equal(7.85f, ridgeCap.MaxX, 3);
            Assert.Equal(7.9f, ridgeCap.MinZ, 3);
            Assert.Equal(12.1f, ridgeCap.MaxZ, 3);
        }
    }
}
