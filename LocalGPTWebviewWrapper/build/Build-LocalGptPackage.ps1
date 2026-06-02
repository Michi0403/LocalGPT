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
    "/p:UseSharedCompilation=false",
    "/p:BuildInParallel=false",
    "/m:1",
    "/v:minimal",
    "/nr:false"
)
