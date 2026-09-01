$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build-windows-helper.ps1')
& (Join-Path $PSScriptRoot 'package-module.ps1')
