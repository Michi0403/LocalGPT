param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [ValidateSet("x64", "x86", "arm64")]
    [string]$Platform = "x64",
    [switch]$UseWireProtocolPackage,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.sln"
$packageDirectory = Join-Path $root "packages"
$useProject = if ($UseWireProtocolPackage) { "false" } else { "true" }

$loggingGuard = Join-Path $root "build\Assert-LoggingIntegrity.ps1"
& $loggingGuard

if ($Clean) {
    Get-ChildItem (Join-Path $root "LocalGPTWebviewWrapper") -Directory -Recurse -Force |
        Where-Object { $_.Name -in @("bin", "obj") } |
        Sort-Object FullName -Descending |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

$properties = @(
    "-p:UseLocalWireProtocolProject=$useProject",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:Platform=$Platform",
    "-p:SkipLoggingIntegrityGuard=true"
)

& dotnet restore $solution @properties
if ($LASTEXITCODE -ne 0) { throw "LocalGPT solution restore failed." }
& dotnet build $solution -c $Configuration --no-restore @properties
if ($LASTEXITCODE -ne 0) { throw "LocalGPT solution build failed." }

Write-Host "LocalGPT development build succeeded with UseLocalWireProtocolProject=$useProject and Platform=$Platform." -ForegroundColor Green
