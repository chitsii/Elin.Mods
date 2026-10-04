# PR5 LogRefined native integration cases

Run only on a backed-up disposable save whose PC name contains `RUNTIME_TEST`.
The serial game operator owns deployment, CWL/pipe execution and save restoration.
These case sources do not deploy, save, load, or operate the game externally.

Use the shared runtime-test-v2 runner with `-ModRoot <repo>\Elin_LogRefined
-Suite smoke -Tag pr5 -RequiredNameContains RUNTIME_TEST`; select a single case
with `-CaseId` when investigating a failure. No separate product test hook is required.

| Case ID | Actual native operation and assertions |
| --- | --- |
| `pr5.log.condition_outcomes` | CharaGen/Create/AddCard fixture; real AddCondition new Wet, existing Wet stack with null return and real OnStacked, zero-power refusal, genuine success after refusal, Burning resistance, Wet->Burning native TryNullify, StanceIai Kill/toggle, and unmet ConDark override. Condition identity/owner/duration/refresh, native return, stack call count, filtered refined logs, and raw throttle key presence must agree. |
| `pr5.log.capped_native_stack` | Select a known native bounded Condition with inherited OnStacked, seed through native AddCondition, set only its fixture duration to the native maximum, then apply again. Native null return, actual OnStacked, retained existing instance, unchanged capped duration and one refined success log are required. Missing suitable native data fails rather than skips/passes. |
| `pr5.log.reentry_owner_isolation` | Test-only OnStartOrStack boundary invokes native AddCondition while the outer real stack scope is live. Other-owner/same-ID stack gives two actual stack calls and two owner-specific logs; other-owner refusal and same-owner/different-ID resistance give no inner success log/throttle. |
| `pr5.log.exception_finalizer` | Test-only exception after real OnStacked mutation propagates out of native AddCondition. No success log/throttle is created; scope is empty. A following rejection cannot use stale evidence, and a real successful retry logs/registers once. Partial native condition mutation is asserted rather than silently reversed before verification. |

The observers have a unique `runtime.pr5.log.*` Harmony owner and only count owned
fixture UIDs/condition identities. `Msg.SayRaw` is pass-through; its rich-text-free
RefinedLog bracket format must contain both unique fixture name and condition name.
Vanilla messages and game message history remain intact. Product patches are
required to be already installed; the suite never calls or reapplies their methods.

Configuration entries and ConfigFile.SaveOnConfigSet are snapshotted. Auto-save of
configuration is disabled while temporary settings are assigned and restored;
the config file's SHA256 must remain identical on disk.
Rollback is registered before config/observer/world mutations. All cases are
synchronous, so there are no yielded child IEnumerator exception gaps. Rollback
unpatches only the test owner, kills owned fixture conditions, destroys only cards
and descendants captured by UID/reference immediately after generation, restores
all throttle timestamps/counter and settings, and asserts
original map/global/party/carryover membership plus preexisting card HP, position,
master, conditions and elements. Cleanup failure is a failed case: stop and reload
the disposable save before another run. A test-only failure latch blocks further
native operations in later cases of the same suite even if the shared host continues
collecting results. Initial execution should select cases individually.

Cleanup never adopts end-of-test inventory. Every destruction callback rechecks
the complete captured tree, UID/reference identity and parent ownership. Native
Card.Destroy and Condition.Kill receive an additional cleanup-only Harmony guard,
ordered after the installed owners, including native recursive destruction calls.
Conditions must be generation-time/explicitly-created instances whose owner is the
same fixture. Children are destroyed before parents. Unknown children, moved items,
changed owner/faction/held state, or missing live captured items block destruction
and fail cleanup. Do not detach, delete, or relabel the unknown objects to make the
case pass: the serial game operator must restore the external disposable-save baseline.

`offline/run-offline.ps1 -OutputRoot <task-or-TEMP-directory>` runs 13 pure ownership
gate counterexamples/control tests with no game dependencies. Unknown direct/nested
items, same-UID replacement references, changed UIDs/owners, moved/missing items,
unknown destruction targets, foreign Condition owners and late insertion must
produce zero rejected destruction callbacks. These policy tests use the same gate
as native cleanup and do not count as a game integration pass.

Native source/type/visibility mismatches fail preparation/execution; they are never
counted as integration passes. Native effects/messages and fixture UID allocation
are expected transient side effects; reload the disposable save after the suite.
These tests do not assert visual FX rendering, audio playback, unrelated Workshop
DamageHP reports, or compatibility with every third-party Mod.

`compile-offline.ps1 -RepoRoot <repo> -OutputRoot <task-or-TEMP-directory>` only
generates the shared CSX and compiles its exact flattened source against the existing
net472 references and actual game DLLs. A compile-only summary is not runtime proof.
