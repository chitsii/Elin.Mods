using System;
using System.Collections.Generic;
using Elin_Elinikki.Quest.Quest;

namespace Elin_Elinikki.Quest.Placement
{
    /// <summary>
    /// Swap driver for the per-layer 3D trace objects that live in
    /// <see cref="SharedWorldObjectManager"/>. Invoked by
    /// <c>Patch_Zone_Activate_PlacementRefresh</c> on every Zone.Activate
    /// postfix so that entering an Elinikki chapter zone installs that
    /// layer's trace set, and entering any other zone (world map, a
    /// non-quest Nefia, etc.) removes any prior Elinikki set.
    ///
    /// <para>Task 3.3 wiring: on every zone activation the manager
    /// first wipes the prior Elinikki set with
    /// <see cref="SharedWorldObjectManager.RemoveDefinitionsByPrefix"/>
    /// using <see cref="PlacementPrefix"/> as the filter, then — if the
    /// new zone is Elinikki-owned — re-upserts the per-layer
    /// definitions returned by <see cref="ElinikkiPlacementData"/>.
    /// Running the clear unconditionally keeps the operation idempotent
    /// and makes a late zone notification (e.g. save reload back into
    /// the same chapter zone) self-heal rather than duplicate entries.</para>
    ///
    /// Previous-zone tracking is kept deliberately isolated from
    /// <c>ElinikkiQuestFlow._lastObservedZoneId</c>: quest progress and
    /// visual placement should not share mutable state, so a reset of
    /// one cannot desync the other.
    /// </summary>
    public static class ElinikkiZonePlacementManager
    {
        /// <summary>
        /// Shared id prefix for every definition registered by this
        /// manager into <see cref="SharedWorldObjectManager"/>. Task 3.3
        /// uses this as the argument to the manager's
        /// <c>RemoveDefinitionsByPrefix</c> pipeline so a zone swap
        /// cleanly removes the prior set without touching definitions
        /// owned by other subsystems (notably the manager's own
        /// <c>"demo/"</c> dream-test set).
        /// </summary>
        public const string PlacementPrefix = "elinikki/";

        /// <summary>
        /// Set of zone content ids the quest owns. Used to decide
        /// whether the incoming zone is Elinikki-scoped, and whether the
        /// outgoing one was. A transition between two Elinikki zones is
        /// a layer swap; elinikki -> non-elinikki is a clear; anything
        /// else is a no-op.
        /// </summary>
        private static readonly HashSet<string> ElinikkiZoneIdSet = new HashSet<string>(
            new[]
            {
                ElinikkiZoneIds.NefiaEntrance,
                ElinikkiZoneIds.LayerWaterstone,
                ElinikkiZoneIds.LayerEcho,
                ElinikkiZoneIds.LayerBloom,
                ElinikkiZoneIds.YuuCamp,
            },
            StringComparer.Ordinal);

        /// <summary>
        /// Last zone id observed by <see cref="OnZoneActivated"/>. Null
        /// until the first call. Used exclusively to classify the
        /// transition kind (enter / swap / leave) so the Task 3.3
        /// pipeline can decide whether to rebuild the active set.
        /// </summary>
        private static string _lastZoneId;

