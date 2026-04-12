using System.Collections.Generic;

namespace Elin_Elinikki
{
    internal readonly struct FpsWallRunSeed
    {
        public FpsWallRunSeed(int cellX, int cellZ, int dir, float bottomY, float topY, int surfaceKey)
        {
            CellX = cellX;
            CellZ = cellZ;
            Dir = dir;
            BottomY = bottomY;
            TopY = topY;
            SurfaceKey = surfaceKey;
        }

        public int CellX { get; }

        public int CellZ { get; }

        public int Dir { get; }

        public float BottomY { get; }

        public float TopY { get; }

        public int SurfaceKey { get; }
    }

    internal readonly struct FpsWallRun
    {
        public FpsWallRun(FpsWallStructureAxis axis, int dir, float constant, float minAlong, float maxAlong, float bottomY, float topY, int surfaceKey)
        {
            Axis = axis;
            Dir = dir;
            Constant = constant;
            MinAlong = minAlong;
            MaxAlong = maxAlong;
            BottomY = bottomY;
            TopY = topY;
            SurfaceKey = surfaceKey;
        }

        public FpsWallStructureAxis Axis { get; }

        public int Dir { get; }

        public float Constant { get; }

        public float MinAlong { get; }

        public float MaxAlong { get; }

        public float BottomY { get; }

        public float TopY { get; }

        public int SurfaceKey { get; }
    }

    internal static class FpsWallRunPlanner
    {
        public static IEnumerable<FpsWallRun> BuildRuns(IEnumerable<FpsWallRunSeed> seeds)
        {
            List<FpsWallRunSeed> northSouth = new List<FpsWallRunSeed>();
            List<FpsWallRunSeed> eastWest = new List<FpsWallRunSeed>();

            foreach (FpsWallRunSeed seed in seeds)
            {
                if (seed.Dir == 0 || seed.Dir == 2)
                {
                    northSouth.Add(seed);
                }
                else
                {
                    eastWest.Add(seed);
                }
            }

            northSouth.Sort(CompareAlongX);
            eastWest.Sort(CompareAlongZ);

            foreach (FpsWallRun run in BuildSortedRuns(northSouth, FpsWallStructureAxis.AlongX))
            {
                yield return run;
            }

            foreach (FpsWallRun run in BuildSortedRuns(eastWest, FpsWallStructureAxis.AlongZ))
            {
                yield return run;
            }
        }

        private static IEnumerable<FpsWallRun> BuildSortedRuns(List<FpsWallRunSeed> seeds, FpsWallStructureAxis axis)
        {
            if (seeds.Count == 0)
            {
                yield break;
            }

            FpsWallRunSeed current = seeds[0];
            float minAlong = axis == FpsWallStructureAxis.AlongX ? current.CellX : current.CellZ;
            float maxAlong = minAlong + 1f;
            for (int i = 1; i < seeds.Count; i++)
            {
                FpsWallRunSeed next = seeds[i];
                bool sameBand = current.Dir == next.Dir
                    && current.SurfaceKey == next.SurfaceKey
                    && current.BottomY == next.BottomY
                    && current.TopY == next.TopY
                    && GetConstant(current) == GetConstant(next);
                float expectedAlong = maxAlong;
                float nextAlong = axis == FpsWallStructureAxis.AlongX ? next.CellX : next.CellZ;
                if (!sameBand || nextAlong != expectedAlong)
                {
                    yield return CreateRun(axis, current, minAlong, maxAlong);
                    current = next;
                    minAlong = nextAlong;
                    maxAlong = nextAlong + 1f;
                    continue;
                }

                maxAlong += 1f;
            }

            yield return CreateRun(axis, current, minAlong, maxAlong);
        }

        private static FpsWallRun CreateRun(FpsWallStructureAxis axis, FpsWallRunSeed seed, float minAlong, float maxAlong)
        {
            return new FpsWallRun(
                axis,
                seed.Dir,
                GetConstant(seed),
                minAlong,
                maxAlong,
                seed.BottomY,
                seed.TopY,
                seed.SurfaceKey);
        }

        private static float GetConstant(FpsWallRunSeed seed)
        {
            switch (seed.Dir)
            {
                case 0:
                    return seed.CellZ;
                case 1:
                    return seed.CellX + 1f;
                case 2:
                    return seed.CellZ + 1f;
                default:
                    return seed.CellX;
            }
        }

        private static int CompareAlongX(FpsWallRunSeed a, FpsWallRunSeed b)
        {
            int comparison = GetConstant(a).CompareTo(GetConstant(b));
            if (comparison != 0) return comparison;
            comparison = a.Dir.CompareTo(b.Dir);
            if (comparison != 0) return comparison;
            comparison = a.SurfaceKey.CompareTo(b.SurfaceKey);
            if (comparison != 0) return comparison;
            comparison = a.BottomY.CompareTo(b.BottomY);
            if (comparison != 0) return comparison;
            comparison = a.TopY.CompareTo(b.TopY);
            if (comparison != 0) return comparison;
            return a.CellX.CompareTo(b.CellX);
        }

        private static int CompareAlongZ(FpsWallRunSeed a, FpsWallRunSeed b)
        {
            int comparison = GetConstant(a).CompareTo(GetConstant(b));
            if (comparison != 0) return comparison;
            comparison = a.Dir.CompareTo(b.Dir);
            if (comparison != 0) return comparison;
            comparison = a.SurfaceKey.CompareTo(b.SurfaceKey);
            if (comparison != 0) return comparison;
            comparison = a.BottomY.CompareTo(b.BottomY);
            if (comparison != 0) return comparison;
            comparison = a.TopY.CompareTo(b.TopY);
            if (comparison != 0) return comparison;
            return a.CellZ.CompareTo(b.CellZ);
        }
    }
}
