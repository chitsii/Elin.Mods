# Elinikki Quest Implementation Loop Prompt

## Overview

This file contains the prompt used to drive the implementation loop for the "帰らなかった遠足" quest scenario. The loop executes this prompt repeatedly until all phases are complete.

## Loop Usage

```
/loop docs/plans/2026-04-13-elinikki-quest-implementation-loop.md
```

---

## Prompt

You are implementing the "帰らなかった遠足" quest scenario for the Elin mod `Elin_Elinikki`. This is a loop-driven task: you will be invoked repeatedly until the implementation is complete.

### Before doing anything

1. **Read the story design** to understand what you are building:
   - `story/story.md` ── bible
   - `story/chapters/_index.md` ── flag design, chapter list, ending logic, drama trigger mapping
   - `story/chapters/chapter-00.md` through `chapter-05.md` ── chapter outlines with dialogue samples
   - `story/worldbuilding/locations/*.md` ── 6 locations (5 Nefia layers + entrance)
   - `story/characters/*.md` ── 4 character profiles
   - `story/plot/arcs/*.md` ── 3 arcs (main + sora + mina)
   - `.claude/plans/zesty-zooming-dusk.md` ── master plan with technical decisions

2. **Read the implementation progress tracker**:
   - `docs/plans/2026-04-13-elinikki-quest-implementation-progress.md`

   If the tracker does not exist, create it from the template at the end of this file and start at Phase 1 Task 1.

3. **Identify the next incomplete task** from the progress tracker. Work on exactly one task per loop iteration, OR a small batch of tightly related tasks if they would naturally commit together.

### Rules

- **Do NOT rewrite the story design.** Treat `story/` as the authoritative spec. If you find a contradiction, stop and report it; do not silently resolve.
- **Fail-loud over silent fallback.** If a subsystem is broken, log and throw — do not paper over with a stub.
- **Every task must end with a commit** describing what was done. Follow the global Codex review rule before committing (skip for docs-only changes).
- **Update the progress tracker** before committing: mark the task complete, note any deviations or follow-ups.
- **If you get stuck**, write your findings to the progress tracker under "Blockers" and stop. Do not guess.
- **Follow AGENTS.md** in `C:\Users\tishi\programming\elin_modding\Elin.Mods\Elin_Elinikki\AGENTS.md` for coding conventions and Harmony patterns.
- **Reuse existing infrastructure**:
  - `Elin_QuestMod` is the quest template. Copy from it, do not rewrite.
  - `SharedWorldObjectManager` is already implemented — use it, do not replace.
  - Drama DSL is `Elin.DramaDsl` (git submodule).
- **Verification**: run `build.bat debug` after each code change. Run the Elin game only when the user asks or when integration testing is unavoidable.

### Technical decisions (already settled)

- Mod base: copy `Elin_QuestMod` src/ into `Elin_Elinikki/src/Quest/`, rename namespace, set ModGuid to `chitsii.elinikki.quest`
- Flag prefix: `chitsii.elinikki.quest.*`
- Maps: create with Elin devmode (manual step — skip in loop, mark as "needs user action")
- Custom visual objects: `SharedWorldObjectManager` with `Visibility = FpsView`, textures 512x512 photorealistic
- Companions: drama dialogue only, no NPC rendering in FPS
- Quest start: `Patch_Zone_Activate_QuestPulse` checks fame ≥ 5000 on home zone entry, then triggers intro drama
- BGM/SE: Elin existing assets first, drama commands

### Implementation phases

Work through these phases in order. Each phase has discrete tasks in the progress tracker.

**Phase 1: Quest infrastructure foundation**
1. Copy Elin_QuestMod src/ into Elin_Elinikki/src/Quest/, rename namespaces
2. Update csproj, package.xml, Plugin.cs with Elinikki ModGuid
3. Define `ElinikkiQuestStage` enum (not_started through ending_seen, 8 states)
4. Rewrite `QuestFlow.cs` as `ElinikkiQuestFlow` with stage transitions matching chapters/_index.md
5. Implement fame-5000 gate in `Patch_Zone_Activate_QuestPulse`
6. Verify build with `build.bat debug`

