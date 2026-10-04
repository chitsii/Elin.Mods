[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [string]$SleepOfferAssembly = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$modRoot = Join-Path $repoRoot 'Elin_AutoOfferingAlter'
$project = Join-Path $PSScriptRoot 'RuntimeCompile.csproj'
$output = [System.IO.Path]::GetFullPath($OutputRoot)
New-Item -ItemType Directory -Force -Path $output | Out-Null
$env:DOTNET_CLI_HOME = Join-Path $output 'dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:TEMP = Join-Path $output 'tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
$productArg = @()
if ($SleepOfferAssembly) { $productArg = @('-p:SleepOfferAssembly=' + [System.IO.Path]::GetFullPath($SleepOfferAssembly)) }
$productDll = if ($SleepOfferAssembly) { [System.IO.Path]::GetFullPath($SleepOfferAssembly) } else { Join-Path $repoRoot '.codex-build\out\task7-auto-offering-build-b\Elin_AutoOfferingAlter.dll' }
$caseFiles = @(Get-ChildItem (Join-Path $PSScriptRoot 'src\cases') -Filter '*.cs' -File)
$ids = @()
foreach ($file in $caseFiles) {
    $raw = Get-Content -LiteralPath $file.FullName -Raw
    if (-not $raw.StartsWith('#if RUNTIME_TEST')) { throw "Missing RUNTIME_TEST guard: $($file.Name)" }
    foreach ($match in [regex]::Matches($raw, 'override string Id => "([^"]+)"')) { $ids += $match.Groups[1].Value }
}
if ($ids.Count -ne 11 -or @($ids | Select-Object -Unique).Count -ne 11) { throw 'Expected 11 unique PR8 case IDs.' }
if (@($ids | Where-Object { -not $_.StartsWith('pr8.sleep.') }).Count) { throw 'Foreign case ID.' }
& python -B (Join-Path $PSScriptRoot 'offline\verify_player_fixture.py')
if ($LASTEXITCODE -ne 0) { throw 'Fixture Player source contract failed.' }
$counterexampleProject = Join-Path $PSScriptRoot 'offline\Pr8ReloadContents.Tests.csproj'
& dotnet build $counterexampleProject --no-restore '-p:UseSharedCompilation=false' ('-p:IntermediateOutputPath=' + (Join-Path $output 'reload-counterexamples-obj\')) ('-p:OutputPath=' + (Join-Path $output 'reload-counterexamples-out\'))
if ($LASTEXITCODE -ne 0) { throw 'Reload counterexample compile failed.' }
& (Join-Path $output 'reload-counterexamples-out\Pr8ReloadContents.Tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Reload conservation counterexamples failed.' }
$preservationProject = Join-Path $PSScriptRoot 'offline\FixturePreservation.Tests.csproj'
& dotnet build $preservationProject --no-restore '-p:UseSharedCompilation=false' ('-p:IntermediateOutputPath=' + (Join-Path $output 'fixture-preservation-obj\')) ('-p:OutputPath=' + (Join-Path $output 'fixture-preservation-out\'))
if ($LASTEXITCODE -ne 0) { throw 'Fixture preservation test compile failed.' }
& (Join-Path $output 'fixture-preservation-out\Pr8FixturePreservation.Tests.exe') (Join-Path $modRoot 'elin_link\Elin_Data\Managed')
if ($LASTEXITCODE -ne 0) { throw 'Native codex callback isolation/preservation counterexamples failed.' }
$patchProject = Join-Path $PSScriptRoot 'offline\PatchRegistration.Tests.csproj'
& dotnet build $patchProject --no-restore '-p:UseSharedCompilation=false' ('-p:IntermediateOutputPath=' + (Join-Path $output 'patch-registration-obj\')) ('-p:OutputPath=' + (Join-Path $output 'patch-registration-out\'))
if ($LASTEXITCODE -ne 0) { throw 'Patch registration test compile failed.' }
& (Join-Path $output 'patch-registration-out\Pr8PatchRegistration.Tests.exe') $productDll (Join-Path $modRoot 'elin_link\Elin_Data\Managed') (Join-Path $modRoot 'elin_link\BepInEx\core')
if ($LASTEXITCODE -ne 0) { throw 'Offline product Harmony registration failed.' }
& dotnet build $project --no-restore '-p:UseSharedCompilation=false' ('-p:OutputPath=' + (Join-Path $output 'source-out\')) ('-p:IntermediateOutputPath=' + (Join-Path $output 'source-obj\')) @productArg
if ($LASTEXITCODE -ne 0) { throw 'Native source compile failed.' }
$csx = Join-Path $output 'pr8-generated.csx'
& (Join-Path $repoRoot 'runtime-test-v2\runner\build_runtime_suite_v2.ps1') -ModRoot $modRoot -Suite smoke -CaseId 'pr8.sleep.consume_water_reject' -RequiredNameContains 'RUNTIME_TEST' -OutputPath $csx -ResultPath (Join-Path $output 'not-executed-result.json')
$generated = Get-Content -LiteralPath $csx -Raw
if (-not $generated.StartsWith('#define RUNTIME_TEST')) { throw 'Generated script excludes guarded runtime cases.' }
if ($generated.Contains('__SOURCE_BLOCK__') -or $generated.Contains('__ENTRYPOINT_CLASS__')) { throw 'Unexpanded csx template.' }
foreach ($id in $ids) { if (-not $generated.Contains('"' + $id + '"')) { throw "Case missing from csx: $id" } }
$entry = 'var runnerObject = new GameObject("ArsRuntimeTestRunnerV2");'
$index = $generated.LastIndexOf($entry, [StringComparison]::Ordinal)
if ($index -lt 0) { throw 'Shared script entrypoint changed; inspect before updating compile adapter.' }
# Mechanical compile adapter: retain the exact generated boot statements in a method.
# It compiles the using-stripped script and native AddComponent call without executing them.
$adapted = $generated.Substring(0, $index) + "`npublic static class Pr8GeneratedCompileEntry { public static void CompileOnlyBoot() {`n" + $generated.Substring($index) + "`n} }`n"
$source = Join-Path $output 'pr8-generated-compile.cs'
[System.IO.File]::WriteAllText($source, $adapted)
& dotnet build $project --no-restore '-p:UseSharedCompilation=false' ('-p:GeneratedCompileSource=' + $source) ('-p:OutputPath=' + (Join-Path $output 'generated-out\')) ('-p:IntermediateOutputPath=' + (Join-Path $output 'generated-obj\')) @productArg
if ($LASTEXITCODE -ne 0) { throw 'Generated using-stripped csx compile failed.' }
Write-Host "PASS: native codex callback isolation/preservation counterexamples; fixture Player source contract; product Harmony registration; reload conservation counterexamples; 11 unique guarded cases; native source and generated-source compile. Game/CWL execution not performed."
