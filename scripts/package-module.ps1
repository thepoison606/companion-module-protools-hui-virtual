$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Helper = Join-Path $RepoRoot 'runtime\win-x64\ProToolsHuiBridge.exe'

if (-not (Test-Path $Helper)) {
    throw 'Bundled helper is missing. Run .\scripts\build-windows-helper.ps1 first.'
}

Push-Location $RepoRoot
try {
    corepack enable
    yarn install
    yarn check
    yarn package
}
finally {
    Pop-Location
}
