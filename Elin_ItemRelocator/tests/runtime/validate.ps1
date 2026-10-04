[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [Parameter(Mandatory = $true)][string]$ModDll,
    [string]$GameRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$modRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$repoRoot = (Resolve-Path (Join-Path $modRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($GameRoot)) {
    $GameRoot = (Resolve-Path (Join-Path $modRoot "elin_link")).Path
}
$ModDll = (Resolve-Path $ModDll).Path
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$OutputRoot = (Resolve-Path $OutputRoot).Path
$script = Join-Path $OutputRoot "pr9-runtime.csx"
$result = Join-Path $OutputRoot "pr9-runtime-result.json"
$artifacts = Join-Path $OutputRoot "compiler"
$project = Join-Path $PSScriptRoot "tools\CsxValidation\CsxValidation.csproj"
$previousTemp = $env:TEMP
$previousTmp = $env:TMP
try {
    $env:TEMP = $OutputRoot
    $env:TMP = $OutputRoot
    & (Join-Path $repoRoot "runtime-test-v2\runner\build_runtime_suite_v2.ps1") `
        -ModRoot $modRoot -OutputPath $script -ResultPath $result -Suite smoke -Tag integration
    # Restore against an empty local source; this tool has no NuGet packages.
    & dotnet restore $project --source $OutputRoot --artifacts-path $artifacts
    if ($LASTEXITCODE -ne 0) { throw "Offline validator restore failed." }
    & dotnet run --project $project --no-restore --artifacts-path $artifacts `
        /p:UseSharedCompilation=false -- $script $GameRoot $ModDll
    if ($LASTEXITCODE -ne 0) { throw "Generated script compilation failed." }
}
finally {
    $env:TEMP = $previousTemp
    $env:TMP = $previousTmp
}
