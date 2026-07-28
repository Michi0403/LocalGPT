param(
    [ValidateSet("all", "win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "all",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.0",
    [string]$WireProtocolPackageUrl = "",
    [switch]$UseBundledWireProtocolPackage,
    [switch]$IncludeWindowsWrapper
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Join-Path $root "LocalGPTWebviewWrapper"
$artifacts = Join-Path $root "artifacts\release"
$packageDirectory = Join-Path $root "packages"
$appProject = Join-Path $solutionRoot "LocalGPT\LocalGPT.csproj"
$setupProject = Join-Path $solutionRoot "LocalGPTInstallerConsole\LocalGPTInstallerConsole.csproj"
$wrapperProject = Join-Path $solutionRoot "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.csproj"
$wireProject = Join-Path $solutionRoot "LocalGPT.WireProtocolVersion\LocalGPT.WireProtocolVersion.csproj"
$wirePackageName = "LocalGPT.WireProtocolVersion.$WireProtocolVersion.nupkg"
$wirePackage = Join-Path $packageDirectory $wirePackageName
$localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$sharedWirePackageDirectory = if ([string]::IsNullOrWhiteSpace($localApplicationData)) { $null } else { Join-Path $localApplicationData "LocalGPT\NuGet" }

$loggingGuard = Join-Path $root "build\Assert-OneWireArchitecture.ps1"
& $loggingGuard
& (Join-Path $root "build\Assert-JavaScriptDiagnostics.ps1")
& (Join-Path $root "build\Assert-PublishConfiguration.ps1")
& (Join-Path $root "build\Assert-InstallerWorkflow.ps1")
& (Join-Path $root "build\Assert-RuntimeValueOwnership.ps1")
& (Join-Path $root "build\Assert-LocalizationIntegrity.ps1")
& (Join-Path $root "build\Assert-GitSourceVisibility.ps1")
& (Join-Path $root "build\Assert-ProjectMaintenanceArchitecture.ps1")

$multiFileSelfContainedProperties = @(
    "--self-contained", "true",
    "-p:PublishTrimmed=false",
    "-p:PublishSingleFile=false",
    "-p:PublishReadyToRun=false",
    "-p:DeleteExistingFiles=true"
)

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$FailureMessage)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

function Resolve-ReleaseProfile {
    param([Parameter(Mandatory)][string]$Rid)
    switch ($Rid) {
        "win-x64"     { return @{ AppFolder = "winx64";     SetupFolder = "setupwinx64";     AppAsset = "winx64.zip";     SetupAsset = "setupwinx64.zip";     WrapperPlatform = "x64" } }
        "win-x86"     { return @{ AppFolder = "winx86";     SetupFolder = "setupwinx86";     AppAsset = "winx86.zip";     SetupAsset = "setupwinx86.zip";     WrapperPlatform = "x86" } }
        "win-arm64"   { return @{ AppFolder = "winarm64";   SetupFolder = "setupwinarm64";   AppAsset = "winarm64.zip";   SetupAsset = "setupwinarm64.zip";   WrapperPlatform = "ARM64" } }
        "linux-x64"   { return @{ AppFolder = "linuxx64";   SetupFolder = "setuplinuxx64";   AppAsset = "linuxx64.zip";   SetupAsset = "setuplinuxx64.zip";   WrapperPlatform = $null } }
        "linux-arm64" { return @{ AppFolder = "linuxarm64"; SetupFolder = "setuplinuxarm64"; AppAsset = "linuxarm64.zip"; SetupAsset = "setuplinuxarm64.zip"; WrapperPlatform = $null } }
        "osx-x64"     { return @{ AppFolder = "macosx64";   SetupFolder = "setupmacosx64";   AppAsset = "macosx64.zip";   SetupAsset = "setupmacosx64.zip";   WrapperPlatform = $null } }
        "osx-arm64"   { return @{ AppFolder = "macosarm64"; SetupFolder = "setupmacosarm64"; AppAsset = "macosarm64.zip"; SetupAsset = "setupmacosarm64.zip"; WrapperPlatform = $null } }
        default { throw "Unsupported release runtime: $Rid" }
    }
}

function Ensure-WireProtocolPackage {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null

    if ($UseBundledWireProtocolPackage) {
        if (-not (Test-Path $wirePackage)) {
            throw "The bundled RID-neutral LocalGPT 1-Wire package is missing: $wirePackage"
        }
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($WireProtocolPackageUrl)) {
        Write-Host "Downloading LocalGPT.WireProtocolVersion $WireProtocolVersion..." -ForegroundColor Cyan
        $temporary = "$wirePackage.download"
        Remove-Item $temporary -Force -ErrorAction SilentlyContinue
        Invoke-WebRequest -Uri $WireProtocolPackageUrl -OutFile $temporary -UseBasicParsing
        if (-not (Test-Path $temporary)) { throw "The protocol package download did not create a file." }
        Move-Item $temporary $wirePackage -Force
        return
    }

    Write-Host "Packing the RID-neutral LocalGPT 1-Wire protocol once..." -ForegroundColor Cyan
    Remove-Item $wirePackage -Force -ErrorAction SilentlyContinue
    Invoke-DotNet -Arguments @(
        "pack", $wireProject,
        "-c", $Configuration,
        "-o", $packageDirectory,
        "-p:PackageVersion=$WireProtocolVersion",
        "-p:GeneratePackageOnBuild=false",
        "-p:Platform=AnyCPU",
        "-p:PlatformTarget=AnyCPU",
        "-p:RuntimeIdentifier=",
        "-p:RuntimeIdentifiers=",
        "-p:SkipLoggingIntegrityGuard=true",
    "-p:SkipOneWireArchitectureGuard=true",
    "-p:SkipLocalizationIntegrityGuard=true",
    "-p:SkipGitSourceVisibilityGuard=true",
    "-p:SkipProjectMaintenanceArchitectureGuard=true"
    ) -FailureMessage "LocalGPT 1-Wire package creation failed."

    if (-not (Test-Path $wirePackage)) {
        throw "The expected protocol package was not produced: $wirePackage"
    }
}

function Publish-Runtime {
    param([Parameter(Mandatory)][string]$Rid)

    $profile = Resolve-ReleaseProfile $Rid
    $appFolder = Join-Path $artifacts $profile.AppFolder
    $setupFolder = Join-Path $artifacts $profile.SetupFolder
    $appZip = Join-Path $artifacts $profile.AppAsset
    $setupZip = Join-Path $artifacts $profile.SetupAsset

    Remove-Item $appFolder, $setupFolder, $appZip, $setupZip -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $appFolder, $setupFolder -Force | Out-Null

    $sharedProperties = @(
        "-p:UseLocalWireProtocolProject=false",
        "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
        "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
        "-p:RestoreAdditionalProjectSources=$packageDirectory",
        "-p:SkipLoggingIntegrityGuard=true",
    "-p:SkipOneWireArchitectureGuard=true",
    "-p:SkipLocalizationIntegrityGuard=true",
    "-p:SkipGitSourceVisibilityGuard=true",
    "-p:SkipProjectMaintenanceArchitectureGuard=true"
    )

    Write-Host "Restoring LocalGPT application for $Rid in package mode..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@("restore", $appProject, "-r", $Rid, "--disable-parallel") + $sharedProperties) -FailureMessage "LocalGPT restore failed for $Rid."

    Write-Host "Publishing LocalGPT application for $Rid..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@(
        "publish", $appProject,
        "-c", $Configuration,
        "-f", "net10.0",
        "-r", $Rid,
        "--no-restore",
        "-p:IncludeWireProtocolPackageInPublish=true",
        "-o", $appFolder
    ) + $multiFileSelfContainedProperties + $sharedProperties) -FailureMessage "LocalGPT application publish failed for $Rid."

    Write-Host "Restoring LocalGPT setup for $Rid..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @("restore", $setupProject, "-r", $Rid, "--disable-parallel", "-p:SkipLoggingIntegrityGuard=true",
    "-p:SkipOneWireArchitectureGuard=true",
    "-p:SkipLocalizationIntegrityGuard=true",
    "-p:SkipGitSourceVisibilityGuard=true",
    "-p:SkipProjectMaintenanceArchitectureGuard=true") -FailureMessage "LocalGPT setup restore failed for $Rid."

    Write-Host "Publishing LocalGPT setup for $Rid..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@(
        "publish", $setupProject,
        "-c", $Configuration,
        "-f", "net10.0",
        "-r", $Rid,
        "--no-restore",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-o", $setupFolder,
        "-p:SkipLoggingIntegrityGuard=true",
    "-p:SkipOneWireArchitectureGuard=true",
    "-p:SkipLocalizationIntegrityGuard=true",
    "-p:SkipGitSourceVisibilityGuard=true",
    "-p:SkipProjectMaintenanceArchitectureGuard=true"
    ) + $multiFileSelfContainedProperties) -FailureMessage "LocalGPT setup publish failed for $Rid."

    $appExecutable = if ($Rid.StartsWith("win-")) { "LocalGPT.exe" } else { "LocalGPT" }
    $setupExecutable = if ($Rid.StartsWith("win-")) { "LocalGPTInstallerConsole.exe" } else { "LocalGPTInstallerConsole" }
    if (-not (Test-Path (Join-Path $appFolder $appExecutable))) {
        throw "Published LocalGPT executable not found: $(Join-Path $appFolder $appExecutable)"
    }
    if (-not (Test-Path (Join-Path $setupFolder $setupExecutable))) {
        throw "Published LocalGPT setup executable not found: $(Join-Path $setupFolder $setupExecutable)"
    }

    $requiredSetupFiles = @(
        "Default.cmd", "Install.cmd", "Update.cmd", "Start.cmd", "Start-NoBrowser.cmd",
        "Install-Ollama.cmd", "Pull-Models-Slim.cmd", "Pull-Models-RTX3060.cmd",
        "Pull-Models-Full.cmd", "Setup-Learning-Base.cmd", "Import-Recommended.cmd", "Uninstall.cmd"
    )
    $missingSetupFiles = @($requiredSetupFiles | Where-Object { -not (Test-Path (Join-Path $setupFolder $_)) })
    if ($missingSetupFiles.Count -gt 0) { throw "Published LocalGPT setup is incomplete. Missing: $($missingSetupFiles -join ', ')" }

    $protocolSetupDirectory = Join-Path $setupFolder "protocol"
    New-Item -ItemType Directory -Path $protocolSetupDirectory -Force | Out-Null
    Copy-Item $wirePackage (Join-Path $protocolSetupDirectory $wirePackageName) -Force

    if ($IncludeWindowsWrapper -and $profile.WrapperPlatform) {
        $wrapperFolder = Join-Path $artifacts "wrapper-$($profile.AppFolder)"
        Remove-Item $wrapperFolder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Building the optional WinUI wrapper for $($profile.WrapperPlatform)..." -ForegroundColor Cyan
        Invoke-DotNet -Arguments @(
            "restore", $wrapperProject,
            "-r", $Rid,
            "--disable-parallel",
            "-p:Platform=$($profile.WrapperPlatform)",
            "-p:UseLocalWireProtocolProject=false",
            "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
            "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
            "-p:RestoreAdditionalProjectSources=$packageDirectory",
            "-p:SkipLoggingIntegrityGuard=true",
    "-p:SkipOneWireArchitectureGuard=true",
    "-p:SkipLocalizationIntegrityGuard=true",
    "-p:SkipGitSourceVisibilityGuard=true",
    "-p:SkipProjectMaintenanceArchitectureGuard=true"
        ) -FailureMessage "WinUI wrapper restore failed for $Rid."
        Invoke-DotNet -Arguments (@(
            "publish", $wrapperProject,
            "-c", $Configuration,
            "-r", $Rid,
            "--no-restore",
            "-p:Platform=$($profile.WrapperPlatform)",
            "-p:UseLocalWireProtocolProject=false",
            "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
            "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
            "-p:RestoreAdditionalProjectSources=$packageDirectory",
            "-o", $wrapperFolder,
            "-p:SkipLoggingIntegrityGuard=true",
    "-p:SkipOneWireArchitectureGuard=true",
    "-p:SkipLocalizationIntegrityGuard=true",
    "-p:SkipGitSourceVisibilityGuard=true",
    "-p:SkipProjectMaintenanceArchitectureGuard=true"
        ) + $multiFileSelfContainedProperties) -FailureMessage "WinUI wrapper publish failed for $Rid."
        Copy-Item (Join-Path $wrapperFolder "*") $appFolder -Recurse -Force
    }

    Compress-Archive -Path $appFolder -DestinationPath $appZip -CompressionLevel Optimal -Force
    Compress-Archive -Path $setupFolder -DestinationPath $setupZip -CompressionLevel Optimal -Force
    Write-Host "Created $appZip" -ForegroundColor Green
    Write-Host "Created $setupZip" -ForegroundColor Green
}

New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
Ensure-WireProtocolPackage
Copy-Item $wirePackage (Join-Path $artifacts $wirePackageName) -Force
if ($sharedWirePackageDirectory) {
    New-Item -ItemType Directory -Path $sharedWirePackageDirectory -Force | Out-Null
    Copy-Item $wirePackage (Join-Path $sharedWirePackageDirectory $wirePackageName) -Force
    Write-Host "Updated shared LocalGPT protocol package cache: $sharedWirePackageDirectory" -ForegroundColor Green
}

$runtimes = if ($Runtime -eq "all") {
    @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
} else {
    @($Runtime)
}

foreach ($rid in $runtimes) { Publish-Runtime $rid }

Write-Host "Release output: $artifacts" -ForegroundColor Green
Write-Host "Protocol package: $(Join-Path $artifacts $wirePackageName)" -ForegroundColor Green
