[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Fail([string]$Message) {
    throw "LocalGPT publish configuration validation failed: $Message"
}

function Assert-Profile(
    [string]$RelativePath,
    [string]$Runtime,
    [string]$OutputProperty,
    [string]$Folder
) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Fail "Missing publish profile $RelativePath"
    }

    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $properties = $xml.Project.PropertyGroup
    $requirements = @(
        @{ Name = 'RuntimeIdentifier'; Value = $Runtime },
        @{ Name = 'SelfContained'; Value = 'true' },
        @{ Name = 'PublishSingleFile'; Value = 'false' },
        @{ Name = 'PublishTrimmed'; Value = 'false' },
        @{ Name = 'PublishReadyToRun'; Value = 'false' },
        @{ Name = 'DeleteExistingFiles'; Value = 'true' }
    )

    foreach ($requirement in $requirements) {
        $actual = [string]$properties.($requirement.Name)
        if ($actual -ne $requirement.Value) {
            Fail "$RelativePath must set $($requirement.Name)=$($requirement.Value), found '$actual'."
        }
    }

    $expectedOutput = "..\..\artifacts\release\$Folder\"
    $actualOutput = [string]$properties.$OutputProperty
    if ($actualOutput -ne $expectedOutput) {
        Fail "$RelativePath must set $OutputProperty to $expectedOutput, found '$actualOutput'."
    }
}

$appProfiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Folder = 'winx64' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; Folder = 'winx86' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Folder = 'winarm64' },
    @{ File = 'linuxx64.pubxml'; Runtime = 'linux-x64'; Folder = 'linuxx64' },
    @{ File = 'linuxarm64.pubxml'; Runtime = 'linux-arm64'; Folder = 'linuxarm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; Folder = 'macosx64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; Folder = 'macosarm64' }
)
$setupProfiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Folder = 'setupwinx64' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; Folder = 'setupwinx86' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Folder = 'setupwinarm64' },
    @{ File = 'linuxx64.pubxml'; Runtime = 'linux-x64'; Folder = 'setuplinuxx64' },
    @{ File = 'linuxarm64.pubxml'; Runtime = 'linux-arm64'; Folder = 'setuplinuxarm64' },
    @{ File = 'macosx64.pubxml'; Runtime = 'osx-x64'; Folder = 'setupmacosx64' },
    @{ File = 'macosarm64.pubxml'; Runtime = 'osx-arm64'; Folder = 'setupmacosarm64' }
)
$wrapperProfiles = @(
    @{ File = 'winx64.pubxml'; Runtime = 'win-x64'; Folder = 'wrapper-winx64' },
    @{ File = 'winx86.pubxml'; Runtime = 'win-x86'; Folder = 'wrapper-winx86' },
    @{ File = 'winarm64.pubxml'; Runtime = 'win-arm64'; Folder = 'wrapper-winarm64' }
)

foreach ($profile in $appProfiles) {
    Assert-Profile "LocalGPTWebviewWrapper\LocalGPT\Properties\PublishProfiles\$($profile.File)" $profile.Runtime 'PublishUrl' $profile.Folder
}
foreach ($profile in $setupProfiles) {
    Assert-Profile "LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Properties\PublishProfiles\$($profile.File)" $profile.Runtime 'PublishDir' $profile.Folder
}
foreach ($profile in $wrapperProfiles) {
    Assert-Profile "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\Properties\PublishProfiles\$($profile.File)" $profile.Runtime 'PublishDir' $profile.Folder
}

$release = Get-Content -LiteralPath (Join-Path $root 'Build-Release.ps1') -Raw
if (-not $release.Contains('$multiFileSelfContainedProperties = @(')) {
    Fail 'Build-Release.ps1 must own one shared multi-file self-contained property list.'
}
if (([regex]::Matches($release, '\+\s*\$multiFileSelfContainedProperties')).Count -ne 3) {
    Fail 'Build-Release.ps1 must apply the shared publish properties to the application, setup and WinUI wrapper.'
}
if ($release -match 'PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true') {
    Fail 'Single-file publishing is forbidden.'
}
if ($release -match 'maxos') {
    Fail 'The historical maxos publish-profile typo has returned.'
}

$projectFiles = @(
    'LocalGPTWebviewWrapper\LocalGPT\LocalGPT.csproj',
    'LocalGPTWebviewWrapper\LocalGPTInstallerConsole\LocalGPTInstallerConsole.csproj',
    'LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.csproj'
)
foreach ($projectFile in $projectFiles) {
    $content = Get-Content -LiteralPath (Join-Path $root $projectFile) -Raw
    if (-not $content.Contains('<SelfContained Condition="''$(RuntimeIdentifier)'' != ''''">true</SelfContained>')) {
        Fail "$projectFile must default RID publishes to self-contained."
    }
    foreach ($marker in @(
        '<PublishSingleFile>false</PublishSingleFile>',
        '<PublishTrimmed>false</PublishTrimmed>',
        '<PublishReadyToRun>false</PublishReadyToRun>'
    )) {
        if (-not $content.Contains($marker)) {
            Fail "$projectFile is missing $marker."
        }
    }
}


$installerReadme = Get-Content -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper\LocalGPTInstallerConsole\README.md') -Raw
if ($installerReadme -match '--self-contained\s+false|PublishSingleFile=true|<PublishSingleFile>true') {
    Fail 'Installer documentation must not recommend framework-dependent or single-file publishing.'
}
foreach ($marker in @('--self-contained true', 'PublishSingleFile=false')) {
    if (-not $installerReadme.Contains($marker)) {
        Fail "Installer documentation is missing the synchronized publish marker $marker."
    }
}

Write-Host 'LocalGPT publish configuration validation passed for 7 application, 7 setup and 3 wrapper profiles. All RID publishes are self-contained multi-file outputs.'
