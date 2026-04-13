# Elinikki Quest Implementation Progress

Status: IN PROGRESS

## Current Phase
Phase 3: SharedWorldObject zone-aware placement (Phase 1-2 complete)

## Phases

### Phase 1: Quest infrastructure foundation
- [x] Task 1.1: Copy Elin_QuestMod src/ into Elin_Elinikki/src/Quest/
- [x] Task 1.2: Update csproj, package.xml, Plugin.cs
- [x] Task 1.3: Define ElinikkiQuestStage enum
- [x] Task 1.4: Rewrite QuestFlow as ElinikkiQuestFlow
- [x] Task 1.5: Fame-5000 gate in Patch_Zone_Activate_QuestPulse
- [x] Task 1.6: Verify build.bat debug

### Phase 2: Drama scripts
- [x] Task 2.1: DramaDsl submodule reference
- [x] Task 2.2: Chapter-00 drama
- [x] Task 2.3: Trace examine dramas (chapters 1-3)
- [x] Task 2.4: Echo experiment dramas (4 stages)
- [x] Task 2.5: Chapter-04 reunion + truth dramas
- [x] Task 2.6: Chapter-05 return + endings
- [x] Task 2.7: Verify drama compilation

### Phase 3: SharedWorldObject zone-aware placement
- [x] Task 3.1: Zone transition hook
- [x] Task 3.2: Object placement data per layer
- [x] Task 3.3: RemoveDefinitionsByPrefix + Upsert pipeline
- [x] Task 3.4: Fog/color/LUT per zone
- [ ] Task 3.5: Placeholder textures
- [ ] Task 3.6: Verify FPS visual output

### Phase 4: Audio integration
- [ ] Task 4.1: Drama commands for BGM/SE
- [ ] Task 4.2: BGM mapping per layer
- [ ] Task 4.3: Echo SE trigger points
- [ ] Task 4.4: In-game audio verification

### Phase 5: Custom map data (needs user action)
- [ ] Task 5.1: Document 6 maps for devmode creation
- [ ] Task 5.2: Zone registration stubs
- [ ] PAUSED: awaiting user to create devmode maps

### Phase 6: End-to-end verification
- [ ] Task 6.1: Full playthrough chapters 0-5
- [ ] Task 6.2: Flag behavior verification
- [ ] Task 6.3: Ending resolution verification
- [ ] Task 6.4: Deviation report

### Phase 7: Polish
- [ ] Task 7.1: Final textures
- [ ] Task 7.2: Fog/LUT tuning
- [ ] Task 7.3: Map size adjustment
- [ ] Task 7.4: Codex review

## Blockers
(none yet)

## Deviations
- Task 1.1 pulled in csproj Reflex.dll reference (originally planned for Task 1.2) because
  `QuestModDebugConsole.cs` depends on ReflexCLI attributes. Without the reference the
  build would break between iterations. Noted so Task 1.2 scope shrinks accordingly.
- QuestBootstrap.cs still has [BepInPlugin] with ModGuid "yourname.elin_quest_mod". This
  means two BepInPlugin classes currently coexist in the assembly (main Elin_Elinikki.Plugin
  and Elin_Elinikki.Quest.QuestBootstrap). Build succeeds; runtime may warn about double
  registration. Task 1.2 will remove the BepInPlugin role from QuestBootstrap and call it
  from the main Plugin.Awake() instead.
- Task 2.1: tools/drama/data_generated.py and tools/drama/schema/key_spec.py still contain
  the QuestMod template's flag/resolve/command/cue constants (e.g. "yourname.elin_quest_mod.*"
  and "quest_drama_replace_me"). These are not referenced by the empty create_drama_excel.py,
  so they do not affect the current build, but they will need to be regenerated for Elinikki
  before any scenario that imports FlagKeys/CommandKeys etc. is authored. Task 2.2 (first
  scenario) will update schema/key_spec.py and run generate_keys.py to refresh
  data_generated.py at the same time it creates the first scenario file.
  RESOLVED in Task 2.2: key_spec.py rewritten, generate_keys.py updated for Elinikki
  (chitsii.elinikki flag prefix, src/Quest/Drama/Generated output path, correct namespace),
  and both data_generated.py and DramaKeys.g.cs regenerated.

