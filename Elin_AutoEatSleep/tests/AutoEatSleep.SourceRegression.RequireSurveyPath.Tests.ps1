param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'AutoEatSleep.SourceRegression.ps1'
$previous = [Environment]::GetEnvironmentVariable('ELIN_API_SURVEY_CS', 'Process')
try {
    [Environment]::SetEnvironmentVariable('ELIN_API_SURVEY_CS', $null, 'Process')
    $output = & cmd /c powershell -NoProfile -ExecutionPolicy Bypass -File "$scriptPath" -RepoRoot "$RepoRoot" 2`>`&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        throw 'FAIL: SourceRegression should require -ApiSurveyRoot or ELIN_API_SURVEY_CS when neither is set.'
    }

    $message = ($output | Out-String)
    if ($message -notmatch 'ApiSurveyRoot' -or $message -notmatch 'ELIN_API_SURVEY_CS') {
        throw "FAIL: Missing guidance for ApiSurveyRoot/ELIN_API_SURVEY_CS. Output was: $message"
    }

    Write-Host 'AutoEatSleep source regression path requirement checks passed.'
}
finally {
    [Environment]::SetEnvironmentVariable('ELIN_API_SURVEY_CS', $previous, 'Process')
}
