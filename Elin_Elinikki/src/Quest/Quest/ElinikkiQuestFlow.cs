using System;
using System.Collections.Generic;
using Elin_Elinikki.Quest.Drama;

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
        /// <summary>
        /// Minimum player fame required to unlock the intro drama. Matches
        /// the design decision in <c>.claude/plans/zesty-zooming-dusk.md</c>
        /// (Q3 — quest start condition).
        /// </summary>
        public const int IntroFameThreshold = 5000;

        /// <summary>
        /// Drama id for the chapter-0 intro sequence delivered by Mina at the
        /// player's home.
        /// </summary>
        public const string IntroDramaId = "elinikki_quest_intro";

        /// <summary>
        /// Phase gate: set to <c>true</c> when the Phase 2 drama authoring
        /// task ships <c>Dialog/Drama/drama_elinikki_quest_intro.xlsx</c>.
        /// Until then, <see cref="TryStartIntroQuest"/> performs the fame /
        /// home-zone / UI checks and logs the result but does NOT call into
        /// the drama runtime — because <c>GameQuestDramaRuntimeContext.TryActivateDrama</c>
        /// will instantiate a <c>LayerDrama</c> and throw when the underlying
        /// drama sheet is missing. Phase 2 Task 2.2 flips this constant to
        /// <c>true</c> together with the committed drama asset.
        /// </summary>
        private const bool IntroDramaAvailable = false;

        /// <summary>
        /// Lazy singleton for drama invocation. Instantiated on first use so
        /// we do not force the ctor to run during Mod loading.
        /// </summary>
        private static IQuestDramaRuntimeContext _dramaContext;

        private static IQuestDramaRuntimeContext DramaContext
            => _dramaContext ?? (_dramaContext = new GameQuestDramaRuntimeContext());

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
        /// must never break zone loading. Runs both the intro gate and the
        /// zone-based advancement logic, because Zone.Activate is the only
        /// legitimate trigger for zone stage transitions.
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

                TryStartIntroQuest(currentStage);
                AdvanceForZone(zoneId, previousZoneId, currentStage);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Quest pulse failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Secondary pulse used as a retry for the chapter-0 intro drama
        /// when the player is idling at home (hourly tick via
        /// <c>Patch_Player_OnAdvanceHour_QuestPulse</c>). Intentionally
        /// does NOT run <see cref="AdvanceForZone"/>: zone-based stage
        /// transitions must only fire on a real Zone.Activate so that
        /// standing inside a chapter zone during an hour advance cannot
        /// skip the quest forward without an actual zone transition.
        /// </summary>
        public static void IntroRetryPulse()
        {
            try
            {
                ElinikkiQuestStage currentStage = ElinikkiQuestStageExtensions.GetCurrentStage();
                if (currentStage != ElinikkiQuestStage.NotStarted)
                {
                    return;
                }

                TryStartIntroQuest(currentStage);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Intro retry pulse failed: " + ex.Message);
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
        /// Checks the Chapter 0 start condition. When all of the following
        /// hold, starts the intro drama:
        ///   * Current stage is <see cref="ElinikkiQuestStage.NotStarted"/>.
        ///   * The player is standing in THEIR OWN home zone
        ///     (<c>pc.homeBranch.owner</c>), not any arbitrary settlement
        ///     the player happens to own. Mina's chapter-0 drama is authored
        ///     for the canonical home, so multi-settlement players must only
        ///     see the intro in the correct zone.
        ///   * Player fame is at least <see cref="IntroFameThreshold"/>.
        ///
        /// The drama itself advances the stage to <see cref="ElinikkiQuestStage.Accepted"/>
        /// via <c>cmd.elinikki.stage.advance.accepted</c>, so this method
        /// does NOT touch <c>quest.stage</c> directly. If the drama cannot
        /// currently start (UI busy, PC in conversation, drama id not yet
        /// authored in Phase 2) the call is a no-op and the next Pulse will
        /// retry automatically.
        /// </summary>
        private static void TryStartIntroQuest(ElinikkiQuestStage currentStage)
        {
            if (currentStage != ElinikkiQuestStage.NotStarted)
            {
                return;
            }

            var pc = EClass.pc;
            if (pc == null)
            {
                return;
            }

            // Resolve the canonical home zone for the player. Prefer
            // pc.homeBranch.owner (the zone that holds the active home
            // branch) and fall back to pc.homeZone if the branch reference
            // is not yet initialized. Either way, we want a single specific
            // zone — not just "any zone the player owns".
            var homeZone = pc.homeBranch?.owner ?? pc.homeZone;
            if (homeZone == null)
            {
                // Player has not claimed a home yet; no valid place to run
                // the intro.
                return;
            }

            var zone = EClass._zone;
            if (zone == null || zone != homeZone)
            {
                return;
            }

            var player = EClass.player;
            if (player == null)
            {
                return;
            }

            if (player.fame < IntroFameThreshold)
            {
                return;
            }

            // Do not open the intro drama if another UI layer is already
            // active (book, menu, dialog, sleep cutscene, another drama,
            // etc.). LayerDrama.Activate would stack on top of whatever is
            // open, which is visually wrong and can break the active layer's
            // state. Wait until the UI is idle and retry next pulse.
            if (IsUiBusy())
            {
                return;
            }

            // Phase gate: until the drama asset is packaged (Phase 2 Task 2.2),
            // do not attempt to open the drama. Calling TryStartDrama* against
            // a missing sheet still instantiates a LayerDrama and makes the
            // game's drama loader throw on every pulse. Log that the gate
            // passed and return — the Pulse will retry next zone activation
            // or next hour until Phase 2 flips IntroDramaAvailable to true.
            if (!IntroDramaAvailable)
            {
                QuestModLog.Info(
                    "Intro gate passed (fame=" + player.fame + " >= " + IntroFameThreshold +
                    ", home zone=" + (zone.source?.id ?? "<unknown>") +
                    "). Drama '" + IntroDramaId + "' not yet packaged — skipping start.");
                return;
            }

            // Post-gate path. Phase 2 will flip IntroDramaAvailable to true
            // and this code becomes live. Until then it is unreachable by
            // design; the warning is suppressed locally so the overall build
            // stays warning-clean.
#pragma warning disable CS0162 // Unreachable code detected
            QuestModLog.Info(
                "Intro gate passed (fame=" + player.fame + " >= " + IntroFameThreshold +
                ", home zone=" + (zone.source?.id ?? "<unknown>") +
                "). Requesting intro drama '" + IntroDramaId + "'.");

            try
            {
                // Use TryStartDramaUntilComplete, NOT TryStartDrama:
                // plain TryStartDrama is one-shot — once it succeeds the
                // drama is marked "started" in dialogFlags and will not run
                // again until completed. If the intro is interrupted before
                // advancing to Accepted (save/reload, UI exit, mod reload,
                // etc.) the quest would be permanently stranded in
                // NotStarted. The *_until_complete variant relaunches on
                // every pulse until cmd.elinikki.stage.advance.accepted
                // actually fires and moves the quest out of NotStarted.
                bool started = DramaContext.TryStartDramaUntilComplete(IntroDramaId);
                if (!started)
                {
                    QuestModLog.Info(
                        "Intro drama did not start this pulse (likely UI busy). Will retry.");
                }
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Intro drama start raised: " + ex.Message);
            }
#pragma warning restore CS0162
        }

        /// <summary>
        /// Returns true if the Elin UI already has an active layer. Used to
        /// defer opening the intro drama when the player is in a menu,
        /// reading a book, sleeping, or watching another drama. Fail-safe:
        /// if the UI reference itself is missing, report "busy" so we do
        /// not try to open a drama layer against a half-initialised UI.
        /// </summary>
        private static bool IsUiBusy()
        {
            var ui = EClass.ui;
            if (ui == null)
            {
                return true;
            }

            return ui.IsActive;
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
