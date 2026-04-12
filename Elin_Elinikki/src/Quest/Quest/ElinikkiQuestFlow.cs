using System;
using System.Collections.Generic;

namespace Elin_Elinikki.Quest.Quest
{
    /// <summary>
    /// Chapter-based quest flow driver for "帰らなかった遠足". Replaces the
    /// generic QuestMod template flow (Bootstrap/Intro/Followup/Completed)
    /// with the 8-stage progression defined in
    /// <see cref="ElinikkiQuestStage"/> and
    /// <c>story/chapters/_index.md</c>.
    ///
    /// Transitions come from two sources:
    /// 1. Drama scripts call <see cref="TryAdvanceStage"/> directly for
    ///    narrative beats (intro drama -> Accepted, reunion drama -> YuuFound,
    ///    ending drama -> EndingSeen). These are routed through
    ///    <c>QuestDramaResolver</c> via keys like
    ///    <c>cmd.elinikki.stage.advance.accepted</c>.
    /// 2. <see cref="Pulse"/> (invoked from
    ///    <c>Patch_Zone_Activate_QuestPulse</c>) inspects the zone the player
    ///    is entering and advances the stage if a zone rule matches the
    ///    current stage as its predecessor. This covers zone-exit transitions
    ///    (Layer1/2/3 clear, Returned) without requiring per-zone drama.
    ///
    /// Zone rules are predecessor-gated: entering a zone only advances to its
    /// target stage if the player's current stage matches the rule's
    /// <see cref="ZoneStageRule.Predecessor"/>. This prevents a reused zone
    /// (for example the shared Nefia entrance, visited in chapter 0 and again
    /// in chapter 5) from jumping the quest forward incorrectly.
    ///
    /// Stages are forward-only: <see cref="ElinikkiQuestStageExtensions.AdvanceToStage"/>
    /// silently ignores attempts to move backwards.
    /// </summary>
    public static class ElinikkiQuestFlow
    {
        private sealed class ZoneStageRule
        {
            public readonly ElinikkiQuestStage Predecessor;
            public readonly ElinikkiQuestStage Target;

            /// <summary>
            /// Optional. When non-null the rule only fires if the player's
            /// previous zone (the one they came from) equals this id. Used
            /// to gate transitions whose destination map is also reachable
            /// from the world map or recall: the rule must only match an
            /// actual walk from the source chapter zone.
            /// </summary>
            public readonly string RequirePreviousZoneId;

            public ZoneStageRule(
                ElinikkiQuestStage predecessor,
                ElinikkiQuestStage target,
                string requirePreviousZoneId = null)
            {
                Predecessor = predecessor;
                Target = target;
                RequirePreviousZoneId = requirePreviousZoneId;
            }
        }

        /// <summary>
        /// Maps a zone content-id to a list of ordered advancement rules. One
        /// zone may carry multiple rules because the same map can be reused
        /// across chapters (e.g. the Nefia entrance). Rules are evaluated in
        /// registration order and the first matching rule fires; a single
        /// Pulse never triggers more than one rule per zone.
        /// </summary>
        private static readonly Dictionary<string, List<ZoneStageRule>> _zoneStageRules
            = new Dictionary<string, List<ZoneStageRule>>(StringComparer.Ordinal);

        private static bool _defaultRulesRegistered;

        /// <summary>
        /// Tracks the zone id we were in on the previous <see cref="Pulse"/>
        /// call, so rules with <see cref="ZoneStageRule.RequirePreviousZoneId"/>
        /// can verify the player actually walked from the expected source
        /// zone rather than teleporting into the destination.
        /// </summary>
        private static string _lastObservedZoneId;

        /// <summary>
        /// Registers the canonical zone-stage rules for the "帰らなかった遠足"
        /// quest. Called from <c>QuestBootstrap.Initialize</c> at mod startup
        /// so the flow is live as soon as the player enters any chapter zone
        /// (Phase 5 will create the actual devmode maps bound to the same
        /// ids defined in <see cref="ElinikkiZoneIds"/>).
        ///
        /// CRUCIAL: <see cref="Pulse"/> runs after <c>Zone.Activate</c> on the
        /// DESTINATION zone, so "clear" stages must be attached to the NEXT
        /// chapter's entry, not the one being cleared. Entering Echo after
        /// having been Accepted means Waterstone has just been cleared, so
        /// the stage advances to Layer1Clear on that entry.
        /// </summary>
        public static void RegisterDefaultZoneRules()
        {
            if (_defaultRulesRegistered)
            {
                return;
            }

            // Chapter 1 cleared: player walks from Waterstone into Echo.
            RegisterZoneStage(
                ElinikkiZoneIds.LayerEcho,
                predecessor: ElinikkiQuestStage.Accepted,
                target: ElinikkiQuestStage.Layer1Clear);

            // Chapter 2 cleared: player walks from Echo into Bloom.
            RegisterZoneStage(
                ElinikkiZoneIds.LayerBloom,
                predecessor: ElinikkiQuestStage.Layer1Clear,
                target: ElinikkiQuestStage.Layer2Clear);

            // Chapter 3 cleared: player walks from Bloom into Yuu's camp.
            // The reunion drama at the camp then advances to YuuFound via
            // cmd.elinikki.stage.advance.yuu_found, so two transitions can
            // happen on a single camp entry (Layer3Clear -> YuuFound) — the
            // zone rule sets Layer3Clear first and the drama bumps it up.
            RegisterZoneStage(
                ElinikkiZoneIds.YuuCamp,
                predecessor: ElinikkiQuestStage.Layer2Clear,
                target: ElinikkiQuestStage.Layer3Clear);

            // Chapter 5 return: player walks from YuuCamp back to the Nefia
            // entrance. Gated on YuuFound (set by the reunion drama) so the
            // first visit in chapter 0 (stage == NotStarted) does not match.
            // Also gated on the previous zone being YuuCamp so that recall,
            // world-map teleport, or any other path into the entrance does
            // not falsely unlock the return state.
            RegisterZoneStage(
                ElinikkiZoneIds.NefiaEntrance,
                predecessor: ElinikkiQuestStage.YuuFound,
                target: ElinikkiQuestStage.Returned,
                requirePreviousZoneId: ElinikkiZoneIds.YuuCamp);

            _defaultRulesRegistered = true;
        }

