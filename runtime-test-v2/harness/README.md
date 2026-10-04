# Temporary runtime pipe repair

This is test infrastructure for the backed-up `world_11` / `RUNTIME_TEST` session. It does not change product Mod logic, vendor DLLs, loadorder, CWL settings, native cancellation state, or network listeners.

## Why this repair is needed

The installed `ElinModdingKit` 1.4.0.30832, SHA256 `707BBC730B9535198F968A7F47DB7DFBAB11152583B074E2A83073F8984F8852`, starts `EPipe.ProcessCommands()` only from `Awake`. Its iterator exits when the queue is empty. There is no Update/base-EMono restart. UTF-8/LF duplex `cs.version` reached the server PID but produced no response. The current game also registers `cs.version` / `cs.file`, with `.cs` required; legacy `cwl.cs.*` and `.csx` do not match this installation.

Official upstream EPipe has switched to line reception followed by `UniTask.SwitchToMainThread`: https://github.com/gottyduke/Elin.Plugins/blob/master/ModdingKit/ModdingKit/Components/EPipe.cs . Replacing the whole Kit would also change dependencies/API. This temporary repair uses the existing loaded Kit and existing pipe/parser/server instead.

## Harness contract

The BepInEx plugin depends on `elin.plugins.scripting`. It must be discovered within the same Package chainloader as the Kit: install into `Package/_ModdingKit/RuntimeTestPipe`, whose DLL is found by the existing recursive `DiscoverPluginsFrom` scan. The initial BepInEx/plugins chainloader cannot resolve this Package-only dependency and skips the harness; do not use that location. Dependency sorting then loads Kit.Awake followed by Harness.Awake before the Kit.Start pipe creation. No new package, loadorder entry, or vendor DLL replacement is needed. It installs one Harmony prefix on the exact declared, parameterless, IEnumerator-returning `EModding.Components.EPipe.ProcessCommands`. The prefix supplies a persistent frame-yielding iterator to the existing Awake.StartCoroutine call. It does not create another server/consumer and refuses an already-created pipe instance. Never load it into the current session or revive its queued requests.

Activation requires `enable.txt` beside the DLL containing exactly `world_11`, the existing `Scripting.AllowScripting=true`, one Kit assembly with the exact version, loaded location, hash, queue, static CTS, and reply method signatures. Any mismatch fails closed. Dispatch also requires Game.id `world_11`, a PC name containing `RUNTIME_TEST`, and an available existing EScript provider. Only `cs.version`, `cs.eval`, and `cs.file` are accepted. Commands run once per Unity main-thread step, with a thread check and session/timeScale logging. Exceptions are logged/replied and the next request can proceed. OnDestroy/quit sets the iterator's stopping flag and unpatches only the harness owner. Do not consider UnpatchSelf alone sufficient cleanup; stop the game before removing files.

## Build and offline verification

Use .NET SDK 10 and the existing game references. This directory's NuGet.Config disables external package feeds; net472 reference assemblies must already be cached. The console tests need no external packages.

```powershell
dotnet build .\RuntimePipeHarness.csproj -c Release -p:NuGetAudit=false
dotnet run --project .\tests\OfflineTests.csproj -p:UseAppHost=false
& ..\tests\test_transport.ps1
& ..\tests\test_runner.ps1
```

If the shared NuGet scratch directory is inaccessible, set NUGET_SCRATCH to a task-owned temporary directory for that process. Do not delete global locks. A first-use DOTNET_CLI_HOME can be avoided; if one is necessary, set DOTNET_GENERATE_ASPNET_CERTIFICATE=false before using it.

Offline tests cover empty-queue arrival, one command/frame, no duplicate dispatch, exception recovery, stopping, session/settings guards, current/legacy protocol negotiation, fragmented UTF-8/LF responses, connection/read timeout, disabled/compile errors, one file request, suite result timeout, and independent source/error/transport/log preservation. Runner fixture results are synthetic, not game verification.

## Deployment and removal

Only after review, game shutdown, and no concurrent user operation:

```powershell
& .\deploy_harness.ps1 -SessionRoot <new-task-owned-session-directory>
```

Additional backup/restore coverage is precisely the prior absence of `Package/_ModdingKit/RuntimeTestPipe`, the installed DLL/hash and `enable.txt`, and prior existence/content/hash or absence of `BepInEx/config/chitsii.elin.runtime_test_pipe.cfg`. The deployment script writes `deploy-harness.json` before installing and refuses any preexisting harness directory. Existing full Save/config/Package backups remain required.

Launch only the dedicated local world_11. Confirm the harness logs its supported Kit identity and pump startup, then request current readiness and read-only `cs.eval 1+1` once. Record response, thread, slot/name/timeScale. Run the patch-target smoke once using the common runner with `-PipeName 'Elin\Console'`. The auto backend changes to LegacyCwl only after an explicit unknown-command response; timeout never triggers fallback. Script command acknowledgement is not a suite pass: only a nonempty passing result.json proves a suite result. The harness on this current Kit deliberately rejects legacy commands.

After all live testing, stop the game normally, then:

```powershell
& .\remove_harness.ps1 -SessionRoot <same-session-directory>
```

Removal validates the exact target, known filenames, DLL and marker, restores the owned config/absence, and refuses altered/untracked files. No recursive deletion is used. Recheck vendor DLL hashes, scripting settings, loadorder, normal/shared/test saves and the broader backup separately. Restoring live game files is prohibited.

Live acceptance still pending: late readiness after an idle frame, read-only arithmetic exactly once on main thread, a throwing command followed by another successful request, one suite runner startup, patch smoke result, and final shutdown/removal/hash/save verification.