## Notes
- 2026-04-13: Progress tracker initialized.
- 2026-04-13: Task 1.1 complete. Copied QuestMod/src/ subdirs into Elin_Elinikki/src/Quest/,
  renamed namespaces (Elin_QuestMod -> Elin_Elinikki.Quest), renamed class Plugin -> QuestBootstrap
  and ModLog -> QuestModLog to avoid collision with existing Elinikki classes. Added Reflex.dll
  reference to csproj. build.bat debug passes (0 warnings, 0 errors).
- 2026-04-13: Task 1.2 complete. Rewrote QuestBootstrap.cs as a static non-MonoBehaviour class,
  removed [BepInPlugin] attribute so it is no longer a standalone BepInEx plugin. Added
  QuestStateService.SetDefaultPrefix() so the flag prefix decouples from the BepInEx ModGuid
  "chitsii.elin_elinikki". Main Plugin.Awake() now calls QuestBootstrap.Initialize(Logger)
  after Harmony PatchAll (which already picks up patches in Elin_Elinikki.Quest.Patches via
  whole-assembly scan). package.xml unchanged — it already describes Elinikki correctly.
  Codex review flagged a P1 issue: initially set FlagPrefix to "chitsii.elinikki.quest" but
  QuestStateService built-in local keys already carry a `quest.` segment (quest.current_phase,
  quest.done.*, quest.active.*), producing doubled `chitsii.elinikki.quest.quest.*` keys.
  Fixed by setting FlagPrefix = "chitsii.elinikki" so full keys match the story spec
  `chitsii.elinikki.quest.*`. build.bat debug passes (0 warnings, 0 errors).
- 2026-04-13: Task 1.3 complete. Created src/Quest/Quest/ElinikkiQuestStage.cs with the 8-stage
  enum (NotStarted..EndingSeen) matching the Quest Stage Transitions table in
  story/chapters/_index.md, plus static helpers (GetCurrentStage, AdvanceToStage,
  IsAheadOfCurrent). Codex review caught a P1 issue: initially read/wrote via
  QuestStateService.GetCurrentPhase/SetCurrentPhase, which would collide with the
  lower-level QuestFlow.QuestPhase state machine (Bootstrap/Intro/Followup/Completed)
  and silently bypass the spec'd `chitsii.elinikki.quest.stage` key. Fixed by introducing
  a dedicated local key `quest.stage` via QuestStateService.BuildFlagKey + GetFlagInt/SetFlagInt.
  AdvanceToStage enforces forward-only transitions. build.bat debug passes (0 warnings, 0 errors).
