[CmdletBinding()]
param(
    [string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Elin',
    [string]$ReferenceRoot = "$env:USERPROFILE\.nuget\packages\microsoft.netframework.referenceassemblies.net48\1.0.3\build\.NETFramework\v4.8",
    [string]$OutputRoot = '',
    [string]$DotNet = 'dotnet'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$mod = Join-Path $repo 'Elin_AutoEatSleep'
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo '.codex-build\Elin_AutoEatSleep\runtime-sourcecompile'
}
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$env:DOTNET_CLI_HOME = Join-Path $OutputRoot 'dotnet-home'
$env:TEMP = Join-Path $OutputRoot 'tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME, $env:TEMP -Force | Out-Null
$suite = Join-Path $OutputRoot 'pr1.generated.csx'
& (Join-Path $repo 'runtime-test-v2\runner\build_runtime_suite_v2.ps1') -ModRoot $mod `
    -OutputPath $suite -ResultPath (Join-Path $OutputRoot 'runtime-result-NOT-RUN.json') `
    -Suite smoke -RequiredNameContains RUNTIME_TEST -Tag pr1
$script = [IO.File]::ReadAllText($suite)
$entry = [regex]::Matches($script, '(?m)^var runnerObject = new GameObject\(')
if ($entry.Count -ne 1) { throw 'Cannot locate exactly one shared CSX launch statement.' }
$source = Join-Path $OutputRoot 'pr1.generated.cs'
[IO.File]::WriteAllText($source, $script.Substring(0, $entry[0].Index), [Text.UTF8Encoding]::new($false))
$sdk = & $DotNet --list-sdks | Select-Object -Last 1
if ($LASTEXITCODE -ne 0 -or $sdk -notmatch '^([^ ]+) \[(.+)\]') { throw 'Installed SDK not resolved.' }
$compiler = Join-Path $Matches[2] "$($Matches[1])\Roslyn\bincore\csc.dll"
if (-not (Test-Path -LiteralPath $ReferenceRoot)) { throw "Installed net48 reference assemblies required: $ReferenceRoot" }
$references = @(Get-ChildItem -LiteralPath $ReferenceRoot -Filter '*.dll' -File |
    Where-Object { $_.Name -notin @('System.EnterpriseServices.Wrapper.dll', 'System.EnterpriseServices.Thunk.dll') } |
    ForEach-Object FullName)
$managed = Join-Path $GameRoot 'Elin_Data\Managed'
foreach ($name in @('Elin', 'Plugins', 'Plugins.BaseCore', 'Plugins.UI', 'Newtonsoft.Json', 'UnityEngine', 'UnityEngine.CoreModule', 'UnityEngine.UI', 'UnityEngine.InputLegacyModule', 'UnityEngine.AudioModule')) {
    $references += Join-Path $managed "$name.dll"
}
foreach ($name in @('0Harmony', 'BepInEx.Core', 'BepInEx.Unity')) {
    $references += Join-Path $GameRoot "BepInEx\core\$name.dll"
}
foreach ($path in $references) { if (-not (Test-Path -LiteralPath $path)) { throw "Reference missing: $path" } }
$dll = Join-Path $OutputRoot 'AutoEatSleep.Pr1.RuntimeCases.dll'
$argsList = @($compiler, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', '/warnaserror+', "/out:$dll")
$argsList += $references | ForEach-Object { "/reference:$_" }
$argsList += $source
& $DotNet @argsList
if ($LASTEXITCODE -ne 0) { throw "Generated native source compilation failed: $LASTEXITCODE" }

# Metadata inspection never loads the game or executes the compiled cases.
Add-Type -Path (Join-Path $GameRoot 'BepInEx\core\Mono.Cecil.dll')
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
try {
    $ids = @()
    foreach ($type in $assembly.MainModule.Types) {
        if ($type.IsAbstract -or $null -eq $type.BaseType -or $type.BaseType.Name -ne 'Pr1AutoEatCase') { continue }
        $getter = $type.Methods | Where-Object Name -eq 'get_Id'
        $ids += @($getter.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' } | ForEach-Object { [string]$_.Operand })
    }
    if ($ids.Count -ne 7 -or @($ids | Select-Object -Unique).Count -ne 7) { throw 'Expected seven unique concrete PR1 cases in generated DLL.' }
    $ids | Sort-Object | ForEach-Object { Write-Output "offline-case: $_" }
    $context = $assembly.MainModule.Types | Where-Object FullName -eq 'Pr1NativePcContext'
    if ($null -eq $context) { throw 'Native PC context missing: generated NPC has no native fixture faction.' }
    $contextCalls = @($context.Methods | Where-Object HasBody | ForEach-Object {
        $_.Body.Instructions | Where-Object { $_.OpCode.Name -in @('call', 'callvirt', 'newobj') } |
            ForEach-Object { [string]$_.Operand }
    })
    foreach ($required in @('FactionManager::OnCreateGame()', 'Chara::SetFaction(Faction)', 'Player::.ctor()')) {
        if (-not ($contextCalls | Where-Object { $_.Contains($required) })) { throw "Native PC context omits $required" }
    }
    $run = ($assembly.MainModule.Types | Where-Object FullName -eq 'Pr1AutoEatFixture').Methods | Where-Object Name -eq 'Run'
    $finallyInstructions = @($run.Body.ExceptionHandlers | Where-Object HandlerType -eq 'Finally' | ForEach-Object {
        $start = $_.HandlerStart.Offset
        $end = if ($null -eq $_.HandlerEnd) { [int]::MaxValue } else { $_.HandlerEnd.Offset }
        $run.Body.Instructions | Where-Object { $_.Offset -ge $start -and $_.Offset -lt $end }
    })
    foreach ($field in @('Game::player', 'Game::factions')) {
        if (-not ($finallyInstructions | Where-Object { $_.OpCode.Name -eq 'stfld' -and ([string]$_.Operand).Contains($field) })) {
            throw "Fixture finally does not restore $field"
        }
    }
    Write-Output 'Offline context contract: real faction initialization/assignment and Player/FactionManager finally restoration present. Native runtime NOT executed.'
}
finally { $assembly.Dispose() }

# Only configuration binding executes offline; game assemblies/cases are not loaded.
Copy-Item -LiteralPath (Join-Path $GameRoot 'BepInEx\core\BepInEx.Core.dll') -Destination $OutputRoot -Force
Copy-Item -LiteralPath (Join-Path $GameRoot 'BepInEx\core\SemanticVersioning.dll') -Destination $OutputRoot -Force
$configSource = Join-Path $mod 'src\ModConfig.cs'
$configTest = Join-Path $PSScriptRoot 'ConfigBindingOffline.cs'
$configExe = Join-Path $OutputRoot 'Pr1.ConfigBindingOffline.exe'
$configArgs = @($compiler, '/nologo', '/target:exe', '/langversion:latest', '/nostdlib+', '/warnaserror+', "/out:$configExe")
$configArgs += @(Get-ChildItem -LiteralPath $ReferenceRoot -Filter '*.dll' -File |
    Where-Object { $_.Name -notin @('System.EnterpriseServices.Wrapper.dll', 'System.EnterpriseServices.Thunk.dll') } |
    ForEach-Object { "/reference:$($_.FullName)" })
$configArgs += "/reference:$(Join-Path $GameRoot 'BepInEx\core\BepInEx.Core.dll')"
& $DotNet @configArgs $configSource $configTest
if ($LASTEXITCODE -ne 0) { throw 'Source-linked configuration test compilation failed.' }
& $configExe $OutputRoot
if ($LASTEXITCODE -ne 0) { throw 'Real BepInEx source-linked configuration regression failed.' }

$mutant = Join-Path $OutputRoot 'ModConfig.default1.mutant.cs'
$text = [IO.File]::ReadAllText($configSource)
$mutated = $text.Replace('"HungerThreshold", 3,', '"HungerThreshold", 1,')
if ($text -eq $mutated) { throw 'Default-value mutation did not match; update the offline mutation fixture.' }
[IO.File]::WriteAllText($mutant, $mutated, [Text.UTF8Encoding]::new($false))
& $DotNet @configArgs $mutant $configTest
if ($LASTEXITCODE -ne 0) { throw 'Configuration mutation fixture compilation failed.' }
$mutantOutput = & $configExe $OutputRoot
if ($LASTEXITCODE -eq 0 -or ($mutantOutput -join "`n") -notmatch 'fresh default: expected 3, got 1') {
    throw 'Real configuration test did not reject the previous default=1.'
}
Write-Output 'Offline mutation check: previous default=1 rejected by real ConfigFile behavior.'
& $DotNet @configArgs $configSource $configTest
if ($LASTEXITCODE -ne 0) { throw 'Final unmutated configuration test compilation failed.' }
$ownershipExe = Join-Path $OutputRoot 'Pr1.OwnershipOffline.exe'
$ownershipArgs = @($compiler, '/nologo', '/target:exe', '/langversion:latest', '/nostdlib+', '/warnaserror+', "/out:$ownershipExe")
$ownershipArgs += @(Get-ChildItem -LiteralPath $ReferenceRoot -Filter '*.dll' -File |
    Where-Object { $_.Name -notin @('System.EnterpriseServices.Wrapper.dll', 'System.EnterpriseServices.Thunk.dll') } |
    ForEach-Object { "/reference:$($_.FullName)" })
& $DotNet @ownershipArgs (Join-Path $PSScriptRoot 'src\cases\Pr1FixtureOwnership.cs') (Join-Path $PSScriptRoot 'OwnershipOffline.cs')
if ($LASTEXITCODE -ne 0) { throw 'Fixture ownership counterexample compilation failed.' }
& $ownershipExe
if ($LASTEXITCODE -ne 0) { throw 'Fixture ownership preservation counterexamples failed.' }
Get-FileHash -LiteralPath $dll -Algorithm SHA256 | Format-List Path, Hash
Write-Output 'Native-reference DLL, seven case metadata checks, real configuration binding and default mutation checks passed. Game/runtime cases NOT executed.'
