using System;

namespace Elin_Elinikki
{
    internal static class FpsRoofStructurePlanner
    {
        public static FpsRoofStructurePlan Build(FpsRoofStructureSpec spec)
        {
            Validate(spec);

            FpsRoofStructurePlan plan = new FpsRoofStructurePlan();
            switch (spec.Kind)
            {
                case FpsRoofStructureKind.Flat:
                    BuildFlat(spec, plan);
                    break;
                case FpsRoofStructureKind.Ridge:
                    BuildRidge(spec, plan);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return plan;
        }

        private static void BuildFlat(FpsRoofStructureSpec spec, FpsRoofStructurePlan plan)
        {
            float minX = spec.MinX - spec.EaveDepth;
            float maxX = spec.MaxX + spec.EaveDepth;
            float minZ = spec.MinZ - spec.EaveDepth;
            float maxZ = spec.MaxZ + spec.EaveDepth;
            float topY = spec.TopY;
            float bottomY = spec.TopY - spec.Thickness;

            plan.Quads.Add(CreateHorizontalQuad(FpsRoofStructureQuadKind.TopSurface, minX, maxX, minZ, maxZ, topY));
            plan.Quads.Add(CreateHorizontalQuad(FpsRoofStructureQuadKind.BottomSurface, minX, maxX, minZ, maxZ, bottomY));
            plan.Quads.Add(CreateVerticalQuadAtX(FpsRoofStructureQuadKind.EdgeBand, minX, minZ, maxZ, bottomY, topY));
            plan.Quads.Add(CreateVerticalQuadAtX(FpsRoofStructureQuadKind.EdgeBand, maxX, minZ, maxZ, bottomY, topY));
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsRoofStructureQuadKind.EdgeBand, minZ, minX, maxX, bottomY, topY));
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsRoofStructureQuadKind.EdgeBand, maxZ, minX, maxX, bottomY, topY));
        }

        private static void BuildRidge(FpsRoofStructureSpec spec, FpsRoofStructurePlan plan)
        {
            if (spec.Reverse)
            {
                BuildRidgeAlongX(spec, plan);
                return;
            }

            float minX = spec.MinX - spec.EaveDepth;
            float maxX = spec.MaxX + spec.EaveDepth;
            float minZ = spec.MinZ - spec.EaveDepth;
            float maxZ = spec.MaxZ + spec.EaveDepth;
            float ridgeStart = spec.RidgeStart - spec.EaveDepth;
            float ridgeEnd = spec.RidgeEnd + spec.EaveDepth;

            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.SlopeSurface,
                new FpsStructurePoint3(minX, spec.BaseY, minZ),
                new FpsStructurePoint3(maxX, spec.BaseY, minZ),
                new FpsStructurePoint3(minX, spec.TopY, ridgeStart),
                new FpsStructurePoint3(maxX, spec.TopY, ridgeStart)));
            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.SlopeSurface,
                new FpsStructurePoint3(maxX, spec.BaseY, maxZ),
                new FpsStructurePoint3(minX, spec.BaseY, maxZ),
                new FpsStructurePoint3(maxX, spec.TopY, ridgeEnd),
                new FpsStructurePoint3(minX, spec.TopY, ridgeEnd)));
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsRoofStructureQuadKind.EdgeBand, minZ, minX, maxX, spec.BaseY, spec.BaseY + spec.Thickness));
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsRoofStructureQuadKind.EdgeBand, maxZ, minX, maxX, spec.BaseY, spec.BaseY + spec.Thickness));
            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.GableFace,
                new FpsStructurePoint3(minX, spec.BaseY, minZ),
                new FpsStructurePoint3(minX, spec.BaseY, maxZ),
                new FpsStructurePoint3(minX, spec.TopY, ridgeStart),
                new FpsStructurePoint3(minX, spec.TopY, ridgeEnd)));
            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.GableFace,
                new FpsStructurePoint3(maxX, spec.BaseY, maxZ),
                new FpsStructurePoint3(maxX, spec.BaseY, minZ),
                new FpsStructurePoint3(maxX, spec.TopY, ridgeEnd),
                new FpsStructurePoint3(maxX, spec.TopY, ridgeStart)));
            AddRidgeCapAlongZ(plan, spec, minX, maxX, ridgeStart, ridgeEnd);
        }

        private static void BuildRidgeAlongX(FpsRoofStructureSpec spec, FpsRoofStructurePlan plan)
        {
            float minX = spec.MinX - spec.EaveDepth;
            float maxX = spec.MaxX + spec.EaveDepth;
            float minZ = spec.MinZ - spec.EaveDepth;
            float maxZ = spec.MaxZ + spec.EaveDepth;
            float ridgeStart = spec.RidgeStart - spec.EaveDepth;
            float ridgeEnd = spec.RidgeEnd + spec.EaveDepth;

            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.SlopeSurface,
                new FpsStructurePoint3(minX, spec.BaseY, minZ),
                new FpsStructurePoint3(minX, spec.BaseY, maxZ),
                new FpsStructurePoint3(ridgeStart, spec.TopY, minZ),
                new FpsStructurePoint3(ridgeStart, spec.TopY, maxZ)));
            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.SlopeSurface,
                new FpsStructurePoint3(maxX, spec.BaseY, maxZ),
                new FpsStructurePoint3(maxX, spec.BaseY, minZ),
                new FpsStructurePoint3(ridgeEnd, spec.TopY, maxZ),
                new FpsStructurePoint3(ridgeEnd, spec.TopY, minZ)));
            plan.Quads.Add(CreateVerticalQuadAtX(FpsRoofStructureQuadKind.EdgeBand, minX, minZ, maxZ, spec.BaseY, spec.BaseY + spec.Thickness));
            plan.Quads.Add(CreateVerticalQuadAtX(FpsRoofStructureQuadKind.EdgeBand, maxX, minZ, maxZ, spec.BaseY, spec.BaseY + spec.Thickness));
            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.GableFace,
                new FpsStructurePoint3(minX, spec.BaseY, minZ),
                new FpsStructurePoint3(maxX, spec.BaseY, minZ),
                new FpsStructurePoint3(ridgeStart, spec.TopY, minZ),
                new FpsStructurePoint3(ridgeEnd, spec.TopY, minZ)));
            plan.Quads.Add(new FpsRoofStructureQuad(
                FpsRoofStructureQuadKind.GableFace,
                new FpsStructurePoint3(maxX, spec.BaseY, maxZ),
                new FpsStructurePoint3(minX, spec.BaseY, maxZ),
                new FpsStructurePoint3(ridgeEnd, spec.TopY, maxZ),
                new FpsStructurePoint3(ridgeStart, spec.TopY, maxZ)));
            AddRidgeCapAlongX(plan, spec, minZ, maxZ, ridgeStart, ridgeEnd);
        }

        private static void AddRidgeCapAlongZ(FpsRoofStructurePlan plan, FpsRoofStructureSpec spec, float minX, float maxX, float ridgeStart, float ridgeEnd)
        {
            float capMinZ = ridgeStart;
            float capMaxZ = Math.Max(ridgeEnd, ridgeStart + spec.RidgeCapWidth);
            plan.Quads.Add(CreateHorizontalQuad(FpsRoofStructureQuadKind.RidgeCap, minX, maxX, capMinZ, capMaxZ, spec.TopY));
        }

        private static void AddRidgeCapAlongX(FpsRoofStructurePlan plan, FpsRoofStructureSpec spec, float minZ, float maxZ, float ridgeStart, float ridgeEnd)
        {
            float capMinX = ridgeStart;
            float capMaxX = Math.Max(ridgeEnd, ridgeStart + spec.RidgeCapWidth);
            plan.Quads.Add(CreateHorizontalQuad(FpsRoofStructureQuadKind.RidgeCap, capMinX, capMaxX, minZ, maxZ, spec.TopY));
        }

        private static FpsRoofStructureQuad CreateHorizontalQuad(FpsRoofStructureQuadKind kind, float minX, float maxX, float minZ, float maxZ, float y)
        {
            return new FpsRoofStructureQuad(
                kind,
                new FpsStructurePoint3(minX, y, minZ),
                new FpsStructurePoint3(maxX, y, minZ),
                new FpsStructurePoint3(minX, y, maxZ),
                new FpsStructurePoint3(maxX, y, maxZ));
        }

        private static FpsRoofStructureQuad CreateVerticalQuadAtX(FpsRoofStructureQuadKind kind, float x, float minZ, float maxZ, float bottomY, float topY)
        {
            return new FpsRoofStructureQuad(
                kind,
                new FpsStructurePoint3(x, bottomY, minZ),
                new FpsStructurePoint3(x, bottomY, maxZ),
                new FpsStructurePoint3(x, topY, minZ),
                new FpsStructurePoint3(x, topY, maxZ));
        }

        private static FpsRoofStructureQuad CreateVerticalQuadAtZ(FpsRoofStructureQuadKind kind, float z, float minX, float maxX, float bottomY, float topY)
        {
            return new FpsRoofStructureQuad(
                kind,
                new FpsStructurePoint3(minX, bottomY, z),
                new FpsStructurePoint3(maxX, bottomY, z),
                new FpsStructurePoint3(minX, topY, z),
                new FpsStructurePoint3(maxX, topY, z));
        }

        private static void Validate(FpsRoofStructureSpec spec)
        {
            if (spec.MaxX <= spec.MinX)
            {
                throw new ArgumentException("maxX must be greater than minX.");
            }

            if (spec.MaxZ <= spec.MinZ)
            {
                throw new ArgumentException("maxZ must be greater than minZ.");
            }

            if (spec.TopY <= spec.BaseY)
            {
                throw new ArgumentException("topY must be greater than baseY.");
            }

            if (spec.Thickness <= 0f)
            {
                throw new ArgumentException("thickness must be positive.");
            }

            if (spec.Kind == FpsRoofStructureKind.Ridge && spec.RidgeEnd < spec.RidgeStart)
            {
                throw new ArgumentException("ridgeEnd must be greater than or equal to ridgeStart.");
            }
        }
    }
}
