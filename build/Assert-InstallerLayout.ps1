# Fail-closed source-only release/install layout contract. Windows PowerShell 5.1 and pwsh compatible.
param([string]$RepositoryRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$installer = Join-Path $RepositoryRoot 'src/LocalGPTInstallerConsole/Program.cs'
$release = Join-Path $RepositoryRoot 'Build-Release.ps1'
if (-not (Test-Path -LiteralPath $installer -PathType Leaf) -or -not (Test-Path -LiteralPath $release -PathType Leaf)) {
    throw 'INSTALLLAYOUT0001: Installer or release packaging source is missing.'
}
$source = [IO.File]::ReadAllText($installer)
$builder = [IO.File]::ReadAllText($release)
$checks = @(
    @{ Text = 'InstallPortableReleaseAsset(zipPath, targetPath, "winx64", "LocalGPT.exe", logger)'; Explain = 'Application ZIP must install into the stable winx64 directory.' },
    @{ Text = 'InstallPortableReleaseAsset(setupZipPath, targetPath, "setupwinx64", "Start.cmd", logger)'; Explain = 'Setup ZIP must install into the stable setupwinx64 directory.' },
    @{ Text = 'ExtractZipWithFallback(zipPath, stage, logger)'; Explain = 'Release archive extraction must be staged before the install root.' },
    @{ Text = 'var source = wrapper is null ? stage : Path.Combine(stage, wrapper)'; Explain = 'ZIP root wrappers must be stripped before deployment.' },
    @{ Text = 'CopyReleasePayload(source, target)'; Explain = 'Only the unwrapped staged payload can be copied into its canonical folder.' },
    @{ Text = 'DeleteIfExists(Path.Combine(targetPath, "winx64"), logger)'; Explain = 'Force refresh must affect only version-independent installed application binaries.' },
    @{ Text = 'DeleteIfExists(Path.Combine(targetPath, "setupwinx64"), logger)'; Explain = 'Force refresh must affect only version-independent setup binaries.' },
    @{ Text = 'if (!File.Exists(Path.Combine(targetPath, "winx64", "LocalGPT.exe"))'; Explain = 'Installer must verify the canonical executable before reporting success.' },
    @{ Text = '|| !File.Exists(Path.Combine(targetPath, "setupwinx64", "Start.cmd")))'; Explain = 'Installer must verify the canonical launcher before reporting success.' },
    @{ Text = 'var cmdPath = Path.Combine(localGptRoot, "setupwinx64", cmdFileName)'; Explain = 'Shortcut targets must resolve only to canonical setupwinx64.' },
    @{ Text = 'throw new FileNotFoundException($"LocalGPT canonical setup shortcut target is missing:'; Explain = 'A missing shortcut target must fail instead of recursively targeting stale version folders.' },
    @{ Text = 'RecoverLegacyVersionedInstallFolders(targetPath, logger)'; Explain = 'Successful installation must recover old generated ZIP-wrapper folders.' },
    @{ Text = 'Directory.EnumerateDirectories(installRoot, "*", SearchOption.TopDirectoryOnly)'; Explain = 'Legacy recovery must inspect only direct user-data children.' },
    @{ Text = 'TryIdentifyLegacyReleaseWrapper(name, out var requiredFile)'; Explain = 'Legacy recovery must strictly identify versioned release payloads.' },
    @{ Text = 'Directory.Move(legacyPath, recoveryPath)'; Explain = 'Legacy wrappers must be preserved in quarantine rather than deleted.' },
    @{ Text = '"runtime", "recovered-installer-payloads"'; Explain = 'Recovered release folders must be placed outside the canonical application and setup.' },
    @{ Text = 'Version.TryParse(versionText, out var version)'; Explain = 'Versioned folders must be validated as exact version wrappers.' },
    @{ Text = 'FileAttributes.ReparsePoint'; Explain = 'Reparse-point folders must never be moved by the recovery pass.' }
)
foreach ($check in $checks) {
    if (-not $source.Contains([string]$check.Text)) {
        throw "INSTALLLAYOUT0002: $($check.Explain) See $installer."
    }
}
if (-not $builder.Contains('New-PortableReleaseArchive -SourceDirectory $appFolder -DestinationPath $appZip -RootFolderName $appRootFolderName') -or
    -not $builder.Contains('New-PortableReleaseArchive -SourceDirectory $setupFolder -DestinationPath $setupZip -RootFolderName $setupRootFolderName')) {
    throw "INSTALLLAYOUT0003: Release payload wrapper contract changed: $release"
}
# Reject accidental regression to extracting into AppData root without canonical normalization.
$installBegin = $source.IndexOf('private static async Task InstallLocalGptAsync(')
$installEnd = $source.IndexOf('private static void InstallPortableReleaseAsset(', $installBegin)
if ($installBegin -lt 0 -or $installEnd -le $installBegin) { throw 'INSTALLLAYOUT0005: Installer method boundary missing.' }
$installSection = $source.Substring($installBegin, $installEnd - $installBegin)
if ($installSection.Contains('DeleteIfExists(targetPath, logger)')) {
    throw "INSTALLLAYOUT0006: Installer --force-delete must not remove persistent AppData user state: $installer"
}
if ($installSection.Contains('ExtractZipWithFallback(zipPath, targetPath, logger)') -or $installSection.Contains('ExtractZipWithFallback(setupZipPath, targetPath, logger)')) {
    throw "INSTALLLAYOUT0004: ZIP extraction directly into LocalGPT data root reintroduced: $installer"
}
# Recovery is post-install only; it must never run before both payloads are checked.
$recoveryCall = $installSection.IndexOf('RecoverLegacyVersionedInstallFolders(targetPath, logger)')
$canonicalCheck = $installSection.IndexOf('if (!File.Exists(Path.Combine(targetPath, "winx64", "LocalGPT.exe"))')
if ($recoveryCall -lt 0 -or $canonicalCheck -lt 0 -or $recoveryCall -lt $canonicalCheck) {
    throw 'INSTALLLAYOUT0007: Legacy folder recovery may only occur after canonical install validation.'
}
$recoveryMethodStart = $source.IndexOf('private static void RecoverLegacyVersionedInstallFolders(')
$recoveryMethodEnd = $source.IndexOf('private static void CopyReleasePayload(', $recoveryMethodStart)
if ($recoveryMethodStart -lt 0 -or $recoveryMethodEnd -lt $recoveryMethodStart) {
    throw 'INSTALLLAYOUT0008: Recovery boundary missing.'
}
$recoverySection = $source.Substring($recoveryMethodStart, $recoveryMethodEnd - $recoveryMethodStart)
if ($recoverySection.Contains('Directory.Delete(') -or $recoverySection.Contains('File.Delete(') -or
    $recoverySection.Contains('SearchOption.AllDirectories')) {
    throw 'INSTALLLAYOUT0009: Recovery must not delete data or recurse into unrelated user folders.'
}
Write-Host 'INSTALLLAYOUT: canonical runtime folders, ZIP wrapper normalization, staged install and shortcut targets verified.'
