# PR6 Quest Media Integration Cases

These test-only cases use the loaded QuestMod resolver/context and native Card
overloads. They are excluded from the normal `smoke` tag. Use one exact case ID
with `-Suite smoke -CaseId <id>` on a disposable backed-up `RUNTIME_TEST` save.
Only the designated runtime operator may deploy or run them. The 2026-10-04
attempt stopped at the former shared listener-volume prerequisite: one Prepare
failure, six unexecuted cases, native media not reached. No runtime pass is
recorded here; the revised fixture still requires an operator rerun.

| Case ID | Required evidence |
| --- | --- |
| `pr6.quest.resolver_fx_only_dispatch` | True; native string FX overload once with true/0/default Vector3; sound zero. Dispatch only. |
| `pr6.quest.empty_media_rejected` | Empty/malformed/unsupported parser keys and empty context effect return false; native media zero. Dispatch only. |
| `pr6.quest.effect_failure_sound_attempted` | Fixture-scoped effect prefix throws; context catches; sound attempted once; resolver false. Fault injection, dispatch only. |
| `pr6.quest.sound_failure_result_false` | Native effect returns; fixture-scoped sound prefix throws; resolver false. Fault injection, dispatch only. |
| `pr6.quest.cleanup_unknown_fx_child_rejected` | An unrecorded test-owned child makes actual FX cleanup refuse destruction; FX/control remain intact; recorded control is removed separately. Cleanup guard only. |
| `pr6.quest.fx_sound_actual` | True; both native overloads once, standard defaults and PC/root render position; active FX with enabled visible sprite or live particles; audio channel playing requested data clip, volume positive, non-spatial. |
| `pr6.quest.showcase_cue_media_actual` | Existing xlsx parsed/compiled by native drama/CWL; original cue and media closures dispatched through native sequence; each succeeds once; native render/audio evidence as above. |

The last two cases record engine render state and channel/clip evidence. They do
not prove framebuffer pixels, mixer output amplitude, physical speaker output, or
that a person saw/heard anything. Retain screenshots/audio capture or an explicit
runtime-operator observation separately when those are required. Source mute or
zero source volume, paused engine audio, missing media, missing compiled cue,
wrong script order, or absent visible renderers fail the actual cases instead of
being treated as a pass. Unknown non-empty media IDs are
not assumed to fail: native fallback behavior may vary.

Audio prerequisites are case-specific. All seven retain the dedicated PC,
player/UI/flags, no pre-existing drama, effect manager, loaded mod types, exact
Card signatures/defaults, and observer/cleanup requirements. The empty case uses
the same effect observer fixture to prove zero native dispatch. Only cases that
allow a native sound call require a sound manager and the requested `revive`
data; the bounded showcase segment never plays `base.ok`, so that data is not a
prerequisite.

| Case ID | Audio coverage / prerequisites |
| --- | --- |
| `pr6.quest.resolver_fx_only_dispatch` | None; no audio manager/data/ignore/pause prerequisite. |
| `pr6.quest.empty_media_rejected` | None; no audio manager/data/ignore/pause prerequisite. |
| `pr6.quest.effect_failure_sound_attempted` | Dispatch; manager + revive data for safe capture/cleanup if a channel is created; ignore/pause allowed; only the independent Card attempt is asserted. |
| `pr6.quest.sound_failure_result_false` | None; fixture throws before native sound playback; no audio manager/data/ignore/pause prerequisite. |
| `pr6.quest.cleanup_unknown_fx_child_rejected` | None; no sound dispatch or audio prerequisite. |
| `pr6.quest.fx_sound_actual` | Playback; manager + revive data, ignoreSounds=false, AudioListener.pause=false. |
| `pr6.quest.showcase_cue_media_actual` | Playback; same, plus the existing compiled showcase cue/media segment. |

