$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $RepoRoot 'helper\ProToolsHuiBridge.csproj'
$PublishDir = Join-Path $RepoRoot 'helper\publish\win-x64'
$RuntimeDir = Join-Path $RepoRoot 'runtime\win-x64'

Write-Host 'Building Windows MIDI Services HUI helper...'
Write-Host 'Requires: .NET 10 SDK and Windows MIDI Services SDK/runtime.'

if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishTrimmed=false `
    -p:PublishAot=false `
    -o $PublishDir

if (Test-Path $RuntimeDir) {
    Remove-Item -Recurse -Force $RuntimeDir
}
New-Item -ItemType Directory -Force -Path $RuntimeDir | Out-Null
Copy-Item -Recurse -Force (Join-Path $PublishDir '*') $RuntimeDir

$Exe = Join-Path $RuntimeDir 'ProToolsHuiBridge.exe'
if (-not (Test-Path $Exe)) {
    throw "Build completed but helper executable was not found at $Exe"
}

Write-Host "Helper ready: $Exe"
