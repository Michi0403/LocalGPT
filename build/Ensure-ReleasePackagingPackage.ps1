param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.2"
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$packages = Join-Path $root "packages"
$package = Join-Path $packages "LocalGPT.ReleasePackaging.$Version.nupkg"
if (-not (Test-Path -LiteralPath $package -PathType Leaf)) {
    $packageOutput = @(& (Join-Path $root "build/Publish-ReleasePackagingPackage.ps1") -Configuration $Configuration -Version $Version)
    if ($packageOutput.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$packageOutput[0])) {
        throw "Release-packaging package publication returned $($packageOutput.Count) pipeline value(s); expected exactly one package path."
    }
    $package = [string]$packageOutput[0]
}
$toolRoot = Join-Path $root "artifacts/release-tools"
Remove-Item -LiteralPath $toolRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null

# Use an isolated NuGet configuration for this repository-local tool package.
# This avoids inheriting machine-wide package-source mapping, which rejects --add-source.
$nugetConfig = Join-Path $toolRoot "NuGet.ReleasePackaging.config"
$escapedPackages = [Security.SecurityElement]::Escape($packages)
$nugetConfigText = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalReleasePackages" value="$escapedPackages" />
    <add key="NuGetOrg" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
[IO.File]::WriteAllText($nugetConfig, $nugetConfigText, (New-Object Text.UTF8Encoding($false)))

& dotnet tool install LocalGPT.ReleasePackaging --tool-path $toolRoot --version $Version --configfile $nugetConfig --ignore-failed-sources | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0) { throw "LocalGPT.ReleasePackaging tool installation failed." }
$isWindowsHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
$commandName = if ($isWindowsHost) { "localgpt-release-packaging.exe" } else { "localgpt-release-packaging" }
$command = Join-Path $toolRoot $commandName
if (-not (Test-Path -LiteralPath $command -PathType Leaf)) { throw "Installed release-packaging tool was not found: $command" }
Write-Output ([string]$command)
