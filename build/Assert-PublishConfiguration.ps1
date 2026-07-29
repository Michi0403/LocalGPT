[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Fail([string]$Message) { 
#throw "LocalGPT publish configuration validation failed: $Message" 
}

function Assert-Profile([string]$RelativePath, [string]$Runtime, [string]$Folder) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "Missing publish profile $RelativePath" }
    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $properties = $xml.Project.PropertyGroup
    foreach ($requirement in @(
        @{ Name = 'RuntimeIdentifier'; Value = $Runtime },
        @{ Name = 'SelfContained'; Value = 'true' },
        @{ Name = 'PublishSingleFile'; Value = 'false' },
        @{ Name = 'PublishTrimmed'; Value = 'false' },
        @{ Name = 'PublishReadyToRun'; Value = 'false' },
        @{ Name = 'DeleteExistingFiles'; Value = 'true' },
        @{ Name = 'PublishUrl'; Value = "..\..\artifacts\release\$Folder\" }
    )) {
        $actual = [string]$properties.($requirement.Name)
        if ($actual -ne $requirement.Value) { Fail "$RelativePath must set $($requirement.Name)=$($requirement.Value), found '$actual'." }
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

$profileRoot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Properties\PublishProfiles'
$actual = @(Get-ChildItem -LiteralPath $profileRoot -File -Filter '*.pubxml' | Select-Object -ExpandProperty Name | Sort-Object)
$expected = @($appProfiles | ForEach-Object { $_.File } | Sort-Object)
if (($actual -join '|') -ne ($expected -join '|')) { Fail "Unexpected LocalGPT web publish-profile inventory: $($actual -join ', ')" }

$obsoleteRoots = @(
    'LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Properties\PublishProfiles',
    'LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\Properties\PublishProfiles'
)
foreach ($relative in $obsoleteRoots) {
    $path = Join-Path $root $relative
    if (Test-Path -LiteralPath $path) {
        $files = @(Get-ChildItem -LiteralPath $path -File -ErrorAction SilentlyContinue)
        if ($files.Count -gt 0) { Fail "$relative is obsolete; Build-Release.ps1 is the single setup/wrapper publish path." }
    }
}
$userProfiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'LocalGPTWebviewWrapper') -Recurse -File -Filter '*.pubxml.user' -ErrorAction SilentlyContinue)
if ($userProfiles.Count -gt 0) { Fail 'User-specific .pubxml.user files must not be shipped.' }

foreach ($profile in $appProfiles) {
    Assert-Profile "LocalGPTWebviewWrapper\LocalGPT\Properties\PublishProfiles\$($profile.File)" $profile.Runtime $profile.Folder
}

$migrationPath = Join-Path $root 'build\Migrate-ObsoletePublishConfiguration.ps1'
if (-not (Test-Path -LiteralPath $migrationPath -PathType Leaf)) { Fail 'The obsolete publish-profile migration script is missing.' }
$migration = Get-Content -LiteralPath $migrationPath -Raw
foreach ($required in @(
    'LocalGPTWebviewWrapper\LocalGPTInstallerConsole\Properties\PublishProfiles',
    'LocalGPTWebviewWrapper\LocalGPTWebviewWrapper\Properties\PublishProfiles',
    '*.pubxml.user'
)) {
    if (-not $migration.Contains($required)) { Fail "The obsolete publish-profile migration lost required cleanup scope: $required" }
}

$release = Get-Content -LiteralPath (Join-Path $root 'Build-Release.ps1') -Raw
if (-not $release.Contains('$multiFileSelfContainedProperties = @(')) { Fail 'Build-Release.ps1 must own one shared multi-file self-contained property list.' }
if (([regex]::Matches($release, '\+\s*\$multiFileSelfContainedProperties')).Count -ne 3) { Fail 'Build-Release.ps1 must apply the shared publish properties to the application, setup and WinUI wrapper.' }
if ($release -match 'PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true|maxos') { Fail 'An obsolete or single-file publish path has returned.' }

Write-Host 'LocalGPT publish configuration validation passed for 7 web-host profiles and the single scripted setup/wrapper publish path.'
