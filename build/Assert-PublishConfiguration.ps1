[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Fail([string]$Message) { throw "LocalGPT publish configuration validation failed: $Message" }

function Read-ProfileProperties([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Missing publish profile $RelativePath" }
    try { [xml]$document = [IO.File]::ReadAllText($path) }
    catch { Fail "Publish profile is not valid XML: $RelativePath. $($_.Exception.Message)" }

    $properties = @{}
    foreach ($group in @($document.Project.PropertyGroup)) {
        foreach ($node in @($group.ChildNodes)) {
            if ($node.NodeType -eq [Xml.XmlNodeType]::Element) { $properties[$node.Name] = [string]$node.InnerText }
        }
    }
    return $properties
}

function Assert-Property([hashtable]$Properties, [string]$Name, [string]$Expected, [string]$RelativePath) {
    if (-not $Properties.ContainsKey($Name)) { Fail "$RelativePath does not define $Name." }
    if (-not [string]::Equals($Properties[$Name], $Expected, [StringComparison]::OrdinalIgnoreCase)) {
        Fail "$RelativePath defines $Name='$($Properties[$Name])'; expected '$Expected'."
    }
}

function Assert-Profile(
    [string]$RelativePath,
    [string]$Runtime,
    [string]$Folder,
    [string]$Platform,
    [string]$TargetFramework) {
    $properties = Read-ProfileProperties $RelativePath
    $output = "..\..\artifacts\release\$Folder\"
    foreach ($requirement in @(
        @{ Name = 'RuntimeIdentifier'; Value = $Runtime },
        @{ Name = 'SelfContained'; Value = 'true' },
        @{ Name = 'PublishSingleFile'; Value = 'false' },
        @{ Name = 'PublishTrimmed'; Value = 'false' },
        @{ Name = 'PublishReadyToRun'; Value = 'false' },
        @{ Name = 'DeleteExistingFiles'; Value = 'true' },
        @{ Name = 'PublishProtocol'; Value = 'FileSystem' },
        @{ Name = 'Platform'; Value = $Platform },
        @{ Name = 'TargetFramework'; Value = $TargetFramework },
        @{ Name = 'PublishUrl'; Value = $output },
        @{ Name = 'PublishDir'; Value = $output }
    )) {
        Assert-Property $properties $requirement.Name $requirement.Value $RelativePath
    }
}

$appProfiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; App = 'winx64'; Setup = 'setupwinx64' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; App = 'winx86'; Setup = 'setupwinx86' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; App = 'winarm64'; Setup = 'setupwinarm64' },
    @{ File = 'linuxx64.pubxml'; Runtime = 'linux-x64'; App = 'linuxx64'; Setup = 'setuplinuxx64' },
    @{ File = 'linuxarm64.pubxml'; Runtime = 'linux-arm64'; App = 'linuxarm64'; Setup = 'setuplinuxarm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; App = 'macosx64'; Setup = 'setupmacosx64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; App = 'macosarm64'; Setup = 'setupmacosarm64' }
)

$wrapperProfiles = @(
    @{ File = 'FolderProfile.pubxml'; Runtime = 'win-x64'; Platform = 'x64'; Folder = 'wrapper-winx64' },
    @{ File = 'win10-x64.pubxml'; Runtime = 'win-x64'; Platform = 'x64'; Folder = 'wrapper-winx64' },
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Platform = 'x64'; Folder = 'wrapper-winx64' },
    @{ File = 'win10-x86.pubxml'; Runtime = 'win-x86'; Platform = 'x86'; Folder = 'wrapper-winx86' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; Platform = 'x86'; Folder = 'wrapper-winx86' },
    @{ File = 'win10-arm64.pubxml'; Runtime = 'win-arm64'; Platform = 'ARM64'; Folder = 'wrapper-winarm64' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Platform = 'ARM64'; Folder = 'wrapper-winarm64' }
)

foreach ($profile in $appProfiles) {
    Assert-Profile "LocalGPTWebviewWrapper\LocalGPT\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.App 'AnyCPU' 'net10.0'
    Assert-Profile "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Setup 'AnyCPU' 'net10.0'
}
foreach ($profile in $wrapperProfiles) {
    Assert-Profile "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Folder $profile.Platform 'net10.0-windows10.0.26100.0'
}

$userProfiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper') -Recurse -File -Filter '*.pubxml.user' -ErrorAction SilentlyContinue)
if ($userProfiles.Count -gt 0) { Fail 'Machine-specific .pubxml.user files must not be shipped in the source package.' }

$migrationPath = Join-Path $root 'build\Migrate-ObsoletePublishConfiguration.ps1'
if (-not (Test-Path -LiteralPath $migrationPath -PathType Leaf)) { Fail 'The publish-profile overlay migration script is missing.' }
$migration = [IO.File]::ReadAllText($migrationPath)
if ($migration.Contains('Remove-ObsoleteProfileRoot') -or $migration.Contains(".Extension -eq '.pubxml'")) {
    Fail 'The migration script must preserve developer .pubxml profiles.'
}
if (-not $migration.Contains('*.pubxml.user')) { Fail 'The migration script must still clean machine-specific .pubxml.user overlays.' }

$release = [IO.File]::ReadAllText((Join-Path $root 'Build-Release.ps1'))
if (-not $release.Contains('$multiFileSelfContainedProperties = @(')) { Fail 'Build-Release.ps1 must own one shared multi-file self-contained property list.' }
if (([regex]::Matches($release, '\+\s*\$multiFileSelfContainedProperties')).Count -ne 3) { Fail 'Build-Release.ps1 must apply the shared publish properties to the application, setup and WinUI wrapper.' }
foreach ($profile in $appProfiles) {
    foreach ($fragment in @(
        '"' + $profile.Runtime + '"',
        'AppFolder = "' + $profile.App + '"',
        'SetupFolder = "' + $profile.Setup + '"',
        'AppAsset = "' + $profile.App + '.zip"',
        'SetupAsset = "' + $profile.Setup + '.zip"'
    )) {
        if (-not $release.Contains($fragment)) { Fail "Build-Release.ps1 is missing synchronized mapping: $fragment" }
    }
}
if ($release -match 'PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true') {
    Fail 'The scripted release lane must remain multi-file and self-contained like the developer profiles.'
}

Write-Host 'LocalGPT publish configuration validation passed for 7 application profiles, 7 setup profiles, 7 wrapper/developer profiles and the synchronized scripted release lane.'
