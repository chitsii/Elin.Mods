# PR8 SleepOffer native runtime integration

Implementation only. No game, CWL pipe, deploy, save/load or Steam operation is performed by offline verification. Runtime task `01a105f7-e910-734f-8fdd-0098916300dd` exclusively owns execution. Use a backed-up disposable `RUNTIME_TEST` save and reload its baseline after each case. The name guard is not a backup.

## Cases

| Case ID (`pr8.sleep.` prefix) | Native operation and required result |
| --- | --- |
| `consume_water_reject` | Actual OfferLogic.Process -> CanOffer(actor,Card)/OnOffer/_OnOffer; Ehekatl fish and meat consumed, uncapped piety changes, 37 water retained/blessed with one call/no Split, element-764 control retained with no OnOffer. |
| `split_nonconsume_stop` | Native altar first confirms Eyth non-consuming branch. Product then splits a bounded meat stack; total quantity and box/root ownership conserved, detached batch returned/merged, later sentinel not offered. |
| `nonconsume_unsplit_stop` | Single native Eyth offering is not split, retained, and stops later sentinel. |
| `stop_on_actor_dead` | After first real consuming OnOffer, fixture-only postfix sets isDead. No later call; first consumed amount is accounted for; remaining items and box metadata retained. This tests the death-state guard, not natural PC Die. |
| `stop_on_pc_change` | After first real OnOffer, fixture Player.chara changes to another generated native Chara; remaining amount conserved and no later offering. |
| `stop_on_faith_change` | After first real OnOffer, fixture actor changes faith through native SetFaith; no later offering and correct quantity accounting. |
| `exception_split_recovery` | Native Split, then fixture-only OnOffer prefix throws. Full quantity/ownership and metadata restored; removing injection permits a real consuming retry. Fault injection, not proof of a natural native exception. |
| `sleep_completion` | Real AddCondition<ConSleep> -> Kill -> OnRemoved with loaded product postfix. Unslept/dead/NPC controls cause zero offerings; completed PC removal consumes fish. `slept` is fixture state: timed bed sleep/UI is a separate operator smoke. |
| `source_craft_coldboot` | Post-coldboot rows/maps/cards/RecipeManager consistency, separate array identities and inherited array values; initialized Init is native no-op only. Actual Recipe.Create/Craft with generated ingredients, crafted ID/container/deity/name metadata. No source reload/reinitialization. |
| `existing_box_reload_prepare` | Generate tagged old-metadata box/water/reject controls, real Process preserves metadata, write UID manifest. Successful preparation intentionally retains these three owned objects for operator save/reload. This phase alone is not integration reload coverage. |
| `existing_box_reload_verify` | Require another native Game instance and same save/PC; reacquire persisted UIDs, assert metadata/owner/root/slots/quantities before and after real Process, reclaim only owned fixtures. |

The first nine cases clone the native Player using IO.DeepCopy and temporarily select generated native Charas synchronously. No frames or nested IEnumerator run while the fixture Player is active. Conditions, piety, dream/recipe state and karma act on the fixture Player/actor. Normal inventory is only read for before/after assertions. Every allocation/mutation is after RegisterRollback. Cleanup restores the original Player first, removes only tracked fixture Things/Charas, removes only the unique test Harmony owner, restores Unity RNG, and asserts original native Player/PC serialized state plus inventory quantities, ownership, root, position, slots and metadata. Cleanup failure is a failed result: stop the run and restore the disposable baseline. Shared world counters, messages, FX and other-mod callbacks are not claimed to be fully rolled back; baseline reload is mandatory.

Observer patches do not skip or replace native processing and do not change arguments/results. They count actual native calls and track generated/split UIDs. Actor changes and exception injection are separately tagged `fault_injection`. Missing source/faith/category, unsuitable split batch or unfulfilled native preconditions fail; they are not passing existence checks. Postboot source registration is explicitly `postboot_contract` coverage; it does not prove the boot prefix timing.

