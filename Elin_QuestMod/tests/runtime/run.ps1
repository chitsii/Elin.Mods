[CmdletBinding()]
param(
    [string]$RequiredNameContains = "RUNTIME_TEST",
    [int]$TimeoutSeconds = 180,
    [string]$PipeName = "Elin\\Console",
    [string]$CaseId = "",
    [string]$Tag = "",
    [string]$PlayerLogPath = "",
    [switch]$DisablePlayerLogDiff,
    [switch]$KeepGeneratedSource,
    [ValidateSet("drama", "smoke")]
    [string]$Suite = "smoke"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$modRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$runner = Join-Path $repoRoot "runtime-test-v2\runner\run_runtime_suite_v2.ps1"

if (-not (Test-Path $runner)) {
    throw "Shared runtime runner not found: $runner"
}

$effectiveTag = $Tag
if ($Suite -eq "smoke" `
    -and [string]::IsNullOrWhiteSpace($Tag) `
    -and [string]::IsNullOrWhiteSpace($CaseId)) {
    $effectiveTag = "smoke"
}

& $runner `
    -ModRoot $modRoot `
    -RequiredNameContains $RequiredNameContains `
    -TimeoutSeconds $TimeoutSeconds `
    -PipeName $PipeName `
    -CaseId $CaseId `
    -Tag $effectiveTag `
    -PlayerLogPath $PlayerLogPath `
    -DisablePlayerLogDiff:$DisablePlayerLogDiff `
    -KeepGeneratedSource:$KeepGeneratedSource `
    -Suite $Suite
