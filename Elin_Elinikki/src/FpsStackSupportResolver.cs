using UnityEngine;

namespace Elin_Elinikki
{
    internal static class FpsStackSupportResolver
    {
        public static float ResolveGroundElevation(Card card)
        {
            if (card == null)
            {
                return 0f;
            }

            float elevation = ResolveVisualSupportHeight(card);
            elevation += Mathf.Clamp(card.altitude, 0, 4) * 0.06f;
            if (card is Thing thing && !card.ignoreStackHeight)
            {
                elevation += Mathf.Clamp(thing.stackOrder, 0, 6) * 0.025f;
            }

            return Mathf.Clamp(elevation, 0f, 3f);
        }

        public static float ResolveBillboardElevation(Card card, BillboardKind kind)
        {
            if (card == null)
            {
                return 0f;
            }

            float elevation = ResolveVisualSupportHeight(card);
            BaseTileMap tileMap = EClass.screen?.tileMap;
            if (tileMap != null && card.altitude != 0)
            {
                elevation += card.altitude * 0.12f;
            }

            if (card is Thing thing && !card.ignoreStackHeight)
            {
                elevation += Mathf.Clamp(thing.stackOrder, 0, 6) * 0.05f;
            }

            if (kind == BillboardKind.InstalledObject || kind == BillboardKind.TallObject)
            {
                SourcePref pref = card.Pref;
                if (pref != null)
                {
                    elevation += Mathf.Clamp(pref.height * 0.08f, 0f, 0.25f);
                }
            }
            else if (kind == BillboardKind.Chara)
            {
                elevation += 0.02f;
            }

            return Mathf.Clamp(elevation, 0f, 3f);
        }

        private static float ResolveVisualSupportHeight(Card card)
        {
            if (!(card is Thing target) || target.ignoreStackHeight || target.pos?.cell?.detail == null)
            {
                return 0f;
            }

            CellDetail detail = target.pos.cell.detail;
            float supportHeight = 0f;
            float lastStackHeight = 0f;
            Card lastInstalled = null;
            for (int i = 0; i < detail.things.Count; i++)
            {
                Thing thing = detail.things[i];
                if (thing == target)
                {
                    break;
                }

                if (!thing.IsInstalled)
                {
                    continue;
                }

                TileType tileType = thing.TileType;
                if (!tileType.CanStack)
                {
                    continue;
                }

                float stackHeight = ResolveInstalledVisualSupportHeight(thing, tileType, thing.Pref);
                if (thing.ignoreStackHeight)
                {
                    supportHeight -= lastStackHeight;
                }

                supportHeight += stackHeight;
                if (!tileType.UseMountHeight && thing.altitude != 0)
                {
                    stackHeight += Mathf.Clamp(thing.altitude, 0, 6) * 0.06f;
                }

                if (thing.trait.IgnoreLastStackHeight && (lastInstalled == null || !lastInstalled.trait.IgnoreLastStackHeight))
                {
                    supportHeight -= lastStackHeight;
                }

                lastStackHeight = stackHeight;
                lastInstalled = thing;
            }

            if (target.ignoreStackHeight)
            {
                supportHeight -= lastStackHeight;
            }

            return Mathf.Clamp(supportHeight, 0f, 3f);
        }

        private static float ResolveInstalledSupportHeight(Thing thing, TileType tileType, SourcePref pref)
        {
            if (thing == null || tileType == null)
            {
                return 0f;
            }

            float stackHeight = tileType.UseMountHeight
                ? 0f
                : ((pref == null || pref.height < 0f) ? 0f : ((Mathf.Abs(pref.height) <= 0.0001f) ? 0.1f : pref.height));

            if (stackHeight <= 0f)
            {
                return stackHeight;
            }

            return stackHeight;
        }

        private static float ResolveInstalledVisualSupportHeight(Thing thing, TileType tileType, SourcePref pref)
        {
            float stackHeight = ResolveInstalledSupportHeight(thing, tileType, pref);
            if (thing == null || tileType == null)
            {
                return stackHeight;
            }

            if (pref != null && pref.Surface)
            {
                float surfaceHeight = pref.height + Mathf.Clamp(thing.altitude, 0, 6) * 0.1f;
                return Mathf.Max(stackHeight, surfaceHeight);
            }

            return stackHeight;
        }
    }
}