        /// <summary>
        /// Called by the Zone.Activate postfix patch with the content id
        /// of the zone the player has just entered. A null or empty
        /// <paramref name="zoneId"/> (startup, headless session, zone
        /// whose source failed to load) still triggers a cleanup of
        /// any prior Elinikki definitions — otherwise leaving a chapter
        /// zone into a runtime-generated map without a resolved source
        /// id would leak the old trace set into the next map. The last
        /// observed zone id is only rotated when a real id was
        /// supplied, so the next real activation can still classify
        /// its transition against the last known chapter zone.
        /// </summary>
        public static void OnZoneActivated(string zoneId)
        {
            string previousZoneId = _lastZoneId;
            bool haveZoneId = !string.IsNullOrEmpty(zoneId);

            if (haveZoneId)
            {
                _lastZoneId = zoneId;
            }

            bool isInElinikkiZone = haveZoneId && ElinikkiZoneIdSet.Contains(zoneId);
            bool wasInElinikkiZone = !string.IsNullOrEmpty(previousZoneId)
                                     && ElinikkiZoneIdSet.Contains(previousZoneId);

            QuestModLog.Info(
                "Zone placement refresh. zone=" + (zoneId ?? "<null>") +
                " prev=" + (previousZoneId ?? "<null>") +
                " elinikki=" + isInElinikkiZone +
                " wasElinikki=" + wasInElinikkiZone);

            // Sync the atmosphere runtime FIRST, before the
            // shared-world cleanup. If
            // RemoveDefinitionsByPrefix/Upsert later throws, the outer
            // Harmony postfix catches and this method exits early —
            // but the atmosphere state is already correct for the new
            // zone, so the renderer never keeps tinting with the
            // previous chapter's profile.
            //
            // Behaviour by case:
            //   * Elinikki zone with known id   → SetZone(zoneId)
            //   * known non-Elinikki zone       → Clear()
            //   * null/empty zoneId (source not
            //     yet attached, runtime-generated
            //     map)                          → Clear()
            //
            // Null-zone activations clear the override so that
            // reloading out of a chapter zone into a non-Elinikki map
            // can never leak the prior chapter's fog/clear/LUT tint,
            // which would be a very visible bug. The cost is that a
            // reload *into* an Elinikki chapter would miss the tint
            // on the first frame if its source is not yet resolved —
            // but the next Zone.Activate with a real id will set the
            // profile correctly, so this self-heals on the first
            // subsequent activation.
            if (isInElinikkiZone)
            {
                ElinikkiAtmosphereRuntime.SetZone(zoneId);
            }
            else
            {
                ElinikkiAtmosphereRuntime.Clear();
            }

            // Now drop any prior Elinikki placements. This is
            // idempotent and scoped to PlacementPrefix so it cannot
            // affect other subsystems' definitions (notably
            // SharedWorldObjectManager's own "demo/" dream-test set,
            // which uses a different prefix). Keeps the null-zone-id
            // path covered: a runtime-generated or source-less zone
            // still drops the stale placements or they would keep
            // rendering in the wrong map until the next real
            // activation resolves.
            SharedWorldObjectManager.RemoveDefinitionsByPrefix(PlacementPrefix);

            // Leaving an Elinikki zone (or entering a zone with no
            // resolved id): the clear above is all we need.
            if (!isInElinikkiZone)
            {
                return;
            }

            // Entering an Elinikki zone: upsert the per-layer definitions
            // from ElinikkiPlacementData. Each definition's id already
            // starts with PlacementPrefix, so the next clear on a zone
            // exit will wipe exactly this set.
            int upserted = 0;
            foreach (SharedWorldPrimitiveDefinition definition
                     in ElinikkiPlacementData.GetDefinitionsForZone(zoneId))
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Id)
                    || !definition.Id.StartsWith(PlacementPrefix, StringComparison.Ordinal))
                {
                    // Fail-loud: a definition that escapes the shared
                    // prefix would survive the next zone clear and
                    // stack up over repeated visits. The story spec
                    // requires every Elinikki placement to share the
                    // prefix so the swap pipeline is symmetric, so an
                    // offender is a data bug, not a runtime concern.
                    QuestModLog.Error(
                        "Rejected placement definition for zone " + zoneId +
                        ": id '" + (definition.Id ?? "<null>") +
                        "' is missing the '" + PlacementPrefix + "' prefix.");
                    continue;
                }

                SharedWorldObjectManager.Upsert(definition);
                upserted++;
            }

            QuestModLog.Info(
                "Zone placement upsert complete. zone=" + zoneId +
                " count=" + upserted);
        }

        /// <summary>
        /// Returns true when the given zone content id is owned by the
        /// Elinikki quest. Exposed for Task 3.3 so the Remove/Upsert
        /// pipeline can share the same classifier as the hook.
        /// </summary>
        public static bool IsElinikkiZone(string zoneId)
        {
            return !string.IsNullOrEmpty(zoneId) && ElinikkiZoneIdSet.Contains(zoneId);
        }

        /// <summary>
        /// Test/diagnostics helper: returns the zone id last processed
        /// by <see cref="OnZoneActivated"/>. Do not use for game logic.
        /// </summary>
        public static string LastZoneIdForDiagnostics => _lastZoneId;

        /// <summary>
        /// Test/tooling helper: clears the internal last-zone tracker so
        /// the next <see cref="OnZoneActivated"/> call is treated as a
        /// fresh entry. Exposed for the Elinikki self-test runner; do
        /// not call from game logic.
        /// </summary>
        public static void ResetStateForTests()
        {
            _lastZoneId = null;
        }
    }
}
