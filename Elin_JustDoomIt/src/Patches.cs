using HarmonyLib;
using System.Collections.Generic;

namespace Elin_ModTemplate
{
    [HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
    public static class Patch_Zone_Activate_CasinoPlacement
    {
        private const string DoomArcadeThingId = "justdoomit_arcade";
        // Elin's zone.lv is 0-based for above-ground floors: 0=1F, 1=2F, ...
        private const int TargetFloorLv = 1;
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

                if (!IsCasinoZone(__instance))
                {
                    return;
                }

                if (__instance.lv != TargetFloorLv)
                {
                    RemoveArcadesInZoneMap(__instance);
                    return;
                }

                // Idempotent placement: skip if cabinet already exists in this map.
                foreach (Thing thing in __instance.map.things)
                {
                    if (thing != null && thing.id == DoomArcadeThingId && thing.ExistsOnMap)
                    {
                        ApplyCasinoOwnershipFlags(thing);
                        return;
                    }
                }

                Point placePoint = FindPlacementPoint(__instance);
                if (placePoint == null)
                {
                    DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell placement skipped: no valid point found. " +
                        "zone=" + __instance.id + " lv=" + __instance.lv);
                    return;
                }

                Card placed = __instance.AddCard(ThingGen.Create(DoomArcadeThingId), placePoint);
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

        private static void RemoveArcadesInZoneMap(Zone zone)
        {
            if (zone?.map?.things == null)
            {
                return;
            }

            var toRemove = new List<Thing>();
            foreach (Thing thing in zone.map.things)
            {
                if (thing != null && thing.id == DoomArcadeThingId && thing.ExistsOnMap)
                {
                    toRemove.Add(thing);
                }
            }

            if (toRemove.Count == 0)
            {
                return;
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                toRemove[i]?.Destroy();
            }

            DoomDiagnostics.Info("[JustDoomIt] Removed " + toRemove.Count +
                " arcade cabinet(s) from non-target casino floor. zone=" + zone.id + " lv=" + zone.lv);
        }

        private static Point FindPlacementPoint(Zone zone)
        {
            var target = new Point(TargetX, TargetZ);
            if (zone?.map != null && zone.map.bounds != null && zone.map.bounds.Contains(target))
            {
                // Prefer exact coordinate first.
                if (!target.IsBlocked && !target.HasChara)
                {
                    return target;
                }

                // If occupied, fallback to nearest valid tile.
                var nearest = target.GetNearestPoint(
                    allowBlock: false,
                    allowChara: false,
                    allowInstalled: false,
                    ignoreCenter: true);
                return nearest ?? target;
            }

            DoomDiagnostics.Warn("[JustDoomIt] Fortune Bell fixed coordinate is outside map bounds: (" +
                TargetX + "," + TargetZ + "), zone=" + zone.id + " lv=" + zone.lv);
            return zone.bounds?.GetCenterPos()?.GetNearestPoint(allowBlock: false, allowChara: false, allowInstalled: false);
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
