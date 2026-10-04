param([string]$RunnerPath = (Join-Path $PSScriptRoot '..\runner\run_runtime_suite_v2.ps1'), [string]$ArtifactRoot = [IO.Path]::GetTempPath())
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
public static class RuntimeRunnerFixture {
 public static Task<List<string>> Run(string pipe,string mode,string log) {
  return Task.Run(async () => {
   var requests=new List<string>();
   int expected=mode=="silent"||mode=="disabled"?1:2;
   for(int i=0;i<expected;i++) {
    using(var server=new NamedPipeServerStream(pipe,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous))
    using(var cancel=new CancellationTokenSource(12000)) {
     await server.WaitForConnectionAsync(cancel.Token);
     using(var reader=new StreamReader(server,new UTF8Encoding(false),false,4096,true)) {
      string cmd=await reader.ReadLineAsync(); requests.Add(cmd);
      if(mode=="silent") { await Task.Delay(5500); continue; }
      string response=cmd=="cs.version" ? (mode=="disabled" ? "scripting is unavailable" : "scripting: fixture") : "(ok): fixture script accepted";
      if(cmd.StartsWith("cs.file ")) {
       string path=cmd.Substring(8);
       if(!path.EndsWith(".cs")||!File.Exists(path)) throw new Exception("Invalid current API script path.");
       string source=File.ReadAllText(path);
       string result=Regex.Match(source,"ResultPath = @\"([^\"]+)\"").Groups[1].Value;
       if(string.IsNullOrEmpty(result)) throw new Exception("No result destination in generated source.");
       File.AppendAllText(log,"fixture command processed\n");
       if(mode=="compileerror") response="(err): EScriptCompilationException CS1002";
       else if(mode!="noresult") File.WriteAllText(result,"{\"status\":\"passed\",\"summary\":{\"total\":1,\"passed\":1,\"failed\":0}}");
      }
      byte[] bytes=new UTF8Encoding(false).GetBytes(response+"\n"); await server.WriteAsync(bytes,0,bytes.Length); await server.FlushAsync();
     }
    }
   }
   return requests;
  });
 }
}
'@
function Require($Ok, [string]$Message) { if (-not $Ok) { throw $Message } }
$root = Join-Path $ArtifactRoot ('runner-fixture-' + [Guid]::NewGuid().ToString('N'))
$cases = Join-Path $root 'tests\runtime\src\cases'
New-Item -ItemType Directory -Path $cases -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $root 'temp') -Force | Out-Null
Set-Content -LiteralPath (Join-Path $cases 'FixtureCase.cs') -Value 'public sealed class FixtureCase : RuntimeCaseBase { public override string Id => "fixture"; }'
$log = Join-Path $root 'fixture.log'
$exe = (Get-Process -Id $PID).Path
foreach ($mode in @('current','disabled','compileerror','noresult','silent')) {
    Set-Content -LiteralPath $log -Value 'fixture baseline'
    $pipe = 'runtime-runner-offline-' + [Guid]::NewGuid().ToString('N')
    $server = [RuntimeRunnerFixture]::Run($pipe, $mode, $log)
    $info = [Diagnostics.ProcessStartInfo]::new($exe)
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $info.Environment['TEMP'] = Join-Path $root 'temp'
    $info.Environment['TMP'] = Join-Path $root 'temp'
    foreach ($arg in @('-NoProfile','-File',$RunnerPath,'-ModRoot',$root,'-Suite','smoke','-CaseId','fixture','-PipeName',$pipe,'-TimeoutSeconds','1','-KeepGeneratedSource','-PlayerLogPath',$log)) { $info.ArgumentList.Add($arg) }
    $process = [Diagnostics.Process]::Start($info)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync(); $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(15000)) { $process.Kill(); throw "Offline runner child exceeded deadline: $mode" }
    $stdout = $stdoutTask.GetAwaiter().GetResult(); $stderr = $stderrTask.GetAwaiter().GetResult()
    Set-Content -LiteralPath (Join-Path $root "$mode.stdout.log") -Value $stdout
    Set-Content -LiteralPath (Join-Path $root "$mode.stderr.log") -Value $stderr
    Require ($server.Wait(15000)) "Offline server did not finish: $mode"
    Require ($process.ExitCode -eq $(if ($mode -eq 'current') {0} else {1})) "Wrong exit code: $mode`n$stdout`n$stderr"
    $run = Get-ChildItem (Join-Path $root 'tests\runtime\_artifacts') -Directory | Sort-Object Name | Select-Object -Last 1
    $transport = Get-Content (Join-Path $run.FullName 'transport.json') -Raw | ConvertFrom-Json
    Require (Test-Path (Join-Path $run.FullName 'runtime_suite_v2_generated.cs')) 'Generated source missing on failure.'
    Require (Test-Path (Join-Path $run.FullName 'playerlog.diff.meta.json')) 'Log metadata missing on failure.'
    Require (Test-Path (Join-Path $run.FullName 'playerlog.diff.log')) 'Log delta missing on failure.'
    $files = @($server.Result | Where-Object { $_ -like 'cs.file *' })
    Require ($files.Count -le 1) 'Suite source sent more than once.'
    if ($mode -ne 'current') { Require (Test-Path (Join-Path $run.FullName 'runner-error.json')) 'Runner error diagnostics missing.' }
    if ($mode -eq 'silent') { Require ($transport.attempts.Count -eq 1 -and $files.Count -eq 0) 'Timeout fell back or submitted suite.' }
    if ($mode -eq 'disabled') { Require ($files.Count -eq 0) 'Disabled scripting executed file.' }
    Write-Host "PASS runner/$mode exit=$($process.ExitCode) requests=$($server.Result.Count) files=$($files.Count) diagnostic=$($run.Name)"
    $process.Dispose()
}
Write-Host 'PASS: runner current API, no success on timeout, compile/disabled failures, one file request, source/error/log/transport retention. All results are synthetic offline fixtures.'

