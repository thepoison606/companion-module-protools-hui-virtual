$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Helper = Join-Path $RepoRoot 'runtime\win-x64\ProToolsHuiBridge.exe'

if (-not (Test-Path $Helper)) {
    throw 'Helper is missing. Run .\scripts\build-windows-helper.ps1 first.'
}

Write-Host 'Starting helper standalone.'
Write-Host 'While it runs, check Pro Tools -> Setup -> Peripherals -> MIDI Controllers.'
Write-Host 'Press Ctrl+C when finished.'
& $Helper --endpoint 'Companion Pro Tools HUI' --release-ms 20
