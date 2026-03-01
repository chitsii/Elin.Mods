[CmdletBinding()]
param(
    [Alias("RequiredNamePrefix")]
    [string]$RequiredNameContains = "RUNTIME_TEST",
    [string]$DramaId = "",
    [int]$PerDramaTimeoutSeconds = 20,
    [int]$MaxBranchRunsPerDrama = 128,
    [int]$TimeoutSeconds = 600,
    [string]$PipeName = "Elin\Console"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "[runtime-test] $Message"
}

function Send-CwlCommand {
    param(
        [string]$Command,
        [string]$NamedPipe,
        [int]$ConnectTimeoutMs = 5000
    )

    $client = New-Object System.IO.Pipes.NamedPipeClientStream(
        ".",
        $NamedPipe,
        [System.IO.Pipes.PipeDirection]::Out
    )

    try {
        $client.Connect($ConnectTimeoutMs)
        $writer = New-Object System.IO.StreamWriter($client)
        try {
            $writer.AutoFlush = $true
            $writer.WriteLine($Command)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $client.Dispose()
    }
}

$modRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$templatePath = Join-Path $modRoot "tests\runtime\ars\suites\drama_integrity_runtime_template.csx"
$dramaDir = Join-Path $modRoot "LangMod\EN\Dialog\Drama"
$artifactRoot = Join-Path $modRoot "tests\runtime\_artifacts"

if (-not (Test-Path $templatePath)) {
    throw "Template not found: $templatePath"
}
if (-not (Test-Path $dramaDir)) {
    throw "Drama directory not found: $dramaDir"
}

$dramaFiles = @(Get-ChildItem -Path $dramaDir -Filter "drama_ars_*.xlsx" | Sort-Object Name)
if ($dramaFiles.Count -eq 0) {
    throw "No drama files found in: $dramaDir"
}

$dramaIds = @()
foreach ($file in $dramaFiles) {
    $baseName = $file.BaseName
    if (-not $baseName.StartsWith("drama_")) {
        continue
    }
    $dramaIds += $baseName.Substring("drama_".Length)
}

if ($dramaIds.Count -eq 0) {
    throw "No drama IDs resolved from filenames."
}

if (-not [string]::IsNullOrWhiteSpace($DramaId)) {
    $normalizedDramaId = $DramaId.Trim()
    if ($normalizedDramaId.StartsWith("drama_")) {
        $normalizedDramaId = $normalizedDramaId.Substring("drama_".Length)
    }

    if (-not ($dramaIds -contains $normalizedDramaId)) {
        throw "DramaId not found in resolved list: $normalizedDramaId"
    }

    $dramaIds = @($normalizedDramaId)
}

$runId = Get-Date -Format "yyyyMMdd_HHmmss"
$runDir = Join-Path $artifactRoot $runId
$resultPath = Join-Path $runDir "result.json"
$runtimeScriptPath = Join-Path $runDir "runtime_drama_integrity_generated.csx"

New-Item -ItemType Directory -Path $runDir -Force | Out-Null

$dramaIdLiteral = ($dramaIds | ForEach-Object {
        '"' + $_.Replace('\', '\\').Replace('"', '\"') + '"'
    }) -join ", "

$template = Get-Content -Path $templatePath -Raw
$scriptText = $template.
    Replace("__RESULT_PATH__", $resultPath).
    Replace("__REQUIRED_PREFIX__", $RequiredNameContains.Replace('\', '\\').Replace('"', '\"')).
    Replace("__PER_DRAMA_TIMEOUT_SECONDS__", [string]$PerDramaTimeoutSeconds).
    Replace("__MAX_BRANCH_RUNS_PER_DRAMA__", [string]$MaxBranchRunsPerDrama).
    Replace("__DRAMA_IDS__", $dramaIdLiteral)

Set-Content -Path $runtimeScriptPath -Value $scriptText -Encoding UTF8 -NoNewline

Write-Info ("run_id: {0}" -f $runId)
Write-Info ("resolved dramas: {0}" -f $dramaIds.Count)
Write-Info ("result path: {0}" -f $resultPath)

Send-CwlCommand -Command "cwl.cs.is_ready" -NamedPipe $PipeName
$runtimeScriptPathForCwl = $runtimeScriptPath -replace '\\', '/'
Write-Info ("cwl.cs.file arg: {0}" -f $runtimeScriptPathForCwl)
Send-CwlCommand -Command ("cwl.cs.file {0}" -f $runtimeScriptPathForCwl) -NamedPipe $PipeName

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while (-not (Test-Path $resultPath)) {
    if ((Get-Date) -gt $deadline) {
        throw "Timed out waiting for result file: $resultPath"
    }
    Start-Sleep -Milliseconds 500
}

$resultJson = Get-Content -Path $resultPath -Raw | ConvertFrom-Json

$status = [string]$resultJson.status
$failed = [int]$resultJson.summary.failed
$passed = [int]$resultJson.summary.passed
$total = [int]$resultJson.summary.total

Write-Info ("status={0} total={1} passed={2} failed={3}" -f $status, $total, $passed, $failed)

if ($status -ne "passed" -or $failed -gt 0) {
    Write-Info ("FAILED: inspect result file -> {0}" -f $resultPath)
    exit 1
}

Write-Info "PASSED"
exit 0
