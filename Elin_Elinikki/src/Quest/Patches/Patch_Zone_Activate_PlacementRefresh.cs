using System;
using System.Reflection;
using Elin_Elinikki.Quest.Placement;
using HarmonyLib;

namespace Elin_Elinikki.Quest.Patches
{
    /// <summary>
    /// Harmony postfix on <c>Zone.Activate</c> that drives the
    /// <see cref="ElinikkiZonePlacementManager"/> so zone-scoped 3D
    /// objects can be swapped when the player enters or leaves an
    /// Elinikki chapter zone. Runs independently of
    /// <c>Patch_Zone_Activate_QuestPulse</c>: both patches target the
    /// same method, but quest progression and visual placement deliberately
    /// do not share mutable state — a fault in one subsystem's postfix
    /// should not break the other.
    ///
    /// Fail-soft: any exception is caught and logged without
    /// propagating, because this runs inside a Harmony postfix and must
    /// never break zone loading.
    ///
    /// Target resolution mirrors <c>Patch_Zone_Activate_QuestPulse</c>:
    /// scan <see cref="Zone"/> for instance methods literally named
    /// "Activate" and pick the lowest-arity overload. Kept inline rather
    /// than shared to keep each patch file a single self-contained unit;
    /// factoring this out is explicitly deferred.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Zone_Activate_PlacementRefresh
    {
        public static MethodBase TargetMethod()
        {
            try
            {
                var methods = typeof(Zone).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo best = null;
                int bestParamCount = int.MaxValue;

                for (int i = 0; i < methods.Length; i++)
                {
                    var method = methods[i];
                    if (method == null || !string.Equals(method.Name, "Activate", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int paramCount = method.GetParameters().Length;
                    if (best == null || paramCount < bestParamCount)
                    {
                        best = method;
                        bestParamCount = paramCount;
                    }
                }

                if (best == null)
                {
                    QuestModLog.Error("Patch target not found: Zone.Activate (placement refresh)");
                }

                return best;
            }
            catch (Exception ex)
            {
                QuestModLog.Error(
                    "Patch target resolution failed (placement refresh): " + ex.Message);
                return null;
            }
        }

        public static void Postfix()
        {
            try
            {
                // Zone.source.id is the content id (matches
                // ElinikkiZoneIds). A null source (e.g. a runtime-generated
                // zone that has not yet attached its source definition) is
                // treated as "unknown" — the manager is responsible for
                // handling null safely.
                var zone = EClass._zone;
                string zoneId = zone?.source?.id;
                ElinikkiZonePlacementManager.OnZoneActivated(zoneId);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn(
                    "Zone.Activate placement refresh postfix failed: " + ex.Message);
            }
        }
    }
}
