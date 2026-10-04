[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [string]$RuntimeRoot = $PSScriptRoot,
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [Parameter(Mandatory = $true)][string]$NiCommentDll
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path
$RuntimeRoot = (Resolve-Path $RuntimeRoot).Path
$NiCommentDll = (Resolve-Path $NiCommentDll).Path
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$csxPath = Join-Path $OutputRoot 'pr7-runtime.generated.csx'
$csPath = Join-Path $OutputRoot 'pr7-runtime.compile-only.cs'
$dllPath = Join-Path $OutputRoot 'pr7-runtime.compile-only.dll'
& (Join-Path $RepoRoot 'runtime-test-v2\runner\build_runtime_suite_v2.ps1') `
    -ModRoot (Join-Path $RepoRoot 'Elin_NiComment') -RuntimeRoot $RuntimeRoot -Suite smoke `
    -OutputPath $csxPath -ResultPath (Join-Path $OutputRoot 'NOT_EXECUTED.runtime-result.json') `
    -RequiredNameContains RUNTIME_TEST -Tag pr7
$generated = Get-Content -LiteralPath $csxPath -Raw
$entry = 'var runnerObject = new GameObject("ArsRuntimeTestRunnerV2");'
if (-not $generated.Contains($entry)) { throw 'Shared entry changed; inspect compile transform.' }
$compile = $generated.Replace($entry, 'public static class NiPr7CompileOnlyEntry { public static void NeverRun() { ' + $entry)
$compile += "`r`n} }`r`n"
[System.IO.File]::WriteAllText($csPath, $compile, [System.Text.UTF8Encoding]::new($false))
$sdk = Get-ChildItem (Join-Path ${env:ProgramFiles} 'dotnet\sdk') -Directory |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$compiler = Join-Path $sdk.FullName 'Roslyn\bincore\csc.dll'
$refRoot = Join-Path $RepoRoot '.codex-build\nuget-packages\microsoft.netframework.referenceassemblies.net472\1.0.2\build\.NETFramework\v4.7.2'
$compilerArgs = [System.Collections.Generic.List[string]]::new()
$referencePaths = [System.Collections.Generic.List[string]]::new()
foreach ($arg in @($compiler, '/noconfig', '/nostdlib+', '/target:library', '/langversion:latest', '/warnaserror+', "/out:$dllPath")) { $compilerArgs.Add($arg) }
foreach ($file in Get-ChildItem $refRoot -Filter '*.dll' -File) {
    if ($file.Name -notmatch '^System\.EnterpriseServices\.(Wrapper|Thunk)\.dll$') {
        $compilerArgs.Add('/reference:' + $file.FullName)
        $referencePaths.Add($file.FullName)
    }
}
$testProject = Join-Path $RepoRoot 'Elin_NiComment\tests\Elin_NiComment.Tests\Elin_NiComment.Tests.csproj'
[xml]$project = Get-Content $testProject -Raw
foreach ($reference in $project.SelectNodes('//Reference[HintPath]')) {
    $hint = [string]$reference.HintPath
    $path = if ([System.IO.Path]::IsPathRooted($hint)) { $hint } else { Join-Path (Split-Path $testProject) $hint }
    $resolvedReference = (Resolve-Path $path).Path
    $compilerArgs.Add('/reference:' + $resolvedReference)
    $referencePaths.Add($resolvedReference)
}
$compilerArgs.Add('/reference:' + $NiCommentDll)
$referencePaths.Add($NiCommentDll)
$compilerArgs.Add($csPath)
$output = & dotnet $compilerArgs.ToArray() 2>&1
$compilerExit = $LASTEXITCODE
$output | Set-Content (Join-Path $OutputRoot 'compile.log') -Encoding utf8
$output | Write-Output
if ($compilerExit -ne 0) { throw "PR7 generated suite compile failed ($compilerExit)." }
# Compile the unmodified generated script as a Roslyn submission; never evaluate it.
$roslynRoot = Join-Path $sdk.FullName 'Roslyn\bincore'
Add-Type -Path (Join-Path $roslynRoot 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $roslynRoot 'Microsoft.CodeAnalysis.CSharp.dll')
$metadata = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
foreach ($path in $referencePaths) { $metadata.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path)) }
$parseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithKind([Microsoft.CodeAnalysis.SourceCodeKind]::Script)
$tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($generated, $parseOptions, $csxPath)
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::CreateScriptCompilation('NiPr7GeneratedScript', $tree, $metadata, $options)
$scriptDll = Join-Path $OutputRoot 'pr7-runtime.script-compile-only.dll'
$stream = [System.IO.File]::Create($scriptDll)
try { $emitted = $compilation.Emit($stream) } finally { $stream.Dispose() }
$emitted.Diagnostics | ForEach-Object ToString | Sort-Object -Unique | Set-Content (Join-Path $OutputRoot 'script-compile.log') -Encoding utf8
if (-not $emitted.Success) { throw 'Unmodified generated csx submission failed to compile; see script-compile.log.' }
$ownershipRoot = Join-Path $OutputRoot 'ownership'
New-Item -ItemType Directory -Path $ownershipRoot -Force | Out-Null
$ownershipExe = Join-Path $ownershipRoot 'NiPr7OwnershipChecks.exe'
$checkArgs = [System.Collections.Generic.List[string]]::new()
foreach ($arg in @($compiler, '/noconfig', '/nostdlib+', '/target:exe', '/langversion:latest', '/warnaserror+', "/out:$ownershipExe")) { $checkArgs.Add($arg) }
foreach ($path in $referencePaths) { $checkArgs.Add('/reference:' + $path) }
$checkArgs.Add('/reference:' + $dllPath)
$checkArgs.Add((Join-Path $RuntimeRoot 'offline\OwnershipChecks.cs'))
$checkOutput = & dotnet $checkArgs.ToArray() 2>&1
if ($LASTEXITCODE -ne 0) { $checkOutput | Write-Output; throw 'Offline ownership counterexample fixture compile failed.' }
foreach ($path in $referencePaths) {
    if (-not $path.StartsWith($refRoot, [System.StringComparison]::OrdinalIgnoreCase)) { Copy-Item -LiteralPath $path -Destination $ownershipRoot -Force }
}
Copy-Item -LiteralPath $dllPath -Destination $ownershipRoot -Force
$checkOutput = & $ownershipExe 2>&1
$ownershipExit = $LASTEXITCODE
$checkOutput | Set-Content (Join-Path $OutputRoot 'ownership-checks.log') -Encoding utf8
$checkOutput | Write-Output
if ($ownershipExit -ne 0) { throw 'Offline ownership counterexample checks failed.' }
@{
    status = 'compile_only_passed'; runtimeExecuted = $false
    cases = @('pr7.ni.death_real','pr7.ni.quest_real','pr7.ni.death_reentry_exception','pr7.ni.quest_reentry_exception')
    generatedCsx = $csxPath; compiledAssembly = $dllPath
    scriptAssembly = $scriptDll
    ownershipChecks = 7
    scriptWarningIds = @($emitted.Diagnostics | Where-Object Severity -eq Warning | ForEach-Object Id | Sort-Object -Unique)
    nativeGameSha256 = (Get-FileHash (Join-Path $RepoRoot 'Elin_NiComment\elin_link\Elin_Data\Managed\Elin.dll')).Hash
    niCommentSha256 = (Get-FileHash $NiCommentDll).Hash
    sourceSha256 = (Get-FileHash (Join-Path $RuntimeRoot 'src\cases\Pr7LifecycleIntegrationCases.cs')).Hash
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputRoot 'compile-summary.json') -Encoding utf8
Write-Host 'PR7 generated csx compile passed; gameplay assertions were NOT executed.'
