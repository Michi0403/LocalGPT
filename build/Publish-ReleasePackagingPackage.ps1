param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$project = Join-Path $root "src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj"
$packages = Join-Path $root "packages"
New-Item -ItemType Directory -Path $packages -Force | Out-Null
& dotnet pack $project -c $Configuration -o $packages "-p:PackageVersion=$Version" "-p:GeneratePackageOnBuild=false"
if ($LASTEXITCODE -ne 0) { throw "LocalGPT.ReleasePackaging package creation failed." }
$package = Join-Path $packages "LocalGPT.ReleasePackaging.$Version.nupkg"
if (-not (Test-Path -LiteralPath $package -PathType Leaf)) { throw "Expected release-packaging package was not created: $package" }
Write-Output $package
