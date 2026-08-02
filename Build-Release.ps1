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

function Resolve-PublishProfilePath {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ProfileName
    )

    $projectDirectory = Split-Path -Parent $ProjectPath
    $profilePath = Join-Path $projectDirectory "Properties\PublishProfiles\$ProfileName.pubxml"
    if (-not (Test-Path -LiteralPath $profilePath)) {
        throw "Publish profile not found: $profilePath"
    }

    return $profilePath
}

function Resolve-ProfilePublishFolder {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ProfileName
    )

    $profilePath = Resolve-PublishProfilePath -ProjectPath $ProjectPath -ProfileName $ProfileName
    [xml]$profile = Get-Content -LiteralPath $profilePath -Raw
    $propertyGroups = @($profile.Project.PropertyGroup)
    $publishDirectory = @(
        $propertyGroups |
            ForEach-Object { $_.PublishDir } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
    ) | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace([string]$publishDirectory)) {
        $publishDirectory = @(
            $propertyGroups |
                ForEach-Object { $_.PublishUrl } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
        ) | Select-Object -First 1
    }

    if ([string]::IsNullOrWhiteSpace([string]$publishDirectory)) {
        throw "Publish profile does not define PublishDir or PublishUrl: $profilePath"
    }

    $projectDirectory = Split-Path -Parent $ProjectPath
    $resolved = if ([IO.Path]::IsPathRooted([string]$publishDirectory)) {
        [string]$publishDirectory
    } else {
        Join-Path $projectDirectory ([string]$publishDirectory)
    }

    return [IO.Path]::GetFullPath($resolved)
}

function Resolve-ReleaseProfile {
    param([Parameter(Mandatory)][string]$Rid)
    switch ($Rid) {
        "win-x64"     { return @{ AppAsset = "winx64.zip";     SetupAsset = "setupwinx64.zip";     AppProfile = "winx64";     SetupProfile = "winx64";     WrapperProfile = "winx64" } }
        "win-x86"     { return @{ AppAsset = "winx86.zip";     SetupAsset = "setupwinx86.zip";     AppProfile = "winx86";     SetupProfile = "winx86";     WrapperProfile = "winx86" } }
        "win-arm64"   { return @{ AppAsset = "winarm64.zip";   SetupAsset = "setupwinarm64.zip";   AppProfile = "winarm64";   SetupProfile = "winarm64";   WrapperProfile = "winarm64" } }
        "linux-x64"   { return @{ AppAsset = "linuxx64.zip";   SetupAsset = "setuplinuxx64.zip";   AppProfile = "linuxx64";   SetupProfile = "linuxx64";   WrapperProfile = $null } }
        "linux-arm64" { return @{ AppAsset = "linuxarm64.zip"; SetupAsset = "setuplinuxarm64.zip"; AppProfile = "linuxarm64"; SetupProfile = "linuxarm64"; WrapperProfile = $null } }
        "osx-x64"     { return @{ AppAsset = "macosx64.zip";   SetupAsset = "setupmacosx64.zip";   AppProfile = "macosx64";   SetupProfile = "macosx64";   WrapperProfile = $null } }
        "osx-arm64"   { return @{ AppAsset = "macosarm64.zip"; SetupAsset = "setupmacosarm64.zip"; AppProfile = "macosarm64"; SetupProfile = "macosarm64"; WrapperProfile = $null } }
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
    $appFolder = Resolve-ProfilePublishFolder -ProjectPath $appProject -ProfileName $profile.AppProfile
    $setupFolder = Resolve-ProfilePublishFolder -ProjectPath $setupProject -ProfileName $profile.SetupProfile
    $appZip = Join-Path $artifacts $profile.AppAsset
    $setupZip = Join-Path $artifacts $profile.SetupAsset

    Remove-Item $appFolder, $setupFolder, $appZip, $setupZip -Recurse -Force -ErrorAction SilentlyContinue

    $appExecutable = if ($Rid.StartsWith("win-")) { "LocalGPT.exe" } else { "LocalGPT" }
    $setupExecutable = if ($Rid.StartsWith("win-")) { "LocalGPTInstallerConsole.exe" } else { "LocalGPTInstallerConsole" }

    Write-Host "Publishing LocalGPT application through profile $($profile.AppProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @(
        "publish", $appProject,
        "-p:PublishProfile=$($profile.AppProfile)",
        "-p:RequireLocalGptDocumentationPdf=false"
    ) -FailureMessage "LocalGPT application publish failed for $Rid."

    Write-Host "Publishing LocalGPT setup through profile $($profile.SetupProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @(
        "publish", $setupProject,
        "-p:PublishProfile=$($profile.SetupProfile)"
    ) -FailureMessage "LocalGPT setup publish failed for $Rid."

    if (-not (Test-Path (Join-Path $appFolder $appExecutable))) {
        throw "Published LocalGPT executable not found in the publish-profile output: $(Join-Path $appFolder $appExecutable)"
    }
    if (-not (Test-Path (Join-Path $setupFolder $setupExecutable))) {
        throw "Published LocalGPT setup executable not found in the publish-profile output: $(Join-Path $setupFolder $setupExecutable)"
    }

    $requiredSetupFiles = @(
        "Default.cmd", "Install.cmd", "Update.cmd", "Start.cmd", "Start-NoBrowser.cmd",
        "Install-Ollama.cmd", "Pull-Models-Slim.cmd", "Pull-Models-RTX3060.cmd",
        "Pull-Models-Full.cmd", "Setup-Learning-Base.cmd", "Import-Recommended.cmd", "Uninstall.cmd"
    )
 
    $protocolSetupDirectory = Join-Path $setupFolder "protocol"
    New-Item -ItemType Directory -Path $protocolSetupDirectory -Force | Out-Null
    Copy-Item $wirePackage (Join-Path $protocolSetupDirectory $wirePackageName) -Force

    if ($IncludeWindowsWrapper -and $profile.WrapperProfile) {
        $wrapperFolder = Resolve-ProfilePublishFolder -ProjectPath $wrapperProject -ProfileName $profile.WrapperProfile
        Remove-Item $wrapperFolder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Publishing the optional WinUI wrapper through profile $($profile.WrapperProfile)..." -ForegroundColor Cyan
        Invoke-DotNet -Arguments @(
            "publish", $wrapperProject,
            "-p:PublishProfile=$($profile.WrapperProfile)"
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
