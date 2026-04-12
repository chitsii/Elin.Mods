using System.Collections.Generic;

namespace Elin_Elinikki
{
    internal enum FpsRoofStructureKind
    {
        Flat,
        Ridge
    }

    internal enum FpsRoofStructureQuadKind
    {
        TopSurface,
        BottomSurface,
        SlopeSurface,
        EdgeBand,
        GableFace,
        RidgeCap
    }

    internal readonly struct FpsStructurePoint3
    {
        public FpsStructurePoint3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }
    }

    internal readonly struct FpsRoofStructureQuad
    {
        public FpsRoofStructureQuad(
            FpsRoofStructureQuadKind kind,
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

        public FpsRoofStructureQuadKind Kind { get; }

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
            return System.Math.Min(System.Math.Min(a, b), System.Math.Min(c, d));
        }

        private static float Max(float a, float b, float c, float d)
        {
            return System.Math.Max(System.Math.Max(a, b), System.Math.Max(c, d));
        }
    }

    internal sealed class FpsRoofStructurePlan
    {
        public List<FpsRoofStructureQuad> Quads { get; } = new List<FpsRoofStructureQuad>();
    }

    internal readonly struct FpsRoofStructureSpec
    {
        private FpsRoofStructureSpec(
            FpsRoofStructureKind kind,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float baseY,
            float topY,
            bool reverse,
            float ridgeStart,
            float ridgeEnd,
            float eaveDepth,
            float thickness,
            float ridgeCapWidth)
        {
            Kind = kind;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            BaseY = baseY;
            TopY = topY;
            Reverse = reverse;
            RidgeStart = ridgeStart;
            RidgeEnd = ridgeEnd;
            EaveDepth = eaveDepth;
            Thickness = thickness;
            RidgeCapWidth = ridgeCapWidth;
        }

        public FpsRoofStructureKind Kind { get; }

        public float MinX { get; }

        public float MaxX { get; }

        public float MinZ { get; }

        public float MaxZ { get; }

        public float BaseY { get; }

        public float TopY { get; }

        public bool Reverse { get; }

        public float RidgeStart { get; }

        public float RidgeEnd { get; }

        public float EaveDepth { get; }

        public float Thickness { get; }

        public float RidgeCapWidth { get; }

        public static FpsRoofStructureSpec CreateFlat(
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float topY,
            float eaveDepth,
            float thickness)
        {
            return new FpsRoofStructureSpec(
                FpsRoofStructureKind.Flat,
                minX,
                maxX,
                minZ,
                maxZ,
                topY - thickness,
                topY,
                false,
                0f,
                0f,
                eaveDepth,
                thickness,
                0f);
        }

        public static FpsRoofStructureSpec CreateRidge(
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float baseY,
            float topY,
            bool reverse,
            float ridgeStart,
            float ridgeEnd,
            float eaveDepth,
            float thickness,
            float ridgeCapWidth)
        {
            return new FpsRoofStructureSpec(
                FpsRoofStructureKind.Ridge,
                minX,
                maxX,
                minZ,
                maxZ,
                baseY,
                topY,
                reverse,
                ridgeStart,
                ridgeEnd,
                eaveDepth,
                thickness,
                ridgeCapWidth);
        }
    }
}
