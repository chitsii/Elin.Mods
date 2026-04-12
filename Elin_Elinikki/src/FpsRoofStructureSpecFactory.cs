namespace Elin_Elinikki
{
    internal enum FpsRoofStructureLayoutKind
    {
        Flat,
        Ridge
    }

    internal readonly struct FpsRoofStructureLayoutInput
    {
        public FpsRoofStructureLayoutInput(
            FpsRoofStructureLayoutKind kind,
            bool reverse,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float baseY,
            float topY,
            float minorStart,
            int flatStart,
            int flatEnd,
            bool hasFlatBand)
        {
            Kind = kind;
            Reverse = reverse;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            BaseY = baseY;
            TopY = topY;
            MinorStart = minorStart;
            FlatStart = flatStart;
            FlatEnd = flatEnd;
            HasFlatBand = hasFlatBand;
        }

        public FpsRoofStructureLayoutKind Kind { get; }

        public bool Reverse { get; }

        public float MinX { get; }

        public float MaxX { get; }

        public float MinZ { get; }

        public float MaxZ { get; }

        public float BaseY { get; }

        public float TopY { get; }

        public float MinorStart { get; }

        public int FlatStart { get; }

        public int FlatEnd { get; }

        public bool HasFlatBand { get; }
    }

    internal static class FpsRoofStructureSpecFactory
    {
        public static FpsRoofStructureSpec Create(FpsRoofStructureLayoutInput input, float eaveDepth, float thickness, float ridgeCapWidth)
        {
            switch (input.Kind)
            {
                case FpsRoofStructureLayoutKind.Flat:
                    return FpsRoofStructureSpec.CreateFlat(
                        input.MinX,
                        input.MaxX,
                        input.MinZ,
                        input.MaxZ,
                        input.TopY,
                        eaveDepth,
                        thickness);

                default:
                    float rawRidgeStart = input.MinorStart + input.FlatStart;
                    float rawRidgeEnd = input.MinorStart + input.FlatEnd;
                    float ridgeCenter = (rawRidgeStart + rawRidgeEnd) * 0.5f;
                    float ridgeStart = input.HasFlatBand ? rawRidgeStart : ridgeCenter;
                    float ridgeEnd = input.HasFlatBand ? rawRidgeEnd : ridgeCenter;
                    return FpsRoofStructureSpec.CreateRidge(
                        input.MinX,
                        input.MaxX,
                        input.MinZ,
                        input.MaxZ,
                        input.BaseY,
                        input.TopY,
                        input.Reverse,
                        ridgeStart,
                        ridgeEnd,
                        eaveDepth,
                        thickness,
                        ridgeCapWidth);
            }
        }
    }
}
