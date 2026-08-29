param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$packages = Join-Path $root "packages"
$package = Join-Path $packages "LocalGPT.ReleasePackaging.$Version.nupkg"
if (-not (Test-Path -LiteralPath $package -PathType Leaf)) {
    $package = & (Join-Path $root "build/Publish-ReleasePackagingPackage.ps1") -Configuration $Configuration -Version $Version
}
$toolRoot = Join-Path $root "artifacts/release-tools"
Remove-Item -LiteralPath $toolRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null
& dotnet tool install LocalGPT.ReleasePackaging --tool-path $toolRoot --version $Version --add-source $packages --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw "LocalGPT.ReleasePackaging tool installation failed." }
$isWindowsHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
$commandName = if ($isWindowsHost) { "localgpt-release-packaging.exe" } else { "localgpt-release-packaging" }
$command = Join-Path $toolRoot $commandName
if (-not (Test-Path -LiteralPath $command -PathType Leaf)) { throw "Installed release-packaging tool was not found: $command" }
Write-Output $command