All seven permit `AudioListener.volume=0` and `Application.isFocused=false`.
They log both conditions and never change listener volume, focus, or game audio
settings. In the extracted public24892994 `Plugins.Sound.dll`,
`SoundManager._Play` reaches `SoundSource.Play`, which configures the clip/channel
and calls `AudioSource.Play`; neither managed method gates on listener volume or
focus. Unity documents [listener volume](https://docs.unity3d.com/ScriptReference/AudioListener-volume.html)
as global sound output volume and [listener pause](https://docs.unity3d.com/ScriptReference/AudioListener-pause.html)
as pausing sources/DSP (new requests also start paused). Thus the last two can
verify engine channel/clip/play-state and FX render state while global output is
muted. They cannot establish audible output. If being unfocused actually stops
engine playback or rendering on the runtime host, the unchanged strict channel
and renderer assertions fail; no synthetic playback pass is substituted.
Playback prerequisites are rechecked when asserting audio. Paused dispatch-only
cases conservatively protect active clip-bearing channels as baseline resources.

The showcase fixture enters the native `end` step to load the existing book
without playing its intro, then resumes its own sequence at the existing compiled
cue and following FX command. Closure traversal identifies the exact original CWL
`line["param"]` scripts; it neither constructs new drama actions nor replaces the
resolver. The test-owned sequence retains the original native events up through
the media event and omits subsequent events, so native sequence progression exits
before completion/follow-up. The stored xlsx/scenario is unchanged. This does not cover the whole
showcase UI, branch navigation, or completion/reward workflow.

Observers are pass-through except the explicitly tagged fixture-only exception
prefixes. They are scoped to the PC and a synchronous call window. Cleanup is
registered before patching/mutation, attempts all cleanup actions even after one
fails, unpatches only its Harmony owner, kills only observed new FX, stops only
observed fixture audio clips, removes only its own layer, restores scoped QuestMod
flags/UI fields, and asserts PC identity/position/HP, flags, top layer and owner
removal. Pre-existing playing sound channels and effects are not removed.
FX/layer parent and full descendant references are captured during fixture
creation; unknown children or changed parents prevent destructive cleanup. Layers
are observed at creation, never inferred from whatever layer exists at cleanup.
FX ownership is tied to a reference and activation generation. The pass-through
`Effect.Activate()` prefix observes both `_Play(...)` and `Play(Vector3)`, including
callback replays; only the first expected native activation updates the claim.
Later generations are never adopted, even with unchanged position/parent/children.
The observer remains installed through cleanup so layer-close callbacks also
invalidate reused FX. Swapped live `dialogFlags` dictionaries fail before any
flag restoration writes.
Run cases individually and reload the dedicated save after each; any cleanup failure
requires stopping further cases and reloading. These assertions are not a claim
to restore every engine cache, RNG state, or entire world.

The frame cases yield only `null` in their own IEnumerator, bounded at 45 frames;
they do not rely on child coroutine exceptions reaching the host. Source uses
fully qualified types where template imports are insufficient.

Offline validation (no deploy, pipe, game launch, save, or runtime execution):

```powershell
& .\tests\runtime\compile-pr6-offline.ps1 -RepoRoot <isolated-repo> -OutputRoot <task-output-dir>
```

This invokes the existing shared generator, compiles the generated source against
real Elin/Plugins.Sound/Unity DLLs and cached .NET Framework references, and emits
`compile-summary.json` with hashes and `runtimeExecuted=false`. Only the final
top-level runner statements are wrapped in an uncalled method for library
compilation; no case imports or bodies are repaired by the offline transform.
The untouched csx is also compiled as a Roslyn Script submission against those
same real DLL references, without evaluating the submission entry point.
The same command runs 18 source-linked pure managed counterexamples
under the local .NET 10 SDK. They create no Unity/Elin objects and exercise the
actual fixture guard helper: alternate-entry/callback generation changes, queued
cleanup, reference identity, independent cleanup, live-dictionary swaps, and
case-specific audio prerequisites and paused baseline channel protection. Metadata checks verify all seven compiled
coverage assignments and the native managed audio call chain; they do not run
Unity audio or establish hardware output.

Last checked: 2026-10-04. Runtime execution, actual framebuffer/audio output,
manual UI navigation and operator observation remain unexecuted.
