param(
    [Parameter(Mandatory)][string]$Solution,
    [ValidateSet('Release', 'Debug')][string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Solution))
if (-not $solutionPath.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The validation solution must be inside this repository.'
}
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) { throw "Solution not found: $Solution" }
$logRoot = Join-Path $repositoryRoot 'artifacts/validation'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$env:DOTNET_CLI_HOME = Join-Path $logRoot 'dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:NUGET_PACKAGES = Join-Path $logRoot 'nuget-packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $logRoot 'nuget-http-cache'
& (Join-Path $PSScriptRoot 'Assert-PowerShellCompatibility.ps1')
& (Join-Path $PSScriptRoot 'Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $PSScriptRoot 'Assert-JavaScriptDiagnostics.ps1')

# Build targets retain every normal maintenance guard. This path deliberately stops
# before documentation generation, native packaging, setup or application launch.
& dotnet build $solutionPath --configuration $Configuration --verbosity minimal "-p:RestoreConfigFile=$(Join-Path $repositoryRoot 'NuGet.Config')" 2>&1 |
    Tee-Object -FilePath (Join-Path $logRoot 'release-compile.log')
if ($LASTEXITCODE -ne 0) { throw "Release compilation failed with exit code $LASTEXITCODE. See artifacts/validation/release-compile.log." }
Write-Host 'Release compilation and build-time maintenance checks passed. No build output was executed.'
