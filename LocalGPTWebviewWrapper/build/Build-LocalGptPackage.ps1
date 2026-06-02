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

dotnet publish $localGptProjectPath `
    -c $Configuration `
    -r $runtimeIdentifier `
    --self-contained false `
    "-p:Platform=$Platform" `
    -p:UseSharedCompilation=false

& $msbuild $solutionPath `
    /t:Restore `
    "/p:Platform=$Platform" `
    "/p:Configuration=$Configuration" `
    "/p:RuntimeIdentifier=$runtimeIdentifier" `
    /p:UseSharedCompilation=false `
    /p:BuildInParallel=false `
    /v:minimal `
    /nr:false

& $msbuild $solutionPath `
    "/p:Platform=$Platform" `
    "/p:Configuration=$Configuration" `
    "/p:RuntimeIdentifier=$runtimeIdentifier" `
    /p:UseSharedCompilation=false `
    /p:BuildInParallel=false `
    /m:1 `
    /v:minimal `
    /nr:false
