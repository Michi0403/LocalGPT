[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x64", "x86", "arm64")]
    [string]$Platform = "x64",

    [switch]$KeepRunningApp
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solutionPath = Join-Path $repoRoot "LocalGPTWebviewWrapper.sln"
$localGptProjectPath = Join-Path $repoRoot "LocalGPT\LocalGPT.csproj"

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

function Remove-IntermediateDirectory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if (-not $resolved.StartsWith($repoRoot.Path, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove intermediate directory outside the repository: $resolved"
    }

    Remove-Item -LiteralPath $resolved -Recurse -Force
}

function Assert-MsixStaticWebAssets {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackagePath
    )

    Add-Type -AssemblyName System.IO.Compression

    $stream = [System.IO.File]::OpenRead($PackagePath)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new(
            $stream,
            [System.IO.Compression.ZipArchiveMode]::Read)
        try {
            $entryNames = [System.Collections.Generic.HashSet[string]]::new(
                [System.StringComparer]::OrdinalIgnoreCase)

            foreach ($entry in $archive.Entries) {
                [void]$entryNames.Add($entry.FullName.Replace("\", "/"))
            }

            $requiredEntries = @(
                "LocalGPTWebviewWrapper/wwwroot/_framework/blazor.web.js",
                "LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor/dx-blazor.svg",
                "LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor.Themes/office-white.bs5.min.css",
                "LocalGPTWebviewWrapper/wwwroot/LocalGPT.styles.css"
            )

            $missingEntries = $requiredEntries | Where-Object { -not $entryNames.Contains($_) }
            if ($missingEntries.Count -gt 0) {
                throw "MSIX package is missing required Blazor/DevExpress static assets: $($missingEntries -join ', ')"
            }

            Write-Host "Verified MSIX Blazor/DevExpress static web assets."
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

$msbuildCandidates = @(
    "$env:ProgramFiles\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
    "$env:ProgramFiles\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "$env:ProgramFiles\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "$env:ProgramFiles\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe",
    "$env:ProgramFiles\Microsoft Visual Studio\17\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "$env:ProgramFiles\Microsoft Visual Studio\17\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuild = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($null -eq $msbuild) {
    throw "Could not find Visual Studio MSBuild."
}

if (-not $KeepRunningApp) {
    Get-Process LocalGPTWebviewWrapper -ErrorAction SilentlyContinue | Stop-Process -Force
}

$runtimeIdentifier = switch ($Platform) {
    "x86" { "win-x86" }
    "x64" { "win-x64" }
    "arm64" { "win-arm64" }
}

@(
    "LocalGPT\obj\$Platform\$Configuration",
    "LocalGPT\obj\$Configuration",
    "LocalGPTWebviewWrapper\obj\$Platform\$Configuration",
    "LocalGPTWebviewWrapper\obj\$Configuration",
    "LocalGPTWebviewWrapper (Package)\obj\$Platform\$Configuration",
    "LocalGPTWebviewWrapper (Package)\obj\$Configuration"
) | ForEach-Object {
    Remove-IntermediateDirectory (Join-Path $repoRoot $_)
}

Invoke-CheckedNative "dotnet" @(
    "publish",
    $localGptProjectPath,
    "-c",
    $Configuration,
    "-r",
    $runtimeIdentifier,
    "--self-contained",
    "false",
    "-p:Platform=$Platform",
    "-p:UseSharedCompilation=false"
)

Invoke-CheckedNative $msbuild @(
    $solutionPath,
    "/t:Restore",
    "/p:Platform=$Platform",
    "/p:Configuration=$Configuration",
    "/p:RuntimeIdentifier=$runtimeIdentifier",
    "/p:IncludeLocalGptPublishedPayload=true",
    "/p:UseSharedCompilation=false",
    "/p:BuildInParallel=false",
    "/v:minimal",
    "/nr:false"
)

Invoke-CheckedNative $msbuild @(
    $solutionPath,
    "/p:Platform=$Platform",
    "/p:Configuration=$Configuration",
    "/p:RuntimeIdentifier=$runtimeIdentifier",
    "/p:IncludeLocalGptPublishedPayload=true",
    "/p:UseSharedCompilation=false",
    "/p:BuildInParallel=false",
    "/m:1",
    "/v:minimal",
    "/nr:false"
)

$packageSearchRoot = Join-Path $env:TEMP "LocalGPTWebviewWrapper\AppPackages"
$package = Get-ChildItem $packageSearchRoot -Recurse -Filter "*.msix" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "*_${Platform}.msix" -or $_.Name -like "*_${Platform}_*.msix" } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $package) {
    throw "Could not find generated $Platform MSIX under $packageSearchRoot"
}

Assert-MsixStaticWebAssets -PackagePath $package.FullName
