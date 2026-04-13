using System;
using System.Collections.Generic;
using Elin_Elinikki.Quest.Drama;
using Elin_Elinikki.Quest.DramaKeys;

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

        // IntroDramaAvailable gate removed in Phase 2 Task 2.2 —
        // drama_elinikki_quest_intro.xlsx is now packaged, so
        // TryStartIntroQuest can call the drama runtime unconditionally.
        // Historical gate kept in git history if Phase 3 needs to reinstate
        // it before authoring the next drama.

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

                // Reset the chapter-4 truth menu latches on any
                // zone activation whose destination zone is known
                // AND is not YuuCamp. Without this, a player who
                // leaves the camp with TMP_TRUTH_MENU_DISMISSED=1
                // would return and never see the menu re-open.
                // A null zoneId means the zone could not be
                // resolved yet (startup, load pipeline mid-tear-
                // down) — clearing the latch in that case would
                // fire while the player is still physically in
                // YuuCamp on a save/reload and cause the menu to
                // reopen without the player ever leaving, so we
                // skip the reset until the zone id is known.
                if (!string.IsNullOrEmpty(zoneId)
                    && !string.Equals(
                           zoneId,
                           ElinikkiZoneIds.YuuCamp,
                           StringComparison.Ordinal))
                {
                    ClearTruthMenuState();
                }

                // Reunion dispatch runs after AdvanceForZone because
                // the YuuCamp zone rule promotes Layer2Clear ->
                // Layer3Clear on this same activation. Re-read the
                // stage so the dispatcher sees the freshly-promoted
                // Layer3Clear and can run the reunion drama on the
                // very first camp entry instead of making the player
                // leave and come back.
                ElinikkiQuestStage reunionStage =
                    ElinikkiQuestStageExtensions.GetCurrentStage();
                TryDispatchReunion(zoneId, reunionStage);

                // Chapter-4 truth flow: first dispatch any pending
                // pick (if the player just closed the menu with a
                // slot set), then open/re-open the menu itself.
                // Order matters — pending dispatch clears the slot
                // so the subsequent menu dispatcher sees slot == 0
                // and can re-open the menu on the next drama-close
                // retry after the truth drama finishes.
                TryDispatchPendingTruth();

                ElinikkiQuestStage menuStage =
                    ElinikkiQuestStageExtensions.GetCurrentStage();
                TryDispatchTruthMenu(zoneId, menuStage);

                // Return journey dispatch: the YuuCamp -> NefiaEntrance
                // zone rule just promoted YuuFound -> Returned, so the
                // return journey drama can play on the same activation
                // the player walks back into the entrance. The drama
                // does not advance the stage and marks itself done via
                // cmd.quest.complete.elinikki_return_journey so
                // subsequent Pulses skip it.
                ElinikkiQuestStage returnStage =
                    ElinikkiQuestStageExtensions.GetCurrentStage();
                TryDispatchReturnJourney(zoneId, returnStage);

                // Ending dispatch runs after AdvanceForZone because
                // the zone rules may have just promoted the stage to
                // Returned on this same activation. Re-read the
                // current stage so the dispatcher sees the updated
                // value instead of the one snapshotted at Pulse entry.
                // previousZoneId is passed through so the revisit
                // ending can gate on an actual outside-the-Nefia
                // re-entry rather than any re-activation of an
                // Elinikki map (save reloads, zone edits, etc.).
                // TryDispatchEnding itself checks that the return
                // journey drama has already finished — on the first
                // Returned activation that gate fails and the
                // ending waits until the next pulse after the drama
                // completes.
                ElinikkiQuestStage updatedStage =
                    ElinikkiQuestStageExtensions.GetCurrentStage();
                TryDispatchEnding(zoneId, previousZoneId, updatedStage);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Quest pulse failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Chapter-5 ending dispatcher. Runs from <see cref="Pulse"/>
        /// after the zone stage rules have fired, so the stage
        /// parameter reflects any promotion that just happened on
        /// this activation. Two triggers:
        /// <list type="bullet">
        /// <item><description><b>First visit ending</b>: stage is
        /// Returned and <c>quest.ending</c> is still None. Count the
        /// eight truth flags, pick Return vs Silence, and start the
        /// matching ending drama. The drama itself sets the ending
        /// flag and advances the stage to <c>EndingSeen</c>.</description></item>
        /// <item><description><b>Revisit ending</b>: the player has
        /// already seen the Return ending (<c>quest.ending == 1</c>
        /// and <c>stage == EndingSeen</c>) and re-enters any
        /// Elinikki chapter zone. The story bible explicitly says
        /// the revisit ending has "最後のテキスト: なし" — it is an
        /// environmental beat, no drama. The dispatcher just
        /// overwrites <c>quest.ending</c> to <see cref="ElinikkiEndingKind.Revisit"/>
        /// so the ending bucket is reachable.</description></item>
        /// </list>
        /// Fail-soft: any exception is caught and logged without
        /// propagating, because this runs inside the same try/catch
        /// as the main <see cref="Pulse"/> body.
        /// </summary>
        private static void TryDispatchEnding(
            string zoneId,
            string previousZoneId,
            ElinikkiQuestStage stage)
        {
            try
            {
                // Revisit detection first — it is the only path that
                // applies after the main ending fires, so checking it
                // up front lets the first-visit branch stay scoped to
                // the Returned -> EndingSeen window.
                //
                // The revisit trigger is "re-enter the Nefia after
                // seeing the return ending", and nefia-entrance.md
                // explicitly names the entrance map as the place the
                // revisit beat occurs. The gate therefore requires
                // THREE things:
                //   * the NEW zone is an Elinikki quest zone
                //     (entrance or any chapter layer);
                //   * the PREVIOUS zone is a concrete, non-quest
                //     zone (not null);
                //   * the previous zone was NOT itself a quest zone.
                // The "non-null previous zone" check is critical:
                // _lastObservedZoneId resets to null at session
                // start, so a save reload while standing in the
                // Nefia would otherwise pass as "came from outside"
                // and spend the hidden ending without any real
                // exit/re-entry. A null previous zone simply means
                // "no prior activation observed yet"; we cannot
                // prove it was a real re-entry, so we skip.
                ElinikkiEndingKind currentEnding = ElinikkiEndingResolver.GetCurrentEnding();
                if (currentEnding == ElinikkiEndingKind.Return
                    && stage == ElinikkiQuestStage.EndingSeen
                    && IsElinikkiQuestZone(zoneId)
                    && !string.IsNullOrEmpty(previousZoneId)
                    && !IsElinikkiQuestZone(previousZoneId))
                {
                    ElinikkiEndingResolver.SetCurrentEnding(ElinikkiEndingKind.Revisit);
                    QuestModLog.Info(
                        "Elinikki revisit ending reached on zone " + zoneId +
                        " from " + (previousZoneId ?? "<null>"));
                    return;
                }

                // First-visit ending: the player just returned to
                // the entrance with stage=Returned and has not
                // picked an ending yet. Pick by truth count and
                // start the matching drama. The drama advances the
                // stage to EndingSeen and sets the ending flag; if
                // it fails to start (UI busy, drama asset missing),
                // the next Pulse or hourly RetryPulse will retry —
                // the zone gate below (zone == NefiaEntrance)
                // keeps the ending from popping outside the
                // entrance map.
                if (stage != ElinikkiQuestStage.Returned
                    || currentEnding != ElinikkiEndingKind.None
                    || !string.Equals(
                           zoneId,
                           ElinikkiZoneIds.NefiaEntrance,
                           StringComparison.Ordinal))
                {
                    return;
                }

                // Return journey drama must finish first. The
                // chapter-5 walk-out narration runs before the
                // ending scene; without this gate the ending
                // would race the return journey on the first
                // Returned activation (TryDispatchReturnJourney
                // starts the walk-out drama, UI becomes busy,
                // TryDispatchEnding is skipped this tick, and
                // only on the next pulse — after the walk-out
                // finishes — do we actually want the ending to
                // run). IsDramaDone flips once the return
                // journey drama emits
                // cmd.quest.complete.elinikki_return_journey.
                if (!DramaContext.IsDramaDone(ReturnJourneyDramaId))
                {
                    return;
                }

                int truthCount = ElinikkiEndingResolver.CountTruthFlags();
                ElinikkiEndingKind resolved =
                    ElinikkiEndingResolver.ResolveEndingFromTruthCount(truthCount);
                string dramaId = resolved == ElinikkiEndingKind.Return
                    ? ReturnEndingDramaId
                    : SilenceEndingDramaId;

                QuestModLog.Info(
                    "Elinikki ending dispatch. truthCount=" + truthCount +
                    " ending=" + resolved +
                    " drama=" + dramaId);

                if (IsUiBusy())
                {
                    return;
                }

                DramaContext.TryStartDramaUntilComplete(dramaId);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Ending dispatch failed: " + ex.Message);
            }
        }

        private const string ReturnEndingDramaId = "elinikki_ending_return";
        private const string SilenceEndingDramaId = "elinikki_ending_silence";

        /// <summary>
        /// Drama id for the chapter-4 reunion sequence that plays when
        /// the player reaches Yuu's camp. The drama itself calls
        /// <c>cmd.elinikki.stage.advance.yuu_found</c> to move the
        /// quest stage from <see cref="ElinikkiQuestStage.Layer3Clear"/>
        /// to <see cref="ElinikkiQuestStage.YuuFound"/>, so the
        /// dispatcher stops firing as soon as the drama completes
        /// even once (the next Pulse will see YuuFound and skip the
        /// Layer3Clear gate).
        /// </summary>
        private const string ReunionDramaId = "elinikki_reunion";

        /// <summary>
        /// Drama id for the chapter-4 post-reunion dialogue menu.
        /// Auto-dispatched from <see cref="TryDispatchTruthMenu"/>
        /// after the reunion drama closes and after every truth
        /// drama closes, while the player is still in YuuCamp and
        /// has not dismissed the menu this visit. The menu writes
        /// the chosen topic to
        /// <see cref="FlagKeys.TMP_TRUTH_MENU_SLOT"/> so
        /// <see cref="TryDispatchPendingTruth"/> can launch the
        /// matching truth drama after the menu closes.
        /// </summary>
        private const string ReunionMenuDramaId = "elinikki_reunion_menu";

        /// <summary>
        /// Slot-to-drama map for the chapter-4 truth dialogue menu.
        /// Index 0 is unused (slot 0 means "no pending pick"), so
        /// the array is size 9 and slots 1..8 line up with the
        /// <c>_SLOT_*</c> constants in
        /// <c>tools/drama/scenarios/elinikki_reunion_menu.py</c>.
        /// The two sides MUST stay in lockstep; rearranging one
        /// without the other would route a menu pick to the
        /// wrong truth drama. The array is readonly and initialised
        /// once at type-load time.
        /// </summary>
        private static readonly string[] TruthDramaIdsBySlot =
        {
            null,                    // slot 0 = no pending pick
            "elinikki_truth_marks",  // slot 1 = pick_marks
            "elinikki_truth_channel",// slot 2 = pick_channel
            "elinikki_truth_stones", // slot 3 = pick_stones
            "elinikki_truth_echo",   // slot 4 = pick_echo
            "elinikki_truth_map",    // slot 5 = pick_map
            "elinikki_truth_shadow", // slot 6 = pick_shadow
            "elinikki_truth_flowers",// slot 7 = pick_flowers
            "elinikki_truth_weave",  // slot 8 = pick_weave
        };

        /// <summary>
        /// Prerequisite trace flag for each truth slot. Parallel to
        /// <see cref="TruthDramaIdsBySlot"/>: index i names the
        /// <c>chitsii.elinikki.quest.event.trace_*</c> flag the
        /// player must have set (via a chapter 1-3 examine drama or
        /// the chapter-2 echo experiment) before the matching truth
        /// conversation is allowed to fire.
        /// <para>
        /// Without this guard, <see cref="TryDispatchPendingTruth"/>
        /// would launch a truth drama for a slot whose trace was
        /// never examined. The drama sets the corresponding
        /// <c>truth_*</c> flag, and
        /// <see cref="ElinikkiEndingResolver"/> counts only truth
        /// flags, so a player could reach the Return ending without
        /// discovering any of the prerequisite clues — contradicting
        /// the story spec at <c>story/chapters/_index.md</c>
        /// ("quest.event.trace_X == 1 AND quest.event.truth_X == 0"
        /// gate on truth conversations).
        /// </para>
        /// </summary>
        private static readonly string[] TruthPrerequisiteTraceFlagsBySlot =
        {
            null,                                // slot 0 = no pending pick
            FlagKeys.ELINIKKI_TRACE_MARKS,       // slot 1 = pick_marks
            FlagKeys.ELINIKKI_TRACE_CHANNEL,     // slot 2 = pick_channel
            FlagKeys.ELINIKKI_TRACE_STONES,      // slot 3 = pick_stones
            FlagKeys.ELINIKKI_TRACE_ECHO,        // slot 4 = pick_echo
            FlagKeys.ELINIKKI_TRACE_MAP,         // slot 5 = pick_map
            FlagKeys.ELINIKKI_TRACE_SHADOW,      // slot 6 = pick_shadow
            FlagKeys.ELINIKKI_TRACE_FLOWERS,     // slot 7 = pick_flowers
            FlagKeys.ELINIKKI_TRACE_WEAVE,       // slot 8 = pick_weave
        };

        /// <summary>
        /// Drama id for the chapter-5 return journey narration that
        /// plays once the player walks from YuuCamp back into the
        /// Nefia entrance. Unlike the reunion drama this one does
        /// NOT advance <c>quest.stage</c> — the stage advances to
        /// <see cref="ElinikkiQuestStage.Returned"/> via the zone
        /// rule on the same activation, and the drama just narrates
        /// the walk-out. The drama marks itself complete with
        /// <c>cmd.quest.complete.elinikki_return_journey</c> so
        /// <see cref="IQuestDramaRuntimeContext.IsDramaDone"/>
        /// flips to true; that flag is the gate both for
        /// re-dispatching this drama and for releasing
        /// <see cref="TryDispatchEnding"/>.
        /// </summary>
        private const string ReturnJourneyDramaId = "elinikki_return_journey";

        /// <summary>
        /// Chapter-4 reunion dispatcher. Runs from <see cref="Pulse"/>
        /// after the zone rules have promoted the player to
        /// Layer3Clear on the first YuuCamp entry. Conditions:
        /// <list type="bullet">
        /// <item><description>The active zone is
        /// <see cref="ElinikkiZoneIds.YuuCamp"/>.</description></item>
        /// <item><description>The current stage is exactly
        /// <see cref="ElinikkiQuestStage.Layer3Clear"/> — i.e. the
        /// player just arrived but has not met Yuu yet.</description></item>
        /// <item><description>No other UI layer is active (drama,
        /// menu, book, sleep cutscene).</description></item>
        /// </list>
        /// Uses <see cref="IQuestDramaRuntimeContext.TryStartDramaUntilComplete"/>
        /// so an interrupted reunion drama retries on the next Pulse
        /// until its stage-advance command actually fires. Once the
        /// stage is YuuFound the gate fails naturally (stage !=
        /// Layer3Clear) and this method becomes a no-op until a new
        /// playthrough resets the quest. Fail-soft: any exception is
        /// caught and logged so a drama runtime failure cannot break
        /// <see cref="Pulse"/>.
        /// </summary>
        private static void TryDispatchReunion(
            string zoneId,
            ElinikkiQuestStage stage)
        {
            try
            {
                if (stage != ElinikkiQuestStage.Layer3Clear)
                {
                    return;
                }

                if (!string.Equals(
                        zoneId,
                        ElinikkiZoneIds.YuuCamp,
                        StringComparison.Ordinal))
                {
                    return;
                }

                if (IsUiBusy())
                {
                    return;
                }

                QuestModLog.Info(
                    "Elinikki reunion dispatch. zone=" + zoneId +
                    " stage=" + stage +
                    " drama=" + ReunionDramaId);

                DramaContext.TryStartDramaUntilComplete(ReunionDramaId);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Reunion dispatch failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Chapter-5 return journey dispatcher. Runs from
        /// <see cref="Pulse"/> after the zone rules have promoted
        /// YuuFound -> Returned on the first NefiaEntrance entry
        /// out of YuuCamp. Conditions:
        /// <list type="bullet">
        /// <item><description>The active zone is
        /// <see cref="ElinikkiZoneIds.NefiaEntrance"/>.</description></item>
        /// <item><description>The current stage is exactly
        /// <see cref="ElinikkiQuestStage.Returned"/> — i.e. the
        /// player just walked out.</description></item>
        /// <item><description>The drama has not already been
        /// marked complete (<see cref="IQuestDramaRuntimeContext.IsDramaDone"/>).
        /// This is the single source of "walk-out narration
        /// already played", checked both here and in
        /// <see cref="TryDispatchEnding"/>.</description></item>
        /// <item><description>No other UI layer is active.</description></item>
        /// </list>
        /// Uses <see cref="IQuestDramaRuntimeContext.TryStartDramaUntilComplete"/>
        /// so an interrupted drama retries on the next Pulse /
        /// RetryPulse until
        /// <c>cmd.quest.complete.elinikki_return_journey</c>
        /// fires at the drama's final step. The <c>IsDramaDone</c>
        /// check is redundant with the same check inside
        /// <c>TryStartDramaUntilComplete</c>, but keeping it here
        /// lets us skip the log-chatty "skipped complete" branch
        /// on every Pulse once the walk-out is done.
        /// Fail-soft: any exception is caught and logged so a
        /// drama runtime failure cannot break <see cref="Pulse"/>.
        /// </summary>
        private static void TryDispatchReturnJourney(
            string zoneId,
            ElinikkiQuestStage stage)
        {
            try
            {
                if (stage != ElinikkiQuestStage.Returned)
                {
                    return;
                }

                if (!string.Equals(
                        zoneId,
                        ElinikkiZoneIds.NefiaEntrance,
                        StringComparison.Ordinal))
                {
                    return;
                }

                if (DramaContext.IsDramaDone(ReturnJourneyDramaId))
                {
                    return;
                }

                if (IsUiBusy())
                {
                    return;
                }

                QuestModLog.Info(
                    "Elinikki return journey dispatch. zone=" + zoneId +
                    " stage=" + stage +
                    " drama=" + ReturnJourneyDramaId);

                DramaContext.TryStartDramaUntilComplete(ReturnJourneyDramaId);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Return journey dispatch failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Chapter-4 truth dialogue menu dispatcher. Runs after the
        /// reunion drama closes (via the
        /// <c>LayerDrama.OnKill</c> retry hook) and after every
        /// completed truth drama. Conditions:
        /// <list type="bullet">
        /// <item><description>Stage is
        /// <see cref="ElinikkiQuestStage.YuuFound"/> — i.e. the
        /// reunion drama has already advanced the stage.</description></item>
        /// <item><description>Active zone is
        /// <see cref="ElinikkiZoneIds.YuuCamp"/>.</description></item>
        /// <item><description><see cref="FlagKeys.TMP_TRUTH_MENU_DISMISSED"/>
        /// is 0. The "leave" choice in the menu sets it to 1 and
        /// <see cref="ClearTruthMenuState"/> wipes it when the
        /// player walks out of YuuCamp.</description></item>
        /// <item><description><see cref="FlagKeys.TMP_TRUTH_MENU_SLOT"/>
        /// is 0. A non-zero slot means the previous menu pass
        /// already picked a topic and we should let
        /// <see cref="TryDispatchPendingTruth"/> fire the matching
        /// truth drama first.</description></item>
        /// <item><description>No UI layer is currently active.</description></item>
        /// </list>
        /// The menu is launched via
        /// <see cref="IQuestDramaRuntimeContext.TryStartDramaRepeatable"/>
        /// — NOT <c>TryStartDramaUntilComplete</c> — because we
        /// intentionally allow the menu to re-open as many times
        /// as the player wants (once per "leave" cycle), and the
        /// completion-flag check inside
        /// <c>TryStartDramaUntilComplete</c> would permanently
        /// block the second opening. Fail-soft: any exception is
        /// caught and logged.
        /// </summary>
        private static void TryDispatchTruthMenu(
            string zoneId,
            ElinikkiQuestStage stage)
        {
            try
            {
                if (stage != ElinikkiQuestStage.YuuFound)
                {
                    return;
                }

                if (!string.Equals(
                        zoneId,
                        ElinikkiZoneIds.YuuCamp,
                        StringComparison.Ordinal))
                {
                    return;
                }

                if (QuestStateService.GetFlagInt(FlagKeys.TMP_TRUTH_MENU_DISMISSED, 0) != 0)
                {
                    return;
                }

                if (QuestStateService.GetFlagInt(FlagKeys.TMP_TRUTH_MENU_SLOT, 0) != 0)
                {
                    return;
                }

                if (IsUiBusy())
                {
                    return;
                }

                QuestModLog.Info(
                    "Elinikki truth menu dispatch. zone=" + zoneId +
                    " stage=" + stage +
                    " drama=" + ReunionMenuDramaId);

                DramaContext.TryStartDramaRepeatable(ReunionMenuDramaId);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Truth menu dispatch failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Chapter-4 pending-truth dispatcher. Reads the slot flag
        /// set by the reunion menu drama, launches the matching
        /// <c>elinikki_truth_*</c> drama via
        /// <see cref="IQuestDramaRuntimeContext.TryStartDramaUntilComplete"/>,
        /// and clears the slot so the next
        /// <see cref="TryDispatchTruthMenu"/> pass re-opens the menu
        /// once the truth drama closes (handled by the
        /// <c>LayerDrama.OnKill</c> retry hook).
        /// <para>
        /// The slot is cleared BEFORE launching the drama so that an
        /// exception in <c>TryStartDramaUntilComplete</c> cannot
        /// leave a stale pending pick that fires again on the next
        /// Pulse. If the launch fails the player just sees the menu
        /// re-open on the next drama-close retry and can re-pick.
        /// </para>
        /// Fail-soft: any exception is caught and logged.
        /// </summary>
        private static void TryDispatchPendingTruth()
        {
            try
            {
                int slot = QuestStateService.GetFlagInt(FlagKeys.TMP_TRUTH_MENU_SLOT, 0);
                if (slot <= 0 || slot >= TruthDramaIdsBySlot.Length)
                {
                    return;
                }

                string dramaId = TruthDramaIdsBySlot[slot];
                if (string.IsNullOrEmpty(dramaId))
                {
                    return;
                }

                // UI-busy check runs BEFORE the slot is cleared. If
                // another UI layer is active (load screen, another
                // mod's cutscene, etc.) we leave the slot intact so
                // the next retry — from Zone.Activate, the hourly
                // RetryPulse, or the LayerDrama.OnKill hook once the
                // blocker closes — can still launch the truth drama
                // the player actually picked. Clearing before this
                // check would silently drop the pick.
                if (IsUiBusy())
                {
                    return;
                }

                // Trace prerequisite gate. The menu drama currently
                // shows all eight topics regardless of which traces
                // the player actually examined (filter-by-trace is
                // deferred to Task 6.1a.4), so we MUST re-check the
                // prerequisite here before letting the truth drama
                // set the corresponding truth_* flag. Otherwise the
                // player could collect truths and reach the Return
                // ending without ever finding the prerequisite
                // clues, which violates the story-spec gate in
                // story/chapters/_index.md. A rejected pick clears
                // the slot (the pick itself was invalid and the
                // player should re-pick from a fresh menu).
                string traceFlagKey = TruthPrerequisiteTraceFlagsBySlot[slot];
                if (!string.IsNullOrEmpty(traceFlagKey)
                    && QuestStateService.GetFlagInt(traceFlagKey, 0) == 0)
                {
                    QuestStateService.SetFlagInt(FlagKeys.TMP_TRUTH_MENU_SLOT, 0);
                    QuestModLog.Info(
                        "Elinikki truth dispatch rejected: prereq trace not set. slot=" + slot +
                        " trace=" + traceFlagKey +
                        " drama=" + dramaId);
                    return;
                }

                // Clear the slot right before we launch. A crash in
                // TryStartDramaUntilComplete below still leaves the
                // slot at 0 (the clear happened), so the pick cannot
                // pin indefinitely. The only failure mode we need to
                // worry about is "UI was free at the check, drama
                // start returned false anyway" — in that case the
                // menu re-opens on the next retry and the player can
                // re-pick.
                QuestStateService.SetFlagInt(FlagKeys.TMP_TRUTH_MENU_SLOT, 0);

                QuestModLog.Info(
                    "Elinikki truth dispatch. slot=" + slot +
                    " drama=" + dramaId);

                DramaContext.TryStartDramaUntilComplete(dramaId);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Pending truth dispatch failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Clears the chapter-4 truth menu transient flags. Invoked
        /// from <see cref="Pulse"/> on any zone activation whose
        /// destination is NOT YuuCamp: once the player walks out
        /// the per-visit dismissed latch and any stranded pending
        /// pick must reset so a future return to the camp opens
        /// the menu fresh.
        /// </summary>
        private static void ClearTruthMenuState()
        {
            try
            {
                if (QuestStateService.GetFlagInt(FlagKeys.TMP_TRUTH_MENU_SLOT, 0) != 0)
                {
                    QuestStateService.SetFlagInt(FlagKeys.TMP_TRUTH_MENU_SLOT, 0);
                }
                if (QuestStateService.GetFlagInt(FlagKeys.TMP_TRUTH_MENU_DISMISSED, 0) != 0)
                {
                    QuestStateService.SetFlagInt(FlagKeys.TMP_TRUTH_MENU_DISMISSED, 0);
                }
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Truth menu state clear failed: " + ex.Message);
            }
        }

        /// <summary>
        /// True when <paramref name="zoneId"/> is any zone owned by
        /// the Elinikki quest, including the shared Nefia entrance.
        /// Used by <see cref="TryDispatchEnding"/> to pick up the
        /// hidden revisit ending — the story spec explicitly
        /// names the entrance as the revisit location, so turning
        /// back at the door still counts as a re-entry.
        /// </summary>
        private static bool IsElinikkiQuestZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                return false;
            }

            return zoneId == ElinikkiZoneIds.NefiaEntrance
                || zoneId == ElinikkiZoneIds.LayerWaterstone
                || zoneId == ElinikkiZoneIds.LayerEcho
                || zoneId == ElinikkiZoneIds.LayerBloom
                || zoneId == ElinikkiZoneIds.YuuCamp;
        }

        /// <summary>
        /// Secondary pulse used as a retry for drama-start gates that
        /// can fail a Zone.Activate pulse when the UI is busy (load
        /// screen, another drama, menu, book, sleep cutscene, etc.).
        /// Runs hourly via <c>Patch_Player_OnAdvanceHour_QuestPulse</c>.
        /// Intentionally does NOT run <see cref="AdvanceForZone"/>:
        /// zone-based stage transitions must only fire on a real
        /// Zone.Activate so that standing inside a chapter zone
        /// during an hour advance cannot skip the quest forward
        /// without an actual zone transition.
        ///
        /// Each dispatch target re-checks its own gate (stage, zone,
        /// flags), so calling them from here is safe even if none
        /// apply. The intent is that an interrupted first-entry
        /// drama gets another chance once the UI frees up.
        /// </summary>
        public static void RetryPulse()
        {
            try
            {
                ElinikkiQuestStage currentStage = ElinikkiQuestStageExtensions.GetCurrentStage();
                string zoneId = ResolveCurrentZoneId();

                // Intro drama retry: only meaningful while the
                // quest is still at NotStarted. TryStartIntroQuest
                // handles the home-zone / fame gate internally.
                if (currentStage == ElinikkiQuestStage.NotStarted)
                {
                    TryStartIntroQuest(currentStage);
                }

                // Reunion drama retry: fires when the player is
                // already inside YuuCamp at Layer3Clear (for
                // example after loading a save that was taken
                // immediately after the zone rule promoted them
                // there) and the reunion drama never actually
                // ran. TryDispatchReunion has its own zone / stage
                // / UI-busy guards.
                TryDispatchReunion(zoneId, currentStage);

                // Chapter-4 truth loop retry. Drives the menu and
                // pending-pick chain whenever any drama closes
                // (the LayerDrama.OnKill hook reuses RetryPulse),
                // so after the reunion drama closes the menu
                // opens, after a truth drama closes the menu
                // re-opens, and after a menu pick the
                // corresponding truth drama fires. Pending
                // dispatch runs first so the menu's own gate
                // (slot == 0) releases before the menu re-opens.
                TryDispatchPendingTruth();
                TryDispatchTruthMenu(zoneId, currentStage);

                // Return journey drama retry: same pattern as the
                // reunion retry. The walk-out narration may have
                // been interrupted on its first Zone.Activate
                // attempt (UI busy, save at the doorway, etc.).
                // The gate inside TryDispatchReturnJourney keeps
                // this cheap when the drama is already complete.
                TryDispatchReturnJourney(zoneId, currentStage);

                // Ending retry: after the return journey drama
                // finishes, the player is still standing in the
                // NefiaEntrance and no Zone.Activate will fire
                // until they leave. Without an hourly retry the
                // ending drama would never auto-start and the
                // player would have to manually re-enter the
                // zone. TryDispatchEnding's own guards (zone,
                // stage, ending flag, IsDramaDone of the return
                // journey) keep this safe; the previousZoneId
                // argument is passed as null because the revisit
                // branch requires a non-null previousZoneId and
                // will early-out on its own.
                TryDispatchEnding(zoneId, previousZoneId: null, stage: currentStage);
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Retry pulse failed: " + ex.Message);
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
