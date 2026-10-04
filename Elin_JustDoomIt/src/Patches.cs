using HarmonyLib;

namespace Elin_JustDoomIt
{
    [HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
    public static class Patch_Zone_Activate_CasinoPlacement
    {
        private const string DoomArcadeThingId = "justdoomit_arcade";
        // Elin's zone.lv is 0-based for above-ground floors: 0=1F, 1=2F, ...
        private const int TargetFloorLv = DoomArcadePlacementPolicy.TargetFloorLv;
        private const int TargetX = 49;
        private const int TargetZ = 66;

        static void Postfix(Zone __instance)
        {
            try
            {
                if (__instance == null || __instance.map == null)
                {
                    return;
                }

                var isCasinoZone = IsCasinoZone(__instance);
                if (!isCasinoZone)
                {
                    return;
                }

                var hasExistingCabinet = HasExistingArcadeCabinet(__instance);
                var sourceReady = true;
                if (__instance.lv == TargetFloorLv && !hasExistingCabinet)
                {
                    sourceReady = IsArcadeSourceReady();
                }

                var decision = DoomArcadePlacementPolicy.Decide(
                    isCasinoZone,
                    __instance.lv,
                    hasExistingCabinet,
                    sourceReady);

                if (decision == DoomArcadePlacementDecision.SkipMissingSource)
                {
                    DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell placement skipped: custom source not ready. " +
                        "id=" + DoomArcadeThingId + " zone=" + __instance.id + " lv=" + __instance.lv);
                    return;
                }

                if (decision != DoomArcadePlacementDecision.PlaceNewCabinet)
                {
                    return;
                }

                Point placePoint = FindPlacementPoint(__instance);
                if (placePoint == null)
                {
                    DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell placement skipped: no valid point found. " +
                        "zone=" + __instance.id + " lv=" + __instance.lv);
                    return;
                }

                Thing arcade = CreateValidatedArcadeThing();
                if (arcade == null)
                {
                    return;
                }

                Card placed = __instance.AddCard(arcade, placePoint);
                ApplyCasinoOwnershipFlags(placed);
                placed?.Install();
                DoomDiagnostics.Info("[JustDoomIt] Placed arcade cabinet at " + placePoint +
                    " zone=" + __instance.id + " lv=" + __instance.lv + ".");
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] Zone.Activate casino placement failed.", ex);
            }
        }

        private static bool IsCasinoZone(Zone zone)
        {
            return string.Equals(zone?.id, "casino", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasExistingArcadeCabinet(Zone zone)
        {
            if (zone?.map?.things == null)
            {
                return false;
            }

            foreach (Thing thing in zone.map.things)
            {
                if (thing != null && thing.id == DoomArcadeThingId && thing.ExistsOnMap)
                {
                    return true;
                }
            }

            return false;
        }

        private static Point FindPlacementPoint(Zone zone)
        {
            var target = new Point(TargetX, TargetZ);
            if (zone?.map != null && zone.map.bounds != null && zone.map.bounds.Contains(target))
            {
                // Prefer exact coordinate first.
                if (IsValidPlacementPoint(target))
                {
                    return target;
                }

                // If occupied, fallback to nearest valid tile.
                var nearest = target.GetNearestPoint(
                    allowBlock: false,
                    allowChara: false,
                    allowInstalled: false,
                    ignoreCenter: true);
                return IsValidPlacementPoint(nearest) ? nearest : null;
            }

            DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell fixed coordinate is outside map bounds: (" +
                TargetX + "," + TargetZ + "), zone=" + zone.id + " lv=" + zone.lv);
            var fallback = zone.bounds?.GetCenterPos()?.GetNearestPoint(allowBlock: false, allowChara: false, allowInstalled: false);
            return IsValidPlacementPoint(fallback) ? fallback : null;
        }

        private static bool IsValidPlacementPoint(Point point)
        {
            return point != null &&
                   point.IsValid &&
                   !point.IsBlocked &&
                   !point.HasChara &&
                   !point.HasThing;
        }

        private static bool IsArcadeSourceReady()
        {
            var hasCard = EClass.sources?.cards?.map?.ContainsKey(DoomArcadeThingId) ?? false;
            var hasThing = EClass.sources?.things?.map?.ContainsKey(DoomArcadeThingId) ?? false;
            return hasCard && hasThing;
        }

        private static Thing CreateValidatedArcadeThing()
        {
            Thing arcade = ThingGen.Create(DoomArcadeThingId);
            if (arcade == null)
            {
                DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell placement skipped: ThingGen returned null for " + DoomArcadeThingId + ".");
                return null;
            }

            if (!string.Equals(arcade.id, DoomArcadeThingId, System.StringComparison.OrdinalIgnoreCase))
            {
                DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell placement skipped: generated fallback id=" + arcade.id +
                    " expected=" + DoomArcadeThingId + ".");
                return null;
            }

            if (!(arcade.trait is TraitJustDoomArcade))
            {
                var traitName = arcade.trait != null ? arcade.trait.GetType().FullName : "(null)";
                DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell placement skipped: generated trait=" + traitName +
                    " expected=" + nameof(TraitJustDoomArcade) + ".");
                return null;
            }

            return arcade;
        }

        private static void ApplyCasinoOwnershipFlags(Card card)
        {
            if (card == null)
            {
                return;
            }

            // Casino-placed cabinet should be treated as NPC property (steal/crime rules).
            card.isNPCProperty = true;
            card.isStolen = false;
            card.isLostProperty = false;
        }
    }
}
