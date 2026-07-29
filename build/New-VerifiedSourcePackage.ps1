param(
    [Parameter(Mandatory)][string]$Version,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RepositoryValidation.Common.ps1')

$repoRoot = Get-LocalGptRepositoryRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "artifacts/packages/LocalGPT-v$Version.zip"
}

$stampPath = Join-Path $repoRoot 'artifacts/validation/compile-success.json'
if (-not (Test-Path -LiteralPath $stampPath -PathType Leaf)) {
    throw 'No compile-success stamp exists. Run ./build/Invoke-RepositoryValidation.ps1 first.'
}

$stamp = Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json
if (-not $stamp.Succeeded) {
    throw 'The existing validation stamp does not record a successful build.'
}
if (@($stamp.Configurations) -notcontains 'Debug' -or @($stamp.Configurations) -notcontains 'Release') {
    throw 'Both Debug and Release builds are required before verified packaging.'
}

$currentFingerprint = Get-RepositorySourceFingerprint -RepositoryRoot $repoRoot
if ($currentFingerprint -ne $stamp.SourceFingerprint) {
    throw 'Source changed after the successful build. Re-run repository validation before packaging.'
}

& (Join-Path $PSScriptRoot 'Assert-LocalizationIntegrity.ps1')
& (Join-Path $PSScriptRoot 'Assert-ProjectClosure.ps1')
& (Join-Path $PSScriptRoot 'Assert-CSharpSyntax.ps1')
& (Join-Path $PSScriptRoot 'Assert-SourceFormatting.ps1')

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $outputFullPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Force
}

$stage = Join-Path $repoRoot ("artifacts/package-stage-{0}" -f ([Guid]::NewGuid().ToString('N')))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
try {
    foreach ($file in Get-MaintainedRepositoryFiles -RepositoryRoot $repoRoot) {
        $relative = Get-RelativePathPortable -BasePath $repoRoot -TargetPath $file.FullName
        $destination = Join-Path $stage $relative
        $destinationDirectory = Split-Path -Parent $destination
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $stage,
        $outputFullPath,
        [IO.Compression.CompressionLevel]::Optimal,
        $false)
    $archive = [IO.Compression.ZipFile]::OpenRead($outputFullPath)
    try {
        if ($archive.Entries.Count -eq 0) {
            throw 'The generated ZIP contains no entries.'
        }
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName.StartsWith('/') -or $entry.FullName -match '(^|/)\.\.(/|$)') {
                throw "Unsafe archive entry detected: $($entry.FullName)"
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
}

$hash = (Get-FileHash -LiteralPath $outputFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$outputFullPath.sha256" -Value "$hash  $([IO.Path]::GetFileName($outputFullPath))" -Encoding ascii
Write-Host "Verified source package created: $outputFullPath"
