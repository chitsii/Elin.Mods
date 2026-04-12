using System;
using System.Collections.Generic;

namespace Elin_Elinikki
{
    internal enum FpsWallStructureAxis
    {
        AlongX,
        AlongZ
    }

    internal enum FpsWallStructureQuadKind
    {
        SideFace,
        TopCap,
        EndCap
    }

    internal readonly struct FpsWallStructureSpec
    {
        private FpsWallStructureSpec(
            FpsWallStructureAxis axis,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float bottomY,
            float topY,
            float thickness)
        {
            Axis = axis;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            BottomY = bottomY;
            TopY = topY;
            Thickness = thickness;
        }

        public FpsWallStructureAxis Axis { get; }

        public float MinX { get; }

        public float MaxX { get; }

        public float MinZ { get; }

        public float MaxZ { get; }

        public float BottomY { get; }

        public float TopY { get; }

        public float Thickness { get; }

        public static FpsWallStructureSpec CreateAlongX(float minX, float maxX, float z, float bottomY, float topY, float thickness)
        {
            float halfThickness = thickness * 0.5f;
            return new FpsWallStructureSpec(
                FpsWallStructureAxis.AlongX,
                minX,
                maxX,
                z - halfThickness,
                z + halfThickness,
                bottomY,
                topY,
                thickness);
        }

        public static FpsWallStructureSpec CreateAlongZ(float x, float minZ, float maxZ, float bottomY, float topY, float thickness)
        {
            float halfThickness = thickness * 0.5f;
            return new FpsWallStructureSpec(
                FpsWallStructureAxis.AlongZ,
                x - halfThickness,
                x + halfThickness,
                minZ,
                maxZ,
                bottomY,
                topY,
                thickness);
        }
    }

    internal readonly struct FpsWallStructureQuad
    {
        public FpsWallStructureQuad(
            FpsWallStructureQuadKind kind,
            FpsStructurePoint3 bottomLeft,
            FpsStructurePoint3 bottomRight,
            FpsStructurePoint3 topLeft,
            FpsStructurePoint3 topRight)
        {
            Kind = kind;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            TopLeft = topLeft;
            TopRight = topRight;
        }

        public FpsWallStructureQuadKind Kind { get; }

        public FpsStructurePoint3 BottomLeft { get; }

        public FpsStructurePoint3 BottomRight { get; }

        public FpsStructurePoint3 TopLeft { get; }

        public FpsStructurePoint3 TopRight { get; }

        public float MinX => Min(BottomLeft.X, BottomRight.X, TopLeft.X, TopRight.X);

        public float MaxX => Max(BottomLeft.X, BottomRight.X, TopLeft.X, TopRight.X);

        public float MinY => Min(BottomLeft.Y, BottomRight.Y, TopLeft.Y, TopRight.Y);

        public float MaxY => Max(BottomLeft.Y, BottomRight.Y, TopLeft.Y, TopRight.Y);

        public float MinZ => Min(BottomLeft.Z, BottomRight.Z, TopLeft.Z, TopRight.Z);

        public float MaxZ => Max(BottomLeft.Z, BottomRight.Z, TopLeft.Z, TopRight.Z);

        private static float Min(float a, float b, float c, float d)
        {
            return Math.Min(Math.Min(a, b), Math.Min(c, d));
        }

        private static float Max(float a, float b, float c, float d)
        {
            return Math.Max(Math.Max(a, b), Math.Max(c, d));
        }
    }

    internal sealed class FpsWallStructurePlan
    {
        public List<FpsWallStructureQuad> Quads { get; } = new List<FpsWallStructureQuad>();
    }

    internal static class FpsWallStructurePlanner
    {
        public static FpsWallStructurePlan Build(FpsWallStructureSpec spec)
        {
            Validate(spec);

            FpsWallStructurePlan plan = new FpsWallStructurePlan();
            if (spec.Axis == FpsWallStructureAxis.AlongX)
            {
                BuildAlongX(spec, plan);
            }
            else
            {
                BuildAlongZ(spec, plan);
            }

            return plan;
        }