## Offline checks

Product DLL must be the PR8 compile-only DLL, never an older Workshop binary. Default is `Elin_AutoOfferingAlter/_bin/Elin_AutoOfferingAlter.dll`; pass `-SleepOfferAssembly` to choose the verified build. Managed references are the real `elin_link/Elin_Data/Managed` DLLs.

```powershell
& .\Elin_AutoOfferingAlter\tests\runtime\verify-offline.ps1 `
  -OutputRoot .\.codex-build\Elin_AutoOfferingAlter\runtime-offline
```

This compiles native runtime source, generates the normal shared-runner csx, and compiles a mechanical adapter retaining the exact using-stripped code/boot statements. It checks all 11 case IDs and RUNTIME_TEST guards. It does not execute script code or certify CWL compilation/transport.

Every reload AssertRecord (before Process, after Process, and Verify) requires exactly two children, both saved UIDs, and both saved quantities. Offline execution uses that same test-only helper: intact/reversed contents pass; water/rejected/both disappearance, extra content, either UID replacement, duplicate UID, and either quantity change must throw. These native-free counterexamples verify the assertion itself, not game behavior.

## Operator protocol

Run exactly one CaseId through `tests/runtime/run.ps1`; do not run the entire suite unfiltered. The wrapper forces `RUNTIME_TEST` and requires CaseId. Record game build/channel, product/dependency hashes, loaded assembly identity, Harmony owner, case/result/cleanup logs and fixture before/after UID records. Other automation may execute from native sleep callbacks; inventory/Player restoration assertions detect leakage, and the disposable baseline must be restored regardless.

1. Cold restart with the PR8 product DLL loaded before SourceManager.Init. Run `source_craft_coldboot`. To prove boot timing or unchanged original chest6 before injection, runtime owner needs a preboot observer/source baseline in a separate session; no test changes shared sources to manufacture that evidence. Postboot current chest6 and cloned arrays are checked here.
2. Run ordinary offering, Eyth conservation, sleep removal, then fault-injection cases independently, restoring baseline after each. Actual bed/UI sleep and natural penalty death are not covered by setting `slept`/`isDead`; retain separate manifest fields for those smokes.
3. From a disposable baseline, run `existing_box_reload_prepare`. Its owned box carries the old `c_idDeity=tishi.offering_box` and unique old name. The operator alone saves this disposable save, then reloads (or cold restarts and reloads it). Run `existing_box_reload_verify` in the fresh Game. No manual source injection/recipe registration/metadata repair is allowed between phases. Verify checks UIDs and rejects foreign box content before registering deletion. Afterwards restore the pre-prepare baseline; do not save fixture cleanup to any normal save.
4. A failed prepare removes its owned artifacts. A verify rejected before recognizing ownership leaves the save/manifest untouched for diagnosis. A recognized verify cleans up on success/failure/exception. If either phase or cleanup fails, stop and restore baseline; do not continue other cases. The fixed manifest path prevents overlapping staged reload fixtures.

## Remaining checks / product finding

Actual DLL shows `Recipe.Create` selects RecipeCard for item recipes and `RecipeCard.Craft` overrides the base method without calling base Craft. Product PatchRecipe currently targets only `Recipe.Craft`. Thus its new-box c_altName postfix may not run on the actual item-craft path; the craft case deliberately asserts that field and does not manually call the postfix. Source naming may still make the display name look correct, so record the actual field and runtime recipe type. Minimal product proposal for the parent: target the real RecipeCard.Craft path (with exact signature) if the intended metadata contract is required. Product sources are unchanged by these tests.

Native penalty/artifact reforge, natural PC death, elapsed bed/UI sleep, early boot timing, full-world restoration and game/CWL execution remain separate coverage. The cases do not claim these are verified. Tests/runtime is outside the product csproj Compile glob and every case/helper has RUNTIME_TEST guards.
