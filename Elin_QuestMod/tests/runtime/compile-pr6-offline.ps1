[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$OutputRoot = (Join-Path $env:TEMP "elin-pr6-runtime-compile")
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path }
$RepoRoot = (Resolve-Path $RepoRoot).Path
$modRoot = Join-Path $RepoRoot "Elin_QuestMod"
$runtimeRoot = Join-Path $modRoot "tests\runtime"
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
@{ status = 'compile_started'; runtimeExecuted = $false } | ConvertTo-Json | Set-Content (Join-Path $OutputRoot 'compile-summary.json') -Encoding utf8
$csxPath = Join-Path $OutputRoot "pr6-runtime.generated.csx"
$csPath = Join-Path $OutputRoot "pr6-runtime.compile-only.cs"
$dllPath = Join-Path $OutputRoot "pr6-runtime.compile-only.dll"
& (Join-Path $RepoRoot "runtime-test-v2\runner\build_runtime_suite_v2.ps1") `
    -ModRoot $modRoot -RuntimeRoot $runtimeRoot -OutputPath $csxPath `
    -ResultPath (Join-Path $OutputRoot "NOT_EXECUTED.runtime-result.json") `
    -RequiredNameContains "RUNTIME_TEST" -Tag pr6 -Suite smoke
$generated = Get-Content -LiteralPath $csxPath -Raw
$entry = 'var runnerObject = new GameObject("ArsRuntimeTestRunnerV2");'
if (-not $generated.Contains($entry)) { throw "Shared csx entry changed; inspect offline transform." }
$compile = $generated.Replace($entry, 'public static class Pr6CompileOnlyEntry { public static void NeverRun() { ' + $entry)
$compile += "`r`n} }`r`n"
[System.IO.File]::WriteAllText($csPath, $compile, [System.Text.UTF8Encoding]::new($false))
$sdk = Get-ChildItem (Join-Path ${env:ProgramFiles} "dotnet\sdk") -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$compiler = Join-Path $sdk.FullName "Roslyn\bincore\csc.dll"
$refRoot = Join-Path $RepoRoot ".codex-build\nuget-packages\microsoft.netframework.referenceassemblies.net472\1.0.2\build\.NETFramework\v4.7.2"
if (-not (Test-Path (Join-Path $refRoot "mscorlib.dll"))) { throw "Existing net472 reference package missing." }
$argsList = [System.Collections.Generic.List[string]]::new()
foreach ($arg in @($compiler, '/noconfig', '/nostdlib+', '/target:library', '/langversion:latest', '/warnaserror+', "/out:$dllPath")) { $argsList.Add($arg) }
foreach ($file in Get-ChildItem $refRoot -Filter '*.dll' -File) {
    if ($file.Name -notmatch '^System\.EnterpriseServices\.(Wrapper|Thunk)\.dll$') { $argsList.Add('/reference:' + $file.FullName) }
}
$refs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
[xml]$project = Get-Content (Join-Path $modRoot "Elin_QuestMod.csproj") -Raw
$ns = [System.Xml.XmlNamespaceManager]::new($project.NameTable)
$ns.AddNamespace('m', $project.DocumentElement.NamespaceURI)
foreach ($ref in $project.SelectNodes('//m:Reference[m:HintPath]', $ns)) {
    [void]$refs.Add((Resolve-Path (Join-Path $modRoot ([string]$ref.HintPath))).Path)
}
$managed = Join-Path $modRoot "elin_link\Elin_Data\Managed"
foreach ($file in Get-ChildItem $managed -Filter 'UnityEngine.*Module.dll' -File) { [void]$refs.Add($file.FullName) }
foreach ($name in @('UnityEngine.UI.dll', 'Plugins.Sound.dll', 'Plugins.Tween.dll', 'Plugins.Serialization.dll', 'Newtonsoft.Json.dll')) {
    $path = Join-Path $managed $name
    if (Test-Path $path) { [void]$refs.Add($path) }
}
foreach ($ref in $refs) { $argsList.Add('/reference:' + $ref) }
$argsList.Add($csPath)
$responsePath = Join-Path $OutputRoot 'compile.rsp'
$responseArgs = @($argsList | Select-Object -Skip 2 | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' })
[System.IO.File]::WriteAllLines($responsePath, $responseArgs, [System.Text.UTF8Encoding]::new($false))
$output = & dotnet $compiler /noconfig ('@' + $responsePath) 2>&1
$exitCode = $LASTEXITCODE
$output | Set-Content (Join-Path $OutputRoot "compile.log") -Encoding utf8
$output | Write-Output
if ($exitCode -ne 0) { throw "Generated PR6 suite compile failed ($exitCode)." }
# Also compile the untouched csx as a submission; never evaluate its entry point.
Add-Type -Path (Join-Path $sdk.FullName 'Roslyn\bincore\Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $sdk.FullName 'Roslyn\bincore\Microsoft.CodeAnalysis.CSharp.dll')
$metadata = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
foreach ($arg in $argsList) {
    if ($arg.StartsWith('/reference:')) {
        $metadata.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($arg.Substring(11)))
    }
}
$parseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithKind([Microsoft.CodeAnalysis.SourceCodeKind]::Script)
$tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($generated, $parseOptions, $csxPath, [System.Text.Encoding]::UTF8, [System.Threading.CancellationToken]::None)
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary).WithGeneralDiagnosticOption([Microsoft.CodeAnalysis.ReportDiagnostic]::Error)
$options = $options.WithAssemblyIdentityComparer([Microsoft.CodeAnalysis.DesktopAssemblyIdentityComparer]::Default)
$submission = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::CreateScriptCompilation('Pr6RuntimeScriptCompileOnly', $tree, $metadata, $options, $null, $null, $null)
$scriptDllPath = Join-Path $OutputRoot 'pr6-runtime.script-compile-only.dll'
$stream = [System.IO.File]::Create($scriptDllPath)
try { $emit = $submission.Emit($stream, $null, $null, $null, $null, $null, [System.Threading.CancellationToken]::None) }
finally { $stream.Dispose() }
$diagnosticLines = @($emit.Diagnostics | ForEach-Object ToString | Sort-Object -Unique)
[System.IO.File]::WriteAllLines((Join-Path $OutputRoot 'script-compile.log'), [string[]]$diagnosticLines, [System.Text.UTF8Encoding]::new($false))
if (-not $emit.Success) { $emit.Diagnostics | ForEach-Object ToString | Sort-Object -Unique | Select-Object -First 20 | Write-Output; throw 'Untouched csx Script compilation failed.' }
$guardProject = Join-Path $runtimeRoot 'managed\Pr6MediaCleanupGuardTests.csproj'
$guardObj = Join-Path $OutputRoot 'managed-obj\'
$guardOut = Join-Path $OutputRoot 'managed-out\'
$guardBuild = & dotnet build $guardProject --nologo ("-p:BaseIntermediateOutputPath=$guardObj") ("-p:MSBuildProjectExtensionsPath=$guardObj") ("-p:OutputPath=$guardOut") 2>&1
$guardBuild | Set-Content (Join-Path $OutputRoot 'managed-build.log') -Encoding utf8
if ($LASTEXITCODE -ne 0) { $guardBuild | Write-Output; throw 'Managed cleanup guard build failed.' }
$guardResult = & dotnet (Join-Path $guardOut 'Pr6MediaCleanupGuardTests.dll') 2>&1
$guardExit = $LASTEXITCODE
$guardResult | Set-Content (Join-Path $OutputRoot 'managed-counterexamples.log') -Encoding utf8
$guardResult | Write-Output
if ($guardExit -ne 0) { throw 'Managed cleanup counterexamples failed.' }
Add-Type -Path (Join-Path $modRoot "elin_link\BepInEx\core\Mono.Cecil.dll")
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed "Elin.dll"))
$soundAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed 'Plugins.Sound.dll'))
$compiled = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dllPath)
try {
    # Characterize only managed native dispatch, never Unity playback/hardware output.
    $managerType = $soundAssembly.MainModule.Types | Where-Object FullName -EQ SoundManager
    $sourceType = $soundAssembly.MainModule.Types | Where-Object FullName -EQ SoundSource
    $managerPlay = @($managerType.Methods | Where-Object { $_.Name -eq '_Play' -and $_.Parameters.Count -eq 3 -and $_.Parameters[0].ParameterType.FullName -eq 'SoundData' })
    $sourcePlay = @($sourceType.Methods | Where-Object { $_.Name -eq 'Play' -and $_.Parameters.Count -eq 3 -and $_.Parameters[0].ParameterType.FullName -eq 'SoundData' })
    if ($managerPlay.Count -ne 1 -or $sourcePlay.Count -ne 1) { throw 'Native sound dispatch targets changed.' }
    $audioEvidence = [System.Collections.Generic.List[string]]::new()
    foreach ($method in @($managerPlay[0], $sourcePlay[0])) {
        $callOperands = @($method.Body.Instructions | Where-Object {
            $_.Operand -is [Mono.Cecil.MethodReference] -and $_.OpCode.Code.ToString() -in @('Call', 'Callvirt')
        } | ForEach-Object Operand)
        $forbidden = @($callOperands | Where-Object {
            ($_.DeclaringType.FullName -eq 'UnityEngine.AudioListener' -and $_.Name -eq 'get_volume') -or
            ($_.DeclaringType.FullName -eq 'UnityEngine.Application' -and $_.Name -eq 'get_isFocused')
        })
        if ($forbidden.Count -ne 0) { throw ('Native sound has a direct listener-volume/focus dependency: ' + $method.FullName) }
        $target = if ($method.DeclaringType.FullName -eq 'SoundManager') { 'SoundSource' } else { 'UnityEngine.AudioSource' }
        $playCalls = @($callOperands | Where-Object { $_.DeclaringType.FullName -eq $target -and $_.Name -eq 'Play' })
        if ($playCalls.Count -ne 1) { throw ('Native audio Play call chain changed: ' + $method.FullName) }
        $audioEvidence.Add($method.FullName + ' -> ' + $playCalls[0].FullName + '; direct_listener_volume/focus_calls=0; runtime_not_executed')
    }
    $audioEvidence | Set-Content (Join-Path $OutputRoot 'native-audio-call-chain.txt') -Encoding utf8
    $card = $game.MainModule.Types | Where-Object FullName -EQ Card
    $native = @($card.Methods | Where-Object { $_.Name -in @('PlayEffect', 'PlaySound') -and $_.Parameters[0].ParameterType.FullName -eq 'System.String' })
    if ($native.Count -ne 2) { throw "Native string media overload count changed." }
    $effectType = $game.MainModule.Types | Where-Object FullName -EQ Effect
    $activate = @($effectType.Methods | Where-Object { $_.Name -eq 'Activate' -and $_.Parameters.Count -eq 0 -and $_.IsFamily })
    $activationPaths = @($effectType.Methods | Where-Object {
        ($_.Name -eq '_Play' -and $_.Parameters.Count -eq 5) -or
        ($_.Name -eq 'Play' -and $_.Parameters.Count -eq 1 -and $_.Parameters[0].ParameterType.FullName -eq 'UnityEngine.Vector3')
    })
    if ($activate.Count -ne 1 -or $activationPaths.Count -ne 2) { throw 'Native Effect activation targets changed.' }
    foreach ($path in $activationPaths) {
        $calls = @($path.Body.Instructions | Where-Object {
            $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.DeclaringType.FullName -eq 'Effect' -and
            $_.Operand.Name -eq 'Activate' -and $_.Operand.Parameters.Count -eq 0
        })
        if ($calls.Count -ne 1) { throw ('Native replay path does not reach shared Activate(): ' + $path.FullName) }
    }
    foreach ($method in $native) {
        $method.FullName | Write-Output
        foreach ($param in $method.Parameters) {
            "$($method.Name).$($param.Name): type=$($param.ParameterType.FullName); optional=$($param.IsOptional); constant=$($param.Constant)" | Write-Output
        }
    }
    $names = @('Pr6QuestFxOnlyCase', 'Pr6QuestEmptyMediaCase', 'Pr6QuestEffectFailureCase', 'Pr6QuestSoundFailureCase', 'Pr6QuestCleanupOwnershipCase', 'Pr6QuestMediaActualCase', 'Pr6QuestShowcaseMediaCase')
    $found = @($compiled.MainModule.Types | Where-Object Name -In $names)
    if ($found.Count -ne $names.Count) { throw "Compiled suite lacks a concrete PR6 case." }
    $expectedCoverage = [ordered]@{
        Pr6QuestFxOnlyCase = 0; Pr6QuestEmptyMediaCase = 0; Pr6QuestEffectFailureCase = 1
        Pr6QuestSoundFailureCase = 0; Pr6QuestCleanupOwnershipCase = 0
        Pr6QuestMediaActualCase = 2; Pr6QuestShowcaseMediaCase = 2
    }
    $coverageEvidence = [ordered]@{}
    foreach ($case in $found) {
        $owner = $case
        $getter = @($owner.Methods | Where-Object Name -EQ get_AudioCoverage)
        while ($getter.Count -eq 0) {
            $baseName = $owner.BaseType.FullName
            $owner = $compiled.MainModule.Types | Where-Object FullName -EQ $baseName
            if ($null -eq $owner) { throw ('No compiled audio coverage getter for ' + $case.Name) }
            $getter = @($owner.Methods | Where-Object Name -EQ get_AudioCoverage)
        }
        $constants = @($getter[0].Body.Instructions | Where-Object { $_.OpCode.Code.ToString() -in @('Ldc_I4_0', 'Ldc_I4_1', 'Ldc_I4_2') })
        if ($constants.Count -ne 1) { throw ('Inspect nonconstant audio coverage getter: ' + $case.Name) }
        $value = [int]$constants[0].OpCode.Code.ToString().Substring(7)
        if ($value -ne $expectedCoverage[$case.Name]) { throw ('Wrong compiled audio prerequisite assignment: ' + $case.Name) }
        $coverageEvidence[$case.Name] = @('None', 'Dispatch', 'Playback')[$value]
    }
    @{
        status = 'compile_only_passed'; runtimeExecuted = $false
        generatedSource = $csxPath; compiledAssembly = $dllPath; caseTypes = $names
        scriptCompiledAssembly = $scriptDllPath; untouchedScriptCompiled = $true
        managedCleanupCounterexamples = 'passed'; gameTypesCreatedByManagedTests = $false
        compiledAudioCoverageVerified = $coverageEvidence
        nativeManagedAudioCallChainVerified = @($audioEvidence)
        nativeEffectActivationPathsVerified = @($activationPaths | ForEach-Object FullName)
        cleanupGuardSourceSha256 = (Get-FileHash (Join-Path $runtimeRoot 'src\cases\Pr6MediaCleanupGuards.cs')).Hash
        gameDllSha256 = (Get-FileHash (Join-Path $managed 'Elin.dll')).Hash
        soundDllSha256 = (Get-FileHash (Join-Path $managed 'Plugins.Sound.dll')).Hash
        sourceSha256 = (Get-FileHash (Join-Path $runtimeRoot 'src\cases\Pr6DramaMediaIntegrationCases.cs')).Hash
    } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputRoot 'compile-summary.json') -Encoding utf8
}
finally { $compiled.Dispose(); $soundAssembly.Dispose(); $game.Dispose() }
Write-Host 'PR6 generated-source compile passed; game/runtime assertions NOT executed.'
