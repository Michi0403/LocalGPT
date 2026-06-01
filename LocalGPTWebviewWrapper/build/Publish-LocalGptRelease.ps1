[CmdletBinding()]
param(
    [string]$Version = "",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64", "x86", "arm64")]
    [string[]]$Platforms = @("x64", "x86", "arm64"),

    [string[]]$BackendRuntimeIdentifiers = @("win-x64", "linux-x64", "osx-x64", "osx-arm64"),

    [switch]$SkipBuild,
    [switch]$SkipWrapper,
    [switch]$SkipBackend,
    [switch]$CreateGitHubRelease,
    [switch]$Draft
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$wrapperRoot = Join-Path $repoRoot "LocalGPTWebviewWrapper"
$buildScript = Join-Path $wrapperRoot "build\Build-LocalGptPackage.ps1"
$backendProject = Join-Path $wrapperRoot "LocalGPT\LocalGPT.csproj"

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
Write-Host "Windows wrapper platforms: $($Platforms -join ', ')"
Write-Host "Backend runtime identifiers: $($BackendRuntimeIdentifiers -join ', ')"
Write-Host "Output: $releaseRoot"

if (-not $SkipWrapper) {
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

        & dotnet publish $backendProject `
            -c $Configuration `
            -r $rid `
            --self-contained false `
            -o $backendRoot `
            /p:Platform=AnyCPU `
            /p:UseSharedCompilation=false `
            /p:PublishSingleFile=false

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
    "- Minecraft builder diagnostics can generate and validate datapack workspaces without loading a large model.",
    "- SQLite knowledge and log tables help the council reuse compact project knowledge instead of bloated prompts.",
    "",
    "## Integrity",
    "",
    'SHA256 hashes are listed in `release-manifest.txt`.'
) -join [Environment]::NewLine
Set-Content -LiteralPath $releaseNotesPath -Value $releaseNotes -Encoding utf8

Write-Host ""
Write-Host "Release manifest: $manifestPath"
Write-Host "Release notes: $releaseNotesPath"

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
    $ghArgs += "--notes-file"
    $ghArgs += $releaseNotesPath
    $ghArgs += (Get-ChildItem $releaseRoot -Filter "*.zip").FullName
    $ghArgs += $manifestPath
    $ghArgs += $releaseNotesPath

    & gh @ghArgs
}