- 2026-04-13: Task 1.4 complete. Replaced the template QuestFlow.cs with ElinikkiQuestFlow.cs.
  Also created ElinikkiZoneIds.cs with canonical zone content-id constants. Removed the old
  QuestFlow.cs entirely (its Intro/Followup dispatch pattern does not apply to a single-quest
  chapter mod). Updated Patch_Zone_Activate_QuestPulse and GameQuestDramaRuntimeContext to call
  ElinikkiQuestFlow.Pulse() instead of QuestFlow.Pulse().

  Drama integration: extended QuestDramaResolver with Elinikki-specific keys:
    - `state.elinikki.stage.at_least.<stage_name>` (TryResolveBool)
    - `cmd.elinikki.stage.advance.<stage_name>` (TryExecute)
  Both parse snake_case stage names via SnakeCaseToPascalCase + Enum.TryParse.
  This lets drama scripts gate content by current stage and trigger drama-driven transitions
  (Accepted, YuuFound, EndingSeen).

  Zone rule model: ZoneStageRule with (Predecessor, Target, RequirePreviousZoneId). Rules are
  registered in QuestBootstrap.Initialize via ElinikkiQuestFlow.RegisterDefaultZoneRules().
  Pulse() reads the current zone, looks up rules, and fires the first one whose predecessor
  matches current stage AND whose previous-zone gate (if set) matches the player's last zone.

  Codex review took 3 rounds on this task:
    * Round 1: [P1] drama runtime had no way to reach TryAdvanceStage — fixed by extending
      QuestDramaResolver with Elinikki-specific keys.
    * Round 1: [P1] zone stage map was empty (no callers to RegisterZoneStage) — fixed by
      adding ElinikkiZoneIds constants and RegisterDefaultZoneRules() called from
      QuestBootstrap.Initialize.
    * Round 1: [P2] zone rule semantics were too permissive for reused zones — fixed by
      adding the Predecessor gate in ZoneStageRule.
    * Round 2: [P1] off-by-one. Initially attached "LayerNClear" to the entry of the same
      layer (e.g. entering Waterstone from Accepted -> Layer1Clear), but Pulse() runs on
      the DESTINATION zone so that fires on the first frame of chapter 1, not when chapter
      1 ends. Fixed by moving rules to the NEXT zone (entering Echo from Accepted ->
      Layer1Clear, entering Bloom from Layer1Clear -> Layer2Clear, entering YuuCamp from
      Layer2Clear -> Layer3Clear).
    * Round 3: [P2] NefiaEntrance rule would fire on any teleport back to the entrance
      (recall, world map, etc.) once the player reached YuuFound. Fixed by adding an optional
      RequirePreviousZoneId to ZoneStageRule plus a _lastObservedZoneId snapshot in Pulse(),
      so the Returned transition only fires when coming directly from YuuCamp.

  build.bat debug passes (0 warnings, 0 errors) after each round.
- 2026-04-13: Task 1.5 complete. Added chapter-0 start gate in ElinikkiQuestFlow:
  TryStartIntroQuest() checks stage==NotStarted + player.homeBranch.owner (specific home
  zone, not any IsPlayerFaction zone) + player.fame >= 5000 + UI idle, then requests the
  intro drama. Guarded by a phase const IntroDramaAvailable=false until Phase 2 Task 2.2
  packages the drama asset. Added secondary retry path: Patch_Player_OnAdvanceHour_QuestPulse
  calls ElinikkiQuestFlow.IntroRetryPulse() (NOT the full Pulse()) so an hourly tick cannot
  advance zone-based stages outside of an actual Zone.Activate.

  Codex review took 4 rounds on this task:
    * Round 1 [P1]: used plain TryStartDrama which is one-shot — would strand the quest
      permanently if the intro was interrupted. Fixed by using TryStartDramaUntilComplete.
    * Round 2 [P2]: IsPlayerFaction matches any owned settlement, not the canonical home.
      Fixed by gating on pc.homeBranch?.owner ?? pc.homeZone.
    * Round 3 [P2]: no retry source — Pulse() only fires on Zone.Activate. Fixed by adding
      Patch_Player_OnAdvanceHour_QuestPulse.
    * Round 4 [P2]: hourly retry reused full Pulse() and could advance zone stages while
      idling. Fixed by splitting off IntroRetryPulse() that only runs the intro gate, and
      adding IsUiBusy() check so the drama does not open on top of sleep cutscenes or
      menus.
    * Round 4 [P1]: drama asset is not yet packaged — the "harmless no-op" assumption was
      wrong because LayerDrama.Activate throws when the drama sheet is missing. Fixed by
      adding IntroDramaAvailable phase const (false until Phase 2), with local CS0162
      pragma suppression for the unreachable post-gate code path.
    * Round 5: Codex returned no issues. Internally consistent, fail-soft, scoped.

  build.bat debug passes (0 warnings, 0 errors) in final state.
