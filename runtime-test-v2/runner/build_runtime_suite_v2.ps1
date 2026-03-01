[CmdletBinding()]
param(
    [string]$ModRoot = "",
    [string]$RuntimeRoot = "",
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [Parameter(Mandatory = $true)]
    [string]$ResultPath,
    [string]$RequiredNameContains = "RUNTIME_TEST",
    [string]$CaseId = "",
    [string]$Tag = "",
    [ValidateSet("drama", "smoke")]
    [string]$Suite = "drama"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ModRoot)) {
    $ModRoot = (Get-Location).Path
}
else {
    $ModRoot = (Resolve-Path $ModRoot).Path
}

$resolvedRuntimeRoot = $null
if ([string]::IsNullOrWhiteSpace($RuntimeRoot)) {
    $runtimeRootCandidates = @(
        (Join-Path $ModRoot "tests\runtime")
        Join-Path $ModRoot "tests\runtime_v2"
    )
    foreach ($candidate in $runtimeRootCandidates) {
        if (Test-Path $candidate) {
            $resolvedRuntimeRoot = $candidate
            break
        }
    }
}
else {
    $resolvedRuntimeRoot = (Resolve-Path $RuntimeRoot).Path
}

if ($null -eq $resolvedRuntimeRoot) {
    throw "Runtime test root not found under ModRoot"
}

$kitRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sharedTemplatePath = Join-Path $kitRoot "framework\suites\runtime_suite_v2_template.csx"
$templatePath = Join-Path $resolvedRuntimeRoot "suites\runtime_suite_v2_template.csx"
if (-not (Test-Path $templatePath)) {
    $templatePath = Join-Path $resolvedRuntimeRoot "ars\suites\runtime_suite_v2_template.csx"
}
if (-not (Test-Path $templatePath)) {
    $templatePath = $sharedTemplatePath
}

if (-not (Test-Path $templatePath)) {
    throw "Template not found: $templatePath"
}

$sharedSrcRoot = Join-Path $kitRoot "framework\src"
$modSrcRoot = Join-Path $resolvedRuntimeRoot "src"
if (-not (Test-Path $modSrcRoot)) {
    $modSrcRoot = Join-Path $resolvedRuntimeRoot "ars\src"
}

if (-not (Test-Path $sharedSrcRoot)) {
    throw "Shared source root not found: $sharedSrcRoot"
}

if (-not (Test-Path $modSrcRoot)) {
    throw "Mod source root not found: $modSrcRoot"
}

$files = New-Object System.Collections.Generic.List[System.IO.FileInfo]
$sharedOrder = @("core", "compat", "drama")
foreach ($segment in $sharedOrder) {
    $dir = Join-Path $sharedSrcRoot $segment
    if (-not (Test-Path $dir)) {
        continue
    }
    Get-ChildItem -Path $dir -Filter *.cs -File | Sort-Object Name | ForEach-Object { [void]$files.Add($_) }
}

$modOrder = @("cases", "drama", "catalog")
foreach ($segment in $modOrder) {
    $dir = Join-Path $modSrcRoot $segment
    if (-not (Test-Path $dir)) {
        continue
    }
    Get-ChildItem -Path $dir -Filter *.cs -File | Sort-Object Name | ForEach-Object { [void]$files.Add($_) }
}

if ($files.Count -eq 0) {
    throw "No source files found under: $sharedSrcRoot and $modSrcRoot"
}

$sourceBuilder = New-Object System.Text.StringBuilder
foreach ($file in $files) {
    [void]$sourceBuilder.AppendLine("// ----- source: $($file.FullName)")
    $raw = Get-Content -Path $file.FullName -Raw
    $normalized = [regex]::Replace($raw, '(?m)^\s*using\s+[^\r\n;]+;\s*\r?\n', '')
    [void]$sourceBuilder.AppendLine($normalized)
    [void]$sourceBuilder.AppendLine()
}

$template = Get-Content -Path $templatePath -Raw
$entryClass = if ($Suite -eq "smoke") { "RuntimeTestRunnerV2" } else { "DramaRuntimeRunnerV2" }
$script = $template.
    Replace("__SOURCE_BLOCK__", $sourceBuilder.ToString()).
    Replace("__RESULT_PATH__", $ResultPath).
    Replace("__REQUIRED_NAME_CONTAINS__", $RequiredNameContains.Replace('\', '\\').Replace('"', '\"')).
    Replace("__MOD_ROOT__", $ModRoot).
    Replace("__CASE_ID_FILTER__", $CaseId.Replace('\', '\\').Replace('"', '\"')).
    Replace("__TAG_FILTER__", $Tag.Replace('\', '\\').Replace('"', '\"')).
    Replace("__ENTRYPOINT_CLASS__", $entryClass)

$outDir = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrEmpty($outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

Set-Content -Path $OutputPath -Value $script -Encoding UTF8 -NoNewline
Write-Host "[runtime-v2] generated csx: $OutputPath"
