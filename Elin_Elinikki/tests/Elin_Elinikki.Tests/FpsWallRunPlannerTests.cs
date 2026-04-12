using System.Linq;
using Xunit;

namespace Elin_Elinikki.Tests
{
    public sealed class FpsWallRunPlannerTests
    {
        [Fact]
        public void BuildRuns_MergesContiguousNorthFacesAlongX()
        {
            FpsWallRunSeed[] seeds =
            {
                new FpsWallRunSeed(2, 5, 0, 1f, 3f, 10),
                new FpsWallRunSeed(3, 5, 0, 1f, 3f, 10),
                new FpsWallRunSeed(4, 5, 0, 1f, 3f, 10)
            };

            FpsWallRun[] runs = FpsWallRunPlanner.BuildRuns(seeds).ToArray();

            Assert.Single(runs);
            Assert.Equal(FpsWallStructureAxis.AlongX, runs[0].Axis);
            Assert.Equal(2f, runs[0].MinAlong, 3);
            Assert.Equal(5f, runs[0].MaxAlong, 3);
            Assert.Equal(5f, runs[0].Constant, 3);
        }

        [Fact]
        public void BuildRuns_BreaksAtGap()
        {
            FpsWallRunSeed[] seeds =
            {
                new FpsWallRunSeed(2, 5, 0, 1f, 3f, 10),
                new FpsWallRunSeed(4, 5, 0, 1f, 3f, 10)
            };

            FpsWallRun[] runs = FpsWallRunPlanner.BuildRuns(seeds).ToArray();

            Assert.Equal(2, runs.Length);
        }

        [Fact]
        public void BuildRuns_BreaksOnDifferentSurfaceKeyOrHeight()
        {
            FpsWallRunSeed[] seeds =
            {
                new FpsWallRunSeed(10, 8, 1, 0f, 2f, 10),
                new FpsWallRunSeed(10, 9, 1, 0f, 2f, 11),
                new FpsWallRunSeed(10, 10, 1, 0f, 3f, 11)
            };

            FpsWallRun[] runs = FpsWallRunPlanner.BuildRuns(seeds).ToArray();

            Assert.Equal(3, runs.Length);
            Assert.All(runs, run => Assert.Equal(FpsWallStructureAxis.AlongZ, run.Axis));
        }
    }
}
