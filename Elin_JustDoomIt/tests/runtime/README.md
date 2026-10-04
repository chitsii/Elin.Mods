# PR2 native placement integration tests

These tests use runtime-test-v2 and real `Zone.Activate`, `ThingGen.Create`,
`Zone.AddCard`, `Card.Install`, native inventory transfer and real CWL traits.
They do not start DOOM gameplay or change the product's rewards/save behavior.
Only the assigned runtime operator runs/deploys them, one CaseId at a time.

## Fixture contract

Use a backed-up, disposable save with PC name containing `RUNTIME_TEST` and no
companions/drama. The active zone must be a separate registered, non-instance
casino whose explicit `name` starts with `RUNTIME_TEST_PR2_`. Do not rename or
reuse the ordinary Fortune Bell casino. Use native `SpatialGen.Create("casino",
region, true)` to create a separate UID; name it before entry, set `lv=0`, and
enter it using native `pc.MoveZone`. The case does not create/enter/delete zones:
the runtime operator owns this fixture and restores its baseline save afterward.
Record its UID, original casino UID and save backup in the run manifest.
For `noncasino_control`, prepare a separate native `field` zone with the same name
prefix. It is a real field source, not a renamed casino source or skipped native method.

Generation cases require zero existing arcade cabinets; fail preparation instead
of deleting any. Coordinate cases require a free (49,66) plus another valid tile.
Activate twice does not bypass the original game method or manually invoke the
product postfix. Source-key removal is synchronous with exact-row restoration;
never run source mutations concurrently with another case/save/load.

## Cases

| CaseId (`pr2.doom.` prefix) | Actual operation and assertions |
| --- | --- |
| `cabinet_preservation` | Three native installed cabinets with distinct property/stolen/lost states and a held two-item cabinet; native Activate twice at lv -2,-1,0,1,2,7; assert UID, quantity, position, name, material, trait, installation and parent/ownership unchanged; native relocate held cabinet and reassert |
| `new_cabinet_idempotent` | Empty non-target floors generate nothing; lv=1 creates exactly one correct ID/trait, installed NPC-property cabinet on a valid tile; second Activate preserves UID and count |
| `noncasino_control` | Real isolated field zone at lv=1 generates no cabinet; an existing native cabinet keeps UID and ownership after Activate |
| `new_missing_source` | Remove only the arcade key from cards and things registries independently; real Activate must call neither ThingGen nor AddCard; original row references restored |
| `occupied_tile_native` | Place a real non-cabinet blocker at (49,66); Activate must place one cabinet on a different, valid unoccupied native tile and preserve blocker |
| `generation_faults` | Fixture-only faults: redirect actual native factory to money or unregistered ID (native fallback 869), replace generated trait, or throw at factory entry; real product rejects results/fails softly before AddCard; a normal native retry must then succeed |
| `invalid_tile_fault` | Occupy fixed tile natively, then inject an invalid return after the actual nearest-point method; product must reject it before ThingGen/AddCard |
| `existing_save_prepare` | Create installed/held fixtures, Activate at lv=0, write UID/state manifest and retain fixtures for externally saved/reloaded disposable save; preparation only, not a save/reload pass |
| `existing_save_verify` | Require replaced Game object and identical save/PC/zone UID; reacquire cards by UID, assert persisted state, Activate at lv=0/1/2 without mutation/duplication, cleanup |
| `existing_save_abort` | Remove only manifest-owned fixture cards; cleanup only, no integration claim |

Floors exercise native ground-floor (lv=0), target (lv=1), higher and basement
branches on the same isolated map. This is not a claim to have visited every
casino map. The invalid-result case is fault injection, not a naturally fully
blocked map test. DOOM launch/UI/input/BGM and full world restoration are separate
coverage, not implied by placement success.

## Cleanup and evidence

Rollback is registered before card/config/source/Harmony mutations. Observers are
pass-through and scoped to the product call stack and fixture zone; fault owners
are separate and removed in finally. Only test owners use UnpatchSelf. Track
native generated results even when rejected/detached so cleanup can destroy their
UIDs. Assert original map/inventory snapshots, cardinality, PC/zone, floor,
autosave and source identities; do not repair a changed original to obtain pass.

Always reload the fixture's baseline save after each case: native Activate has
world/date/scene side effects not fully rolled back by local cleanup. On any
cleanup/timeout failure STOP, retain logs and reload baseline before continuing.
No general/tag-only runs. A pending save manifest blocks other cases. Save verify
must happen after real save/load by the runtime operator, using fresh references;
serialization alone is not accepted. A successful prepare deliberately retains
fixtures; use verify or abort, then reload baseline.

Case logs contain actual Activate/ThingGen/AddCard counts, fixture IDs and assertion
results. The runtime operator additionally records loaded product/game DLL hashes,
channel/build, save backup, test Harmony owners, result JSON and Player.log tail.
Existence checks are preparation guards, never integration evidence.

## Offline only

`validate-offline.ps1` runs the existing CSX builder, compiles its composed
declarations against real game/Harmony/PR2 DLLs and checks ten distinct IDs. Its
output explicitly says runtime `NOT_RUN`. Output defaults to `.codex-build/Elin_JustDoomIt/runtime-offline`; use `-OutputRoot` to override it and `-ProductDll` to select a verified compile-only DLL (the default is the Mod's `_bin/Elin_JustDoomIt.dll`);
it does not deploy, launch, save/load, or execute cases. Use the shared runner with
`-Suite smoke -CaseId pr2.doom.<case>` for actual serial runs by the runtime owner.
The Mod-local template adds `PR2_RUNTIME_TEST`; case files are guarded by that
symbol so the sibling net8 pure-test project's recursive Compile glob stays intact.
