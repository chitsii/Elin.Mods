namespace Elin_Elinikki
{
    internal static class FpsSpriteVisibilityResolver
    {
        private const float LargeObjectDistanceMultiplier = 1.45f;
        private const float GameplayDistanceMultiplier = 1.35f;
        private const float ItemDistanceMultiplier = 1.1f;
        private const float EffectDistanceMultiplier = 0.9f;
        private const float LargeObjectConeDot = 0.17364818f;
        private const float SmallObjectConeDot = 0.259f;
        private const float GameplayConeDot = 0.0f;

        public static float ResolveCardVisibilityDistance(BillboardKind kind, float maxDistance)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return maxDistance * GameplayDistanceMultiplier;
                case BillboardKind.InstalledObject:
                case BillboardKind.TallObject:
                    return maxDistance * LargeObjectDistanceMultiplier;
                default:
                    return maxDistance * ItemDistanceMultiplier;
            }
        }

        public static float ResolveCardVisibilityConeDot(BillboardKind kind)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return GameplayConeDot;
                case BillboardKind.InstalledObject:
                case BillboardKind.TallObject:
                    return LargeObjectConeDot;
                default:
                    return SmallObjectConeDot;
            }
        }

        public static float ResolveCellObjectVisibilityDistance(bool upright, float maxDistance)
        {
            return upright
                ? maxDistance * LargeObjectDistanceMultiplier
                : maxDistance * GameplayDistanceMultiplier;
        }

        public static float ResolveCellObjectVisibilityConeDot(bool upright)
        {
            return upright ? LargeObjectConeDot : SmallObjectConeDot;
        }

        public static float ResolveEffectVisibilityDistance(float maxDistance)
        {
            return maxDistance * EffectDistanceMultiplier;
        }

        public static float GameplayConeDotValue => GameplayConeDot;
    }
}
