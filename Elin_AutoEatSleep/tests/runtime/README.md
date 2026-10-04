# PR1 AutoEatSleep native integration cases

These are test-only runtime-test-v2 smoke-suite cases. The game operator owns deployment, CWL execution, the result manifest, and save reload. Compile/generation alone is not a runtime pass.

| Case ID (`pr1.autoeat.` prefix) | Actual operation and assertions |
| --- | --- |
| `config_default_preserved` | Real loaded ModConfig + BepInEx ConfigFile: absent key gives Hungry=3; saved explicit 1 remains 1 and file bytes remain unchanged. |
| `reentry_consumption` | Native hunger.Mod crosses the Hungry boundary, reaching the installed Stats.OnChangePhase postfix, InstantEat and FoodEffect.Proc. Each of two outer triggers consumes one unit, changes nutrition/hunger, observes a native nested phase, and never exceeds depth 1. |
| `filters_threshold` | Below threshold, disabled, AI_Eat, important food, native-rejected rotten food, and food in an excluded container produce zero calls/consumption. A matching container provides one meal. |
| `reentry_stress` | Fixture-only observer calls CheckAutoEat once inside real FoodEffect.Proc. One meal and maximum native InstantEat depth 1. This is fault injection, distinct from natural phase feedback. |
| `exception_guard_release` | Fixture-only FoodEffect prefix throws once. Native InstantEat finalizer observes the exception, quantity stays unchanged, then a normal native retry consumes one unit. Product catch/guard behavior remains active. |
| `anorexia_native_effects` | Healthy cooked meal changes strength nutrition. Real ConAnorexia + FoodEffect invokes one Vomit, reduces nutrition relative to the healthy control, triggers native phase feedback, and consumes one unit. Generated vomit UID is tracked for cleanup. |
| `sleep_native_resume` | Native AddCondition on NPC fixture, then PC-fixture Condition.Kill -> ConSleep.OnRemoved -> installed product postfix restores captured AI and clears saved AI. Disabled-resume control keeps idle AI. |

## Fixture contract

- Both the shared runner and the local fixture require the original PC name to contain `RUNTIME_TEST`; an active loaded plugin/map/renderer and its installed postfixes are mandatory. Missing prerequisites fail instead of passing.
- Use a backed-up, disposable dedicated save/zone. Run one CaseId at a time; stop and reload on failed restoration or cleanup. A sticky local guard rejects subsequent cases in the same compiled script after a restore/cleanup failure; use a fresh script/session after reloading. Do not run alongside another suite, save/load, world transition, or pending next-frame initialization.
- The fixture creates a non-global Chara, a small native prepared-food stack, an important-food control, and (when needed) a container. Source-created inventory and meal effect traits are controlled only on newly created fixtures. No preexisting item is moved/deleted.
- Setup disables auto-sleep in a separate ConfigFile with SaveOnConfigSet=false. Product MyConfig is replaced only during the synchronous operation; the original live settings and saved cfg bytes are checked. Existing saved thresholds are never forced to 3.
- Each operation temporarily uses a new Player pointing at the dedicated fixture Chara. Its queues, notices, stats, disease, nutrition and AI do not belong to the original Player. Native AddCondition is performed while the fixture is NPC, avoiding PC EndTurn during preparation. No frames or child IEnumerator are yielded while the Player/config references are replaced.
- finally restores Player, config, saved AI, the captured eating-guard value, Act.CC/TC/TP, shared Stats raw bindings and BaseStats.CC. Guard restoration happens only after the case's normal retry/assertions, so a stuck guard still fails the exception case. Assertions compare original PC stats, elements, conditions, inventory roots/parents/slots/quantities, party/carryover/global UIDs, other map Chara HP/stats/position/conditions/inventory, existing map items, world date and live config bytes. Phase observers read Stats.Hunger and BaseStats.CC directly rather than rebinding shared stats through a Chara getter.
- Rollback is registered before fixture creation. At known creation boundaries, the destruction gate records every descendant UID, exact reference, and parent; original map/global/carryover/inventory UIDs cannot be adopted. Explicit native fixture moves update only known records. Cleanup validates the complete graph before any destruction and again immediately before each Destroy. An unknown child, replaced reference/UID, or unexpected owner leaves cards intact and fails cleanup. It never adopts cards discovered at the end of a run. Configuration/static restoration and observer removal continue after cleanup failures. Condition.Kill requires the exact recorded ConSleep with fixture owner. Vomit ownership requires a fresh unparented Thing ID 731 passed by this fixture's native Vomit and then inserted into the current map.
- Runtime logs include fixture/food/container UIDs, quantities, roots, native invocation counts, nested phases, injection counts, restoration and cleanup results. Preserve the result JSON and Player.log plus game/build and loaded DLL hashes in the operator manifest.

## Offline verification

```powershell
& .\Elin_AutoEatSleep\tests\runtime\verify_source.ps1
```

The verifier uses the shared builder without editing it, strips only the generated CSX launch statements for library compilation, and compiles against the installed real Elin/BepInEx/Unity DLLs and installed net48 reference assemblies. It has no restore, deployment, pipe, or game-launch step. It then inspects seven concrete case IDs through Cecil metadata. A separate executable source-links production ModConfig and tests actual BepInEx binding/file preservation; a generated default=1 mutant must fail. Destruction-gate offline counterexamples prove refusal before the destruction callback for original/unknown children, owner changes, replaced references and changed UIDs. These are fixture-policy tests, not native Card.Destroy integration evidence. SDK home, TEMP and outputs stay under `.codex-build/Elin_AutoEatSleep/runtime-sourcecompile`. `-GameRoot`, `-ReferenceRoot`, `-OutputRoot`, and `-DotNet` are explicit overrides.

To generate a single runtime CSX, use the shared builder with `-Suite smoke -CaseId pr1.autoeat.reentry_consumption -RequiredNameContains RUNTIME_TEST`. Only runtime operator task `01a105f7-e910-734f-8fdd-0098916300dd` executes it. The offline verifier's `runtime-result-NOT-RUN.json` is a destination placeholder, not a result file.

## Coverage limits and candidates

- All game cases still require actual CWL compilation/execution and runtime cleanup evidence. Static native-reference compilation does not validate Harmony patch installation/injection order or active scene behavior.
- Sleep exercises real condition removal with `slept=false`, avoiding dream/recipe/night advancement. Completed overnight sleep, hot-item input, AutoAct child restoration and visible UI progression remain separate unexecuted checks.
- The current product searches loose root food even when UseContainerFilter=true; the excluded-container case covers unmatched nested containers. The config description says "Only search for food in specific containers", so loose root food is a specification/defect candidate to clarify, not a silently changed product expectation.
- Fixture generation advances native UID/RNG counters. Do not rewind UID allocation. Reload the disposable save after the suite to discard these and any scene/UI/native side effects beyond the explicit state assertions.
