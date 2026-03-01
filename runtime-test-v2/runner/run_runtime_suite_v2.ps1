[CmdletBinding()]
param(
    [string]$ModRoot = "",
    [string]$RequiredNameContains = "RUNTIME_TEST",
    [int]$TimeoutSeconds = 180,
    [string]$PipeName = "Elin\\Console",
    [string]$CaseId = "",
    [string]$Tag = "",
    [string]$PlayerLogPath = "",
    [switch]$DisablePlayerLogDiff,
    [switch]$KeepGeneratedSource,
    [ValidateSet("drama", "smoke")]
    [string]$Suite = "drama"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "[runtime-v2] $Message"
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

function Resolve-PlayerLogPath {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (Test-Path $ExplicitPath) {
            return (Resolve-Path $ExplicitPath).Path
        }
        Write-Info ("playerlog not found at explicit path: {0}" -f $ExplicitPath)
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
        return $null
    }

    $candidate = Join-Path $env:USERPROFILE "AppData\LocalLow\Lafrontier\Elin\Player.log"
    if (Test-Path $candidate) {
        return (Resolve-Path $candidate).Path
    }

    return $null
}

function Get-PlayerLogBaseline {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return [ordered]@{
            Exists = $false
            Length = 0L
            LastWriteUtc = ""
        }
    }

    $item = Get-Item $Path
    return [ordered]@{
        Exists = $true
        Length = [int64]$item.Length
        LastWriteUtc = $item.LastWriteTimeUtc.ToString("o")
    }
}

function Read-TextFromOffset {
    param(
        [string]$Path,
        [int64]$Offset
    )

    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::ReadWrite
    )

    try {
        if ($Offset -gt 0 -and $Offset -lt $stream.Length) {
            [void]$stream.Seek($Offset, [System.IO.SeekOrigin]::Begin)
        }
        elseif ($Offset -ge $stream.Length) {
            return ""
        }

        $remaining = $stream.Length - $stream.Position
        if ($remaining -le 0) {
            return ""
        }
        if ($remaining -gt [int]::MaxValue) {
            throw "Player.log delta is too large to read in one pass."
        }

        $buffer = New-Object byte[] ([int]$remaining)
        $read = $stream.Read($buffer, 0, $buffer.Length)
        if ($read -le 0) {
            return ""
        }

        return [System.Text.Encoding]::UTF8.GetString($buffer, 0, $read)
    }
    finally {
        $stream.Dispose()
    }
}

function Write-PlayerLogDiffArtifacts {
    param(
        [string]$Path,
        [System.Collections.IDictionary]$Baseline,
        [string]$RunDir
    )

    $diffPath = Join-Path $RunDir "playerlog.diff.log"
    $metaPath = Join-Path $RunDir "playerlog.diff.meta.json"
    $meta = [ordered]@{
        enabled = $true
        playerLogPath = $Path
        baselineExists = $Baseline.Exists
        baselineLength = $Baseline.Length
        baselineLastWriteUtc = $Baseline.LastWriteUtc
        afterExists = $false
        afterLength = 0L
        afterLastWriteUtc = ""
        diffMode = "append_only"
        bytesWritten = 0L
        linesWritten = 0
        generatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    }

    try {
        $appendedText = ""

        if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
            $meta.diffMode = "missing_after"
        }
        else {
            $item = Get-Item $Path
            $afterLength = [int64]$item.Length
            $meta.afterExists = $true
            $meta.afterLength = $afterLength
            $meta.afterLastWriteUtc = $item.LastWriteTimeUtc.ToString("o")

            if (-not $Baseline.Exists) {
                # Append-only policy: baseline missing means no reliable "new lines" range.
                $meta.diffMode = "baseline_missing_no_output"
                $appendedText = ""
            }
            elseif ($afterLength -lt $Baseline.Length) {
                # Append-only policy: truncation/rotation is treated as no delta for this run.
                $meta.diffMode = "truncated_or_rotated_no_output"
                $appendedText = ""
            }
            elseif ($afterLength -eq $Baseline.Length) {
                $meta.diffMode = "no_growth"
                $appendedText = ""
            }
            else {
                $meta.diffMode = "appended_from_baseline"
                $appendedText = Read-TextFromOffset -Path $Path -Offset $Baseline.Length
            }
        }

        if ($null -eq $appendedText) {
            $appendedText = ""
        }

        $meta.bytesWritten = [int64]([System.Text.Encoding]::UTF8.GetByteCount($appendedText))
        if ([string]::IsNullOrEmpty($appendedText)) {
            $meta.linesWritten = 0
        }
        else {
            $meta.linesWritten = [regex]::Split($appendedText, "`r?`n").Length
        }

        [System.IO.File]::WriteAllText($diffPath, $appendedText, (New-Object System.Text.UTF8Encoding($false)))
        Write-Info ("playerlog appended path: {0}" -f $diffPath)
    }
    catch {
        $meta.diffMode = "artifact_write_error"
        $meta.error = $_.Exception.Message
        Write-Info ("playerlog appended-lines capture failed: {0}" -f $_.Exception.Message)
    }
    finally {
        $metaJson = $meta | ConvertTo-Json -Depth 4
        Set-Content -Path $metaPath -Value $metaJson -Encoding UTF8
        Write-Info ("playerlog appended meta: {0}" -f $metaPath)
    }
}

