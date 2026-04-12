using UnityEngine;

namespace Elin_Elinikki
{
    internal static class FpsSpritePlacementResolver
    {
        public static float ResolveBaseHeight(BillboardKind kind)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return 1.05f;
                case BillboardKind.InstalledObject:
                    return 0.9f;
                case BillboardKind.TallObject:
                    return 1.3f;
                default:
                    return 0.18f;
            }
        }

        public static float ResolvePivotY(BillboardKind kind)
        {
            switch (kind)
            {
                case BillboardKind.Chara:
                    return 0.04f;
                case BillboardKind.InstalledObject:
                    return 0.03f;
                case BillboardKind.TallObject:
                    return 0.02f;
                default:
                    return 0.01f;
            }
        }

        public static Vector2 ApplyScatterOffset(Vector2 position, bool freePos, float fx, float fy, BillboardKind kind)
        {
            if (freePos && kind != BillboardKind.LooseItem)
            {
                return new Vector2(
                    position.x + Mathf.Clamp(fx * 0.05f, -0.12f, 0.12f),
                    position.y + Mathf.Clamp(fy * 0.05f, -0.12f, 0.12f));
            }

            return position;
        }

        public static Vector2 ResolveGroundScatter(bool freePos, float fx, float fy, int stableHash)
        {
            if (freePos)
            {
                return new Vector2(
                    Mathf.Clamp(fx * 0.03f, -0.08f, 0.08f),
                    Mathf.Clamp(fy * 0.03f, -0.08f, 0.08f));
            }

            float offsetX = (((stableHash >> 1) & 3) - 1.5f) * 0.025f;
            float offsetZ = (((stableHash >> 3) & 3) - 1.5f) * 0.025f;
            return new Vector2(offsetX, offsetZ);
        }
    }
}
