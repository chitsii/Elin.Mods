using System;

namespace Elin_Elinikki
{
    internal enum FpsAutoSurfaceMaterialKind
    {
        Front,
        Side,
        Top
    }

    internal readonly struct FpsSurfaceColor32
    {
        public FpsSurfaceColor32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R { get; }

        public byte G { get; }

        public byte B { get; }

        public byte A { get; }
    }

    internal static class FpsSurfaceMaterialSynthesis
    {
        public static FpsSurfaceColor32[] BuildFrontMaterial(FpsSurfaceColor32[] source, int width, int height)
        {
            return BuildFrontMaterial(source, width, height, width, height);
        }

        public static FpsSurfaceColor32[] BuildFrontMaterial(FpsSurfaceColor32[] source, int width, int height, int outputWidth, int outputHeight)
        {
            Validate(source, width, height);
            ValidateOutput(outputWidth, outputHeight);
            OpaqueBounds bounds = FindOpaqueBounds(source, width, height);
            Patch patch = SelectInteriorPatch(bounds, width, height);
            return BuildTileableMaterial(source, width, patch, outputWidth, outputHeight, 1.00f, 1.04f, 0.96f, 0.08f, 0.035f, 0.010f);
        }

        public static FpsSurfaceColor32[] BuildSideMaterial(FpsSurfaceColor32[] source, int width, int height)
        {
            return BuildSideMaterial(source, width, height, width, height);
        }

        public static FpsSurfaceColor32[] BuildSideMaterial(FpsSurfaceColor32[] source, int width, int height, int outputWidth, int outputHeight)
        {
            Validate(source, width, height);
            ValidateOutput(outputWidth, outputHeight);
            OpaqueBounds bounds = FindOpaqueBounds(source, width, height);
            Patch patch = SelectInteriorPatch(bounds, width, height);
            return BuildTileableMaterial(source, width, patch, outputWidth, outputHeight, 0.76f, 0.88f, 0.68f, 0.12f, 0.018f, 0.045f);
        }

        public static FpsSurfaceColor32[] BuildTopMaterial(FpsSurfaceColor32[] source, int width, int height)
        {
            return BuildTopMaterial(source, width, height, width, height);
        }

        public static FpsSurfaceColor32[] BuildTopMaterial(FpsSurfaceColor32[] source, int width, int height, int outputWidth, int outputHeight)
        {
            Validate(source, width, height);
            ValidateOutput(outputWidth, outputHeight);
            FpsSurfaceColor32 sample = ComputeTopRepresentativeColor(source, width, height);
            OpaqueBounds bounds = FindOpaqueBounds(source, width, height);
            Patch patch = SelectInteriorPatch(bounds, width, height);
            FpsSurfaceColor32[] front = BuildTileableMaterial(source, width, patch, outputWidth, outputHeight, 0.22f, 1.00f, 1.00f, 0.04f, 0.012f, 0.012f);
            FpsSurfaceColor32[] result = new FpsSurfaceColor32[front.Length];
            for (int y = 0; y < outputHeight; y++)
            {
                for (int x = 0; x < outputWidth; x++)
                {
                    int index = y * outputWidth + x;
                    FpsSurfaceColor32 detail = front[index];
                    bool checker = ((x + y) & 1) == 0;
                    float shade = checker ? 1.04f : 0.96f;
                    byte r = Scale((byte)Math.Round((sample.R * 0.82f) + (detail.R * 0.18f)), shade);
                    byte g = Scale((byte)Math.Round((sample.G * 0.82f) + (detail.G * 0.18f)), shade);
                    byte b = Scale((byte)Math.Round((sample.B * 0.82f) + (detail.B * 0.18f)), shade);
                    result[index] = new FpsSurfaceColor32(r, g, b, 255);
                }
            }

            ForceTileableEdges(result, outputWidth, outputHeight);
            return result;
        }

