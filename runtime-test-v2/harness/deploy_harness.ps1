[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SessionRoot,
    [string]$ElinRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Elin',
    [string]$Dll = (Join-Path $PSScriptRoot 'bin\Release\net472\Elin.RuntimeTestPipe.dll')
)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name Elin -ErrorAction SilentlyContinue) { throw 'Elin is running; deployment is forbidden.' }
$ElinRoot = (Resolve-Path -LiteralPath $ElinRoot).Path
$target = Join-Path $ElinRoot 'Package\_ModdingKit\RuntimeTestPipe'
if (Test-Path -LiteralPath $target) { throw 'Harness directory already exists; refusing to overwrite untracked files.' }
if (-not (Test-Path -LiteralPath $Dll)) { throw 'Build the reviewed harness first.' }
if (Test-Path -LiteralPath (Join-Path $SessionRoot 'deploy-harness.json')) { throw 'Deployment manifest already exists; use a new session directory.' }
New-Item -ItemType Directory -Path $SessionRoot -Force | Out-Null
$config = Join-Path $ElinRoot 'BepInEx\config\chitsii.elin.runtime_test_pipe.cfg'
$original = Join-Path $SessionRoot 'harness-config.original'
$configExisted = Test-Path -LiteralPath $config
if ($configExisted) { Copy-Item -LiteralPath $config -Destination $original }
$manifest = [ordered]@{
    utc = [DateTime]::UtcNow.ToString('o'); target = $target; directory_existed = $false
    dll_hash = (Get-FileHash -LiteralPath $Dll -Algorithm SHA256).Hash
    config = $config; config_existed = $configExisted; original_config = $original
    original_config_hash = if ($configExisted) { (Get-FileHash -LiteralPath $original).Hash } else { '' }
    installed = $false; restored = $false
}
$manifestPath = Join-Path $SessionRoot 'deploy-harness.json'
$manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8
if (Get-Process -Name Elin -ErrorAction SilentlyContinue) { throw 'Elin started during preparation; deployment cancelled.' }
New-Item -ItemType Directory -Path $target | Out-Null
Copy-Item -LiteralPath $Dll -Destination (Join-Path $target 'Elin.RuntimeTestPipe.dll')
Set-Content -LiteralPath (Join-Path $target 'enable.txt') -Value 'world_11' -Encoding UTF8
if ((Get-FileHash -LiteralPath (Join-Path $target 'Elin.RuntimeTestPipe.dll')).Hash -ne $manifest.dll_hash) { throw 'Harness deployment hash mismatch.' }
$manifest.installed = $true
$manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host "Installed test-only harness: $target (original DLLs, loadorder, scripting settings unchanged)"
