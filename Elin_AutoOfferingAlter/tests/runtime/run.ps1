[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('pr8.sleep.consume_water_reject','pr8.sleep.split_nonconsume_stop','pr8.sleep.nonconsume_unsplit_stop',
        'pr8.sleep.stop_on_actor_dead','pr8.sleep.stop_on_pc_change','pr8.sleep.stop_on_faith_change',
        'pr8.sleep.exception_split_recovery','pr8.sleep.sleep_completion','pr8.sleep.source_craft_coldboot',
        'pr8.sleep.existing_box_reload_prepare','pr8.sleep.existing_box_reload_verify')]
    [string]$CaseId,
    [int]$TimeoutSeconds = 180,
    [ValidateSet('Auto','Game','LegacyCwl')][string]$ScriptBackend = 'Auto'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
& (Join-Path $repoRoot 'runtime-test-v2\runner\run_runtime_suite_v2.ps1') -ModRoot (Join-Path $repoRoot 'Elin_AutoOfferingAlter') -Suite smoke -CaseId $CaseId -RequiredNameContains 'RUNTIME_TEST' -TimeoutSeconds $TimeoutSeconds -ScriptBackend $ScriptBackend -KeepGeneratedSource
