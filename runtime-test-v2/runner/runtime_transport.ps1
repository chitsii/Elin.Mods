Set-StrictMode -Version Latest

function Invoke-RuntimePipeCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][string]$NamedPipe,
        [int]$ConnectTimeoutMs = 5000,
        [int]$ResponseTimeoutMs = 5000,
        [System.Collections.Generic.List[object]]$Diagnostics
    )
    $record = [ordered]@{ command = $Command; pipe = $NamedPipe; started_utc = [DateTime]::UtcNow.ToString('o'); status = 'pending'; phase = 'connect'; response = ''; error = '' }
    $client = [IO.Pipes.NamedPipeClientStream]::new('.', $NamedPipe, [IO.Pipes.PipeDirection]::InOut, [IO.Pipes.PipeOptions]::Asynchronous)
    try {
        $client.Connect($ConnectTimeoutMs)
        $record.phase = 'write'
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($Command + "`n")
        $client.Write($bytes, 0, $bytes.Length)
        $client.Flush()
        $record.phase = 'response'
        $buffer = [byte[]]::new(4096)
        $text = [Text.StringBuilder]::new()
        $decoder = [Text.Encoding]::UTF8.GetDecoder()
        $chars = [char[]]::new(4096)
        $timer = [Diagnostics.Stopwatch]::StartNew()
        do {
            $remaining = [int]($ResponseTimeoutMs - $timer.ElapsedMilliseconds)
            $read = $client.ReadAsync($buffer, 0, $buffer.Length)
            if ($remaining -le 0 -or -not $read.Wait($remaining)) {
                throw [TimeoutException]::new("Pipe response timeout for '$Command'. The request may be queued; it must not be retried or treated as executed.")
            }
            if ($read.Result -eq 0) { throw [IO.EndOfStreamException]::new("Pipe closed without a complete response for '$Command'.") }
            $count = $decoder.GetChars($buffer, 0, $read.Result, $chars, 0)
            [void]$text.Append($chars, 0, $count)
            $record.response = $text.ToString()
            if ($text.Length -gt 65536) { throw 'Command acknowledgement exceeds 64 KiB.' }
        } while ($record.response.IndexOf("`n") -lt 0)
        $record.response = $record.response.Trim()
        if ([string]::IsNullOrWhiteSpace($record.response)) { throw 'Empty command acknowledgement.' }
        $record.status = 'received'
        return $record.response
    }
    catch { $record.status = 'failed'; $record.error = $_.Exception.Message; throw }
    finally { $client.Dispose(); if ($null -ne $Diagnostics) { $Diagnostics.Add([pscustomobject]$record) } }
}

function Test-RuntimeCommandError {
    param([string]$Response)
    return $Response -match '(?i)^\(err\)|scripting is unavailable|script.*disabled|internal exception|file does not exist|runtime_guard_rejected|compilation.*(error|failed)|\bCS\d{4}\b'
}

function Test-RuntimeUnknownCommand {
    param([string]$Response)
    return $Response -match '(?i)(unknown|not found|could not find|cannot find|unable to find|no such|invalid).{0,80}command|command.{0,80}(unknown|not found|invalid|does not exist)'
}

function Resolve-RuntimeScriptBackend {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$NamedPipe,
        [ValidateSet('Auto','Game','LegacyCwl')][string]$Backend = 'Auto',
        [int]$ConnectTimeoutMs = 5000,
        [int]$ResponseTimeoutMs = 5000,
        [System.Collections.Generic.List[object]]$Diagnostics
    )
    if ($Backend -ne 'LegacyCwl') {
        $response = Invoke-RuntimePipeCommand -Command 'cs.version' -NamedPipe $NamedPipe -ConnectTimeoutMs $ConnectTimeoutMs -ResponseTimeoutMs $ResponseTimeoutMs -Diagnostics $Diagnostics
        if ($response.StartsWith('scripting:', [StringComparison]::OrdinalIgnoreCase)) { return 'Game' }
        if ($Backend -eq 'Game' -or -not (Test-RuntimeUnknownCommand $response)) {
            throw "Current scripting readiness failed: $response"
        }
        # Only an explicit unknown-command response permits legacy negotiation.
        # A connection/read timeout propagates immediately; no fallback or file resend.
    }
    $response = Invoke-RuntimePipeCommand -Command 'cwl.cs.is_ready' -NamedPipe $NamedPipe -ConnectTimeoutMs $ConnectTimeoutMs -ResponseTimeoutMs $ResponseTimeoutMs -Diagnostics $Diagnostics
    if ((Test-RuntimeCommandError $response) -or (Test-RuntimeUnknownCommand $response)) { throw "Legacy scripting readiness failed: $response" }
    return 'LegacyCwl'
}

function Invoke-RuntimeScriptFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$NamedPipe,
        [ValidateSet('Game','LegacyCwl')][string]$Backend,
        [int]$ConnectTimeoutMs = 5000,
        [int]$ResponseTimeoutMs = 30000,
        [System.Collections.Generic.List[object]]$Diagnostics
    )
    if ([IO.Path]::GetExtension($Path) -ne '.cs') { throw "Script file must have .cs extension: $Path" }
    $prefix = if ($Backend -eq 'Game') { 'cs.file' } else { 'cwl.cs.file' }
    $argument = $Path.Replace('\', '/')
    $response = Invoke-RuntimePipeCommand -Command "$prefix $argument" -NamedPipe $NamedPipe -ConnectTimeoutMs $ConnectTimeoutMs -ResponseTimeoutMs $ResponseTimeoutMs -Diagnostics $Diagnostics
    if ((Test-RuntimeCommandError $response) -or (Test-RuntimeUnknownCommand $response)) { throw "Script execution request failed: $response" }
    # An acknowledgement is not a passing suite; result.json remains authoritative.
    return $response
}
