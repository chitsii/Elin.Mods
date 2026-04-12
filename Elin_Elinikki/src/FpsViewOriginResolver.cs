using UnityEngine;

namespace Elin_Elinikki
{
    internal static class FpsViewOriginResolver
    {
        public static Vector2 Resolve(Vector2 logicalOrigin, Vector2 projectedOrigin, bool lockToLogicalOrigin, float maxProjectedDeviation)
        {
            if (lockToLogicalOrigin)
            {
                return logicalOrigin;
            }

            if ((projectedOrigin - logicalOrigin).sqrMagnitude > maxProjectedDeviation * maxProjectedDeviation)
            {
                return logicalOrigin;
            }

            return projectedOrigin;
        }

        public static Vector2 ResolveAxisLocked(Vector2 logicalOrigin, Vector2 projectedOrigin, int dir, float maxAxisDeviation)
        {
            float axisDelta;
            switch (NormalizeDir(dir))
            {
                case 0:
                case 2:
                    axisDelta = projectedOrigin.y - logicalOrigin.y;
                    if (Mathf.Abs(axisDelta) > maxAxisDeviation)
                    {
                        return logicalOrigin;
                    }

                    return new Vector2(logicalOrigin.x, projectedOrigin.y);
                case 1:
                case 3:
                    axisDelta = projectedOrigin.x - logicalOrigin.x;
                    if (Mathf.Abs(axisDelta) > maxAxisDeviation)
                    {
                        return logicalOrigin;
                    }

                    return new Vector2(projectedOrigin.x, logicalOrigin.y);
                default:
                    return logicalOrigin;
            }
        }

        public static bool IsAxisSettled(Vector2 logicalOrigin, Vector2 projectedOrigin, int dir, float settleThreshold)
        {
            switch (NormalizeDir(dir))
            {
                case 0:
                case 2:
                    return Mathf.Abs(projectedOrigin.y - logicalOrigin.y) <= settleThreshold;
                case 1:
                case 3:
                    return Mathf.Abs(projectedOrigin.x - logicalOrigin.x) <= settleThreshold;
                default:
                    return true;
            }
        }

        private static int NormalizeDir(int dir)
        {
            int normalized = dir % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }
    }
}
