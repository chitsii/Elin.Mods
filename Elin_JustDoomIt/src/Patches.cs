using HarmonyLib;

namespace Elin_ModTemplate
{
    [HarmonyPatch(typeof(TraitSlotMachine), nameof(TraitSlotMachine.OnUse))]
    public static class Patch_TraitSlotMachine_OnUse
    {
        private const string DoomArcadeThingId = "justdoomit_arcade";

        static bool Prefix(TraitSlotMachine __instance, Chara c, ref bool __result)
        {
            try
            {
                if (__instance == null || __instance.owner == null)
                {
                    return true;
                }

                // Only handle the custom DOOM cabinet here.
                if (__instance.owner.id != DoomArcadeThingId)
                {
                    return true;
                }

                if (DoomSessionManager.Instance == null)
                {
                    return true;
                }

                if (!DoomSessionManager.Instance.TryHandleMachineUse(__instance.owner, c, ref __result))
                {
                    // Fallback to vanilla behavior when no WAD is available, etc.
                    return true;
                }

                // We handled the interaction.
                return false;
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] TraitSlotMachine.OnUse patch failed. Fallback to vanilla.", ex);
                return true;
            }
        }
    }

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
            var name = zone.Name ?? string.Empty;
            if (name.Contains("フォーチュン・ベル") || name.Contains("Fortune Bell"))
            {
                return true;
            }

            // Fallback for non-localized names/ids.
            var id = (zone.id ?? string.Empty).ToLowerInvariant();
            return id.Contains("fortunebell") || id.Contains("fortune_bell");
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