**Phase 2: Drama scripts**
1. Set up Elin.DramaDsl submodule reference in csproj
2. Write drama script for chapter-00 (依頼 intro at home zone)
3. Write drama scripts for each trace examine event (chapter-01, 02, 03)
4. Write drama scripts for echo experiment stages 1-4 (chapter-02)
5. Write drama script for chapter-04 reunion + 8 truth questions
6. Write drama scripts for chapter-05 return journey (4 stages) + 3 endings
7. Verify drama compilation

**Phase 3: SharedWorldObject zone-aware placement**
1. Add zone transition hook for object set swapping
2. Define object placement data per layer in code (position, rotation, scale per trace)
3. Implement `RemoveDefinitionsByPrefix` + `Upsert` pipeline on zone change
4. Add fog/color/LUT override per zone (read from story location files)
5. Create placeholder textures (solid color) for all 6 traces
6. Verify visual output in FPS view

**Phase 4: Audio integration**
1. Add drama commands for BGM change and SE play
2. Map each layer to an Elin existing BGM asset (document choices in progress tracker)
3. Map echo SE trigger points (4 stages)
4. Verify audio in-game

**Phase 5: Custom map data (user-driven)**
1. Write documentation for the 6 maps to be created in devmode
2. Add zone registration stubs that will bind to the devmode maps
3. **Mark as "needs user action"** and pause this phase

**Phase 6: End-to-end verification**
1. Full playthrough of chapters 0 → 5 using debug console to skip devmode maps if needed
2. Verify all 21 flags behave correctly
3. Verify truth flag counting and ending resolution (return, silence, revisit)
4. Report any deviations or polish items to progress tracker

**Phase 7: Polish**
1. Produce final textures (still 512x512 photorealistic)
2. Tune fog/LUT values per layer
3. Adjust map sizes if walk times feel wrong
4. Final Codex review before merging to main

### What "complete" means

The loop is done when:
- All Phase 1-4 and Phase 6 tasks are marked complete in the progress tracker
- Phase 5 is in "needs user action" state (maps pending)
- `build.bat release` succeeds
- Progress tracker "Blockers" section is empty or only contains items needing user input

Report completion by writing `LOOP COMPLETE` at the top of the progress tracker.

### Progress tracker template

If `docs/plans/2026-04-13-elinikki-quest-implementation-progress.md` does not exist, create it with this template:

```markdown
# Elinikki Quest Implementation Progress

Status: IN PROGRESS

## Current Phase
Phase 1: Quest infrastructure foundation

## Phases

### Phase 1: Quest infrastructure foundation
- [ ] Task 1.1: Copy Elin_QuestMod src/ into Elin_Elinikki/src/Quest/
- [ ] Task 1.2: Update csproj, package.xml, Plugin.cs
- [ ] Task 1.3: Define ElinikkiQuestStage enum
- [ ] Task 1.4: Rewrite QuestFlow as ElinikkiQuestFlow
- [ ] Task 1.5: Fame-5000 gate in Patch_Zone_Activate_QuestPulse
- [ ] Task 1.6: Verify build.bat debug

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
(none yet)

## Notes
(loop iteration notes go here)
```

### What you do each iteration

1. Read the progress tracker.
2. If `LOOP COMPLETE` is at the top, exit immediately with the message "Implementation loop complete".
3. If Phase 5 is the only remaining phase and it is in "needs user action" state, exit with "Paused: awaiting user to create devmode maps".
4. Otherwise, pick the next unchecked task in the current phase.
5. Do the work for that task (and closely related follow-ups if they naturally commit together).
6. Update the progress tracker: mark the task complete, note deviations.
7. Run `build.bat debug` if code changed.
8. Commit with a message describing the task.
9. Exit. The loop will re-invoke you for the next task.

**Do not try to complete the entire loop in one iteration.** Pick one task, do it well, commit, exit.
