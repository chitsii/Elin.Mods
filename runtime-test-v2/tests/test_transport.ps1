param([string]$TransportPath = (Join-Path $PSScriptRoot '..\runner\runtime_transport.ps1'))
$ErrorActionPreference = 'Stop'
. $TransportPath
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
public static class RuntimeTransportFixture {
 public static Task<List<string>> Run(string name,string mode,int expected) {
  return Task.Run(async () => {
   var commands=new List<string>();
   for(int i=0;i<expected;i++) {
    using(var server=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous))
    using(var cancel=new CancellationTokenSource(1500)) {
     try { await server.WaitForConnectionAsync(cancel.Token); } catch(OperationCanceledException) { break; }
     using(var reader=new StreamReader(server,new UTF8Encoding(false),false,4096,true)) {
      string command=await reader.ReadLineAsync(); commands.Add(command);
      if(mode=="silent") { await Task.Delay(250); continue; }
      string response;
      if(command=="cs.version") response=mode=="legacy" ? "Unknown command: cs.version" : mode=="disabled" ? "scripting is unavailable" : (mode=="fragmented" ? "scripting: 編訳" : "scripting: fixture");
      else if(command=="cwl.cs.is_ready") response="legacy fixture ready";
      else if((command.StartsWith("cs.file ")||command.StartsWith("cwl.cs.file ")) && command.EndsWith(".cs")) response=mode=="compileerror" ? "(err): EScriptCompilationException CS1002" : "(ok): command completed";
      else response="Unknown command: "+command;
      byte[] bytes=new UTF8Encoding(false).GetBytes(response+"\n");
      if(mode=="fragmented") { await server.WriteAsync(bytes,0,12); await server.FlushAsync(); await Task.Delay(20); await server.WriteAsync(bytes,12,bytes.Length-12); }
      else await server.WriteAsync(bytes,0,bytes.Length);
      await server.FlushAsync();
     }
    }
   }
   return commands;
  });
 }
}
'@
function Require($Condition, $Message) { if (-not $Condition) { throw $Message } }
function Run-Fixture([string]$Mode, [int]$Expected, [bool]$ShouldFail, [bool]$ExecuteFile = $true) {
    $pipe = 'runtime-offline-' + [Guid]::NewGuid().ToString('N')
    $server = [RuntimeTransportFixture]::Run($pipe, $Mode, $Expected)
    $diagnostics = [Collections.Generic.List[object]]::new()
    $failed = $false
    try {
        $backend = Resolve-RuntimeScriptBackend -NamedPipe $pipe -ConnectTimeoutMs 1000 -ResponseTimeoutMs 100 -Diagnostics $diagnostics
        if ($ExecuteFile) { $null = Invoke-RuntimeScriptFile -Path 'C:\fixture\suite.cs' -NamedPipe $pipe -Backend $backend -ResponseTimeoutMs 1000 -Diagnostics $diagnostics }
    } catch { $failed = $true }
    Require ($server.Wait(3000)) 'Fixture server did not finish.'
    Require ($failed -eq $ShouldFail) "Unexpected outcome: $Mode"
    $commands = @($server.Result)
    Require ($commands.Count -eq $Expected) "Wrong command count/fallback/resend: $Mode"
    Require ($commands[0] -eq 'cs.version') 'UTF-8 without BOM / LF command is not exact.'
    if ($Mode -eq 'fragmented') { Require ($diagnostics[0].response.Contains('編訳')) 'Fragmented UTF8 decoding failed.' }
    if ($Mode -eq 'legacy') { Require ($commands[1] -eq 'cwl.cs.is_ready') 'Legacy API was not explicitly negotiated.' }
    $fileCommands = @($commands | Where-Object { $_ -like '*cs.file *' })
    Require ($fileCommands.Count -le 1) 'Script file was executed more than once.'
    if ($Mode -eq 'silent') { Require ($diagnostics.Count -eq 1 -and $diagnostics[0].status -eq 'failed' -and $diagnostics[0].phase -eq 'response') 'Timeout diagnostics/fail-closed were lost.' }
    Write-Host "PASS $Mode commands=$($commands.Count) fileRequests=$($fileCommands.Count) failed=$failed"
}
$missingDiagnostics = [Collections.Generic.List[object]]::new()
$missingFailed = $false
try { $null = Resolve-RuntimeScriptBackend -NamedPipe ('runtime-no-server-' + [Guid]::NewGuid().ToString('N')) -ConnectTimeoutMs 100 -ResponseTimeoutMs 100 -Diagnostics $missingDiagnostics } catch { $missingFailed = $true }
Require ($missingFailed -and $missingDiagnostics.Count -eq 1 -and $missingDiagnostics[0].phase -eq 'connect') 'Connection timeout did not fail closed with diagnostics.'
Write-Host 'PASS connection-timeout attempts=1'
Run-Fixture current 2 $false
Run-Fixture fragmented 2 $false
Run-Fixture legacy 3 $false
Run-Fixture silent 1 $true
Run-Fixture disabled 1 $true
Run-Fixture compileerror 2 $true
Write-Host 'PASS: current/legacy negotiation, UTF8/LF, fragmented reply, timeout without fallback, disabled scripting, compilation errors, file request exactly once.'

