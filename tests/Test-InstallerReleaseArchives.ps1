# Optional smoke test. Never invoked automatically by Build-Release.ps1 or MSBuild.
param(
    [Parameter(Mandatory)][string]$AppArchive,
    [Parameter(Mandatory)][string]$SetupArchive,
    [Parameter(Mandatory)][string]$ExpectedVersion,
    [Parameter(Mandatory)][string]$ExpectedSetupVersion,
    [ValidateSet('winx64', 'winx86', 'winarm64', 'linuxx64', 'linuxarm64', 'macosx64', 'macosarm64')][string]$RuntimeFolder = 'winx64'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue

$root = Join-Path ([IO.Path]::GetTempPath()) ("LocalGPT-InstallerSmoke-" + [guid]::NewGuid().ToString('N'))
try {
    $null = New-Item -ItemType Directory -Path $root -Force
    $runtime = Join-Path $root $RuntimeFolder
    $setup = Join-Path $root ("setup" + $RuntimeFolder)
    $null = New-Item -ItemType Directory -Path $runtime -Force
    $null = New-Item -ItemType Directory -Path $setup -Force

    # Simulate an existing outdated installation and unrelated persistent data.
    [IO.File]::WriteAllText((Join-Path $runtime 'RELEASE-VERSION.txt'), '6.4.1')
    [IO.File]::WriteAllText((Join-Path $root 'user-data-sentinel.txt'), 'keep-user-data')
    [IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($AppArchive), $root, $true)
    [IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($SetupArchive), $root, $true)

    $isWindowsRuntime = $RuntimeFolder.StartsWith('win', [StringComparison]::Ordinal)
    $appExecutable = if ($isWindowsRuntime) { 'LocalGPT.exe' } else { 'LocalGPT' }
    $setupExecutable = if ($isWindowsRuntime) { 'LocalGPTInstallerConsole.exe' } else { 'LocalGPTInstallerConsole' }
    foreach ($name in @($appExecutable, 'LocalGPT.dll', 'RELEASE-VERSION.txt')) {
        if (-not (Test-Path -LiteralPath (Join-Path $runtime $name) -PathType Leaf)) { throw "Missing installed app file: $name" }
    }
    $setupFiles = @($setupExecutable, 'RELEASE-VERSION.txt', 'SETUP-VERSION.txt')
    if ($isWindowsRuntime) { $setupFiles += 'Start.cmd' }
    foreach ($name in $setupFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $setup $name) -PathType Leaf)) { throw "Missing installed setup file: $name" }
    }
    foreach ($marker in @((Join-Path $runtime 'RELEASE-VERSION.txt'), (Join-Path $setup 'RELEASE-VERSION.txt'))) {
        $actual = [IO.File]::ReadAllText($marker).Trim()
        if ($actual -cne $ExpectedVersion) { throw "Installed version '$actual' does not match $ExpectedVersion in $marker" }
    }
    $actualSetupVersion = [IO.File]::ReadAllText((Join-Path $setup 'SETUP-VERSION.txt')).Trim()
    if ($actualSetupVersion -cne $ExpectedSetupVersion) { throw "Setup version '$actualSetupVersion' does not match $ExpectedSetupVersion" }
    if ([IO.File]::ReadAllText((Join-Path $root 'user-data-sentinel.txt')) -cne 'keep-user-data') { throw 'User data changed.' }
    $unexpectedRoots = @(Get-ChildItem -LiteralPath $root -Directory | Where-Object { $_.Name -like 'LocalGPT-*-win-*' -or $_.Name -like 'LocalGPT-Setup-*-win-*' })
    if ($unexpectedRoots.Count -gt 0) { throw "Version-named release wrappers were extracted: $($unexpectedRoots.Name -join ', ')" }
    Write-Host "PASS: $ExpectedVersion overlays the canonical $RuntimeFolder and setup$RuntimeFolder, preserves unrelated data and creates no versioned directories." -ForegroundColor Green
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
