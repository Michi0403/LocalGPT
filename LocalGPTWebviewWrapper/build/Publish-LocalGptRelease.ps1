[CmdletBinding()]
param(
    [string]$Version = "",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64", "x86", "arm64")]
    [string[]]$Platforms = @("x64", "x86", "arm64"),

    [switch]$SkipBuild,
    [switch]$CreateGitHubRelease,
    [switch]$Draft
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$wrapperRoot = Join-Path $repoRoot "LocalGPTWebviewWrapper"
$buildScript = Join-Path $wrapperRoot "build\Build-LocalGptPackage.ps1"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $commit = (& git -C $repoRoot rev-parse --short HEAD 2>$null)
    if ([string]::IsNullOrWhiteSpace($commit)) {
        $commit = Get-Date -Format "yyyyMMdd-HHmmss"
    }

    $Version = "0.0.0-$commit"
}

$releaseRoot = Join-Path $repoRoot "artifacts\releases\$Version"
New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

Write-Host "LocalGPT release packaging"
Write-Host "Version: $Version"
Write-Host "Configuration: $Configuration"
Write-Host "Platforms: $($Platforms -join ', ')"
Write-Host "Output: $releaseRoot"

foreach ($platform in $Platforms) {
    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "== Building $platform =="
        & $buildScript -Configuration $Configuration -Platform $platform
    }

    $packageSearchRoot = Join-Path $env:TEMP "LocalGPTWebviewWrapper\AppPackages"
    $package = Get-ChildItem $packageSearchRoot -Recurse -Filter "*.msix" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*_${platform}_*" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $package) {
        throw "Could not find generated $platform MSIX under $packageSearchRoot"
    }

    $platformRoot = Join-Path $releaseRoot "LocalGPT-$Version-$platform"
    New-Item -ItemType Directory -Force -Path $platformRoot | Out-Null
    Copy-Item -LiteralPath $package.FullName -Destination $platformRoot -Force

    $symbols = Get-ChildItem (Split-Path -Parent $package.FullName) -Filter "*.appxsym" -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -ne $symbols) {
        Copy-Item -LiteralPath $symbols.FullName -Destination $platformRoot -Force
    }

    $notes = @"
# LocalGPT $Version $platform

This folder contains the LocalGPT Windows MSIX package for $platform.

Install notes:

1. Install the Windows App SDK runtime if Windows asks for it.
2. Trust or install the local development certificate for unsigned debug/test packages.
3. Open the `.msix` package or register a loose layout from Visual Studio during development.

For development setup, see the top-level README and `LocalGPTWebviewWrapper/readme.md`.
"@
    Set-Content -LiteralPath (Join-Path $platformRoot "README.md") -Value $notes -Encoding utf8

    $zipPath = Join-Path $releaseRoot "LocalGPT-$Version-$platform.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $platformRoot "*") -DestinationPath $zipPath
    Write-Host "Created $zipPath"
}

$manifestPath = Join-Path $releaseRoot "release-manifest.txt"
Get-ChildItem $releaseRoot -Filter "*.zip" | ForEach-Object {
    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName
    "{0}  {1}" -f $hash.Hash, $_.Name
} | Set-Content -LiteralPath $manifestPath -Encoding utf8

Write-Host ""
Write-Host "Release manifest: $manifestPath"

if ($CreateGitHubRelease) {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -eq $gh) {
        throw "GitHub CLI 'gh' was not found. Install it or upload the zip files from $releaseRoot manually."
    }

    $tag = "v$Version"
    $ghArgs = @("release", "create", $tag)
    if ($Draft) {
        $ghArgs += "--draft"
    }

    $ghArgs += "--title"
    $ghArgs += "LocalGPT $Version"
    $ghArgs += "--notes"
    $ghArgs += "LocalGPT Windows packages for $($Platforms -join ', '). See release-manifest.txt for SHA256 hashes."
    $ghArgs += (Get-ChildItem $releaseRoot -Filter "*.zip").FullName
    $ghArgs += $manifestPath

    & gh @ghArgs
}
