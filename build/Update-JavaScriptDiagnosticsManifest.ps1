[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Update-JavaScriptDiagnosticsManifest.Common.ps1') `
    -RepositoryRoot $root `
    -JavaScriptRoot (Join-Path $root 'src\LocalGPT\wwwroot\js') `
    -ManifestPath (Join-Path $PSScriptRoot 'javascript-diagnostics-files.sha256') `
    -ProductName 'LocalGPT'
