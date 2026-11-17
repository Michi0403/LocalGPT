<#
.SYNOPSIS
Installs an administrator-reviewed offline Wasmer browser SDK and Clang WebC package into a LocalGPT build.
.DESCRIPTION
The SDK package must already be extracted from an independently verified @wasmer/sdk npm archive.
The Clang package must already have been obtained from a trusted source and matched to the expected digest.
This command does not download, execute, extract, or modify third-party code and never replaces an existing installation.
#>
param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [Parameter(Mandatory = $true)][string]$SdkPackageDirectory,
    [Parameter(Mandatory = $true)][string]$ClangPackagePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$ExpectedClangSha256
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$descriptorPath = Join-Path $root 'src/LocalGPT/wwwroot/browser-compiler/runtime.json'
$descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $output 'LocalGPT.dll') -PathType Leaf)) {
    throw 'OutputDirectory must be an existing compiled LocalGPT application directory.'
}
$sdk = (Resolve-Path -LiteralPath $SdkPackageDirectory).Path
$package = Get-Content -LiteralPath (Join-Path $sdk 'package.json') -Raw | ConvertFrom-Json
if ($package.name -ne '@wasmer/sdk' -or $package.version -ne $descriptor.sdkVersion) {
    throw ('Use the reviewed @wasmer/sdk package version {0}.' -f $descriptor.sdkVersion)
}
$dist = Join-Path $sdk 'dist'
if (-not (Test-Path -LiteralPath (Join-Path $dist 'index.mjs') -PathType Leaf)) { throw 'The reviewed SDK dist/index.mjs is missing.' }
$clang = Get-Item -LiteralPath $ClangPackagePath
if ($clang.PSIsContainer -or (($clang.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) -or $clang.Length -gt $descriptor.maximumToolchainBytes) {
    throw 'Use a regular, bounded offline Clang WebC package file.'
}
if ((Get-FileHash -LiteralPath $clang.FullName -Algorithm SHA256).Hash -ne $ExpectedClangSha256) { throw 'Clang package SHA-256 differs from the reviewed digest.' }
# Reject links before copying any SDK assets. Walk directories individually, without following links.
$pending = New-Object 'System.Collections.Generic.Queue[string]'
$files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
$pending.Enqueue($dist)
while ($pending.Count -gt 0) {
    $directory = Get-Item -LiteralPath ($pending.Dequeue())
    if (($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Linked SDK directories are not permitted.' }
    foreach ($entry in @(Get-ChildItem -LiteralPath $directory.FullName -Force)) {
        if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw ('Linked SDK asset is not permitted: {0}' -f $entry.Name) }
        if ($entry.PSIsContainer) { $pending.Enqueue($entry.FullName) }
        else { $files.Add($entry) }
    }
}
# Keep build assets with the selected application output, never in a global cache.
$parent = Join-Path $output 'wwwroot/browser-compiler'
$target = Join-Path $parent 'toolchain'
if (Test-Path -LiteralPath $target) { throw 'A browser toolchain is already installed. Retain its receipt; explicitly move the old directory before installing a different reviewed bundle.' }
$null = New-Item -ItemType Directory -Path $parent -Force
$stage = Join-Path $output ('.browser-toolchain-stage-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path (Join-Path $stage 'sdk') -Force
try {
    $hashes = New-Object 'System.Collections.Generic.List[object]'
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($dist.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
        $destination = Join-Path (Join-Path $stage 'sdk') $relative
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force
        Copy-Item -LiteralPath $file.FullName -Destination $destination
        $hashes.Add([ordered]@{ path = ($relative -replace '\\', '/'); sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() })
    }
    $compiler = Join-Path $stage 'compiler.bin'
    Copy-Item -LiteralPath $clang.FullName -Destination $compiler
    $actual = (Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $ExpectedClangSha256) { throw 'Clang package changed during installation.' }
    $receipt = [ordered]@{
        sdkVersion = $descriptor.sdkVersion
        compilerPackage = $descriptor.compilerPackage
        compilerSha256 = $actual
        sdkFiles = @($hashes.ToArray())
        installedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    $receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $stage 'manifest.json') -Encoding UTF8
    Move-Item -LiteralPath $stage -Destination $target
    Write-Host ('Installed offline browser compiler bundle: {0}' -f $target)
    Write-Host 'The manifest records installed asset hashes. Open the browser workbench to test real compilation and execution.'
} finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
