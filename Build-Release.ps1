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


function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$FailureMessage)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

function Resolve-ReleaseProfile {
    param([Parameter(Mandatory)][string]$Rid)
    switch ($Rid) {
        "win-x64"     { return @{ AppFolder = "winx64";     SetupFolder = "setupwinx64";     AppAsset = "winx64.zip";     SetupAsset = "setupwinx64.zip";     AppProfile = "winx64";     SetupProfile = "winx64";     WrapperProfile = "winx64";     WrapperPlatform = "x64" } }
        "win-x86"     { return @{ AppFolder = "winx86";     SetupFolder = "setupwinx86";     AppAsset = "winx86.zip";     SetupAsset = "setupwinx86.zip";     AppProfile = "winx86";     SetupProfile = "winx86";     WrapperProfile = "winx86";     WrapperPlatform = "x86" } }
        "win-arm64"   { return @{ AppFolder = "winarm64";   SetupFolder = "setupwinarm64";   AppAsset = "winarm64.zip";   SetupAsset = "setupwinarm64.zip";   AppProfile = "winarm64";   SetupProfile = "winarm64";   WrapperProfile = "winarm64";   WrapperPlatform = "ARM64" } }
        "linux-x64"   { return @{ AppFolder = "linuxx64";   SetupFolder = "setuplinuxx64";   AppAsset = "linuxx64.zip";   SetupAsset = "setuplinuxx64.zip";   AppProfile = "linuxx64";   SetupProfile = "linuxx64";   WrapperProfile = $null; WrapperPlatform = $null } }
        "linux-arm64" { return @{ AppFolder = "linuxarm64"; SetupFolder = "setuplinuxarm64"; AppAsset = "linuxarm64.zip"; SetupAsset = "setuplinuxarm64.zip"; AppProfile = "linuxarm64"; SetupProfile = "linuxarm64"; WrapperProfile = $null; WrapperPlatform = $null } }
        "osx-x64"     { return @{ AppFolder = "macosx64";   SetupFolder = "setupmacosx64";   AppAsset = "macosx64.zip";   SetupAsset = "setupmacosx64.zip";   AppProfile = "macosx64";   SetupProfile = "macosx64";   WrapperProfile = $null; WrapperPlatform = $null } }
        "osx-arm64"   { return @{ AppFolder = "macosarm64"; SetupFolder = "setupmacosarm64"; AppAsset = "macosarm64.zip"; SetupAsset = "setupmacosarm64.zip"; AppProfile = "macosarm64"; SetupProfile = "macosarm64"; WrapperProfile = $null; WrapperPlatform = $null } }
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
        "-p:RuntimeIdentifiers="
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
        "-p:RestoreAdditionalProjectSources=$packageDirectory"
    )

    Write-Host "Publishing LocalGPT application through profile $($profile.AppProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@(
        "publish", $appProject,
        "-p:PublishProfile=$($profile.AppProfile)",
        "-p:IncludeWireProtocolPackageInPublish=true"
    ) + $sharedProperties) -FailureMessage "LocalGPT application publish failed for $Rid."

    Write-Host "Publishing LocalGPT setup through profile $($profile.SetupProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @(
        "publish", $setupProject,
        "-p:PublishProfile=$($profile.SetupProfile)"
    ) -FailureMessage "LocalGPT setup publish failed for $Rid."

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

    if ($IncludeWindowsWrapper -and $profile.WrapperProfile) {
        $wrapperFolder = Join-Path $artifacts "wrapper-$($profile.AppFolder)"
        Remove-Item $wrapperFolder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Publishing the optional WinUI wrapper through profile $($profile.WrapperProfile)..." -ForegroundColor Cyan
        Invoke-DotNet -Arguments @(
            "publish", $wrapperProject,
            "-p:PublishProfile=$($profile.WrapperProfile)",
            "-p:UseLocalWireProtocolProject=false",
            "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
            "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
            "-p:RestoreAdditionalProjectSources=$packageDirectory"
        ) -FailureMessage "WinUI wrapper publish failed for $Rid."
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