        private static FpsSurfaceColor32[] BuildTileableMaterial(
            FpsSurfaceColor32[] source,
            int sourceWidth,
            Patch patch,
            int outputWidth,
            int outputHeight,
            float detailWeight,
            float topShade,
            float bottomShade,
            float contrastBoost,
            float verticalPatternStrength,
            float horizontalPatternStrength)
        {
            ColumnAverage[] columnAverages = BuildColumnAverages(source, sourceWidth, patch);
            RowAverage[] rowAverages = BuildRowAverages(source, sourceWidth, patch);
            FpsSurfaceColor32 fallback = ComputeAverage(source, sourceWidth, patch);
            FpsSurfaceColor32[] result = new FpsSurfaceColor32[outputWidth * outputHeight];

            for (int y = 0; y < outputHeight; y++)
            {
                int patchY = patch.MinY + (y % patch.Height);
                float yLerp = outputHeight <= 1 ? 0f : y / (float)(outputHeight - 1);
                float shade = Lerp(topShade, bottomShade, yLerp);
                RowAverage row = rowAverages[y % patch.Height];
                for (int x = 0; x < outputWidth; x++)
                {
                    int patchX = patch.MinX + (x % patch.Width);
                    FpsSurfaceColor32 sample = ResolveOpaqueOrFallback(source[patchY * sourceWidth + patchX], fallback);
                    ColumnAverage column = columnAverages[x % patch.Width];

                    const float columnWeight = 0.55f;
                    const float rowWeight = 0.20f;
                    float totalWeight = detailWeight + columnWeight + rowWeight;
                    float r = ((sample.R * detailWeight) + (column.R * columnWeight) + (row.R * rowWeight)) / totalWeight;
                    float g = ((sample.G * detailWeight) + (column.G * columnWeight) + (row.G * rowWeight)) / totalWeight;
                    float b = ((sample.B * detailWeight) + (column.B * columnWeight) + (row.B * rowWeight)) / totalWeight;
                    float normalizedX = patch.Width <= 1 ? 0f : (x % patch.Width) / (float)(patch.Width - 1);
                    float normalizedY = patch.Height <= 1 ? 0f : (y % patch.Height) / (float)(patch.Height - 1);
                    float average = (r + g + b) / 3f;
                    float contrast = 1f + contrastBoost;
                    r = average + ((r - average) * contrast);
                    g = average + ((g - average) * contrast);
                    b = average + ((b - average) * contrast);
                    float periodicShade = 1f
                        + ((float)Math.Sin(normalizedX * Math.PI * 2d) * verticalPatternStrength)
                        + ((float)Math.Sin(normalizedY * Math.PI * 2d) * horizontalPatternStrength);
                    float microVariation = ((((x % patch.Width) * 13 + (y % patch.Height) * 7) & 3) - 1.5f) / 1.5f * 0.015f;
                    shade *= periodicShade + microVariation;

                    result[y * outputWidth + x] = new FpsSurfaceColor32(
                        Scale((byte)Math.Round(ClampColor(r)), shade),
                        Scale((byte)Math.Round(ClampColor(g)), shade),
                        Scale((byte)Math.Round(ClampColor(b)), shade),
                        255);
                }
            }

            ForceTileableEdges(result, outputWidth, outputHeight);
            return result;
        }

        private static ColumnAverage[] BuildColumnAverages(FpsSurfaceColor32[] source, int width, Patch patch)
        {
            ColumnAverage[] columns = new ColumnAverage[patch.Width];
            for (int x = 0; x < patch.Width; x++)
            {
                long r = 0;
                long g = 0;
                long b = 0;
                int count = 0;
                int sourceX = patch.MinX + x;
                for (int y = patch.MinY; y <= patch.MaxY; y++)
                {
                    FpsSurfaceColor32 color = source[y * width + sourceX];
                    if (color.A <= 8)
                    {
                        continue;
                    }

                    r += color.R;
                    g += color.G;
                    b += color.B;
                    count++;
                }

                if (count == 0)
                {
                    columns[x] = new ColumnAverage(128f, 128f, 128f);
                    continue;
                }

                columns[x] = new ColumnAverage(r / (float)count, g / (float)count, b / (float)count);
            }

            return columns;
        }

        private static RowAverage[] BuildRowAverages(FpsSurfaceColor32[] source, int width, Patch patch)
        {
            RowAverage[] rows = new RowAverage[patch.Height];
            for (int y = 0; y < patch.Height; y++)
            {
                long r = 0;
                long g = 0;
                long b = 0;
                int count = 0;
                int sourceY = patch.MinY + y;
                int rowOffset = sourceY * width;
                for (int x = patch.MinX; x <= patch.MaxX; x++)
                {
                    FpsSurfaceColor32 color = source[rowOffset + x];
                    if (color.A <= 8)
                    {
                        continue;
                    }

                    r += color.R;
                    g += color.G;
                    b += color.B;
                    count++;
                }

                if (count == 0)
                {
                    rows[y] = new RowAverage(128f, 128f, 128f);
                    continue;
                }

                rows[y] = new RowAverage(r / (float)count, g / (float)count, b / (float)count);
            }

            return rows;
        }

        private static FpsSurfaceColor32 ResolveOpaqueOrFallback(FpsSurfaceColor32 sample, FpsSurfaceColor32 fallback)
        {
            return sample.A > 8 ? sample : fallback;
        }

        private static FpsSurfaceColor32 ComputeAverage(FpsSurfaceColor32[] source, int width, Patch patch)
        {
            long r = 0;
            long g = 0;
            long b = 0;
            long a = 0;
            long count = 0;
            for (int y = patch.MinY; y <= patch.MaxY; y++)
            {
                int rowOffset = y * width;
                for (int x = patch.MinX; x <= patch.MaxX; x++)
                {
                    FpsSurfaceColor32 color = source[rowOffset + x];
                    if (color.A <= 8)
                    {
                        continue;
                    }

                    r += color.R;
                    g += color.G;
                    b += color.B;
                    a += color.A;
                    count++;
                }
            }

            if (count == 0)
            {
                return new FpsSurfaceColor32(128, 128, 128, 255);
            }

            return new FpsSurfaceColor32((byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)(a / count));
        }

