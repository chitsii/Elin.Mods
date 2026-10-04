[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$OutputRoot = (Join-Path $env:TEMP "elin-pr5-runtime-compile")
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
}
$RepoRoot = (Resolve-Path $RepoRoot).Path
$modRoot = Join-Path $RepoRoot "Elin_LogRefined"
$runtimeRoot = Join-Path $modRoot "tests\runtime"
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$csxPath = Join-Path $OutputRoot "pr5-runtime.generated.csx"
$csPath = Join-Path $OutputRoot "pr5-runtime.generated.cs"
$dllPath = Join-Path $OutputRoot "pr5-runtime.compile-only.dll"
& (Join-Path $RepoRoot "runtime-test-v2\runner\build_runtime_suite_v2.ps1") `
    -ModRoot $modRoot -RuntimeRoot $runtimeRoot -OutputPath $csxPath `
    -ResultPath (Join-Path $OutputRoot "NOT_EXECUTED.runtime-result.json") `
    -RequiredNameContains "RUNTIME_TEST" -Tag "pr5" -Suite smoke
$generated = Get-Content -LiteralPath $csxPath -Raw
$entry = 'var runnerObject = new GameObject("ArsRuntimeTestRunnerV2");'
if (-not $generated.Contains($entry)) { throw "Shared generated entry changed; inspect compile transform." }
$compile = $generated.Replace($entry, 'public static class Pr5CompileOnlyEntry { public static void NeverRun() { ' + $entry)
$compile += "`r`n} }`r`n"
[System.IO.File]::WriteAllText($csPath, $compile, [System.Text.UTF8Encoding]::new($false))
$source = Get-Content (Join-Path $runtimeRoot "src\cases\Pr5ConditionIntegrationCases.cs") -Raw
$caseIds = @("pr5.log.condition_outcomes", "pr5.log.capped_native_stack", "pr5.log.reentry_owner_isolation", "pr5.log.exception_finalizer")
foreach ($id in $caseIds) {
    if (-not $generated.Contains($id)) { throw "Generated suite lacks case: $id" }
}
if ($source -match '(?m)^\s*using\s') { throw "Case source depends on using directives stripped by builder." }
if ($source -match 'yield\s+return|Msg\.ignoreAll\s*=|Msg\.Clear\(|UnpatchAll|PatchChara\.(Prefix|Postfix|Finalizer)\(') {
    throw "Case source contains forbidden global-message/patch or child-coroutine operation."
}
$sdkRoot = Join-Path ${env:ProgramFiles} "dotnet\sdk"
$sdk = Get-ChildItem $sdkRoot -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$compiler = Join-Path $sdk.FullName "Roslyn\bincore\csc.dll"
$refRoot = Join-Path $RepoRoot ".codex-build\nuget-packages\microsoft.netframework.referenceassemblies.net472\1.0.2\build\.NETFramework\v4.7.2"
if (-not (Test-Path (Join-Path $refRoot "mscorlib.dll"))) { throw "Existing net472 reference package missing." }
$argsList = [System.Collections.Generic.List[string]]::new()
foreach ($arg in @($compiler, '/noconfig', '/nostdlib+', '/target:library', '/langversion:latest', '/warnaserror+', "/out:$dllPath")) { $argsList.Add($arg) }
foreach ($file in Get-ChildItem $refRoot -Filter '*.dll' -File) {
    if ($file.Name -notmatch '^System\.EnterpriseServices\.(Wrapper|Thunk)\.dll$') {
        $argsList.Add('/reference:' + $file.FullName)
    }
}
[xml]$project = Get-Content (Join-Path $modRoot "tests\Elin_LogRefined.Tests\Elin_LogRefined.Tests.csproj") -Raw
$projectDir = Join-Path $modRoot "tests\Elin_LogRefined.Tests"
foreach ($reference in $project.SelectNodes('//Reference[HintPath]')) {
    $hint = [string]$reference.HintPath
    $path = if ([System.IO.Path]::IsPathRooted($hint)) { $hint } else { Join-Path $projectDir $hint }
    $argsList.Add('/reference:' + (Resolve-Path $path).Path)
}
$argsList.Add($csPath)
$output = & dotnet $argsList.ToArray() 2>&1
$exitCode = $LASTEXITCODE
$output | Set-Content (Join-Path $OutputRoot "compile.log") -Encoding utf8
$output | Write-Output
if ($exitCode -ne 0) { throw "Generated PR5 suite compile failed ($exitCode)." }
$cecilPath = Join-Path $modRoot "elin_link\BepInEx\core\Mono.Cecil.dll"
Add-Type -Path $cecilPath
$gameAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $modRoot "elin_link\Elin_Data\Managed\Elin.dll"))
$compiledAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dllPath)
try {
    $native = $gameAssembly.MainModule.Types | Where-Object { $_.FullName -eq 'Chara' }
    $add = @($native.Methods | Where-Object {
        $_.Name -eq 'AddCondition' -and $_.Parameters.Count -eq 2 -and
        $_.Parameters[0].ParameterType.FullName -eq 'Condition' -and $_.Parameters[1].ParameterType.FullName -eq 'System.Boolean'
    })
    if ($add.Count -ne 1) { throw "Native condition/bool target is ambiguous or absent." }
    $nativeCalls = @($add[0].Body.Instructions | Where-Object {
        $_.Operand -is [Mono.Cecil.MethodReference]
    } | ForEach-Object { $_.Operand.FullName } | Sort-Object -Unique)
    foreach ($name in @('OnStacked', 'OnStartOrStack', 'TryNullify', 'Kill')) {
        if (-not ($nativeCalls | Where-Object { $_ -match ('::' + $name + '\(') })) { throw "Native AddCondition no longer calls $name" }
    }
    $wet = $gameAssembly.MainModule.Types | Where-Object { $_.FullName -eq 'ConWet' }
    if ($wet.Methods.Name -contains 'OnStacked' -or $wet.Methods.Name -contains 'OnStartOrStack') {
        throw "ConWet overrides observed methods; update test-only targets before game run."
    }
    $caseTypes = @($compiledAssembly.MainModule.Types | Where-Object {
        $_.Name -in @('Pr5ConditionOutcomesCase', 'Pr5ConditionCappedStackCase', 'Pr5ConditionReentryCase', 'Pr5ConditionExceptionCase')
    })
    if ($caseTypes.Count -ne 4) { throw "Compiled suite does not contain all four concrete PR5 cases." }
    $nativeCalls | Set-Content (Join-Path $OutputRoot "native-addcondition-calls.txt") -Encoding utf8
}
finally {
    $compiledAssembly.Dispose()
    $gameAssembly.Dispose()
}
@{
    status = "compile_only_passed"
    runtimeExecuted = $false
    cases = $caseIds
    generatedSource = $csxPath
    compiledAssembly = $dllPath
    nativeAddConditionCallsVerified = @('OnStacked', 'OnStartOrStack', 'TryNullify', 'Kill')
    sourceSha256 = (Get-FileHash (Join-Path $runtimeRoot "src\cases\Pr5ConditionIntegrationCases.cs")).Hash
    cleanupGuardSha256 = (Get-FileHash (Join-Path $runtimeRoot "src\cases\Pr5OwnedCleanupGuard.cs")).Hash
    gameDllSha256 = (Get-FileHash (Join-Path $modRoot "elin_link\Elin_Data\Managed\Elin.dll")).Hash
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputRoot "compile-summary.json") -Encoding utf8
Write-Host "PR5 generated-source compile passed; game/runtime assertions NOT executed."
