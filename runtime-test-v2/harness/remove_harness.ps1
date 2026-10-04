[CmdletBinding()]
param([Parameter(Mandatory)][string]$SessionRoot, [string]$ElinRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Elin')
$ErrorActionPreference = 'Stop'
if (Get-Process -Name Elin -ErrorAction SilentlyContinue) { throw 'Elin is running; stop the game before removing the harness.' }
$ElinRoot = (Resolve-Path -LiteralPath $ElinRoot).Path
$manifestPath = Join-Path $SessionRoot 'deploy-harness.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$expected = [IO.Path]::GetFullPath((Join-Path $ElinRoot 'Package\_ModdingKit\RuntimeTestPipe'))
if ([IO.Path]::GetFullPath($manifest.target) -ne $expected) { throw 'Manifest target is outside the exact harness directory.' }
$dll = Join-Path $expected 'Elin.RuntimeTestPipe.dll'
$marker = Join-Path $expected 'enable.txt'
if (Test-Path -LiteralPath $expected) {
    $unexpected = @(Get-ChildItem -LiteralPath $expected -Force | Where-Object { $_.PSIsContainer -or $_.Name -notin @('Elin.RuntimeTestPipe.dll','enable.txt') })
    if ($unexpected.Count) { throw 'Unexpected files in harness directory; refusing removal.' }
    if ((Test-Path -LiteralPath $dll) -and (Get-FileHash -LiteralPath $dll).Hash -ne $manifest.dll_hash) { throw 'Installed harness hash changed; refusing removal.' }
    if ((Test-Path -LiteralPath $marker) -and (Get-Content -LiteralPath $marker -Raw).Trim() -ne 'world_11') { throw 'Harness marker changed; refusing removal.' }
}
$expectedConfig = Join-Path $ElinRoot 'BepInEx\config\chitsii.elin.runtime_test_pipe.cfg'
if ($manifest.config -ne $expectedConfig) { throw 'Manifest config path mismatch.' }
if ($manifest.config_existed -and (Get-FileHash -LiteralPath $manifest.original_config).Hash -ne $manifest.original_config_hash) { throw 'Config backup hash mismatch.' }
if (Get-Process -Name Elin -ErrorAction SilentlyContinue) { throw 'Elin started during checks; removal cancelled.' }
# Delete only these two reviewed files; no recursive removal.
foreach ($path in @($dll,$marker)) { if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path } }
if (Test-Path -LiteralPath $expected) { Remove-Item -LiteralPath $expected }
if ($manifest.config_existed) { Copy-Item -LiteralPath $manifest.original_config -Destination $expectedConfig -Force }
elseif (Test-Path -LiteralPath $expectedConfig) { Remove-Item -LiteralPath $expectedConfig }
$manifest.restored = $true
$manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host 'Harness removed after game stop; original harness config/absence restored. Recheck save/config/vendor DLL hashes separately.'
