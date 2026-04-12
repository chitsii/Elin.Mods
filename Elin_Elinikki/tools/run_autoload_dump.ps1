param(
    [Parameter(Mandatory = $true)]
    [string]$SaveId,

    [switch]$Cloud,

    [switch]$RestartGame,

    [switch]$KeepGameOpen,

    [double]$DumpDelaySeconds = 1.5,

    [int]$TimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageDir = "C:\Program Files (x86)\Steam\steamapps\common\Elin\Package\Elin_Elinikki"
$gameExe = "C:\Program Files (x86)\Steam\steamapps\common\Elin\Elin.exe"
$requestPath = Join-Path $packageDir "automation_request.json"
$resultPath = Join-Path $packageDir "automation_result.json"

if (-not (Test-Path $packageDir)) {
    throw "Package directory not found: $packageDir"
}

if (-not (Test-Path $gameExe)) {
    throw "Game executable not found: $gameExe"
}

if (Test-Path $resultPath) {
    Remove-Item $resultPath -Force
}

$requestJson = @"
{
  "save_id": "$SaveId",
  "cloud": $($Cloud.IsPresent.ToString().ToLowerInvariant()),
  "auto_dump": true,
  "close_game_after_dump": $((!$KeepGameOpen.IsPresent).ToString().ToLowerInvariant()),
  "dump_delay_seconds": $DumpDelaySeconds,
  "load_timeout_seconds": 20,
  "dump_timeout_seconds": 20
}
"@

Set-Content -Path $requestPath -Value $requestJson -Encoding UTF8

$existing = Get-Process -Name "Elin" -ErrorAction SilentlyContinue
if ($RestartGame -and $existing) {
    $existing | Stop-Process -Force
    $waitDeadline = (Get-Date).AddSeconds(15)
    do {
        Start-Sleep -Milliseconds 500
        $existing = Get-Process -Name "Elin" -ErrorAction SilentlyContinue
    } while ($existing -and (Get-Date) -lt $waitDeadline)

    if ($existing) {
        throw "Elin process did not exit within 15 seconds."
    }
}

if (-not (Get-Process -Name "Elin" -ErrorAction SilentlyContinue)) {
    Start-Process -FilePath $gameExe | Out-Null
}

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    Get-Process -Name "Elin" -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -eq "Fatal error" } |
        Stop-Process -Force

    if (Test-Path $resultPath) {
        Get-Content $resultPath
        exit 0
    }

    Start-Sleep -Milliseconds 500
}

throw "Timed out waiting for automation result: $resultPath"
