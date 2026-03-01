# Elin_QuestMod (Quest Mod Skeleton)

A minimal template for quest mods targeting Elin.
Designed to include "implementation resilient to stable / nightly diffs" and "in-game smoke test workflows" from the start.

## Overview

- Purpose
  - Provide a foundation for quest progression flag management
  - Provide a minimal Harmony patch structure
  - Provide standard runtime smoke test cases
- Core implementation
  - State management based on `EClass.player.dialogFlags` (key prefix defaults to `Plugin.ModGuid`)
  - Declarative progression via a forward-only `QuestStateMachine`
  - Periodic quest evaluation (`Pulse`) via a Postfix on `Zone.Activate`
  - Compat wrappers to absorb API changes at risk-prone call sites

## Architecture

### Runtime Flow

1. `Plugin.Awake()` runs `Harmony.PatchAll()`
2. `Patch_Zone_Activate_QuestPulse.Postfix()` calls `QuestFlow.Pulse()`
3. `QuestFlow.Pulse()` executes the following:
   - `EnsureBootstrap()` sets initial state once
   - `QuestStateMachine` advances phases (forward-only)
   - `QuestStateService.CheckQuestsForDispatch()` determines branching targets
4. Quest state is persisted in `dialogFlags`

### Directory Structure

```text
Elin_QuestMod/
  src/
    Plugin.cs
    ModLog.cs
    Compat/
      PointCompat.cs
    Patches/
      Patch_Zone_Activate_QuestPulse.cs
    Quest/
      QuestFlow.cs
      QuestStateService.cs
    CommonQuest/
      Conditions/
        QuestCondition.cs
      StateMachine/
        QuestStateMachine.cs
        QuestTriggeredStateMachine.cs
  tests/runtime/
    run.ps1
    README.md
    src/cases/
    src/drama/
  build.bat
  config.bat
  package.xml
```

### Key Classes

- `src/Plugin.cs`
  - Mod GUID definition and Harmony initialization
- `src/Quest/QuestStateService.cs`
  - Flag read/write
  - `StartQuest` / `CompleteQuest`
  - Dispatch logic
- `src/Quest/QuestFlow.cs`
  - Bootstrap
  - State machine definition for phase transitions
  - `Pulse` execution order
- `src/CommonQuest/StateMachine/*.cs`
  - Generic state machine (originally from Vile; rejects duplicate/backward transitions)
- `src/Patches/Patch_Zone_Activate_QuestPulse.cs`
  - Postfix on `Zone.Activate`
- `src/Compat/PointCompat.cs`
  - Absorbs 4/5-argument differences in `Point.GetNearestPoint`

## Usage

### Prerequisites

- Windows + Steam version of Elin
- .NET SDK (ability to run `dotnet build`)
- Correct Steam paths in `config.bat`

### `build.bat` Specification and Update Guide

#### Specification

`build.bat` runs in the following order:

1. `python tools/drama/schema/generate_keys.py --write`
2. `python -m unittest discover -s tools/drama/tests -t . -v`
3. `python tools/drama/create_drama_excel.py`
4. `dotnet build Elin_QuestMod.csproj -c <Debug/Release>`
5. Copy DLL / `package.xml` / `LangMod` to `elin_link\Package\<MOD_FOLDER>\` (also copy `Texture` / `Portrait` / `Sound` if present)
6. Copy DLL / `package.xml` / `LangMod` to `STEAM_PACKAGE_DIR` (actual game-side Steam directory; also copy `Texture` / `Portrait` / `Sound` if present)

Arguments (4 patterns):

- `build.bat` : `Debug` build
- `build.bat debug` : `Debug` build
- `build.bat release` : `Release` build
- `build.bat drama` : Drama generation + `LangMod` distribution (also deploy `Texture` / `Portrait` / `Sound` if present, no DLL rebuild)

Failure behavior:

- If drama key generation, drama tests, or drama Excel generation fails, exits immediately (exit code 1)
- If build fails, exits immediately (exit code 1)
- If Steam-side copy fails, exits as well (usually caused by DLL lock while game is running)

`build.bat drama` use case:
- Rapid iteration on drama scenario edits
- Redistributing `LangMod/EN/Dialog/Drama/*.xlsx` without updating the DLL (and optionally refreshing bundled `Texture` / `Portrait` / `Sound`)

#### Configuration File (`config.bat`)

`build.bat` reads `config.bat` and uses the following variables:

- `MOD_FOLDER` : Destination folder name (e.g., `Elin_QuestMod`)
- `MOD_DLL` : Output DLL name (e.g., `Elin_QuestMod.dll`)
- `STEAM_ELIN_DIR` : Elin root under Steam
- `STEAM_PACKAGE_DIR` : Actual copy destination (typically `STEAM_ELIN_DIR\Package\MOD_FOLDER`)

#### Update Guide (When Changing Mod Name or Destination)

1. Update `config.bat`
   - `MOD_FOLDER`, `MOD_DLL`, `STEAM_ELIN_DIR`
2. Update `package.xml` metadata
   - `<id>`, `<title>`, `<author>`, `<version>`
3. Update `ModGuid` in `src/Plugin.cs`
4. If needed, update `AssemblyName` / `RootNamespace` in `Elin_QuestMod.csproj`
5. Run `build.bat` and verify the copy destination and DLL name are correct

#### Updating `build.bat` Itself

1. Maintain the 6-step flow
   - drama key generate -> drama test -> drama excel generate -> build -> linked copy -> steam copy
2. Maintain `exit /b 1` on failure
3. Keep variables in `config.bat`; do not hardcode in `build.bat`
4. After changes, run both `build.bat` and `build.bat release` to verify
5. Update this README section at the same time

### Daily Development Flow

1. Build
   - `build.bat`
2. Launch game and load the target save (save name must contain "RUNTIME_TEST" to trigger tests)
3. Run smoke tests
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1`
4. Run drama smoke tests (must-pass cases)
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama`
5. Run critical-only checks
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical`

### Pre-Release Flow

1. `build.bat release`
2. Complete `-Suite smoke`
3. Complete `-Suite drama` (placeholder + feature showcase + followup + branch_a + branch_b + merge)
4. Optionally run individual drama cases
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId quest_drama_feature_showcase`
5. Optionally run active compat checks
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId questmod.patch.compat.cwl_incompatible_scan`
   - Passing this case removes (INCOMPATIBLE) marks from methods.
   - Even if it fails, it may be a compatibility issue between LINQ and the checker; actual behavior may be fine. The game may become unstable during testing.

### Periodic Update Flow

The goal is to "maintain dual stable/nightly support and detect breaking API differences early."
Expected to run roughly once a month when a new stable release comes out.

Path convention:
- Run commands from `Elin_QuestMod/` (the directory containing this README)
- Use the shared tracker entrypoint at `..\tools\elin_channel_tracker\run.py`
- Keep QuestMod-specific tracker inputs/outputs under `.\tools\elin_channel_tracker\config\` and `.\tools\elin_channel_tracker\reports\`

#### 0. Initial Setup (One-Time)

1. Create two dedicated saves (separate from normal play)
   - `RUNTIME_TEST_STABLE`
   - `RUNTIME_TEST_NIGHTLY`
2. Align both saves to the same test conditions
   - Same map (town or field)
   - Same inventory/companion state where possible
3. Create tracker local config folders (if not already present)
   - `tools/elin_channel_tracker/config/`
   - `tools/elin_channel_tracker/reports/`
4. Prepare `tools/elin_channel_tracker/config/compat_targets.json`
   - For first-time setup, copy from another mod and minimize

#### 1. Stable Channel Verification

1. Switch to stable in Steam
   - Steam -> Elin -> Properties -> Betas
   - Select `None` (or the stable equivalent)
2. After update, build and deploy the mod without launching the game
   - `build.bat`
3. Launch game and load the stable save
4. Run smoke tests
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1`
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical`
5. Close the game and collect stable API signatures

```powershell
python ..\tools\elin_channel_tracker\run.py collect-signatures `
  --game-dir "C:\Program Files (x86)\Steam\steamapps\common\Elin" `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --output ".\tools\elin_channel_tracker\config\stable_signatures.json"
```

#### 2. Nightly Channel Verification

1. Switch to nightly in Steam
   - Steam -> Elin -> Properties -> Betas
   - Select `nightly` (or the nearest preview/beta branch)
2. After update, build and deploy the mod without launching the game
   - `build.bat`
3. Launch game and load the nightly save
4. Run smoke tests
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1`
   - `powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical`
5. Close the game and collect nightly API signatures

```powershell
python ..\tools\elin_channel_tracker\run.py collect-signatures `
  --game-dir "C:\Program Files (x86)\Steam\steamapps\common\Elin" `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --output ".\tools\elin_channel_tracker\config\nightly_signatures.json"
```

#### 3. Compatibility Report Generation and Analysis (Required)

1. Run verify-compat (broken entries cause exit code 1)

```powershell
python ..\tools\elin_channel_tracker\run.py verify-compat `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --stable-signatures ".\tools\elin_channel_tracker\config\stable_signatures.json" `
  --nightly-signatures ".\tools\elin_channel_tracker\config\nightly_signatures.json" `
  --report-json ".\tools\elin_channel_tracker\reports\compat_report.questmod.json" `
  --report-md ".\tools\elin_channel_tracker\reports\compat_report.questmod.md"
```

2. Run detect-target-gaps (audit `compat_targets.json`)
   - `--fail-on-missing` gates unregistered targets
   - `--fail-on-undetected` gates stale targets left in config

```powershell
python ..\tools\elin_channel_tracker\run.py detect-target-gaps `
  --targets ".\tools\elin_channel_tracker\config\compat_targets.json" `
  --src-root ".\src" `
  --include-heuristics `
  --fail-on-missing `
  --fail-on-undetected `
  --report-json ".\tools\elin_channel_tracker\reports\target_gap_report.questmod.json" `
  --report-md ".\tools\elin_channel_tracker\reports\target_gap_report.questmod.md"
```

3. Read `target_gap_report.questmod.md` and verify:
   - `missing_targets = 0`
   - `configured_not_detected = 0`
   - `check_kind_mismatches = 0`
4. How to read the generated reports (useful for auditing stale Compat entries)
   - `compat_report.questmod.md` / `.json`
   - Key fields:
     - `broken`: Must fix (`missing_symbol` / `signature_mismatch`)
     - `risky`: Needs review (`renamed_candidate` / `unresolved_dynamic`)
     - `ok`: Resolved successfully on both channels
   - `target_gap_report.questmod.md` / `.json`
   - Key fields:
     - `missing_targets`: Used in code but not registered in `compat_targets.json`
     - `configured_not_detected`: In config but not detected in code (stale Compat candidate)
     - `check_kind_mismatches`: Potential `check_kind` misconfiguration

#### 4. Fixes and Re-Verification (Required)

1. Classify the diff types in the report
   - Argument changes (added/removed/type change/made optional)
   - Method renames (old name removed, moved to new name)
2. For argument changes (prefer this pattern first)
   - List both old/new signatures in `compat_targets.json` (create a common subset for both channels)
   - Add a Compat wrapper with `MethodInfo` resolution: "new signature first -> old signature fallback"
   - Explicitly pass vanilla defaults for optional arguments (e.g., maintain `allowInstalled=true`)
   - Unify all call sites to go through the Compat wrapper instead of direct calls
3. For renames (two-stage resolution of old/new names)
   - In the Compat wrapper, try `GetMethod("newName", ...)` first, fall back to `GetMethod("oldName", ...)`
   - Register both old and new names in `compat_targets.json` to prevent detection gaps
   - Apply the same two-stage resolution to patch target methods (`TargetMethod`)
4. Re-verification after fixes (mandatory)
   - Run `build.bat` -> smoke on both stable and nightly
   - Re-run `verify-compat` and `detect-target-gaps`
   - Pass criteria: `broken=0` / `missing_targets=0` / `configured_not_detected=0` / `check_kind_mismatches=0`
5. Maintenance of stale Compat entries (removal)
   - Targets are entries in `target_gap_report.questmod.md` under `Configured but Not Detected`
   - First check references (if 0 hits, it's a removal candidate)
     - `rg -n "<Type.Member>" .\\src .\\tests`
   - Verify presence in stable/nightly signature catalogs (if absent from both, it's a removal candidate)
     - `rg -n "\"<Type.Member>\"" "<stable_signatures.json>" "<nightly_signatures.json>"`
   - Cleanup after rename migration
     - Confirm the target is `ok` with `reason_code=matched` in `compat_report.questmod.md`
     - Confirm old `candidate_names` / `candidate_signatures` are absent from both signature catalogs
     - If confirmed, remove the entry from `compat_targets.json` and the old fallback in the Compat wrapper
   - Removal conditions (general rule)
     - No code references
     - Absent from both channel signature catalogs
   - What to remove
     - Target entry in `compat_targets.json`
     - Old fallback branch in the Compat wrapper (old method name / old signature)
     - Old target patch resolution branch
   - Exceptions (do not remove)
     - Intentional dynamic calls that cannot be auto-detected (e.g., string concatenation)
     - In this case, add to `auto_detect_ignore_targets` in `compat_targets.json` with a reason

#### 5. When Things Fail

- `verify-compat` passes but runtime crashes
  - Do not ship as-is; prioritize fixing the runtime failure

## Creating Quests

### 1. Replace IDs First

- `ModGuid` in `src/Plugin.cs`
- `<id>`, `<title>`, `<author>`, `<version>` in `package.xml`

### 2. Design Quest State Keys

- Follow the key naming convention in `QuestStateService`
  - Current phase: `<mod_guid>.quest.current_phase`
  - Active: `<mod_guid>.quest.active.<quest_id>`
  - Done: `<mod_guid>.quest.done.<quest_id>`
  - `<mod_guid>` defaults to `Plugin.ModGuid` when unspecified

### 3. Define Initialization and Branching

- Edit `QuestFlow` to define:
  - `IntroQuestId`, `FollowupQuestId`, etc. (actual IDs)
  - Initial state in `EnsureBootstrap()`
  - Priority order in `DispatchQuestIdsCsv`

### 4. Add Progression Logic

- Call `QuestStateService` from event trigger points
  - Start: `StartQuest("quest_id")`
  - Complete: `CompleteQuest("quest_id", nextPhase)`
- Use `IsQuestActive` / `IsQuestCompleted` / `GetCurrentPhase` for conditional branching

### 5. Extend Triggers (Patches)

- Use `Patch_Zone_Activate_QuestPulse` as the starting point; add other patches as needed
- Recommended rules:
  - Explicitly declare `TargetMethod()`
  - Log exceptions and continue rather than swallowing them (fail-soft)
  - Prefer Postfix over Prefix when possible

### 6. Set Up Compat First

- Unify all risk-prone API calls through Compat helpers
- Example: `PointCompat.GetNearestPointCompat(...)`
  - Maintain the default `allowInstalled=true` to prevent behavioral regression

### 7. Write Runtime Tests First

- Add cases under `tests/runtime/src/cases`
- Maintain at minimum:
  - Plugin/package contract
  - Patch target contract
  - Quest flow bootstrap
  - Quest lifecycle and dispatch

## Runtime Test / Drama Test

See `tests/runtime/README.md` for detailed specifications.
Only operational highlights are listed here.

- Common execution requirements
  - Save name must contain `RUNTIME_TEST` (default save guard)
  - Run `build.bat` before launching the game to ensure the DLL is up to date
- Smoke (functional regression checks)
  - Default: `run.ps1` runs with `Suite=smoke` and `Tag=smoke`
  - Critical only: `-Suite smoke -Tag critical`
  - Single case: `-Suite smoke -CaseId <case_id>`
  - Active compat check: `-CaseId questmod.patch.compat.cwl_incompatible_scan` (outside normal smoke)
- Drama (drama branching checks)
  - `-Suite drama` runs `IDramaCaseProvider` implementations
  - `-CaseId` specifies a `DramaId`
  - Default catalog: `quest_drama_replace_me` / `quest_drama_feature_showcase` / `quest_drama_feature_followup` / `quest_drama_feature_branch_a` / `quest_drama_feature_branch_b` / `quest_drama_feature_merge`
  - Use `-CaseId` for individual execution as needed
  - `-Tag` accepts only empty or `drama` (other tags may result in 0 targets)
  - Replace the template `quest_drama_replace_me` with your actual ID
- Included minimal smoke tests
  - `questmod.drama.feature_showcase.activate_smoke`
  - `questmod.drama.feature_showcase_followup.activate_smoke`
  - Verifies generated xlsx existence + `LayerDrama.Activate` success

## Drama Builder (tools/drama)

`tools/drama` is the Excel generation tool for quest dramas.
The template ships with the following samples:

- `quest_drama_replace_me`: Minimal scenario
- `quest_drama_feature_showcase`: Conversational feature showcase (quest progression, branching, BGM/background/NPC effects)
- `quest_drama_feature_followup`: Transition drama from feature showcase route A

- Updating key definitions and regenerating
  - Edit definitions: `tools/drama/schema/key_spec.py`
  - Run generation: `python tools/drama/schema/generate_keys.py --write`
  - Generated files:
    - `tools/drama/data_generated.py`
    - `src/Drama/Generated/DramaKeys.g.cs`
- Generating drama Excel files
  - `python tools/drama/create_drama_excel.py`
  - Output:
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_replace_me.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_showcase.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_followup.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_branch_a.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_branch_b.xlsx`
    - `LangMod/EN/Dialog/Drama/drama_quest_drama_feature_merge.xlsx`
- Temporary flag operations (drama side)
  - `set_flag("<tmp_flag>", <value>)`
  - `mod_flag("<tmp_flag>", "+/-", <value>)`
  - Branch with `branch_if(...)` / `switch_on_flag(...)`
  - `switch_on_flag(..., fallback=...)` fallback is the default jump when no case matches (not fixed to `==0`)
  - See `tools/drama/scenarios/quest_drama_feature_showcase.py` for examples
  - Route A explicitly transitions to `quest_drama_feature_followup` rather than restarting the same drama
- CWL-only policy (default)
  - `DramaBuilder` prohibits `modInvoke`-dependent APIs by default (`mod_invoke` / `switch_flag`, etc.)
  - Use CWL/vanilla standard actions where possible (e.g., `start_quest` -> `startQuest`, `complete_quest` -> `completeQuest`)
  - Only use `DramaBuilder(allow_mod_invoke=True)` explicitly when `modInvoke` is required for compatibility
- DEBUG: Manual showcase launch
  - Console command: `questmod.debug.showcase`
  - Help: `questmod.debug.showcase.help`
  - Direct `cwl.cs.eval` calls:
    - `cwl.cs.eval Elin_QuestMod.DebugTools.QuestModDebugConsole.StartShowcase();`
    - `cwl.cs.eval Elin_QuestMod.DebugTools.QuestModDebugConsole.EvalShowcase();`
- After copying the template, always replace:
  - `yourname.elin_quest_mod.*` prefix
  - `quest_drama_replace_me` ID

Notes:
- `build.bat` automatically runs drama key generation, drama tests, and drama Excel generation.
- Steam-side copy will fail if the game is running due to DLL lock; close the game first.

## Common Issues

- `Elin.dll is locked`
  - The game is holding the DLL. Close the game and re-run `build.bat`
- Unresolved type (`CS0246`) during runtime
  - The mod DLL is likely not loaded. Rebuild + restart the game to verify
- `drama` suite fails
  - The placeholder ID (`quest_drama_replace_me`) is still in place

## Minimum Checklist

- [ ] `build.bat` passes
- [ ] `build.bat release` passes
- [ ] `smoke` passes
- [ ] `critical` passes
- [ ] `package.xml` ID matches `Plugin.ModGuid`
- [ ] Add corresponding test cases when adding new patches / compat wrappers