        /// <summary>
        /// Registers a zone-to-stage rule. Multiple rules may be registered
        /// for the same <paramref name="zoneId"/> — they are evaluated in
        /// registration order. The first rule whose
        /// <paramref name="predecessor"/> equals the current stage and whose
        /// <paramref name="requirePreviousZoneId"/> (if set) matches the
        /// player's previous zone fires.
        /// </summary>
        public static void RegisterZoneStage(
            string zoneId,
            ElinikkiQuestStage predecessor,
            ElinikkiQuestStage target,
            string requirePreviousZoneId = null)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                QuestModLog.Warn("RegisterZoneStage ignored: empty zoneId");
                return;
            }

            if ((int)target <= (int)predecessor)
            {
                QuestModLog.Warn(
                    "RegisterZoneStage ignored: target (" + target +
                    ") must be strictly later than predecessor (" + predecessor + ")");
                return;
            }

            if (!_zoneStageRules.TryGetValue(zoneId, out List<ZoneStageRule> rules))
            {
                rules = new List<ZoneStageRule>();
                _zoneStageRules[zoneId] = rules;
            }

            rules.Add(new ZoneStageRule(predecessor, target, requirePreviousZoneId));
        }

        /// <summary>
        /// Drama- and code-facing entry point for moving the quest forward.
        /// Returns true iff the stage actually changed (rejected if
        /// <paramref name="target"/> is not strictly greater than the current
        /// stage). Safe to call repeatedly.
        /// </summary>
        public static bool TryAdvanceStage(ElinikkiQuestStage target)
        {
            ElinikkiQuestStage current = ElinikkiQuestStageExtensions.GetCurrentStage();
            if ((int)target <= (int)current)
            {
                return false;
            }

            ElinikkiQuestStageExtensions.AdvanceToStage(target);
            QuestModLog.Info("Quest stage advanced: " + current + " -> " + target);
            return true;
        }

        /// <summary>
        /// Called from <c>Patch_Zone_Activate_QuestPulse</c> after every
        /// <c>Zone.Activate</c>. Fail-soft: any exception is caught and logged
        /// without propagating, because this runs inside a Harmony postfix and
        /// must never break zone loading.
        /// </summary>
        public static void Pulse()
        {
            try
            {
                ElinikkiQuestStage currentStage = ElinikkiQuestStageExtensions.GetCurrentStage();
                string zoneId = ResolveCurrentZoneId();

                // Snapshot and rotate the last-seen zone id. Rules that need
                // a specific source zone use the value captured BEFORE this
                // Pulse, so we read it first then update.
                string previousZoneId = _lastObservedZoneId;
                if (!string.IsNullOrEmpty(zoneId))
                {
                    _lastObservedZoneId = zoneId;
                }

                QuestModLog.Info(
                    "Quest pulse. stage=" + currentStage +
                    " zone=" + (zoneId ?? "<null>") +
                    " prev=" + (previousZoneId ?? "<null>"));

                AdvanceForZone(zoneId, previousZoneId, currentStage);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Quest pulse failed: " + ex.Message);
            }
        }

        private static void AdvanceForZone(
            string zoneId,
            string previousZoneId,
            ElinikkiQuestStage currentStage)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                return;
            }

            if (!_zoneStageRules.TryGetValue(zoneId, out List<ZoneStageRule> rules))
            {
                return;
            }

            // Evaluate rules in registration order. A rule only fires when its
            // predecessor matches the current stage exactly AND its required
            // previous zone (if set) matches the player's previous zone. This
            // gates reused zones (e.g. the entrance map visited in chapters 0
            // and 5) so that the wrong visit cannot skip the quest forward.
            for (int i = 0; i < rules.Count; i++)
            {
                ZoneStageRule rule = rules[i];
                if (rule.Predecessor != currentStage)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(rule.RequirePreviousZoneId)
                    && !string.Equals(rule.RequirePreviousZoneId, previousZoneId, StringComparison.Ordinal))
                {
                    continue;
                }

                TryAdvanceStage(rule.Target);
                return;
            }
        }

        /// <summary>
        /// Resolves the current zone's content id via the Elin API. Returns
        /// null if the map is not yet ready (e.g. during startup). Kept as a
        /// small isolated method so it is easy to stub in tests.
        /// </summary>
        private static string ResolveCurrentZoneId()
        {
            var zone = EClass._zone;
            if (zone == null)
            {
                return null;
            }

            // Zone.source.id is the content id (e.g. "elinikki_layer_waterstone").
            // This matches the ids defined in ElinikkiZoneIds.
            var source = zone.source;
            return source?.id;
        }
    }
}