if ([string]::IsNullOrWhiteSpace($ModRoot)) {
    $ModRoot = (Get-Location).Path
}
else {
    $ModRoot = (Resolve-Path $ModRoot).Path
}

$modRoot = $ModRoot
$buildScript = Join-Path $PSScriptRoot "build_runtime_suite_v2.ps1"
if (-not (Test-Path $buildScript)) {
    throw "Build script not found: $buildScript"
}

$runtimeRootCandidates = @(
    (Join-Path $modRoot "tests\runtime")
    Join-Path $modRoot "tests\runtime_v2"
)
$runtimeRoot = $null
foreach ($candidate in $runtimeRootCandidates) {
    if (Test-Path $candidate) {
        $runtimeRoot = $candidate
        break
    }
}
if ($null -eq $runtimeRoot) {
    throw "Runtime test root not found under ModRoot. expected one of: $($runtimeRootCandidates -join ', ')"
}

$modSrcCandidates = @(
    (Join-Path $runtimeRoot "src")
    Join-Path $runtimeRoot "ars\src"
)
$modSrcRoot = $null
foreach ($candidate in $modSrcCandidates) {
    if (Test-Path $candidate) {
        $modSrcRoot = $candidate
        break
    }
}
if ($null -eq $modSrcRoot) {
    throw "Runtime source root not found. expected one of: $($modSrcCandidates -join ', ')"
}

$artifactRoot = Join-Path $runtimeRoot "_artifacts"
$runId = Get-Date -Format "yyyyMMdd_HHmmss"
$runDir = Join-Path $artifactRoot $runId
$resultPath = Join-Path $runDir "result.json"
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "runtime-test-v2"
$tempRunDir = Join-Path $tempRoot $runId
$generatedPath = Join-Path $tempRunDir "runtime_suite_v2_generated.csx"
$artifactGeneratedPath = Join-Path $runDir "runtime_suite_v2_generated.csx"

New-Item -ItemType Directory -Path $runDir -Force | Out-Null
New-Item -ItemType Directory -Path $tempRunDir -Force | Out-Null

$resolvedPlayerLogPath = $null
$playerLogBaseline = [ordered]@{ Exists = $false; Length = 0L; LastWriteUtc = "" }
if (-not $DisablePlayerLogDiff) {
    $resolvedPlayerLogPath = Resolve-PlayerLogPath -ExplicitPath $PlayerLogPath
    if (-not [string]::IsNullOrWhiteSpace($resolvedPlayerLogPath)) {
        Write-Info ("playerlog path: {0}" -f $resolvedPlayerLogPath)
    }
    else {
        Write-Info "playerlog path: (not found, appended-lines capture skipped)"
    }
    $playerLogBaseline = Get-PlayerLogBaseline -Path $resolvedPlayerLogPath
}

$exitCode = 0
try {
    & $buildScript `
        -ModRoot $modRoot `
        -RuntimeRoot $runtimeRoot `
        -OutputPath $generatedPath `
        -ResultPath $resultPath `
        -RequiredNameContains $RequiredNameContains `
        -CaseId $CaseId `
        -Tag $Tag `
        -Suite $Suite

    Write-Info ("run_id: {0}" -f $runId)
    Write-Info ("runtime_root: {0}" -f $runtimeRoot)
    Write-Info ("mod_src_root: {0}" -f $modSrcRoot)
    Write-Info ("result path: {0}" -f $resultPath)

    Send-CwlCommand -Command "cwl.cs.is_ready" -NamedPipe $PipeName
    $generatedPathForCwl = $generatedPath -replace '\\', '/'
    Write-Info ("cwl.cs.file arg: {0}" -f $generatedPathForCwl)
    Send-CwlCommand -Command ("cwl.cs.file {0}" -f $generatedPathForCwl) -NamedPipe $PipeName

    if ($KeepGeneratedSource) {
        Copy-Item -Path $generatedPath -Destination $artifactGeneratedPath -Force
        Write-Info ("generated source kept: {0}" -f $artifactGeneratedPath)
    }
    else {
        Write-Info "generated source kept: false (use -KeepGeneratedSource to retain)"
    }

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
        $exitCode = 1
    }
    else {
        Write-Info "PASSED"
    }
}
catch {
    Write-Error $_
    $exitCode = 1
}
finally {
    if (-not $DisablePlayerLogDiff) {
        Write-PlayerLogDiffArtifacts `
            -Path $resolvedPlayerLogPath `
            -Baseline $playerLogBaseline `
            -RunDir $runDir
    }

    try {
        if (Test-Path $tempRunDir) {
            Remove-Item -Recurse -Force $tempRunDir
        }
    }
    catch {
        Write-Info ("temp cleanup failed: {0}" -f $_.Exception.Message)
    }
}

exit $exitCode
