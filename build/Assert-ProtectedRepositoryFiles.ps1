Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$required = @('Directory.Build.targets','Directory.Build.props','Build-Release.ps1','build/Initialize-BuildPrerequisites.ps1','build/Assert-RepositoryPowerShellBoundary.ps1','src/LocalGPT/LocalGPT.csproj')
foreach ($relative in $required) { if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) { throw "Protected build/repository owner missing: $relative" } }
Write-Host 'Protected source-repository entrypoints verified.'
