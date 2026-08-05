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
    [string]$TargetFramework,
    [string]$PublishSingleFile) {
    $properties = Read-ProfileProperties $RelativePath
    $output = "..\..\artifacts\release\$Folder\"
    foreach ($requirement in @(
        @{ Name = 'Configuration'; Value = 'Release' },
        @{ Name = 'RuntimeIdentifier'; Value = $Runtime },
        @{ Name = 'SelfContained'; Value = 'true' },
        @{ Name = 'PublishSingleFile'; Value = $PublishSingleFile },
        @{ Name = 'PublishTrimmed'; Value = 'false' },
        @{ Name = 'DeleteExistingFiles'; Value = 'true' },
        @{ Name = 'PublishProtocol'; Value = 'FileSystem' },
        @{ Name = 'Platform'; Value = $Platform },
        @{ Name = 'TargetFramework'; Value = $TargetFramework }
    )) {
        Assert-Property $properties $requirement.Name $requirement.Value $RelativePath
    }

    if ($properties.ContainsKey('PublishReadyToRun')) {
        Assert-Property $properties 'PublishReadyToRun' 'false' $RelativePath
    }

    $declaredOutput = @('PublishDir', 'PublishUrl') |
        Where-Object { $properties.ContainsKey($_) }
    if ($declaredOutput.Count -eq 0) {
        Fail "$RelativePath must define PublishDir or PublishUrl so release scripts can consume the profile-owned output."
    }
    foreach ($outputProperty in $declaredOutput) {
        Assert-Property $properties $outputProperty $output $RelativePath
    }
}

$profiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; App = 'winx64'; Setup = 'setupwinx64'; SetupPlatform = 'x64' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; App = 'winx86'; Setup = 'setupwinx86'; SetupPlatform = 'x86' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; App = 'winarm64'; Setup = 'setupwinarm64'; SetupPlatform = 'arm64' },
    @{ File = 'linuxx64.pubxml'; Runtime = 'linux-x64'; App = 'linuxx64'; Setup = 'setuplinuxx64'; SetupPlatform = 'x64' },
    @{ File = 'linuxarm64.pubxml'; Runtime = 'linux-arm64'; App = 'linuxarm64'; Setup = 'setuplinuxarm64'; SetupPlatform = 'arm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; App = 'macosx64'; Setup = 'setupmacosx64'; SetupPlatform = 'x64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; App = 'macosarm64'; Setup = 'setupmacosarm64'; SetupPlatform = 'arm64' }
)

foreach ($profile in $profiles) {
    Assert-Profile "src\LocalGPT\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.App 'AnyCPU' 'net10.0' 'false'
    Assert-Profile "src\LocalGPTInstallerConsole\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Setup $profile.SetupPlatform 'net10.0' 'true'
}

$wrapperProfiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Platform = 'x64'; Folder = 'wrapper-winx64' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; Platform = 'x86'; Folder = 'wrapper-winx86' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Platform = 'ARM64'; Folder = 'wrapper-winarm64' }
)
foreach ($profile in $wrapperProfiles) {
    Assert-Profile "src\src\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Folder $profile.Platform 'net10.0-windows10.0.26100.0' 'false'
}

$userProfiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper') -Recurse -File -Filter '*.pubxml.user' -ErrorAction SilentlyContinue)
if ($userProfiles.Count -gt 0) { Fail 'Machine-specific .pubxml.user files must not be shipped in the source package.' }

$release = [IO.File]::ReadAllText((Join-Path $root 'Build-Release.ps1'))
foreach ($required in @(
    '-p:PublishProfile=$($profile.AppProfile)',
    '-p:PublishProfile=$($profile.SetupProfile)',
    '-p:PublishProfile=$($profile.WrapperProfile)'
)) {
    if (-not $release.Contains($required)) { Fail "Build-Release.ps1 must publish through checked-in profiles: $required" }
}
foreach ($forbidden in @(
    '-p:PublishSingleFile=',
    '-p:SelfContained=',
    '--self-contained',
    '-p:PublishDir=',
    '-p:PublishUrl=',
    '-p:DebugType=',
    '-p:DebugSymbols='
)) {
    if ($release.Contains($forbidden)) { Fail "Build-Release.ps1 overrides profile-owned publish policy with $forbidden" }
}

$legacy = [IO.File]::ReadAllText((Join-Path $root 'build\PublishBlazorSolutionAndCreateZips.ps1'))
if (([regex]::Matches($legacy, '-p:PublishProfile=')).Count -lt 2) {
    Fail 'The legacy release script must publish both LocalGPT and its installer through checked-in profiles.'
}
foreach ($forbidden in @('-p:PublishSingleFile=', '-p:SelfContained=', '--self-contained', '-p:PublishDir=', '-p:PublishUrl=', '-p:DebugType=', '-p:DebugSymbols=')) {
    if ($legacy.Contains($forbidden)) { Fail "The legacy release script overrides profile-owned publish policy with $forbidden" }
}

Write-Host 'LocalGPT publish configuration validation passed: application, installer and wrapper profiles own publish behavior, and release scripts consume those profiles without overriding them.'
