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
$solutionRoot = Join-Path $root "LocalGPTWebviewWrapper"
$wireProject = Join-Path $solutionRoot "LocalGPT.WireProtocolVersion\LocalGPT.WireProtocolVersion.csproj"
$appProject = Join-Path $solutionRoot "LocalGPT\LocalGPT.csproj"
$setupProject = Join-Path $solutionRoot "LocalGPTInstallerConsole\LocalGPTInstallerConsole.csproj"
$wrapperProject = Join-Path $solutionRoot "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.csproj"
$packageDirectory = Join-Path $root "packages"
$wireVersion = "2.0.1"
$wirePackage = Join-Path $packageDirectory "LocalGPT.WireProtocolVersion.$wireVersion.nupkg"
$useProject = if ($UseWireProtocolPackage) { "false" } else { "true" }

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$FailureMessage)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

& (Join-Path $root "build\Assert-LoggingIntegrity.ps1")

if ($Clean) {
    Get-ChildItem $solutionRoot -Directory -Recurse -Force |
        Where-Object { $_.Name -in @("bin", "obj") } |
        Sort-Object FullName -Descending |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
$wireBuildProperties = @(
    "-p:Platform=AnyCPU",
    "-p:PlatformTarget=AnyCPU",
    "-p:RuntimeIdentifier=",
    "-p:RuntimeIdentifiers=",
    "-p:SkipLoggingIntegrityGuard=true"
)

Write-Host "Restoring the authoritative RID-neutral protocol project first..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("restore", $wireProject, "--disable-parallel") + $wireBuildProperties) -FailureMessage "Wire protocol restore failed."
Write-Host "Building the authoritative RID-neutral protocol project first..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("build", $wireProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1") + $wireBuildProperties) -FailureMessage "Wire protocol build failed."

if ($UseWireProtocolPackage) {
    Remove-Item -LiteralPath $wirePackage -Force -ErrorAction SilentlyContinue
    Write-Host "Packing the protocol before package-mode application restore..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@("pack", $wireProject, "-c", $Configuration, "--no-build", "-o", $packageDirectory, "-p:PackageVersion=$wireVersion", "-maxcpucount:1") + $wireBuildProperties) -FailureMessage "Wire protocol package creation failed."
    if (-not (Test-Path -LiteralPath $wirePackage)) { throw "Expected wire protocol package was not produced: $wirePackage" }
}

$appProperties = @(
    "-p:UseLocalWireProtocolProject=$useProject",
    "-p:LocalGptWireProtocolVersion=$wireVersion",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:Platform=AnyCPU",
    "-p:SkipLoggingIntegrityGuard=true"
)

Write-Host "Restoring LocalGPT only after its protocol dependency is ready..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("restore", $appProject, "--disable-parallel") + $appProperties) -FailureMessage "LocalGPT application restore failed."
Write-Host "Building LocalGPT in deterministic project order..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("build", $appProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1", "-p:BuildProjectReferences=false") + $appProperties) -FailureMessage "LocalGPT application build failed."

Write-Host "Restoring and building the installer after LocalGPT..." -ForegroundColor Cyan
Invoke-DotNet -Arguments @("restore", $setupProject, "--disable-parallel", "-p:SkipLoggingIntegrityGuard=true") -FailureMessage "LocalGPT installer restore failed."
Invoke-DotNet -Arguments @("build", $setupProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1", "-p:SkipLoggingIntegrityGuard=true") -FailureMessage "LocalGPT installer build failed."

Write-Host "Restoring and building the optional WinUI wrapper last..." -ForegroundColor Cyan
$wrapperProperties = @(
    "-p:Platform=$Platform",
    "-p:UseLocalWireProtocolProject=$useProject",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:SkipLoggingIntegrityGuard=true"
)
Invoke-DotNet -Arguments (@("restore", $wrapperProject, "--disable-parallel") + $wrapperProperties) -FailureMessage "LocalGPT WinUI wrapper restore failed."
Invoke-DotNet -Arguments (@("build", $wrapperProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1", "-p:BuildProjectReferences=false") + $wrapperProperties) -FailureMessage "LocalGPT WinUI wrapper build failed."

Write-Host "LocalGPT development build succeeded in strict protocol -> app -> installer -> wrapper order." -ForegroundColor Green