        private static void BuildAlongX(FpsWallStructureSpec spec, FpsWallStructurePlan plan)
        {
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsWallStructureQuadKind.SideFace, spec.MinZ, spec.MinX, spec.MaxX, spec.BottomY, spec.TopY));
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsWallStructureQuadKind.SideFace, spec.MaxZ, spec.MaxX, spec.MinX, spec.BottomY, spec.TopY));
            plan.Quads.Add(CreateHorizontalQuad(FpsWallStructureQuadKind.TopCap, spec.MinX, spec.MaxX, spec.MinZ, spec.MaxZ, spec.TopY));
            plan.Quads.Add(CreateVerticalQuadAtX(FpsWallStructureQuadKind.EndCap, spec.MinX, spec.MinZ, spec.MaxZ, spec.BottomY, spec.TopY));
            plan.Quads.Add(CreateVerticalQuadAtX(FpsWallStructureQuadKind.EndCap, spec.MaxX, spec.MaxZ, spec.MinZ, spec.BottomY, spec.TopY));
        }

        private static void BuildAlongZ(FpsWallStructureSpec spec, FpsWallStructurePlan plan)
        {
            plan.Quads.Add(CreateVerticalQuadAtX(FpsWallStructureQuadKind.SideFace, spec.MinX, spec.MinZ, spec.MaxZ, spec.BottomY, spec.TopY));
            plan.Quads.Add(CreateVerticalQuadAtX(FpsWallStructureQuadKind.SideFace, spec.MaxX, spec.MaxZ, spec.MinZ, spec.BottomY, spec.TopY));
            plan.Quads.Add(CreateHorizontalQuad(FpsWallStructureQuadKind.TopCap, spec.MinX, spec.MaxX, spec.MinZ, spec.MaxZ, spec.TopY));
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsWallStructureQuadKind.EndCap, spec.MinZ, spec.MaxX, spec.MinX, spec.BottomY, spec.TopY));
            plan.Quads.Add(CreateVerticalQuadAtZ(FpsWallStructureQuadKind.EndCap, spec.MaxZ, spec.MinX, spec.MaxX, spec.BottomY, spec.TopY));
        }

        private static FpsWallStructureQuad CreateHorizontalQuad(FpsWallStructureQuadKind kind, float minX, float maxX, float minZ, float maxZ, float y)
        {
            return new FpsWallStructureQuad(
                kind,
                new FpsStructurePoint3(minX, y, minZ),
                new FpsStructurePoint3(maxX, y, minZ),
                new FpsStructurePoint3(minX, y, maxZ),
                new FpsStructurePoint3(maxX, y, maxZ));
        }

        private static FpsWallStructureQuad CreateVerticalQuadAtX(FpsWallStructureQuadKind kind, float x, float minZ, float maxZ, float bottomY, float topY)
        {
            return new FpsWallStructureQuad(
                kind,
                new FpsStructurePoint3(x, bottomY, minZ),
                new FpsStructurePoint3(x, bottomY, maxZ),
                new FpsStructurePoint3(x, topY, minZ),
                new FpsStructurePoint3(x, topY, maxZ));
        }

        private static FpsWallStructureQuad CreateVerticalQuadAtZ(FpsWallStructureQuadKind kind, float z, float minX, float maxX, float bottomY, float topY)
        {
            return new FpsWallStructureQuad(
                kind,
                new FpsStructurePoint3(minX, bottomY, z),
                new FpsStructurePoint3(maxX, bottomY, z),
                new FpsStructurePoint3(minX, topY, z),
                new FpsStructurePoint3(maxX, topY, z));
        }

        private static void Validate(FpsWallStructureSpec spec)
        {
            if (spec.MaxX <= spec.MinX)
            {
                throw new ArgumentException("maxX must be greater than minX.");
            }

            if (spec.MaxZ <= spec.MinZ)
            {
                throw new ArgumentException("maxZ must be greater than minZ.");
            }

            if (spec.TopY <= spec.BottomY)
            {
                throw new ArgumentException("topY must be greater than bottomY.");
            }

            if (spec.Thickness <= 0f)
            {
                throw new ArgumentException("thickness must be positive.");
            }
        }
    }
}
