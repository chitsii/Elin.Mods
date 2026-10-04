# PR9 Native Integration Cases

These are runtime-test-v2 cases, not unit-test stubs. They call the loaded
`RelocatorManager.GetMatches`, `RelocateSingleThing`, and `ExecuteRelocation`,
with native game objects and native inventory/body APIs. No production hooks,
preset writes, or automatic equipment removal are added.

## Cases

| ID | Expected evidence |
| --- | --- |
| `pr9.item.equipped_all_owners` | PC, generated human follower and generated animal party member; normal/cursed equipment excluded by preview and both move paths; native manual unequip reappears without clearing a demonstrably nonempty cache; unequipped transfer preserves quantity. |
| `pr9.item.live_cache_rule_owner` | Post-preview important/hardlock/hotbar/rule/scope/PC-to-party/PC-to-zone changes, destination lock/NPC ownership/destruction/replacement, source gift/NPC ownership/install/destruction, same destination: no native transfer. Moving a gifted item's destination out of PC ownership revokes eligibility. Profile disable hides preview and stops bulk; an independent fixture verifies that the direct single API preserves its existing ability to transfer with the profile disabled. |
| `pr9.item.execution_boundary` | Test-only ConditionText subclass first uses the real text predicate, then changes only generated fixture state on the second match call (after bulk collection). Important/hardlock/equipped/destination lock/destruction must prevent the subsequent native transfer. Tagged `fault_injection`. |
| `pr9.item.native_stack_capacity` | Single/bulk: normal quantity 11 preserved, double move calls native AddThing once; native split 13 into 8+5, true 1x1 full destination merges to 13 without adding a slot; incompatible cursed stack remains at source with quantity 7; merged source cannot execute twice. |

## Runtime Safety And Prerequisites

`RelocationProfile.Enabled` and `RelocationRule.Enabled` have different existing
contracts. Disabling the profile gates preview and bulk relocation; the direct
single-item API does not check that flag. The profile scenario verifies bulk
preservation first, then uses a separate generated item/container for the single
API contract. Disabling the actual rule still blocks both transfer paths. Later
scope/ownership/protection scenarios remain in the same run with fresh profiles.

Only the runtime operator runs these cases. Use a backed-up disposable save whose
PC name contains `RUNTIME_TEST`, a safe neutral zone, empty PC weapon slot, and an
empty existing user hotbar slot. `chest6`, `rock`, `sword`, `ring`, `amulet`,
`helmet`, `begger`, and `dog` source rows must exist. A missing prerequisite is a
failure, never a pass or silent skip. Human and animal fixtures are real global,
accompanying party members; no faction conversion or affinity policy change.

Rollback is registered before cache changes, observers, or fixture mutations.
Objects are immediately tracked by generated UID and use a GUID name marker;
bulk rules use the real ConditionText predicate to select only the named fixture.
Native AddThing observation is pass-through, scoped to the requested source UID
and destination, and uses a unique test Harmony owner. A separate safety assertion
throws before native transfer if an original item or descendant is selected into
a fixture destination. Destruction checks every descendant UID before calling
native Destroy; unknown contents cause a failure rather than being destroyed.
All explicit test destruction, including source/destination destruction during a
case and cleanup, uses the common `DestroyFixture` entry. It validates the tree
before native unequip and revalidates immediately before native Destroy.
Cleanup removes only
tracked generated objects, natively unequips only generated equipment, restores
the original profile/cache contents, recipe ingredient knowledge, and temporary hotbar entry, and asserts original
items' parent/root/quantity/slot/protection/ownership/inventory coordinates/bless,
PC equipment/HP/combat state, party identities/UID order, zone items, and global
character identities. Both explicit Cleanup and registered rollback are idempotent.
Cleanup failure makes subsequent PR9 fixture preparation refuse mutation. Stop
the run and reload the pre-run disposable save; do not continue or save it.

Unity random state is restored. The native UID allocator deliberately remains
monotonic to avoid collisions. Native messages/sounds/UI dirty caches are not
reverted. Rollback is not a complete-world backup: use the original save copy
after the run. No normal save, original inventory item, or original equipment is
destroyed or changed intentionally.

## Offline Validation

`validate.ps1` only builds the csx and emits it to memory using Roslyn; it never
loads/evaluates the script, connects a game pipe, starts a game, deploys, or saves.
It also executes six pure safety-gate checks against the same linked destruction
guard: unknown root/child/grandchild, null root, cyclic descendants, and an all-owned
control. Unknown-object injection must fail before any destructive callback.
These small graphs prove only the safety gate, not native in-game behavior.
The tool needs the installed .NET 10 SDK and references native Managed/BepInEx
DLL metadata plus the separately compiled product DLL. Set a dedicated output
root outside the source tree:

```powershell
.\tests\runtime\validate.ps1 -OutputRoot C:\path\to\temporary\pr9-validation -ModDll C:\path\to\Elin_ItemRelocator.dll
```

The SDK unit project excludes `runtime/**/*.cs`; existing fake-boundary unit
tests remain independent. Native csx compilation can produce CS1701 warnings for
the installed Unity/Harmony mscorlib identities; warnings are printed and counted,
not hidden. A successful emit is not evidence of in-game success.

The runtime operator uses the existing shared runner with `Suite smoke` and an
explicit case ID or `Tag integration`, retaining generated source, result JSON,
Player.log evidence, loaded DLL identity, fixture UID/quantities, and cleanup result.
Do not use the builder's default `Suite drama` for these synchronous cases.

## Remaining Coverage

These cases do not prove UI click workflows, PC/zone replacement or save/reload
across a paused preview, all toolbelt/ability/non-droppable source variants, nested
container capacity acceptance, or arbitrary inter-Mod callbacks. They do not alter
equipped-container destination policy, pet auto-unequip, affinity rules, profile
serialization, or standard UI transaction behavior. The product continues to use
native raw AddThing; these tests do not substitute UI-only transactions.

In-game execution and cleanup success are pending the runtime operator's result.
