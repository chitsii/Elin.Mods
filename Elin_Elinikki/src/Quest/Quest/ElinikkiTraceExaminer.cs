using System;
using System.Collections.Generic;
using Elin_Elinikki.Quest.Drama;
using Elin_Elinikki.Quest.DramaKeys;
using Elin_Elinikki.Quest.Placement;

namespace Elin_Elinikki.Quest.Quest
{
    /// <summary>
    /// Proximity-based trigger for the chapter 1-3 trace examine
    /// dramas. Polled from
    /// <c>Patch_Game_OnUpdate_TraceExaminer</c> on every frame; the
    /// per-frame call is cheap because <see cref="Tick"/> early-exits
    /// when the player's tile-grid position has not moved since the
    /// last tick. When the player actually steps to a new tile the
    /// examiner walks the active zone's placement definitions (from
    /// <see cref="ElinikkiPlacementData.GetDefinitionsForZone"/>),
    /// computes an XZ distance against each known trace landmark,
    /// and — if the player is within
    /// <see cref="TriggerRadiusTiles"/> AND the trace's event flag
    /// is still 0 AND the UI is idle — launches the matching
    /// <c>elinikki_trace_*</c> drama through
    /// <see cref="IQuestDramaRuntimeContext.TryStartDramaUntilComplete"/>.
    ///
    /// <para>Placement id → trace flag key + drama id mapping is a
    /// static table (see <see cref="BuildTriggers"/>). Keeping the
    /// mapping centralised lets us hook new traces in one place and
    /// keeps the examiner itself agnostic to the specific chapter
    /// layout.</para>
    ///
    /// <para>Chapter-2 echo experiment points (<c>elinikki/echo/point_a|b|c</c>)
    /// are handled by the companion
    /// <see cref="TryDispatchEchoExperiment"/> pass in the same
    /// <see cref="Tick"/>. Unlike the trace triggers, the echo
    /// point dispatch is counter-driven: the drama to fire depends
    /// on the current <see cref="FlagKeys.ELINIKKI_ECHO_EXPERIMENT"/>
    /// value (1 → point_a fires stage 2; 2 → point_b fires stage 3;
    /// 3 → point_c fires stage 4), and each stage drama advances
    /// the counter itself. Stage 1 runs on zone entry via
    /// <c>ElinikkiQuestFlow.TryDispatchEchoStage1</c>, NOT this
    /// examiner, because it is not position-gated.</para>
    ///
    /// <para>The dispatcher only fires ONE drama per tick — after a
    /// match, the method returns without continuing the scan. This
    /// prevents two overlapping traces from racing a single tile
    /// step, and matches the player's expectation that examining a
    /// cluster of landmarks takes one step each.</para>
    /// </summary>
    internal static class ElinikkiTraceExaminer
    {
        /// <summary>
        /// Trigger table row. Immutable once built.
        /// </summary>
        private sealed class TraceTrigger
        {
            public readonly string PlacementId;
            public readonly string FlagKey;
            public readonly string DramaId;

            public TraceTrigger(string placementId, string flagKey, string dramaId)
            {
                PlacementId = placementId;
                FlagKey = flagKey;
                DramaId = dramaId;
            }
        }

        /// <summary>
        /// Proximity radius used to decide whether the player is
        /// "on" a trace. 1.5 tiles gives the player some slack so
        /// they do not have to stand on the exact cell — brushing
        /// past a wall marking is enough. Tuned conservatively; the
        /// flag guard ensures a drama never fires twice, so a
        /// slightly generous radius is safe.
        /// </summary>
        private const float TriggerRadiusTiles = 1.5f;
        private const float TriggerRadiusTilesSq = TriggerRadiusTiles * TriggerRadiusTiles;

        private static readonly Dictionary<string, TraceTrigger> _triggersByPlacementId = BuildTriggers();

        // Movement cache — last position we evaluated. Seeded to
        // an impossible pair so the first Tick always runs. The
        // cache is reset via <see cref="InvalidateMovementCache"/>
        // on every Zone.Activate pulse so save/reload-into-a-
        // landmark replays the scan on the first frame after the
        // quest flow pulse, instead of relying on the player to
        // step off the tile.
        private static int _lastPlayerX = int.MinValue;
        private static int _lastPlayerZ = int.MinValue;
        private static string _lastZoneId;

