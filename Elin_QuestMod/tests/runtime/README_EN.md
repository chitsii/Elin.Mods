# Runtime Test (Quest Mod Skeleton)

This document describes the runtime test specification for Quest Mod Skeleton.  
The shared runner under the repository root, `runtime-test-v2/`, is used.

## 1. Prerequisites

- The game is running and a runtime-test save is loaded.
- CWL named pipe (`Elin\Console`) is enabled.
- The player name contains `RUNTIME_TEST` (default save guard).
- The mod DLL has been updated with `build.bat`.

## 2. Commands

```powershell
# Default: smoke suite with Tag=smoke
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1
```

```powershell
# smoke: single case
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId <case_id>
```

```powershell
# smoke: run by tag
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -Tag critical
```

```powershell
# smoke: active compatibility check (excluded from normal smoke by default)
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId questmod.patch.compat.cwl_incompatible_scan
```

```powershell
# debug: showcase console command verification (DEBUG build)
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite smoke -CaseId questmod.debug.console.showcase_command
```

```powershell
# drama: run all cases (must pass)
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama
```

```powershell
# drama: run a single case (CaseId is DramaId)
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId <drama_id>
```

```powershell
# drama: run feature showcase only
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId quest_drama_feature_showcase
```

```powershell
# drama: run feature showcase follow-up only
powershell -ExecutionPolicy Bypass -File .\tests\runtime\run.ps1 -Suite drama -CaseId quest_drama_feature_followup
```

## 3. smoke/drama Selection Rules

- `-Suite smoke`
  - Executes `IRuntimeCase` under `tests/runtime/src/cases`
  - `-Tag` matches case `Tags`
  - `-CaseId` filters by exact `IRuntimeCase.Id`
  - If neither `-Tag` nor `-CaseId` is specified, `Tag=smoke` is applied automatically
- `-Suite drama`
  - Executes `IDramaCaseProvider.BuildCases()` under `tests/runtime/src/drama`
  - `-CaseId` filters by exact `DramaCaseDefinition.DramaId`
  - `-Tag` accepts only empty or `drama` (other tags may result in `no_cases_selected`)

## 4. Drama Test Contract

Implement `IDramaCaseProvider` in `tests/runtime/src/drama/QuestDramaCatalog.cs` and return `DramaCaseDefinition`.

- `DramaId`
  - Target drama ID (lookup key for `-CaseId`)
- `TimeoutSeconds`
  - Per-case max runtime in seconds
- `MaxBranchRuns`
  - Max branch exploration iterations
- `MaxChoiceStepsPerRun`
  - Max choice steps handled in one iteration
- `MaxQueuedPlans`
  - Upper bound for queued branch plans (runaway protection)
- `TargetCoverageRatio`
  - Target branch coverage ratio (runner may stop early once reached)
- `RequiredNpcIds`
  - NPC IDs to prepare near the player before case execution
- `SetupFlags`
  - `dialogFlags` key/value pairs applied before execution

The template default uses `quest_drama_replace_me`; replace it with your actual drama ID for real use.

## 5. Execution Lifecycle (drama)

1. Record the baseline save ID at suite start
2. Reload baseline save before each case
3. Apply `SetupFlags` / `RequiredNpcIds`
4. Run drama and explore branches
5. Clean up case-specific setup
6. Reload baseline save after suite end

Because of this, the drama suite is designed to minimize cross-case side effects.

## 6. Operational Usage (drama)

- Normal release gate:
  - `-Suite drama`
  - Runs catalog-registered cases (placeholder + feature showcase + followup)
- Investigation/debug:
  - `-Suite drama -CaseId <drama_id>`
  - Reproduce a specific problematic case in isolation

## 7. Output Files and Logs

- Output directory: `tests/runtime/_artifacts/<run_id>/`
- Main files:
  - `result.json`: execution result (`suite/status/summary/cases`)
  - `playerlog.diff.log`: appended lines in `Player.log` during this run
  - `playerlog.diff.meta.json`: metadata for log diff extraction
- Only when `-KeepGeneratedSource` is specified:
  - Saves `runtime_suite_v2_generated.csx` into artifacts

## 8. Common Failure Reasons

- `save_guard_rejected`
  - Save name does not satisfy `RequiredNameContains` (default: `RUNTIME_TEST`)
- `no_cases_selected`
  - `-CaseId`/`-Tag` filtering selected zero targets
- `case_failed`
  - Individual case failure (check `cases[*].reason` in `result.json`)
- `cleanup_reload_failed`
  - Baseline reload failed after drama suite completion

## 9. Implemented smoke Cases

- `runtime.boot.pc_available`
- `questmod.plugin.contract.guid_and_package`
- `questmod.patch.targets.zone_activate_postfix`
- `questmod.compat.point_wrapper_default_allow_installed`
- `questmod.quest_flow.bootstrap_defaults`
- `questmod.quest_state.lifecycle_roundtrip`
- `questmod.quest_state.dispatch_priority`
- `questmod.drama.catalog.placeholder_contract`
- `questmod.drama.feature_showcase.activate_smoke`
- `questmod.drama.feature_showcase_followup.activate_smoke`
- `questmod.debug.console.showcase_command` (manual verification case for DEBUG builds)
- `questmod.patch.compat.cwl_incompatible_scan` (active check, outside default smoke)
