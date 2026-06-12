[CmdletBinding()]
param(
    [string]$Version = "",
    [string]$PackageVersion = "",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64", "x86", "arm64")]
    [string[]]$Platforms = @("x64", "x86", "arm64"),

    [string[]]$BackendRuntimeIdentifiers = @("win-x64", "linux-x64", "osx-x64", "osx-arm64"),

    [switch]$SkipBuild,
    [switch]$SkipWrapper,
    [switch]$SkipBackend,
    [switch]$CreateGitHubRelease,
    [switch]$Draft,
    [switch]$AllowPartialGitHubRelease
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$wrapperRoot = Join-Path $repoRoot "LocalGPTWebviewWrapper"
$buildScript = Join-Path $wrapperRoot "build\Build-LocalGptPackage.ps1"
$backendProject = Join-Path $wrapperRoot "LocalGPT\LocalGPT.csproj"
$packageManifest = Join-Path $wrapperRoot "LocalGPTWebviewWrapper (Package)\Package.appxmanifest"
$sourceHygieneScript = Join-Path $repoRoot "build\Assert-SourceFormatting.ps1"
$script:originalPackageManifest = $null
$script:originalPackageManifestBytes = $null

function Resolve-AppxPackageVersion {
    param([string]$ReleaseVersion)

    $numbers = [regex]::Matches($ReleaseVersion, "\d+") | ForEach-Object { [int]$_.Value }
    $major = if ($numbers.Count -gt 0) { $numbers[0] } else { 0 }
    $minor = if ($numbers.Count -gt 1) { $numbers[1] } else { 0 }
    $build = if ($numbers.Count -gt 2) { $numbers[2] } else { 0 }
    $revision = if ($numbers.Count -gt 3 -and $numbers[3] -le 65535) {
        $numbers[3]
    }
    else {
        [int](Get-Date -Format "yy")
    }

    if ($major -lt 1) {
        $major = 1
    }

    $parts = @($major, $minor, $build, $revision)
    if (($parts | Where-Object { $_ -lt 0 -or $_ -gt 65535 }).Count -gt 0) {
        throw "Package version parts must be between 0 and 65535. Derived parts: $($parts -join '.')"
    }

    return $parts -join "."
}

function Set-PackageManifestVersion {
    param([string]$VersionToWrite)

    if (-not (Test-Path $packageManifest)) {
        throw "Package manifest not found: $packageManifest"
    }

    if (-not ($VersionToWrite -match "^\d+\.\d+\.\d+\.\d+$")) {
        throw "PackageVersion must be a four-part numeric MSIX version such as 0.1.1.2."
    }

    $parts = $VersionToWrite.Split(".") | ForEach-Object { [int]$_ }
    if (($parts | Where-Object { $_ -lt 0 -or $_ -gt 65535 }).Count -gt 0) {
        throw "PackageVersion parts must be between 0 and 65535: $VersionToWrite"
    }

    $script:originalPackageManifestBytes = [System.IO.File]::ReadAllBytes($packageManifest)
    $script:originalPackageManifest = [System.Text.Encoding]::UTF8.GetString($script:originalPackageManifestBytes)
    $updated = $script:originalPackageManifest -replace 'Version="\d+\.\d+\.\d+\.\d+"', "Version=`"$VersionToWrite`""
    Set-Content -LiteralPath $packageManifest -Value $updated -Encoding utf8
    Write-Host "Stamped MSIX package identity version: $VersionToWrite"
}

function Restore-PackageManifestVersion {
    if ($null -ne $script:originalPackageManifest) {
        [System.IO.File]::WriteAllBytes($packageManifest, $script:originalPackageManifestBytes)
        $script:originalPackageManifest = $null
        $script:originalPackageManifestBytes = $null
        Write-Host "Restored checked-in package manifest version."
    }
}

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function ConvertTo-ReleaseVersionParts {
    param([string]$ReleaseVersion)

    $match = [regex]::Match($ReleaseVersion, "^(?:v)?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)")
    if (-not $match.Success) {
        throw "Release version must start with semantic major.minor.patch: $ReleaseVersion"
    }

    return [pscustomobject]@{
        Original = $ReleaseVersion
        Major = [int]$match.Groups["major"].Value
        Minor = [int]$match.Groups["minor"].Value
        Patch = [int]$match.Groups["patch"].Value
    }
}

function Compare-ReleaseVersionParts {
    param(
        [Parameter(Mandatory = $true)]
        $Left,

        [Parameter(Mandatory = $true)]
        $Right
    )

    if ($Left.Major -ne $Right.Major) {
        return $Left.Major.CompareTo($Right.Major)
    }

    if ($Left.Minor -ne $Right.Minor) {
        return $Left.Minor.CompareTo($Right.Minor)
    }

    return $Left.Patch.CompareTo($Right.Patch)
}

function Resolve-GitHubCli {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -ne $gh) {
        return $gh.Source
    }

    $ghInstallPath = Join-Path $env:ProgramFiles "GitHub CLI\gh.exe"
    if (Test-Path $ghInstallPath) {
        return $ghInstallPath
    }

    return ""
}

function Invoke-SourceHygieneGuard {
    Write-Host ""
    Write-Host "== Source hygiene guard =="

    if (-not (Test-Path $sourceHygieneScript)) {
        throw "Source hygiene script not found: $sourceHygieneScript"
    }

    & $sourceHygieneScript
    if ($LASTEXITCODE -ne 0) {
        throw "Source hygiene guard failed. Do not publish this release."
    }
}

function Assert-ReleaseVersionIsNewerThanPublished {
    param([string]$ReleaseVersion)

    $candidate = ConvertTo-ReleaseVersionParts $ReleaseVersion
    $published = @()

    $localTags = & git -C $repoRoot tag --list "v*" 2>$null
    foreach ($tag in $localTags) {
        try {
            $published += ConvertTo-ReleaseVersionParts $tag
        }
        catch {
            Write-Warning "Ignoring non-semantic local tag $tag"
        }
    }

    if ($CreateGitHubRelease) {
        $ghCommand = Resolve-GitHubCli
        if ([string]::IsNullOrWhiteSpace($ghCommand)) {
            throw "GitHub CLI 'gh' was not found. Cannot prove release version ordering."
        }

        $releaseLines = & $ghCommand release list --limit 100 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not list GitHub releases. Cannot prove release version ordering."
        }

        foreach ($line in $releaseLines) {
            $match = [regex]::Match($line, "v(?<version>\d+\.\d+\.\d+[^\s]*)")
            if ($match.Success) {
                try {
                    $published += ConvertTo-ReleaseVersionParts $match.Groups["version"].Value
                }
                catch {
                    Write-Warning "Ignoring non-semantic GitHub release entry: $line"
                }
            }
        }
    }

    $highest = $null
    foreach ($publishedVersion in $published) {
        if ($null -eq $highest -or (Compare-ReleaseVersionParts $publishedVersion $highest) -gt 0) {
            $highest = $publishedVersion
        }
    }

    if ($null -ne $highest -and (Compare-ReleaseVersionParts $candidate $highest) -le 0) {
        throw "Release version $ReleaseVersion must be higher than existing public release $($highest.Original)."
    }

    if ($null -ne $highest) {
        Write-Host "Release version $ReleaseVersion is higher than existing release $($highest.Original)."
    }
}

function Assert-PublicReleasePayloadSelection {
    if (-not $CreateGitHubRelease -or $AllowPartialGitHubRelease) {
        return
    }

    $requiredPlatforms = @("x64", "x86", "arm64")
    $requiredRuntimeIdentifiers = @(
        "win-x64",
        "linux-x64",
        "osx-x64",
        "osx-arm64"
    )

    if ($SkipWrapper -or $SkipBackend) {
        throw "Public GitHub releases must include wrapper and backend payloads. Use -AllowPartialGitHubRelease only for an explicitly requested diagnostic release."
    }

    $missingPlatforms = $requiredPlatforms |
        Where-Object { -not ($Platforms -contains $_) }

    $missingRuntimeIdentifiers = $requiredRuntimeIdentifiers |
        Where-Object { -not ($BackendRuntimeIdentifiers -contains $_) }

    if ($missingPlatforms.Count -gt 0 -or $missingRuntimeIdentifiers.Count -gt 0) {
        $missing = @()
        $missing += $missingPlatforms | ForEach-Object { "windows-$_" }
        $missing += $missingRuntimeIdentifiers
        throw "Public GitHub release payload is incomplete. Missing: $($missing -join ', ')."
    }
}

function Assert-PublicReleasePayloadArtifacts {
    if (-not $CreateGitHubRelease -or $AllowPartialGitHubRelease) {
        return
    }

    $expectedFiles = @(
        "LocalGPT-WebView2-$Version-windows-x64.zip",
        "LocalGPT-WebView2-$Version-windows-x86.zip",
        "LocalGPT-WebView2-$Version-windows-arm64.zip",
        "LocalGPT-Backend-$Version-win-x64.zip",
        "LocalGPT-Backend-$Version-linux-x64.zip",
        "LocalGPT-Backend-$Version-osx-x64.zip",
        "LocalGPT-Backend-$Version-osx-arm64.zip",
        "release-manifest.txt",
        "release-notes.md"
    )

    $missing = $expectedFiles |
        Where-Object { -not (Test-Path (Join-Path $releaseRoot $_)) }

    if ($missing.Count -gt 0) {
        throw "Public GitHub release payload files are missing: $($missing -join ', ')."
    }
}

trap {
    Restore-PackageManifestVersion
    throw $_
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $commit = (& git -C $repoRoot rev-parse --short HEAD 2>$null)
    if ([string]::IsNullOrWhiteSpace($commit)) {
        $commit = Get-Date -Format "yyyyMMdd-HHmmss"
    }

    $Version = "0.0.0-$commit"
}

if ([string]::IsNullOrWhiteSpace($PackageVersion)) {
    $PackageVersion = Resolve-AppxPackageVersion $Version
}

Assert-ReleaseVersionIsNewerThanPublished $Version
Assert-PublicReleasePayloadSelection
Invoke-SourceHygieneGuard

$releaseRoot = Join-Path $repoRoot "artifacts\releases\$Version"
New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

Write-Host "LocalGPT release packaging"
Write-Host "Version: $Version"
Write-Host "MSIX package version: $PackageVersion"
Write-Host "Configuration: $Configuration"
Write-Host "Windows wrapper platforms: $($Platforms -join ', ')"
Write-Host "Backend runtime identifiers: $($BackendRuntimeIdentifiers -join ', ')"
Write-Host "Output: $releaseRoot"

if (-not $SkipWrapper) {
    Set-PackageManifestVersion $PackageVersion

    foreach ($platform in $Platforms) {
        if (-not $SkipBuild) {
            Write-Host ""
            Write-Host "== Building Windows WebView2 wrapper $platform =="
            & $buildScript -Configuration $Configuration -Platform $platform
        }

        $packageSearchRoot = Join-Path $env:TEMP "LocalGPTWebviewWrapper\AppPackages"
        $package = Get-ChildItem $packageSearchRoot -Recurse -Filter "*.msix" -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "*_${platform}.msix" -or $_.Name -like "*_${platform}_*.msix" } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($null -eq $package) {
            throw "Could not find generated $platform MSIX under $packageSearchRoot"
        }

        $platformRoot = Join-Path $releaseRoot "LocalGPT-WebView2-$Version-windows-$platform"
        if (Test-Path $platformRoot) {
            Remove-Item $platformRoot -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $platformRoot | Out-Null
        Copy-Item -LiteralPath $package.FullName -Destination $platformRoot -Force

        $symbols = Get-ChildItem (Split-Path -Parent $package.FullName) -Filter "*.appxsym" -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($null -ne $symbols) {
            Copy-Item -LiteralPath $symbols.FullName -Destination $platformRoot -Force
        }

        $notes = @"
# LocalGPT $Version Windows WebView2 wrapper $platform

This folder contains the LocalGPT Windows desktop MSIX package for $platform.

Install notes:

1. Install the Windows App SDK runtime if Windows asks for it.
2. Trust or install the local development certificate for unsigned debug/test packages.
3. Open the `.msix` package or register a loose layout from Visual Studio during development.

The WebView2 wrapper is Windows-only. For Linux and macOS, use the backend-only release zips.

For development setup, see the top-level README and `LocalGPTWebviewWrapper/readme.md`.
"@
        Set-Content -LiteralPath (Join-Path $platformRoot "README.md") -Value $notes -Encoding utf8

        $zipPath = Join-Path $releaseRoot "LocalGPT-WebView2-$Version-windows-$platform.zip"
        if (Test-Path $zipPath) {
            Remove-Item $zipPath -Force
        }

        Compress-Archive -Path (Join-Path $platformRoot "*") -DestinationPath $zipPath
        Write-Host "Created $zipPath"
    }

    Restore-PackageManifestVersion
}

if (-not $SkipBackend) {
    foreach ($rid in $BackendRuntimeIdentifiers) {
        Write-Host ""
        Write-Host "== Publishing backend $rid =="

        $backendRoot = Join-Path $releaseRoot "LocalGPT-Backend-$Version-$rid"
        if (Test-Path $backendRoot) {
            Remove-Item $backendRoot -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $backendRoot | Out-Null

        Invoke-CheckedNative "dotnet" @(
            "publish",
            $backendProject,
            "-c",
            $Configuration,
            "-r",
            $rid,
            "--self-contained",
            "false",
            "-o",
            $backendRoot,
            "/p:Platform=AnyCPU",
            "/p:UseSharedCompilation=false",
            "/p:PublishSingleFile=false"
        )

        $runCommand = if ($rid.StartsWith("win-", [StringComparison]::OrdinalIgnoreCase)) {
            ".\LocalGPT.exe"
        }
        else {
            "dotnet LocalGPT.dll"
        }

        $notes = @"
# LocalGPT $Version backend $rid

This folder contains the ASP.NET Core/Blazor backend-only LocalGPT publish for `$rid`.

Run notes:

1. Install the matching .NET 10 ASP.NET Core runtime for this platform.
2. Start Ollama or LM Studio on the same machine or adjust LocalGPT settings after launch.
3. Run:

```powershell
$runCommand
```

The backend opens a local HTTP server on `127.0.0.1` using a free port and writes its runtime endpoint to:

```text
%LOCALAPPDATA%\LocalGPT\runtime\server.json
```

On Linux/macOS, use the platform-equivalent local application data folder and open the printed localhost URL from the console.

The WinUI/WebView2 desktop wrapper is not included in this backend zip and is Windows-only.
"@
        Set-Content -LiteralPath (Join-Path $backendRoot "README.md") -Value $notes -Encoding utf8

        $zipPath = Join-Path $releaseRoot "LocalGPT-Backend-$Version-$rid.zip"
        if (Test-Path $zipPath) {
            Remove-Item $zipPath -Force
        }

        Compress-Archive -Path (Join-Path $backendRoot "*") -DestinationPath $zipPath
        Write-Host "Created $zipPath"
    }
}

$manifestPath = Join-Path $releaseRoot "release-manifest.txt"
Get-ChildItem $releaseRoot -Filter "*.zip" | ForEach-Object {
    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName
    "{0}  {1}" -f $hash.Hash, $_.Name
} | Set-Content -LiteralPath $manifestPath -Encoding utf8

$assetList = (Get-ChildItem $releaseRoot -Filter "*.zip" | Sort-Object Name | ForEach-Object { '- `{0}`' -f $_.Name }) -join [Environment]::NewLine
$releaseNotesPath = Join-Path $releaseRoot "release-notes.md"
$releaseNotes = @(
    "# LocalGPT $Version",
    "",
    "LocalGPT is packaged here as Windows desktop WebView2/MSIX builds plus portable backend-only ASP.NET Core/Blazor builds for machines that should connect to Ollama or LM Studio without the WinUI wrapper.",
    "",
    "## Release Assets",
    "",
    $assetList,
    "",
    "## Recommended Install Path",
    "",
    ('For Windows desktop use, start with the `LocalGPT-WebView2-{0}-windows-x64.zip` asset on normal Intel/AMD Windows PCs. Use `windows-arm64` only on ARM Windows devices and `windows-x86` only for legacy 32-bit testing.' -f $Version),
    "",
    ('For Linux, macOS, or server-style debugging, use the matching `LocalGPT-Backend-{0}-<rid>.zip`, install the .NET 10 ASP.NET Core runtime, start Ollama or LM Studio, then run the backend from the extracted folder.' -f $Version),
    "",
    "## Highlights",
    "",
    "- DXAiChat and AI Council support Ollama model discovery, memory-backed conversations, visible model-thinking/status output, and safer cancellation.",
    '- Council-generated implementation artifacts can be downloaded through `/__artifacts/council/`, including real `.razor` files and compiled `.dll` examples when compilation succeeds.',
    "- AI host control-plane artifacts now include provider-compatible API route stubs, chat, model catalog UI, running models, model download planning, templates, hardware policy, logs, settings, and explicit native-runner boundaries.",
    "- Minecraft builder diagnostics can generate and validate datapack workspaces without loading a large model.",
    "- SQLite knowledge and log tables help the council reuse compact project knowledge instead of bloated prompts.",
    "- Official DevExpress, Microsoft, and Minecraft source-backed knowledge is seeded into SQLite for safer Blazor/DevExpress/WASM and datapack generation.",
    "- Council knowledge now carries explicit verification status values such as SourceBacked, UserVerified, ModelSuggested, NeedsVerification, and Archived.",
    "- The developer diary is seeded into council knowledge so future models learn the build, DXAiChat, model-handling, diagnostics, and release lessons from this project process.",
    "- Native Minecraft builder commands are allowlisted and logged to SQLite with stdout/stderr artifact paths.",
    "- Source and documentation physical lines are normalized for useful diffs, blame, PR review, diagnostics, and AI patching.",
    "",
    "## Integrity",
    "",
    'SHA256 hashes are listed in `release-manifest.txt`.'
) -join [Environment]::NewLine
Set-Content -LiteralPath $releaseNotesPath -Value $releaseNotes -Encoding utf8

Write-Host ""
Write-Host "Release manifest: $manifestPath"
Write-Host "Release notes: $releaseNotesPath"

Assert-PublicReleasePayloadArtifacts

if ($CreateGitHubRelease) {
    $ghCommand = Resolve-GitHubCli
    if ([string]::IsNullOrWhiteSpace($ghCommand)) {
        throw "GitHub CLI 'gh' was not found. Install it or upload the zip files from $releaseRoot manually."
    }

    $tag = "v$Version"
    $ghArgs = @("release", "create", $tag, "--latest")
    if ($Draft) {
        $ghArgs += "--draft"
    }

    $ghArgs += "--title"
    $ghArgs += "--title LocalGPT $Version"
    $ghArgs += "--notes-file"
    $ghArgs += "--draft"
    $ghArgs += $releaseNotesPath
    $ghArgs += (Get-ChildItem $releaseRoot -Filter "*.zip").FullName
    $ghArgs += $manifestPath
    $ghArgs += $releaseNotesPath

    Invoke-CheckedNative $ghCommand $ghArgs
}