        // Lazy drama context, mirroring ElinikkiQuestFlow's singleton.
        private static IQuestDramaRuntimeContext _dramaContext;

        private static IQuestDramaRuntimeContext DramaContext
            => _dramaContext ?? (_dramaContext = new GameQuestDramaRuntimeContext());

        /// <summary>
        /// Builds the placement-id → trigger map. Each entry is an
        /// immutable <see cref="TraceTrigger"/> so concurrent reads
        /// from different frames are safe without locking.
        /// </summary>
        private static Dictionary<string, TraceTrigger> BuildTriggers()
        {
            var map = new Dictionary<string, TraceTrigger>(StringComparer.Ordinal);

            // Chapter 1: 水石の層
            Register(map,
                placementId: "elinikki/waterstone/trace_marks",
                flagKey: FlagKeys.ELINIKKI_TRACE_MARKS,
                dramaId: "elinikki_trace_marks");
            Register(map,
                placementId: "elinikki/waterstone/trace_channel",
                flagKey: FlagKeys.ELINIKKI_TRACE_CHANNEL,
                dramaId: "elinikki_trace_channel");
            Register(map,
                placementId: "elinikki/waterstone/trace_stones",
                flagKey: FlagKeys.ELINIKKI_TRACE_STONES,
                dramaId: "elinikki_trace_stones");
            // trace_journal uses a different flag (ELINIKKI_JOURNAL_FOUND)
            // because the story spec treats the journal as a one-shot
            // state toggle rather than one of the eight trace flags
            // counted at chapter 5.
            Register(map,
                placementId: "elinikki/waterstone/trace_journal",
                flagKey: FlagKeys.ELINIKKI_JOURNAL_FOUND,
                dramaId: "elinikki_trace_journal");

            // Chapter 2: 反響の層 — non-echo traces. The three
            // echo experiment points (point_a/b/c) are NOT in this
            // table; they are handled by
            // <see cref="TryDispatchEchoExperiment"/> below, which
            // gates on the ELINIKKI_ECHO_EXPERIMENT counter value.
            Register(map,
                placementId: "elinikki/echo/trace_map",
                flagKey: FlagKeys.ELINIKKI_TRACE_MAP,
                dramaId: "elinikki_trace_map");
            Register(map,
                placementId: "elinikki/echo/trace_shadow",
                flagKey: FlagKeys.ELINIKKI_TRACE_SHADOW,
                dramaId: "elinikki_trace_shadow");

            // Chapter 3: 花の層
            Register(map,
                placementId: "elinikki/bloom/trace_flowers",
                flagKey: FlagKeys.ELINIKKI_TRACE_FLOWERS,
                dramaId: "elinikki_trace_flowers");
            Register(map,
                placementId: "elinikki/bloom/trace_weave",
                flagKey: FlagKeys.ELINIKKI_TRACE_WEAVE,
                dramaId: "elinikki_trace_weave");

            return map;
        }

        private static void Register(
            Dictionary<string, TraceTrigger> map,
            string placementId,
            string flagKey,
            string dramaId)
        {
            map[placementId] = new TraceTrigger(placementId, flagKey, dramaId);
        }

        /// <summary>
        /// Clears the movement cache. Called from
        /// <c>ElinikkiQuestFlow.Pulse</c> on every Zone.Activate
        /// so that a save/load onto a landmark tile re-runs the
        /// proximity scan on the next frame, instead of the
        /// cached same-zone-same-tile fast path swallowing it.
        /// </summary>
        public static void InvalidateMovementCache()
        {
            _lastZoneId = null;
            _lastPlayerX = int.MinValue;
            _lastPlayerZ = int.MinValue;
        }

