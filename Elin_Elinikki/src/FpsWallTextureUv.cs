namespace Elin_Elinikki
{
    internal readonly struct FpsUvRect
    {
        public FpsUvRect(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin;
            YMin = yMin;
            XMax = xMax;
            YMax = yMax;
        }

        public float XMin { get; }

        public float YMin { get; }

        public float XMax { get; }

        public float YMax { get; }

        public float Width => XMax - XMin;

        public float Height => YMax - YMin;
    }

    internal static class FpsWallTextureUv
    {
        public static FpsUvRect ComputeUvRect(FpsWallStructureQuad quad)
        {
            float spanX = quad.MaxX - quad.MinX;
            float spanY = quad.MaxY - quad.MinY;
            float spanZ = quad.MaxZ - quad.MinZ;

            switch (quad.Kind)
            {
                case FpsWallStructureQuadKind.TopCap:
                    return new FpsUvRect(0f, 0f, NormalizeRepeat(spanX), NormalizeRepeat(spanZ));
                case FpsWallStructureQuadKind.EndCap:
                    return new FpsUvRect(0f, 0f, NormalizeRepeat(spanX + spanZ), NormalizeRepeat(spanY));
                default:
                    return new FpsUvRect(0f, 0f, NormalizeRepeat(spanX + spanZ), NormalizeRepeat(spanY));
            }
        }

        private static float NormalizeRepeat(float span)
        {
            return span < 1f ? 1f : span;
        }
    }
}
