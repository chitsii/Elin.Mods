using System.Linq;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsWallStructurePlannerTests
    {
        [Fact]
        public void BuildAlongZ_ProducesFiveQuadsWithSymmetricThickness()
        {
            FpsWallStructureSpec spec = FpsWallStructureSpec.CreateAlongZ(
                x: 12f,
                minZ: 4f,
                maxZ: 9f,
                bottomY: 1f,
                topY: 3.5f,
                thickness: 0.4f);

            FpsWallStructurePlan plan = FpsWallStructurePlanner.Build(spec);

            Assert.Equal(5, plan.Quads.Count);
            Assert.Equal(2, plan.Quads.Count(q => q.Kind == FpsWallStructureQuadKind.SideFace));
            Assert.Equal(1, plan.Quads.Count(q => q.Kind == FpsWallStructureQuadKind.TopCap));
            Assert.Equal(2, plan.Quads.Count(q => q.Kind == FpsWallStructureQuadKind.EndCap));

            FpsWallStructureQuad left = plan.Quads
                .Where(q => q.Kind == FpsWallStructureQuadKind.SideFace)
                .OrderBy(q => q.MinX)
                .First();
            FpsWallStructureQuad right = plan.Quads
                .Where(q => q.Kind == FpsWallStructureQuadKind.SideFace)
                .OrderBy(q => q.MinX)
                .Last();

            Assert.Equal(11.8f, left.MinX, 3);
            Assert.Equal(11.8f, left.MaxX, 3);
            Assert.Equal(12.2f, right.MinX, 3);
            Assert.Equal(12.2f, right.MaxX, 3);
        }

        [Fact]
        public void BuildAlongX_ProducesTopCapOverEntireRun()
        {
            FpsWallStructureSpec spec = FpsWallStructureSpec.CreateAlongX(
                minX: 2f,
                maxX: 6f,
                z: 10f,
                bottomY: 0.5f,
                topY: 4f,
                thickness: 0.3f);

            FpsWallStructurePlan plan = FpsWallStructurePlanner.Build(spec);

            FpsWallStructureQuad top = plan.Quads.Single(q => q.Kind == FpsWallStructureQuadKind.TopCap);
            Assert.Equal(2f, top.MinX, 3);
            Assert.Equal(6f, top.MaxX, 3);
            Assert.Equal(9.85f, top.MinZ, 3);
            Assert.Equal(10.15f, top.MaxZ, 3);
            Assert.Equal(4f, top.MinY, 3);
            Assert.Equal(4f, top.MaxY, 3);
        }

        [Fact]
        public void BuildRejectsNonPositiveThickness()
        {
            FpsWallStructureSpec spec = FpsWallStructureSpec.CreateAlongX(
                minX: 0f,
                maxX: 1f,
                z: 0f,
                bottomY: 0f,
                topY: 1f,
                thickness: 0f);

            Assert.Throws<System.ArgumentException>(() => FpsWallStructurePlanner.Build(spec));
        }
    }
}