        /// <summary>
        /// Per-frame polling entry point. Fail-soft: any exception is
        /// caught and logged so the Harmony postfix on
        /// <c>Game.OnUpdate</c> cannot leak into the main game loop.
        /// </summary>
        public static void Tick()
        {
            try
            {
                var zone = EClass._zone;
                if (zone == null)
                {
                    return;
                }

                string zoneId = zone.source?.id;
                if (string.IsNullOrEmpty(zoneId))
                {
                    return;
                }

                var pc = EClass.pc;
                if (pc == null || pc.pos == null)
                {
                    return;
                }

                int px = pc.pos.x;
                int pz = pc.pos.z;

                // Echo stage 1 retry runs on EVERY frame in
                // LayerEcho, not just on movement, so a non-drama
                // UI layer blocking the initial Zone.Activate
                // pulse does not strand the chapter-2 opening
                // beat for the rest of the visit. The dispatcher
                // short-circuits cheaply when the counter is
                // already advanced, the stage is wrong, or the
                // UI is still busy, so the per-frame cost is a
                // few flag reads.
                ElinikkiQuestFlow.TryDispatchEchoStage1FromExaminer();

                // Fast path: position has not changed since the
                // last successful Tick. The cache is only updated
                // when we actually ran a full scan (see below), so
                // a first visit whose UI was busy stays replayable
                // the moment the UI frees up even if the player is
                // still on the same tile.
                if (zoneId == _lastZoneId
                    && px == _lastPlayerX
                    && pz == _lastPlayerZ)
                {
                    return;
                }

                // Only Elinikki quest zones have trace placements.
                // GetDefinitionsForZone returns an empty sequence
                // for non-quest zones, so this bails out cheaply.
                var defs = ElinikkiPlacementData.GetDefinitionsForZone(zoneId);
                if (defs == null)
                {
                    return;
                }

                // IsUiBusy check runs BEFORE the movement cache is
                // updated so that a busy-UI first visit to a trace
                // cell does not commit the cache and permanently
                // skip that tile. Once the UI frees up the next
                // frame still sees (zoneId, px, pz) != (cached)
                // and re-enters the scan.
                if (IsUiBusy())
                {
                    return;
                }

                // Run the dispatchers first. If either started a
                // drama we LEAVE THE MOVEMENT CACHE STALE: the
                // drama layer is about to take UI focus, so the
                // next Tick's IsUiBusy early-out prevents any new
                // dispatch from stacking while it plays. If the
                // drama is later interrupted (reload, layer kill
                // before set_flag) the flag guard inside the
                // dispatchers is still false, and a subsequent
                // Tick re-scans this tile because the cache was
                // never committed. This implements "retry until
                // the trigger flag is actually set" for a
                // stationary player.
                bool dispatched = TryDispatchTraceTriggers(defs, px, pz);
                if (!dispatched)
                {
                    dispatched = TryDispatchEchoExperiment(defs, zoneId, px, pz);
                }

                if (dispatched)
                {
                    return;
                }

                // No trigger fired on this tile. Commit the
                // movement cache so subsequent frames on the same
                // cell take the fast path until the player moves.
                _lastZoneId = zoneId;
                _lastPlayerX = px;
                _lastPlayerZ = pz;
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Trace examiner tick failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Scans the simple trace triggers (chapter 1 / 3 traces
        /// and the chapter-2 non-echo traces). Returns true if a
        /// drama was started, so the caller can skip the echo
        /// pass and avoid stacking two dramas in one tick.
        /// </summary>
        private static bool TryDispatchTraceTriggers(
            IEnumerable<Elin_Elinikki.SharedWorldPrimitiveDefinition> defs,
            int px,
            int pz)
        {
            foreach (var def in defs)
            {
                if (def == null)
                {
                    continue;
                }

                if (!_triggersByPlacementId.TryGetValue(def.Id, out TraceTrigger trig))
                {
                    continue;
                }

                // Flag guard — only fire once per trace per save.
                if (QuestStateService.GetFlagInt(trig.FlagKey, 0) != 0)
                {
                    continue;
                }

                if (!IsWithinTriggerRadius(def.TilePosition.x, def.TilePosition.z, px, pz))
                {
                    continue;
                }

                QuestModLog.Info(
                    "Elinikki trace examine. placement=" + def.Id +
                    " drama=" + trig.DramaId);

                // Only report "dispatched" when the drama runtime
                // actually opened the layer. A missing asset or a
                // silent refusal returns false and we want Tick()
                // to commit the movement cache in that case — the
                // alternative is logging once per frame forever.
                return DramaContext.TryStartDramaUntilComplete(trig.DramaId);
            }

            return false;
        }

        /// <summary>
        /// Chapter-2 echo experiment proximity dispatcher. Walks
        /// the <c>elinikki/echo/point_a|b|c</c> placements and,
        /// when the player is within
        /// <see cref="TriggerRadiusTiles"/> of the point that
        /// matches the current
        /// <see cref="FlagKeys.ELINIKKI_ECHO_EXPERIMENT"/> counter,
        /// launches the next stage drama.
        /// <para>
        /// Counter semantics (synced with
        /// <c>tools/drama/scenarios/elinikki_echo_stage_*.py</c>):
        /// <list type="bullet">
        /// <item><description><c>counter == 0</c>: stage 1 pending
        /// — handled on zone entry by
        /// <c>ElinikkiQuestFlow.TryDispatchEchoStage1</c>. This
        /// method does nothing for counter 0.</description></item>
        /// <item><description><c>counter == 1</c>: point_a fires
        /// <c>elinikki_echo_stage_2</c>.</description></item>
        /// <item><description><c>counter == 2</c>: point_b fires
        /// <c>elinikki_echo_stage_3</c>.</description></item>
        /// <item><description><c>counter == 3</c>: point_c fires
        /// <c>elinikki_echo_stage_4</c> (which also sets
        /// <see cref="FlagKeys.ELINIKKI_TRACE_ECHO"/>).</description></item>
        /// <item><description><c>counter &gt;= 4</c>: experiment
        /// complete, nothing to do.</description></item>
        /// </list>
        /// Each stage drama bumps the counter, so the position
        /// match on the next tick falls through on the already-
        /// fired point and only triggers on the next target point.
        /// </para>
        /// </summary>
        private static bool TryDispatchEchoExperiment(
            IEnumerable<Elin_Elinikki.SharedWorldPrimitiveDefinition> defs,
            string zoneId,
            int px,
            int pz)
        {
            // Only the echo layer carries these placements. Skip
            // the whole pass on other zones — cheap compared to
            // walking the dictionary for every tile step.
            if (!string.Equals(
                    zoneId,
                    ElinikkiZoneIds.LayerEcho,
                    StringComparison.Ordinal))
            {
                return false;
            }

            int counter = QuestStateService.GetFlagInt(
                FlagKeys.ELINIKKI_ECHO_EXPERIMENT, 0);

            // Counter 0 is stage 1, which runs as a zone-entry
            // dispatch (ElinikkiQuestFlow.TryDispatchEchoStage1),
            // not a proximity trigger. Counters 4+ mean the
            // experiment is done. Both cases short-circuit here.
            if (counter <= 0 || counter >= 4)
            {
                return false;
            }

            string targetPlacementId;
            string targetDramaId;
            switch (counter)
            {
                case 1:
                    targetPlacementId = "elinikki/echo/point_a";
                    targetDramaId = "elinikki_echo_stage_2";
                    break;
                case 2:
                    targetPlacementId = "elinikki/echo/point_b";
                    targetDramaId = "elinikki_echo_stage_3";
                    break;
                case 3:
                    targetPlacementId = "elinikki/echo/point_c";
                    targetDramaId = "elinikki_echo_stage_4";
                    break;
                default:
                    return false;
            }

            foreach (var def in defs)
            {
                if (def == null)
                {
                    continue;
                }

                if (!string.Equals(def.Id, targetPlacementId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsWithinTriggerRadius(def.TilePosition.x, def.TilePosition.z, px, pz))
                {
                    return false;
                }

                QuestModLog.Info(
                    "Elinikki echo experiment advance. counter=" + counter +
                    " placement=" + def.Id +
                    " drama=" + targetDramaId);

                // Match TryDispatchTraceTriggers: only return true
                // when the drama runtime actually opened the
                // layer. Otherwise Tick() would commit the
                // movement cache against a failed start and stop
                // retrying, which is the desired behavior for a
                // missing asset but NOT for an interrupted one —
                // the drama runtime distinguishes the two via its
                // IsDramaDone guard, which returns false for both
                // "never ran" and "started but not finished" and
                // true only for "fully completed".
                return DramaContext.TryStartDramaUntilComplete(targetDramaId);
            }

            return false;
        }

        private static bool IsWithinTriggerRadius(float tileX, float tileZ, int px, int pz)
        {
            // Point.x / Point.z are tile-grid indices (integers) and
            // the placement TilePosition uses the cell-centre
            // convention (float, typically ending in .5). Adding
            // 0.5 to the player's integer coords moves them to the
            // same centre convention before the distance test.
            float dx = tileX - (px + 0.5f);
            float dz = tileZ - (pz + 0.5f);
            float distSq = dx * dx + dz * dz;
            return distSq <= TriggerRadiusTilesSq;
        }

        private static bool IsUiBusy()
        {
            var ui = EClass.ui;
            if (ui == null)
            {
                return true;
            }
            return ui.IsActive;
        }
    }
}
