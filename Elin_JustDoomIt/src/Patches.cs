using HarmonyLib;

namespace Elin_ModTemplate
{
    [HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
    public static class Patch_Zone_Activate_CasinoPlacement
    {
        private const string DoomArcadeThingId = "justdoomit_arcade";
        private const int TargetX = 49;
        private const int TargetZ = 66;

        static void Postfix(Zone __instance)
        {
            try
            {
                if (__instance == null || __instance.map == null || EClass._map == null)
                {
                    return;
                }

                if (!IsTargetFortuneBell(__instance))
                {
                    return;
                }

                // Idempotent placement: skip if cabinet already exists in this map.
                foreach (Thing thing in EClass._map.things)
                {
                    if (thing != null && thing.id == DoomArcadeThingId && thing.ExistsOnMap)
                    {
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
                placed?.Install();
                DoomDiagnostics.Info("[JustDoomIt] Placed arcade cabinet at " + placePoint +
                    " zone=" + __instance.id + " lv=" + __instance.lv + ".");
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] Zone.Activate casino placement failed.", ex);
            }
        }

        private static bool IsTargetFortuneBell(Zone zone)
        {
            return string.Equals(zone?.id, "casino", System.StringComparison.OrdinalIgnoreCase);
        }

        private static Point FindPlacementPoint(Zone zone)
        {
            var target = new Point(TargetX, TargetZ);
            if (EClass._map.Contains(target))
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
    }
}
