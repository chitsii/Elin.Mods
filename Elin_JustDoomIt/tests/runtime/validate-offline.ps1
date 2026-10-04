[CmdletBinding()]
param(
    [string]$OutputRoot = '',
    [string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Elin',
    [string]$ProductDll = ''
)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$modRoot = Join-Path $repoRoot 'Elin_JustDoomIt'
if (!$OutputRoot) { $OutputRoot = Join-Path $repoRoot '.codex-build\Elin_JustDoomIt\runtime-offline' }
if (!$ProductDll) { $ProductDll = Join-Path $modRoot '_bin\Elin_JustDoomIt.dll' }
if (!(Test-Path -LiteralPath $ProductDll)) { throw "Compile-only PR2 product DLL required: $ProductDll" }
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$scriptPath = Join-Path $OutputRoot 'pr2-runtime.generated.csx'
& (Join-Path $repoRoot 'runtime-test-v2\runner\build_runtime_suite_v2.ps1') -ModRoot $modRoot -Suite smoke `
    -OutputPath $scriptPath -ResultPath (Join-Path $OutputRoot 'NOT_RUN.result.json') `
    -CaseId 'pr2.doom.cabinet_preservation'
if (!$?) { throw 'Runtime-v2 generation failed.' }
$source = Get-Content -LiteralPath $scriptPath -Raw
if ($source -match '__SOURCE_BLOCK__|__ENTRYPOINT_CLASS__') { throw 'Unexpanded runtime template.' }
# Only omit the CSX launch statements. All actual builder-composed declarations compile.
$declarations = [regex]::Replace($source, '(?m)^var runnerObject = new GameObject\("ArsRuntimeTestRunnerV2"\);\r?\nrunnerObject\.AddComponent<RuntimeTestRunnerV2>\(\);\s*$', '')
if ($declarations -eq $source) { throw 'Unexpected template entrypoint; do not silently drop test declarations.' }
$compilePath = Join-Path $OutputRoot 'pr2-runtime.generated.cs'
Set-Content -LiteralPath $compilePath -Value $declarations -Encoding UTF8
$refsRoot = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.netframework.referenceassemblies.net48\1.0.3\build\.NETFramework\v4.8'
if (!(Test-Path -LiteralPath $refsRoot)) { throw 'net48 reference assemblies must already be installed.' }
$sdkVersion = (& dotnet --version).Trim()
$compiler = Join-Path (Join-Path 'C:\Program Files\dotnet\sdk' $sdkVersion) 'Roslyn\bincore\csc.dll'
$arguments = @('/nologo', '/nostdlib+', '/target:library', '/langversion:latest', '/warn:4',
    ('/out:"' + (Join-Path $OutputRoot 'Pr2Doom.Runtime.CompileOnly.dll') + '"'))
$frameworkNames = @('mscorlib.dll','System.dll','System.Core.dll','System.Runtime.Serialization.dll',
    'System.Xml.dll','System.Xml.Linq.dll','Microsoft.CSharp.dll','System.Numerics.dll')
$references = @($frameworkNames | ForEach-Object { Get-Item -LiteralPath (Join-Path $refsRoot $_) })
$references += Get-Item -LiteralPath (Join-Path $refsRoot 'Facades\netstandard.dll')
$references += @(Get-ChildItem -LiteralPath (Join-Path $GameRoot 'Elin_Data\Managed') -Filter '*.dll' -File |
    Where-Object { $_.Name -notmatch '^(System\.|mscorlib\.|netstandard\.|Microsoft\.CSharp\.|Mono\.|I18N)' })
$references += @(Get-Item -LiteralPath (Join-Path $GameRoot 'BepInEx\core\0Harmony.dll'), $ProductDll)
$arguments += @($references | ForEach-Object { '/reference:"' + $_.FullName + '"' })
$arguments += '"' + $compilePath + '"'
$response = Join-Path $OutputRoot 'compile.rsp'
Set-Content -LiteralPath $response -Value $arguments -Encoding UTF8
& dotnet $compiler ('@' + $response)
if ($LASTEXITCODE -ne 0) { throw "Generated runtime source compile failed ($LASTEXITCODE)." }
$caseIds = @([regex]::Matches($declarations, 'string Id => "(pr2\.doom\.[^"]+)"') | ForEach-Object { $_.Groups[1].Value })
if ($caseIds.Count -ne 10 -or @($caseIds | Sort-Object -Unique).Count -ne 10) { throw 'Expected ten distinct PR2 cases.' }
if ($declarations -match 'yield return ctx\.Wait|UnpatchAll\(') { throw 'Unsafe child enumerator or broad unpatch detected.' }
[ordered]@{
    status = 'offline_compile_passed'; runtime_status = 'NOT_RUN'; case_ids = $caseIds
    generated_csx = $scriptPath; product_sha256 = (Get-FileHash -LiteralPath $ProductDll).Hash
    game_sha256 = (Get-FileHash -LiteralPath (Join-Path $GameRoot 'Elin_Data\Managed\Elin.dll')).Hash
    claims = @('actual builder output compiled with real DLLs', 'no game execution or deployment')
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputRoot 'offline-evidence.json') -Encoding UTF8
Write-Host 'PR2: generated source compiled; ten case IDs checked; runtime NOT_RUN.'
