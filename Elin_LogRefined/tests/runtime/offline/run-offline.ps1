[CmdletBinding()]
param([string]$OutputRoot = (Join-Path $env:TEMP 'pr5-owned-cleanup-offline'))
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$sdk = Get-ChildItem (Join-Path ${env:ProgramFiles} 'dotnet\sdk') -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$pack = Get-ChildItem (Join-Path ${env:ProgramFiles} 'dotnet\packs\Microsoft.NETCore.App.Ref') -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$major = ([version]$pack.Name).Major
$dll = Join-Path $OutputRoot 'OwnedCleanupGuardTests.dll'
$compilerArgs = [System.Collections.Generic.List[string]]::new()
foreach ($arg in @((Join-Path $sdk.FullName 'Roslyn\bincore\csc.dll'), '/noconfig', '/nostdlib+', '/target:exe', '/langversion:latest', '/warnaserror+', "/out:$dll")) { $compilerArgs.Add($arg) }
foreach ($ref in Get-ChildItem (Join-Path $pack.FullName "ref\net$major.0") -Filter '*.dll') { $compilerArgs.Add('/reference:' + $ref.FullName) }
$compilerArgs.Add((Join-Path $PSScriptRoot 'OwnedCleanupGuardTests.cs'))
$compilerArgs.Add((Join-Path $PSScriptRoot '..\src\cases\Pr5OwnedCleanupGuard.cs'))
& dotnet $compilerArgs.ToArray()
if ($LASTEXITCODE -ne 0) { throw 'Offline ownership guard compile failed.' }
@{ runtimeOptions = @{ tfm = "net$major.0"; framework = @{ name = 'Microsoft.NETCore.App'; version = $pack.Name } } } |
    ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputRoot 'OwnedCleanupGuardTests.runtimeconfig.json') -Encoding utf8
$result = & dotnet $dll 2>&1
$exitCode = $LASTEXITCODE
$result | Set-Content (Join-Path $OutputRoot 'ownership-guard-tests.log') -Encoding utf8
$result | Write-Output
if ($exitCode -ne 0) { throw "Offline ownership guard counterexamples failed ($exitCode)." }