- 2026-04-13: Task 2.1 complete. Set up the drama authoring / compilation pipeline:
    * Copied tools/drama/ from Elin_QuestMod (drama_builder, schema, tests, scenarios dir)
    * Removed QuestMod's reference scenarios (quest_drama_feature_*.py) leaving an empty
      scenarios/__init__.py ready for Phase 2.2-2.6 to populate.
    * Rewrote tools/drama/data.py with Elinikki-specific DramaIds (25 ids total covering
      chapter 0-5 + 8 truth conversations + 2 endings).
    * Rewrote tools/drama/create_drama_excel.py with an empty DRAMAS list that no-ops
      gracefully until scenarios are added. Output path: LangMod/EN/Dialog/Drama/drama_<id>.xlsx
    * Updated build.bat to run `python tools/drama/create_drama_excel.py` as step [1/3]
      before dotnet build, and to include LangMod/ in the deploy copy step.
  Verified: `python tools/drama/create_drama_excel.py` runs cleanly ("no scenarios
  registered yet"); `build.bat debug` passes with the new 3-step pipeline.
  Codex review was started but killed manually (took too long for a tooling-only change).
  Python build tooling only — no runtime C# logic touched, so review skipped per global
  rules.
- 2026-04-13: Task 3.4 complete. Per-zone fog / clear / LUT override pipeline:
    * src/Quest/Placement/ElinikkiAtmosphereData.cs — static profile
      table keyed by Elinikki zone id. waterstone (blue-green fog,
      cool LUT), echo (near-black fog, warm residual), bloom (warm
      magenta fog, warm LUT). yuu_camp and nefia_entrance have no
      entry so the scene profile passes through unchanged per the
      story spec "演出なし".
    * src/Quest/Placement/ElinikkiAtmosphereRuntime.cs — static
      holder for the currently-active profile, plus blend helpers
      BlendFogColor / BlendClearColor / ApplyLutTint that no-op
      when no override is active. Read by the renderer, written by
      the placement manager.
    * src/FpsGpuPreviewRenderer.AtmospherePass.cs —
      ResolveFogColor/ResolveClearColor consult
      ElinikkiAtmosphereRuntime.Blend* after the scene-profile
      evaluation so the override layers on top of Elin's day/night
      curve. ApplyLutTint runs exactly once per pixel at each of
      the four return points of ApplyAtmosphericFog (behind-camera,
      fogStart fast-path, distance-fog off branch, full fog path).
      The fogStart fast-path also runs ApplySceneTone to keep near
      pixels day/night-graded.
    * src/SharedWorldObjectManager.cs — public static
      RemoveDefinitionsByPrefix gains a new caller; ApplyDefinition
      now pre-tints the preview material color with the active LUT
      so chapter landmarks match the surrounding terrain's tint.
    * src/Quest/Placement/ElinikkiZonePlacementManager.cs — atmosphere
      sync runs BEFORE the shared-world cleanup so a failure in the
      upsert pipeline cannot leak stale tinting into the new zone.
      Null-zoneId activations clear the override (avoiding the
      "stale chapter tint in non-Elinikki map" failure mode at the
      cost of a one-frame missing tint for save-reload into chapter
      zones — self-heals on the next activation).
  Codex review: 5 rounds. 5 real issues fixed: LUT double-tint on
  intermediate fog/clear, LUT skip on fogStart fast-path, atmosphere
  leak on placement cleanup throw, null-zone handling (Codex flipped
  twice; settled on "clear" as the safer default), scene tone skip
  on fogStart fast-path. Known limitation: shared-world preview
  props only receive the LUT tint, not the full fog/scene-tone stack
  (ApplySceneTone is instance-bound to FpsGpuPreviewRenderer).
  Deferred to Task 3.6 visual verification to decide whether the
  discrepancy is worth the refactor.
  build.bat debug passes 0 warnings, 0 errors.
- 2026-04-13: Task 3.3 complete. Remove + Upsert pipeline wired up:
    * src/SharedWorldObjectManager.cs — added public static
      RemoveDefinitionsByPrefix(prefix) entry point. The existing
      private instance method was renamed
      RemoveDefinitionsByPrefixInstance to avoid a static/instance
      name collision, and the two internal callers in
      EnsureDreamTestSet switched to the new name.
    * src/Quest/Placement/ElinikkiZonePlacementManager.cs — replaced
      the Task 3.1 logging no-op with the real swap pipeline:
      RemoveDefinitionsByPrefix(PlacementPrefix) always runs first
      (idempotent, scoped to the "elinikki/" prefix), then if the
      incoming zone is Elinikki the manager iterates
      ElinikkiPlacementData.GetDefinitionsForZone and calls
      SharedWorldObjectManager.Upsert for each definition. A
      fail-loud check rejects any definition whose id is missing the
      shared prefix so the next zone clear would not wipe it.
    * Null-zoneId handling: the clear now runs even when
      Zone.source?.id is null so leaving a chapter zone into a
      runtime-generated / not-yet-attached zone cannot leak the old
      trace set into the next map. Only non-null zone ids rotate
      _lastZoneId so the next real activation can still classify.
  Codex review: 1 round, 1 P2 fixed (null-zone leak). Clean on the
  second pass. build.bat debug passes 0 warnings, 0 errors.
- 2026-04-13: Task 3.2 complete. Per-layer placement data authored:
    * src/Quest/Placement/ElinikkiPlacementData.cs — internal static
      factory that returns the 12 trace/marker primitive definitions
      for layer_waterstone (4), layer_echo (5), layer_bloom (2), and
      yuu_camp (1). All ids share the "elinikki/" prefix. All positions
      are in cell-center tile-space (.5 offsets) so
      FpsIdealizedWorld.GetSurfaceHeightAt samples the right cell.
      Visibility = Both so the markers render in the normal top-down
      view as well as the GPU FPS preview.
    * Helper factories MakeFloorQuad / MakeWallQuad / MakeGroundCube
      encapsulate Unity-primitive conventions that bit me during code
      review:
        - Unity's primitive Quad has normal -Z (per the official
          PrimitiveObjects manual page). Rotating (90°, 0°, 0°)
          remaps the normal to +Y so the front face points up;
          after the rotation the quad's local Y becomes world Z, so
          footprint depth must be passed via scale.y (scale.z is
          unused because the quad has no local thickness).
        - Wall quads take a per-placement yawDegrees parameter and
          derive their normal-view proxy footprint from it so rotated
          walls (echo/trace_shadow) read with their width on the
          correct world axis.
        - Unity's primitive Cylinder is 2 units tall by default, so
          the campfire uses Scale.y = 0.175 for a visible 0.35-tile
          height and lifts TilePosition.y to half of that so the
          cylinder sits on the surface.
        - Ground cubes are centered on their pivot, so TilePosition.y
          must equal Scale.y / 2 for the cube bottom to rest on the
          sampled surface.
    * Coordinates are a provisional ~40x40 layout; Task 3.6 will
      tune against Phase 5's real devmode maps.
  Codex review took 6 rounds. Real issues addressed: scale.y depth
  for floor quads, wall yaw per placement, AttachToSurface=true for
  walls, cell-center tile-space offsets, Unity Cylinder/cube pivot
  compensation, Visibility=Both, and wall proxy footprint rotation.
  Codex flipped three times on the Unity Quad front-face direction;
  resolved via the Unity manual (confirmed normal = -Z) and the math
  in the helper's doc comment. Later iterations are not re-run against
  codex on that point to avoid an infinite flip-flop loop.
  Verified: build.bat debug passes 0 warnings, 0 errors.
