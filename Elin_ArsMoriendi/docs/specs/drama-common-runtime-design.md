# Drama Common Runtime Design (Draft)

## Goal
- Make drama scripting reusable across mods.
- Keep scenario files readable by hiding inline C# as much as possible.
- Migrate safely with tests first.

## Target Architecture
- `Scenario layer`:
  - Uses high-level builder APIs only.
  - No direct `builder.eval(...)` / `builder.cs_eval(...)` in scenario files.
- `Builder layer`:
  - Owns all `eval` emission.
  - Exposes stable methods (ex: `play_bgm`, `cs_call_common_runtime`).
- `C# runtime layer`:
  - Stable facade class for drama-side calls.
  - Default name: `Elin_CommonDrama.DramaRuntime`.
  - Mod-specific logic is delegated behind this facade.
  - Core entry points: `ResolveFlag(key, flag)` / `ResolveRun(key)`.

## API Boundary
- Preferred scenario API:
  - `builder.play_bgm(...)`
  - `builder.play_bgm_with_fallback(...)`
  - `builder.resolve_flag(dep_key, flag_key)`
  - `builder.resolve_run(dep_key)`
  - `builder.quest_check(condition_key, target_flag_key)`
  - `builder.quest_try_start(drama_id)`
  - `builder.quest_try_start_repeatable(drama_id)`
  - `builder.quest_try_start_until_complete(drama_id)`
  - `builder.cs_call_common_runtime("MethodName", args...)` (fallback)
- Transitional allowance:
  - Existing direct `eval/cs_eval` is allowed only in known files until migration completes.
  - New files must not add direct `eval/cs_eval`.

## Migration Phases
1. Freeze current raw `eval/cs_eval` usage with tests.
2. Introduce common runtime call API in builder.
3. Migrate high-risk scenarios first (`ars_hecatia_talk`, `ars_apotheosis`).
4. Tighten test policy to disallow raw `eval/cs_eval` in all scenarios.

## Dependency Keys (ArsMoriendi)
- Single source:
  - `tools/drama/schema/key_spec.py`
- Generated interfaces:
  - Python: `tools/drama/data_generated.py`
  - C#: `src/Drama/Generated/DramaKeys.g.cs`
- Python constant groups:
  - `FlagKeys`: save-persisted `dialogFlags` keys
  - `ResolveKeys`: bool resolver keys
  - `CommandKeys`: command execution keys
  - `CueKeys`: named cue execution keys
- Naming convention:
  - Resolve keys must start with `state.`
  - Command keys must start with `cmd.`
  - Cue keys must start with `cue.`
  - Quest start condition keys: `state.quest.can_start.*`
  - Quest start command keys: `cmd.quest.try_start.*`
  - Quest repeatable start command keys: `cmd.quest.try_start_repeatable.*`
  - Quest until-complete start command keys: `cmd.quest.try_start_until_complete.*`
- Bool resolve:
  - `state.quest.is_complete`
  - `state.erenos.is_borrowed`
- Commands:
  - `cmd.erenos.ensure_near_player`
  - `cmd.erenos.borrow`
  - `cmd.scene.stop_bgm`
  - `cue.apotheosis.silence`
  - `cue.apotheosis.darkwomb`
  - `cue.apotheosis.curse_burst`
  - `cue.apotheosis.revive`
  - `cue.apotheosis.mutation`
  - `cue.apotheosis.teleport_rebirth`
  - Generic form: `fx.pc.<effect>` / `fx.pc.<effect>+sfx.pc.<sound>`

## Test Strategy
- Contract test:
  - `cs_call_common_runtime` emits correct `eval` entry.
- Policy test:
  - Detect direct `eval/cs_eval` in scenario files via AST.
  - Enforce current baseline counts to prevent spread before migration.
- Key contract test:
  - Ensure Python constants (`ResolveKeys` / `CommandKeys` / `CueKeys`) are implemented in C# resolver switches.
- Generation check:
  - `python tools/drama/schema/generate_keys.py --check`
  - Fails if generated interfaces are out of date.
- Post-migration:
  - Change policy test from baseline check to strict zero.

## Authoring Flow (Recommended)
1. Edit `tools/drama/schema/key_spec.py`.
2. Run `python tools/drama/schema/generate_keys.py --write`.
3. Implement resolver behavior in `src/Drama/ArsDramaResolver.cs`.
4. Implement quest condition/start behavior in quest resolver/executor.
5. Use generated keys from scenario files via `resolve_flag` / `resolve_run` / `quest_check` / `quest_try_start`.
6. Run tests and regenerate drama Excel.

## Build Modes
- Default build:
  - `build.bat` runs `generate_keys.py --check` and fails on drift.
- Explicit regenerate:
  - `build.bat regen` runs `generate_keys.py --write` before validation/tests.

## Quest Bridge Policy
- No runtime registration for quest rules/keys.
- Quest-related keys are fixed at build time and validated in CI/build.
- Scenario files must call runtime facade APIs; direct eval strings for quest start checks are disallowed.
