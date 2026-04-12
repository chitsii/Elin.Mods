# Elinikki Quest Implementation Progress

Status: IN PROGRESS

## Current Phase
Phase 2: Drama scripts (Phase 1 complete)

## Phases

### Phase 1: Quest infrastructure foundation
- [x] Task 1.1: Copy Elin_QuestMod src/ into Elin_Elinikki/src/Quest/
- [x] Task 1.2: Update csproj, package.xml, Plugin.cs
- [x] Task 1.3: Define ElinikkiQuestStage enum
- [x] Task 1.4: Rewrite QuestFlow as ElinikkiQuestFlow
- [x] Task 1.5: Fame-5000 gate in Patch_Zone_Activate_QuestPulse
- [x] Task 1.6: Verify build.bat debug

### Phase 2: Drama scripts
- [ ] Task 2.1: DramaDsl submodule reference
- [ ] Task 2.2: Chapter-00 drama
- [ ] Task 2.3: Trace examine dramas (chapters 1-3)
- [ ] Task 2.4: Echo experiment dramas (4 stages)
- [ ] Task 2.5: Chapter-04 reunion + truth dramas
- [ ] Task 2.6: Chapter-05 return + endings
- [ ] Task 2.7: Verify drama compilation

### Phase 3: SharedWorldObject zone-aware placement
- [ ] Task 3.1: Zone transition hook
- [ ] Task 3.2: Object placement data per layer
- [ ] Task 3.3: RemoveDefinitionsByPrefix + Upsert pipeline
- [ ] Task 3.4: Fog/color/LUT per zone
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