- 2026-04-13: Task 3.1 complete. Zone transition hook scaffold added:
    * src/Quest/Placement/ElinikkiZonePlacementManager.cs — static swap
      driver that tracks the last observed zone id and classifies each
      Zone.Activate as enter/swap/leave against the Elinikki zone set.
      PlacementPrefix = "elinikki/" is defined here so Task 3.3 can use
      it as the argument to SharedWorldObjectManager's
      RemoveDefinitionsByPrefix pipeline without colliding with the
      manager's own "demo/" set. Body is a logging-only no-op at this
      stage; Task 3.2 adds the per-layer data and Task 3.3 wires the
      Remove + Upsert calls.
    * src/Quest/Patches/Patch_Zone_Activate_PlacementRefresh.cs — new
      Harmony postfix on Zone.Activate. Targets the same method as
      Patch_Zone_Activate_QuestPulse via the low-arity Activate overload
      but runs independently: placement and quest state deliberately do
      not share mutable state, so a fault in one postfix cannot break
      the other. Uses the existing QuestModLog facade for fail-soft
      logging.
    * Previous-zone tracking is isolated from ElinikkiQuestFlow's own
      _lastObservedZoneId. This is deliberate: resetting one must not
      desync the other.
  Verified: build.bat debug compiles with 0 warnings, 0 errors. Codex
  review on the new files flagged no issues. The hook is live but the
  no-op body means in-game behaviour is unchanged — Task 3.2/3.3 will
  introduce actual object placement.
