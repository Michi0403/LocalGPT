[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'NativeReleaseText.Common.ps1')

# Run before restoring/building binaries on Windows, macOS and Linux. This guard
# validates the same templates and writer that produce the actual PKG scripts.
$packageSourcePath = Join-Path $PSScriptRoot 'NativeReleasePackaging.ps1'
if (-not (Test-Path -LiteralPath $packageSourcePath -PathType Leaf)) { throw "Missing native packaging source: $packageSourcePath" }
$packageSource = [IO.File]::ReadAllText($packageSourcePath)
foreach ($requiredCall in @('NativeReleaseText.Common.ps1', 'Get-MacPackageScriptText', 'Assert-PortableShellScript', 'Assert-MacPackageResources', "'--resources', `$pkgResources")) {
    if ($packageSource.IndexOf($requiredCall, [StringComparison]::Ordinal) -lt 0) {
        throw "macOS PKG generation lost its verified resource/script contract: '$requiredCall' in $packageSourcePath"
    }
}
$templates = Join-Path $PSScriptRoot 'macpkg'
$readme = Join-Path $root 'README.md'
$license = Join-Path $root 'LICENSE.MD'
$background = Join-Path $PSScriptRoot 'assets/LocalGPT-dmg-background.png'
foreach ($sourceFile in @($readme,$license,$background)) {
    if (-not (Test-Path -LiteralPath $sourceFile -PathType Leaf) -or (Get-Item -LiteralPath $sourceFile).Length -eq 0) {
        throw "Missing or empty macOS Installer source resource: $sourceFile"
    }
}
$temp = Join-Path ([IO.Path]::GetTempPath()) ('localgpt-pkg-contract-' + [Guid]::NewGuid().ToString('N'))
New-Item -Path $temp -ItemType Directory -Force | Out-Null
try {
    foreach ($phase in @('preinstall','postinstall')) {
        $source = Get-MacPackageScriptText -TemplateRoot $templates -ProductName 'LocalGPT' -ExecutableName 'LocalGPT' -Phase $phase
        # Exercise the actual UTF-8 writer with CRLF input to detect the reported regression.
        $path = Join-Path $temp $phase
        $crlfInput = $source.Replace("`n", "`r`n")
        Write-Utf8NoBom $path $crlfInput
        Assert-PortableShellScript -Path $path
    }
    $corrupt = Join-Path $temp 'corrupt-preinstall'
    [IO.File]::WriteAllBytes($corrupt, [Text.Encoding]::UTF8.GetBytes("#!/bin/sh`r`nexit 0`r`n"))
    $rejected = $false
    try { Assert-PortableShellScript -Path $corrupt } catch { $rejected = $true }
    if (-not $rejected) { throw 'macOS package guard incorrectly accepted a CRLF preinstall fixture.' }
    $plainBytes = [Text.Encoding]::UTF8.GetBytes("#!/bin/sh`nexit 0`n")
    $bomBytes = New-Object byte[] ($plainBytes.Length + 3)
    $bomBytes[0] = 239; $bomBytes[1] = 187; $bomBytes[2] = 191
    [Array]::Copy($plainBytes, 0, $bomBytes, 3, $plainBytes.Length)
    [IO.File]::WriteAllBytes($corrupt, $bomBytes)
    $rejected = $false
    try { Assert-PortableShellScript -Path $corrupt } catch { $rejected = $true }
    if (-not $rejected) { throw 'macOS package guard incorrectly accepted a BOM preinstall fixture.' }
    [IO.File]::WriteAllBytes($corrupt, [Text.Encoding]::UTF8.GetBytes("echo no-shebang`n"))
    $rejected = $false
    try { Assert-PortableShellScript -Path $corrupt } catch { $rejected = $true }
    if (-not $rejected) { throw 'macOS package guard incorrectly accepted a missing-shebang fixture.' }
    $resourceStaging = Join-Path $temp 'resources'
    New-Item -ItemType Directory -Path $resourceStaging | Out-Null
    Write-Utf8NoBom (Join-Path $resourceStaging 'README.txt') ([IO.File]::ReadAllText($readme))
    Write-Utf8NoBom (Join-Path $resourceStaging 'LICENSE.txt') ([IO.File]::ReadAllText($license))
    Copy-Item -LiteralPath $background -Destination (Join-Path $resourceStaging 'background.png')
    Assert-MacPackageResources -ResourcesDirectory $resourceStaging
    $readmeStaging = Join-Path $resourceStaging 'README.txt'
    Remove-Item -LiteralPath $readmeStaging -Force
    $rejected = $false
    try { Assert-MacPackageResources -ResourcesDirectory $resourceStaging } catch { $rejected = $true }
    if (-not $rejected) { throw 'macOS package guard incorrectly accepted a missing Installer README resource.' }
    $pngStaging = Join-Path $resourceStaging 'background.png'
    [IO.File]::WriteAllBytes($pngStaging, [Text.Encoding]::ASCII.GetBytes('not-a-png'))
    $rejected = $false
    try { Assert-MacPackageResources -ResourcesDirectory $resourceStaging } catch { $rejected = $true }
    if (-not $rejected) { throw 'macOS package guard incorrectly accepted an invalid Installer background.' }
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'macOS PKG preflight passed: LF-only pre/postinstall, interpreter headers, shell syntax when available, negative fixtures and Installer resources.'
