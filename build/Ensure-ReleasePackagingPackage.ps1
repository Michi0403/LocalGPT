param(
    [string]$Configuration = "Release",
    [string]$Version = "1.1.2"
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$project = Join-Path $root 'src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj'
[xml]$packageProject = Get-Content -LiteralPath $project -Raw
$declaredPackageVersion = [string]$packageProject.Project.PropertyGroup.Version
if (-not [string]::Equals($Version, $declaredPackageVersion, [StringComparison]::Ordinal)) {
    throw "LocalGPT.ReleasePackaging requested package version '$Version' differs from the project version '$declaredPackageVersion'. Align the release script and project before using the package cache."
}
$packages = Join-Path $root "packages"
$package = Join-Path $packages "LocalGPT.ReleasePackaging.$Version.nupkg"
# The release must repair a cached-but-invalid local package itself. The old
# flow rebuilt only missing packages, then threw on stale license entries.
# Do not ask the operator to run a separate packaging command.
$packageNeedsRebuild = -not (Test-Path -LiteralPath $package -PathType Leaf)
if (-not $packageNeedsRebuild) {
    try {
        & (Join-Path $root 'build/Assert-ReleasePackagingPackageNotices.ps1') -RepositoryRoot $root -PackagePath $package
    }
    catch {
        Write-Warning "Cached LocalGPT.ReleasePackaging package failed notice verification; rebuilding automatically. $($_.Exception.Message)"
        $packageNeedsRebuild = $true
    }
}
if ($packageNeedsRebuild) {
    Remove-Item -LiteralPath $package -Force -ErrorAction SilentlyContinue
    $packageOutput = @(& (Join-Path $root 'build/Publish-ReleasePackagingPackage.ps1') -Configuration $Configuration -Version $Version)
    if ($packageOutput.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$packageOutput[0])) {
        throw "Release-packaging package publication returned $($packageOutput.Count) pipeline value(s); expected exactly one package path."
    }
    $package = [string]$packageOutput[0]
}
# Reject even rebuilt packages if the original license bytes cannot be verified.
& (Join-Path $root 'build/Assert-ReleasePackagingPackageNotices.ps1') -RepositoryRoot $root -PackagePath $package
# The package is a distributable, SHA-256-verified artifact. It is NOT a
# prerequisite installation for running LocalGPT's own repository build.
# On macOS/external volumes NuGet's tool installer can fail while extracting
# the license files into .store/.stage even after package verification.
# Run the helper that this checkout builds from source instead. Both normal
# entrypoints call this script; no operator-side dotnet tool install is needed.
$toolRoot = Join-Path $root 'artifacts/release-tools'
$publishedToolRoot = Join-Path $toolRoot 'published'
New-Item -ItemType Directory -Path $publishedToolRoot -Force | Out-Null
Write-Host 'Publishing the repository-owned release-packaging executable directly; NuGet tool installation is not required.' -ForegroundColor DarkCyan
& dotnet publish $project -c $Configuration -o $publishedToolRoot '-p:GeneratePackageOnBuild=false' '-p:UseAppHost=true' | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0) { throw 'LocalGPT.ReleasePackaging direct executable publication failed.' }
$isWindowsHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
$commandName = if ($isWindowsHost) { 'LocalGPT.ReleasePackaging.exe' } else { 'LocalGPT.ReleasePackaging' }
$command = Join-Path $publishedToolRoot $commandName
if (-not (Test-Path -LiteralPath $command -PathType Leaf)) {
    throw "Published LocalGPT.ReleasePackaging executable was not found: $command. The .NET project must generate an apphost for this build host."
}
# Verify the executable that documentation and native packaging will actually
# call, not merely the package metadata. Keep the feature/capability gate.
$capabilities = @(& $command capabilities 2>$null)
$capabilitiesExitCode = [int]$LASTEXITCODE
if ($capabilitiesExitCode -ne 0 -or $capabilities -cnotcontains 'pdf-merge-input-list') {
    throw "Published LocalGPT.ReleasePackaging $Version cannot merge PDF chunks via --input-list (exit=$capabilitiesExitCode): $command. Documentation has not started."
}
Write-Host "LocalGPT.ReleasePackaging $Version executable verified for this build host: $command" -ForegroundColor Green
Write-Output ([string]$command)