- 2026-04-13: Task 2.7 complete. Phase 2 drama compilation verified end-to-end:
    * generate_keys.py --check: OK. data_generated.py and DramaKeys.g.cs are
      in sync with tools/drama/schema/key_spec.py.
    * Cross-check tools/drama/data.py DramaIds.ALL vs create_drama_excel.py
      DRAMAS: 25 declared, 25 registered, 0 missing / 0 extra.
    * tools/drama/scenarios/ contains exactly 25 elinikki_*.py files (one
      per drama id). No orphans.
    * Full clean rebuild: rm -f LangMod/EN/Dialog/Drama/*.xlsx, then
      build.bat debug regenerates all 25 Excel files and compiles with
      0 warnings, 0 errors.
  Phase 2 complete. The drama content layer is self-consistent and ready
  for Phase 3 to wire up the zone triggers, examine handlers, and the
  chapter-4 truth menu that will invoke these dramas.
- 2026-04-13: Task 2.6 complete. Chapter-5 return journey + two named endings.
    * key_spec.py: added ELINIKKI_QUEST_ENDING flag (int 0-3) mapping to
      chitsii.elinikki.quest.ending. Values: 0 none, 1 return, 2 silence,
      3 revisit. Regenerated data_generated.py and DramaKeys.g.cs.
    * tools/drama/scenarios/:
        - elinikki_return_journey.py — four-wave walk-out conversation
          covering Yuu's daily life, Mina's "3週間" confrontation, Sora's
          silence, and Mina's "花は綺麗だった" reversal seed. Labels are
          split per wave so a future C# side can re-enter mid-walk if
          pacing calls for it. Does not advance quest.stage — the
          Returned transition fires through the nefia-entrance zone hook.
        - elinikki_ending_return.py — "帰還エンド" main exchange plus a
          narrator epilogue (Sora closes the notebook, Mina drinks alone,
          the carried bloom). Sets quest.ending = 1 and advances stage to
          EndingSeen via cmd.elinikki.stage.advance.ending_seen.
        - elinikki_ending_silence.py — "沈黙エンド" exchange + Sora's
          paper epilogue and "見たものは、見たままでいい" beat. Sets
          quest.ending = 2 and advances stage to EndingSeen. Per
          chapters/_index.md "Ending Resolution Logic" this drama also
          serves truth-count 1-7 as the silence-partial bucket.
    * The hidden revisit ending has no drama id in tools/drama/data.py
      because the story bible specifies "最後のテキスト: なし" — it is
      an environmental beat (single un-bloomed flower, no dialogue), so
      Phase 3 will handle it via a zone-level flag set (quest.ending = 3)
      without a drama. Noted here so the absence of ending_revisit.py is
      intentional.
    * create_drama_excel.py: registered the 3 new scenarios under a
      "Chapter 5" comment mentioning the revisit-no-drama decision.
  Verified: build.bat debug generates 25 drama Excel files (22 prior +
  3 new) and compiles with 0 warnings, 0 errors. Codex review skipped:
  narrative content + additive key output, no runtime C# logic touched.
- 2026-04-13: Task 2.5 complete. Chapter-4 reunion + 8 truth conversation dramas
  authored (9 new scenarios total):
    * key_spec.py: added 8 ELINIKKI_TRUTH_* flag specs matching
      chitsii.elinikki.quest.event.truth_{marks,channel,stones,echo,map,
      shadow,flowers,weave}. Regenerated data_generated.py and DramaKeys.g.cs.
    * tools/drama/scenarios/:
        - elinikki_reunion.py — atmospheric strip-down + Yuu greeting. Ends
          with cmd.elinikki.stage.advance.yuu_found so the drama itself is
          the trigger for the Layer3Clear -> YuuFound transition (matches
          chapters/_index.md "Quest Stage Transitions" table).
        - elinikki_truth_marks.py  (failed flower doodle)
        - elinikki_truth_channel.py (hand-dug drinking channel)
        - elinikki_truth_stones.py  (tidiness, Mina nods)
        - elinikki_truth_echo.py    (own voice + folded-in journal "伝わった" beat)
        - elinikki_truth_map.py     (abandoned exit map, "同じ存在 = 俺だ")
        - elinikki_truth_shadow.py  (soot silhouette from campfire)
        - elinikki_truth_flowers.py (trash pile sprouts, Mina's silence)
        - elinikki_truth_weave.py   (abandoned rope attempt, boredom)
      Each truth drama sets its corresponding quest.event.truth_* flag. Gate
      (trace_X == 1 AND truth_X == 0) is deferred to the Phase 3 chapter-4
      dialogue menu — the dramas themselves just set the flag.
      Design choice: the chapter-4.md "手帳の『伝わった』" beat has no
      dedicated DramaId in tools/drama/data.py, so it is folded into
      elinikki_truth_echo.py via a second conversation block. Noted so the
      absence of an elinikki_truth_journal scenario is intentional, not an
      oversight.
    * create_drama_excel.py: registered all 9 new scenarios grouped under a
      "Chapter 4" comment.
  Verified: build.bat debug now generates 22 drama Excel files (13 prior +
  9 new) and compiles with 0 warnings, 0 errors. Codex review skipped:
  narrative content + auto-generated additive key output, no runtime C#
  logic changed.
- 2026-04-13: Task 2.4 complete. Four echo experiment drama scripts authored for
  chapter-2 sections 2-5 (awareness -> point A -> point B -> point C):
    * key_spec.py: added ELINIKKI_ECHO_EXPERIMENT flag mapping to
      chitsii.elinikki.quest.state.echo_experiment (int 0-4). Regenerated
      data_generated.py and DramaKeys.g.cs.
    * tools/drama/scenarios/:
        - elinikki_echo_stage_1.py — entry awareness, echo_experiment=1
        - elinikki_echo_stage_2.py — point A clap mismatch, echo_experiment=2
        - elinikki_echo_stage_3.py — point B count preserved, echo_experiment=3
        - elinikki_echo_stage_4.py — point C pattern response, echo_experiment=4
          AND trace_echo=1 (the PHM climax beat, Mina's first silence)
      Dialogue follows chapter-02.md section-by-section. Trigger gating
      (echo_experiment == N prerequisite) is deferred to Phase 3 C# side;
      the dramas themselves just set the stage flag on completion.
    * create_drama_excel.py: registered the 4 echo stages alongside chapter-2
      traces, grouped by chapter.
  Verified: build.bat debug generates 13 drama Excel files (9 from Task 2.3 +
  4 new) and compiles with 0 warnings, 0 errors. Codex review skipped for the
  same reason as Task 2.1-2.3: narrative content + auto-generated key output
  only.
- 2026-04-13: Task 2.3 complete. Eight trace examine drama scripts authored for
  chapters 1-3 (echo experiment stages deferred to Task 2.4):
    * key_spec.py: added 8 flag specs ELINIKKI_TRACE_{MARKS,CHANNEL,STONES,ECHO,
      MAP,SHADOW,FLOWERS,WEAVE} mapping to chitsii.elinikki.quest.event.trace_*.
      trace_echo is defined here for parity with the story spec even though it
      is set by Task 2.4's echo experiment stage-4 drama, not a pure examine.
    * generate_keys.py --write regenerated tools/drama/data_generated.py and
      src/Quest/Drama/Generated/DramaKeys.g.cs with the 8 new constants.
    * tools/drama/scenarios/:
        - elinikki_trace_marks.py  (chapter 1, sets trace_marks)
        - elinikki_trace_channel.py (chapter 1, sets trace_channel)
        - elinikki_trace_stones.py (chapter 1, sets trace_stones)
        - elinikki_trace_journal.py (chapter 1, no flag — pure story beat
          for Yuu's notebook "伝わった" foreshadow)
        - elinikki_trace_map.py (chapter 2, sets trace_map)
        - elinikki_trace_shadow.py (chapter 2, sets trace_shadow)
        - elinikki_trace_flowers.py (chapter 3, sets trace_flowers)
        - elinikki_trace_weave.py (chapter 3, sets trace_weave)
      Dialogue lifted directly from story/chapters/chapter-0{1,2,3}.md section
      headers and condensed only where necessary to fit the conversation DSL.
      Replay suppression is deferred to the Phase 3 C# examine trigger — the
      drama itself does not gate on its own flag.
    * create_drama_excel.py: registered all 8 new scenarios in DRAMAS list,
      keeping them grouped by chapter with a note that echo stages live in 2.4.
  Verified: build.bat debug runs the full 3-step pipeline and generates 9 drama
  Excel files (intro + 8 new traces) with 0 warnings, 0 errors. Codex review
  skipped for the same reason as Task 2.1/2.2: drama scripts are narrative
  content and DramaKeys.g.cs is additive auto-generated output — no hand-
  written runtime logic changed.
- 2026-04-13: Task 2.2 complete. First drama scenario authored:
    * tools/drama/schema/key_spec.py rewritten with Elinikki-specific keys:
      TMP_INTRO_CAN_START (tmp flag), 7 stage_at_least resolve keys, and 3
      stage.advance command keys (accepted, yuu_found, ending_seen).
    * tools/drama/schema/generate_keys.py updated for Elinikki: flag prefix
      chitsii.elinikki., CS output path src/Quest/Drama/Generated, C# namespace
      Elin_Elinikki.Quest.DramaKeys.
    * generate_keys.py --write regenerated both data_generated.py and
      src/Quest/Drama/Generated/DramaKeys.g.cs with the Elinikki constants.
    * tools/drama/scenarios/elinikki_quest_intro.py created with the
      chapter-0 intro drama matching story/chapters/chapter-00.md: Mina
      arrives, explains the job, Sora joins, quest accepted. Ends by
      calling cmd.elinikki.stage.advance.accepted via resolve_run, which
      flips quest.stage from NotStarted to Accepted through the drama
      resolver from Task 1.4.
    * create_drama_excel.py DRAMAS list now includes (QUEST_INTRO,
      define_elinikki_quest_intro). python tools/drama/create_drama_excel.py
      produces LangMod/EN/Dialog/Drama/drama_elinikki_quest_intro.xlsx
      with sheet "elinikki_quest_intro".
    * ElinikkiQuestFlow.IntroDramaAvailable phase gate removed. The
      gate constant and its unreachable-code pragma are deleted;
      TryStartIntroQuest now calls the drama runtime unconditionally
      once the fame/home/UI checks pass.
  Verified: build.bat debug runs end-to-end (drama gen + dotnet build +
  deploy) with 0 warnings, 0 errors.
