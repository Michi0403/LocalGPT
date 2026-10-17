param(
    [string]$Configuration = "Release",
    [string]$Version = "1.1.2"
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$project = Join-Path $root "src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj"
[xml]$packageProject = Get-Content -LiteralPath $project -Raw
$declaredPackageVersion = [string]$packageProject.Project.PropertyGroup.Version
if (-not [string]::Equals($Version, $declaredPackageVersion, [StringComparison]::Ordinal)) {
    throw "LocalGPT.ReleasePackaging requested package version '$Version' differs from the project version '$declaredPackageVersion'. Align them before publishing."
}
& (Join-Path $root 'build/Assert-ThirdPartyNotices.ps1') -RepositoryRoot $root
$packages = Join-Path $root "packages"
New-Item -ItemType Directory -Path $packages -Force | Out-Null
& dotnet pack $project -c $Configuration -o $packages "-p:PackageVersion=$Version" "-p:GeneratePackageOnBuild=false" | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0) { throw "LocalGPT.ReleasePackaging package creation failed." }
$package = Join-Path $packages "LocalGPT.ReleasePackaging.$Version.nupkg"
if (-not (Test-Path -LiteralPath $package -PathType Leaf)) { throw "Expected release-packaging package was not created: $package" }
# PackAsTool may nest SDK content under tools/<TFM>/any instead of placing it at the
# legal-notice root required by this package's public release contract. Finalize
# the locally built, unsigned NuGet ZIP with the original source notice bytes.
# This is an automatic pack stage, never a manual prerequisite or a bypass.
$requiredNotices = @{
    'licenses/THIRD-PARTY-NOTICES.txt' = (Join-Path $root 'THIRD-PARTY-NOTICES.txt')
    'licenses/PDFsharp-6.2.4-MIT-LICENSE.txt' = (Join-Path $root 'third_party/PDFsharp/LICENSE')
}
Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
$archive = [IO.Compression.ZipFile]::Open($package, [IO.Compression.ZipArchiveMode]::Update)
try {
    foreach ($entryName in $requiredNotices.Keys) {
        $sourcePath = $requiredNotices[$entryName]
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Required release notice source is missing: $sourcePath"
        }
        $existingEntry = $archive.GetEntry($entryName)
        if ($null -ne $existingEntry) { $existingEntry.Delete() }
        $entry = $archive.CreateEntry($entryName, [IO.Compression.CompressionLevel]::Optimal)
        $entryStream = $entry.Open()
        try {
            $sourceStream = [IO.File]::OpenRead($sourcePath)
            try { $sourceStream.CopyTo($entryStream) }
            finally { $sourceStream.Dispose() }
        }
        finally { $entryStream.Dispose() }
    }
}
finally { $archive.Dispose() }
# Independent SHA-256 validation remains mandatory after package finalization.
& (Join-Path $root 'build/Assert-ReleasePackagingPackageNotices.ps1') -RepositoryRoot $root -PackagePath $package
Write-Output $package
