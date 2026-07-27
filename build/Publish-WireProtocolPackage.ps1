param(
    [string]$Configuration = "Release",
    [string]$Version = "2.0.1",
    [string[]]$ReleaseDirectories = @()
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "LocalGPTWebviewWrapper\LocalGPT.WireProtocolVersion\LocalGPT.WireProtocolVersion.csproj"
$packageRoot = Join-Path $repoRoot "artifacts\release\protocol"
$packageName = "LocalGPT.WireProtocolVersion.$Version.nupkg"
$packagePath = Join-Path $packageRoot $packageName

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
dotnet pack $project -c $Configuration -p:PackageVersion=$Version -p:RuntimeIdentifier= -p:RuntimeIdentifiers= -p:Platform=AnyCPU -o $packageRoot
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $packagePath)) {
    throw "The authoritative LocalGPT 1-Wire NuGet package was not created: $packagePath"
}

foreach ($directory in $ReleaseDirectories) {
    if ([string]::IsNullOrWhiteSpace($directory)) { continue }
    $resolved = if ([IO.Path]::IsPathRooted($directory)) { $directory } else { Join-Path $repoRoot $directory }
    New-Item -ItemType Directory -Path $resolved -Force | Out-Null
    Copy-Item $packagePath (Join-Path $resolved $packageName) -Force
}

Write-Host "Authoritative 1-Wire package: $packagePath" -ForegroundColor Green
