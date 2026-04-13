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
    /// Task 3.1 scaffold: this class is intentionally minimal. It only
    /// tracks the last observed zone id and logs the transition. The
    /// actual Remove + Upsert pipeline is wired up in Task 3.3 using the
    /// <see cref="PlacementPrefix"/> shared id prefix. Task 3.2 fills in
    /// the per-layer placement data this method will drive.
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
        /// whose source failed to load) is treated as a no-op: the
        /// manager keeps whatever prior state it had so the next real
        /// activation can still classify the transition correctly.
        /// </summary>
        public static void OnZoneActivated(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                return;
            }

            string previousZoneId = _lastZoneId;
            _lastZoneId = zoneId;

            bool isInElinikkiZone = ElinikkiZoneIdSet.Contains(zoneId);
            bool wasInElinikkiZone = !string.IsNullOrEmpty(previousZoneId)
                                     && ElinikkiZoneIdSet.Contains(previousZoneId);

            QuestModLog.Info(
                "Zone placement refresh. zone=" + zoneId +
                " prev=" + (previousZoneId ?? "<null>") +
                " elinikki=" + isInElinikkiZone +
                " wasElinikki=" + wasInElinikkiZone);

            // Task 3.1 scaffold: logging only. Task 3.3 will replace this
            // with the concrete RemoveDefinitionsByPrefix + Upsert pipeline,
            // using Task 3.2's per-layer placement data as its input.
            // Leaving the hook live now means the in-game telemetry is
            // available immediately and the Task 3.3 change set is limited
            // to filling in the body of this method.
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