        private static void ForceTileableEdges(FpsSurfaceColor32[] pixels, int width, int height)
        {
            if (width <= 1 || height <= 1)
            {
                return;
            }

            for (int y = 0; y < height; y++)
            {
                pixels[y * width + (width - 1)] = pixels[y * width];
            }

            int lastRow = (height - 1) * width;
            for (int x = 0; x < width; x++)
            {
                pixels[lastRow + x] = pixels[x];
            }
        }

        private static OpaqueBounds FindOpaqueBounds(FpsSurfaceColor32[] source, int width, int height)
        {
            int minX = width - 1;
            int minY = height - 1;
            int maxX = 0;
            int maxY = 0;
            bool found = false;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (source[rowOffset + x].A <= 8)
                    {
                        continue;
                    }

                    found = true;
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            return found
                ? new OpaqueBounds(minX, minY, maxX, maxY)
                : new OpaqueBounds(0, 0, width - 1, height - 1);
        }

        private static Patch SelectInteriorPatch(OpaqueBounds bounds, int width, int height)
        {
            int opaqueWidth = bounds.Width;
            int opaqueHeight = bounds.Height;
            int marginX = Math.Max(0, opaqueWidth / 4);
            int marginY = Math.Max(0, opaqueHeight / 8);

            int minX = bounds.MinX + marginX;
            int maxX = bounds.MaxX - marginX;
            int minY = bounds.MinY + marginY;
            int maxY = bounds.MaxY - marginY;

            if (maxX - minX + 1 < 2)
            {
                minX = bounds.MinX;
                maxX = bounds.MaxX;
            }

            if (maxY - minY + 1 < 2)
            {
                minY = bounds.MinY;
                maxY = bounds.MaxY;
            }

            minX = Clamp(minX, 0, width - 1);
            maxX = Clamp(maxX, minX, width - 1);
            minY = Clamp(minY, 0, height - 1);
            maxY = Clamp(maxY, minY, height - 1);

            return new Patch(minX, minY, maxX, maxY);
        }

        private static FpsSurfaceColor32 ComputeTopRepresentativeColor(FpsSurfaceColor32[] source, int width, int height)
        {
            int bandHeight = Math.Max(1, height / 3);
            if (TryAverageOpaque(source, width, 0, bandHeight, out FpsSurfaceColor32 bandAverage))
            {
                return bandAverage;
            }

            if (TryAverageOpaque(source, width, 0, height, out FpsSurfaceColor32 fullAverage))
            {
                return fullAverage;
            }

            return new FpsSurfaceColor32(128, 128, 128, 255);
        }

        private static bool TryAverageOpaque(FpsSurfaceColor32[] source, int width, int startRow, int endRowExclusive, out FpsSurfaceColor32 average)
        {
            long r = 0;
            long g = 0;
            long b = 0;
            long a = 0;
            long count = 0;
            for (int y = startRow; y < endRowExclusive; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    FpsSurfaceColor32 color = source[rowOffset + x];
                    if (color.A <= 8)
                    {
                        continue;
                    }

                    r += color.R;
                    g += color.G;
                    b += color.B;
                    a += color.A;
                    count++;
                }
            }

            if (count == 0)
            {
                average = default;
                return false;
            }

            average = new FpsSurfaceColor32(
                (byte)(r / count),
                (byte)(g / count),
                (byte)(b / count),
                (byte)(a / count));
            return true;
        }

        private static void Validate(FpsSurfaceColor32[] source, int width, int height)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("width and height must be positive.");
            }

            if (source.Length != width * height)
            {
                throw new ArgumentException("source length must match width * height.");
            }
        }

        private static void ValidateOutput(int outputWidth, int outputHeight)
        {
            if (outputWidth <= 0 || outputHeight <= 0)
            {
                throw new ArgumentException("outputWidth and outputHeight must be positive.");
            }
        }

        private static byte Scale(byte value, float factor)
        {
            return (byte)Math.Max(0, Math.Min(255, Math.Round(value * factor)));
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static float ClampColor(float value)
        {
            return Math.Max(0f, Math.Min(255f, value));
        }

        private readonly struct OpaqueBounds
        {
            public OpaqueBounds(int minX, int minY, int maxX, int maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public int MinX { get; }
            public int MinY { get; }
            public int MaxX { get; }
            public int MaxY { get; }
            public int Width => MaxX - MinX + 1;
            public int Height => MaxY - MinY + 1;
        }

        private readonly struct Patch
        {
            public Patch(int minX, int minY, int maxX, int maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public int MinX { get; }
            public int MinY { get; }
            public int MaxX { get; }
            public int MaxY { get; }
            public int Width => MaxX - MinX + 1;
            public int Height => MaxY - MinY + 1;
        }

        private readonly struct ColumnAverage
        {
            public ColumnAverage(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
            }

            public float R { get; }
            public float G { get; }
            public float B { get; }
        }

        private readonly struct RowAverage
        {
            public RowAverage(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
            }

            public float R { get; }
            public float G { get; }
            public float B { get; }
        }
    }
}
