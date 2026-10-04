# PR7 NiComment native lifecycle integration cases

These cases run through native `Chara.Die` and `Quest.Complete`, with the already-loaded NiComment prefix/postfix/finalizer. They never call the production gate directly, install a second product patch, create a stub overlay, or replace `FireBarrage`/`NiCommentAPI.Send`. All observations use one unique `runtime.pr7.ni.<token>` Harmony owner and exact signatures; rollback calls `UnpatchSelf` and asserts that owner is absent.

## Operator prerequisites

Use the existing `runtime-test-v2` smoke builder/runner and select one CaseId at a time. The case independently requires PC.Name containing `RUNTIME_TEST`, a live unmounted idle PC of level 2..50, noncriminal karma, enabled NiComment and its real initialized overlay. Never use an ordinary save. Back up the dedicated test save and reload it after every case, including successful cases, before running the next. Do not save generated fixtures; automatic/manual saves during the synchronous test must be disabled by the runtime operator. A cleanup failure blocks further PR7 cases in that submission and requires abandoning the run and reloading the dedicated save.

Disable LLM reactions before boot/loading the test save. Prepare requires `EnableLlm=false`, an absent or inactive provider, and no in-flight request. The cases do not alter config entries, provider state, credentials, external-send hooks, other automation, or ConfigFile.SaveOnConfigSet. If the guard rejects, the runtime operator must prepare/restart the isolated session; do not force a provider or overlay stub to pass.

The normal game's PC is never passed to `Die`. Death targets are direct results of `CharaGen.Create("bat", level)`, tagged with a unique test name and added to the active zone. PC is only the origin for eligible kills. `DeathSentence` takes the native final-death route without normal blood/random SpawnLoot; the faint control calls the ordinary native Die branch with host links between two generated NPCs. It proves the native mounted/faint branch, not the player-facing riding command.

## Cases and expected notifications

| CaseId | Native operation / rows | Expected FireBarrage calls |
|---|---|---|
| `pr7.ni.death_real` | Eligible enemy final death; already-dead retry; null origin; NPC origin; lower LV; mounted faint | `1,0,0,0,0,0` |
| `pr7.ni.quest_real` | Dedicated derived quest completion; already-complete repeat with native side effects still running | `1,0` |
| `pr7.ni.death_reentry_exception` | Same-instance reentry; throw at native body entry; normal retry after finalizer; fixture-only Harmony skip; normal retry | `1,0,1,0,1` (5 native body entries across rows, reentry enters twice) |
| `pr7.ni.quest_reentry_exception` | Same-instance reentry; different nested instances; OnBeforeComplete throw/retry; OnComplete throw after records/retry; fixture-only skip/retry | `1,2,0,1,0,1,0,1` |

Each `row:` log records fixture UID, actual finalized state, native body-entry count, barrage count, Send comment count, exception and finalizer observations. The production event ID and gold color are checked. One notification is one `FireBarrage`, which enqueues **3..5 separate comments** through the actual `Send(string, Color)` overload; comment count is not notification count. Nested quest notifications are also checked separately by instance identity. Observers retain arguments, native IL, return values and exceptions. The transpiler adds only a body-entry marker; the named stress cases use that marker or the fixture's native virtual hooks for one-shot reentry/throws. A test-only skip prefix, ordered after Ni's prefix, applies only to its exact fixture instance. These are tagged `fault_injection`, not natural gameplay coverage.

The reward-free `NiPr7Quest` is a real native `Quest` subtype with real source metadata and current ClientZone. It overrides presentation/reward/affinity and virtual callbacks only; native Complete performs Remove, completedIDs/completedTypes/client-zone records, karma and `isComplete=true`. Repeated native Complete deliberately repeats callbacks/karma even though Ni sends no second notification. Tests do not claim Ni suppresses vanilla quest side effects. A late callback exception verifies `isComplete=false` despite already-written completion records, then verifies a successful retry produces one notification.

## Restoration and ownership

Rollback is registered before fixture additions/patching and runs in reverse order: remove/verify observer owner, remove only generated quests/NPCs/items, restore original karma and Unity random state, assert baseline. The snapshot records original world/PC identities, map/dead/global/carryover membership, every existing Chara's HP/dead/destroyed/LV/host/ride/conditions/inventory, existing quests/task state and membership, quest completion sets, current zone completion set, PC party and existing item quantity/parent/descendants. Unknown changes fail the invariant and require save reload; they are not overwritten to manufacture a pass.

At NPC generation, all descendant item references and UIDs are captured. Immediately before recursive native Destroy, every child must still be an exact captured reference/UID; every captured item must have an exclusively owned root (its generated NPC, known generated containers, original zone, or detached). Unknown children, changed UID/reference, external ownership, held objects, global/faction ownership or a replaced world cause cleanup failure without Destroy of that contaminated fixture. Newly discovered items are never adopted as owned. Initial descendants that were dropped remain exact owned instances and are removed after the NPC. Existing actors/items/conditions are never explicitly removed or killed by the test.

Synchronous rollback is not a complete game-save restore: UID allocation, non-Unity RNG, native UI/message/audio state and arbitrary other-Mod callbacks are not fully reversible. **Reload the dedicated save after every case.** No save/load, game launch, deployment, Steam copy or shared-runtime edit is performed by this implementation.

## Offline verification

`compile-offline.ps1` generates the csx using the unmodified shared builder, compiles a library wrapper against existing net472/game/Harmony references and the specified NiComment DLL, then emits the **unmodified csx** as a Roslyn script submission without evaluating it. It also executes seven ownership graph checks using native Card/Thing/Chara types allocated without constructors or Unity: known descendants, unknown grandchild, same UID/different reference, same reference/changed UID, external actor, unknown container, cyclic owner chain. These checks validate cleanup guards only, not gameplay. Output/TEMP should be isolated outside the repo and Steam. Existing 15 NiComment unit tests remain separate.

```powershell
& .\compile-offline.ps1 -RepoRoot <isolated-worktree> -OutputRoot <isolated-output> -NiCommentDll <built-Elin_NiComment.dll>
```

## Uncovered / execution status

Source/real-DLL/script compilation and offline checks do not prove gameplay passes. All four gameplay cases remain NOT EXECUTED until the runtime operator captures result JSON, Player.log delta, loaded DLL identity, each row and successful cleanup in the dedicated save. Actual PC death/PcDeath/red color, ON/OFF toggles, not-ready overlay control, frame-delayed rendering/visible overlay, natural combat damage into Die, actual riding UI, save/reload behavior, natural reentry/exception sources and interactions with other-Mod postfixes are not covered here. SoulBind's new specification is not tested or changed; the existing VeryLow Ars postfix/default Ni ordering and possible notification before revival remain explicitly uncovered compatibility observations.

Offline script emission records CS1701 warnings for the existing Unity/Harmony/BepInEx mscorlib 2.0 references unified to net472 mscorlib 4.0. The library compile uses warnings-as-errors and passes. These reference-unification warnings and gameplay compatibility require the real runtime operator's verification.
