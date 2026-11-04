# Windows PowerShell 5.1 / pwsh source contract for atomic installer deployment.
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
    @{ Text = 'PreparePortableReleaseAsset(zipPath, targetPath, "winx64", "LocalGPT.exe", logger)'; Explain = 'App ZIP must be staged for stable winx64.' },
    @{ Text = 'PreparePortableReleaseAsset(setupZipPath, targetPath, "setupwinx64", "Start.cmd", logger)'; Explain = 'Setup ZIP must be staged for stable setupwinx64.' },
    @{ Text = 'CommitPortableReleaseAssets(targetPath, appStage, setupStage, logger)'; Explain = 'Both validated payloads must be committed together.' },
    @{ Text = 'ExtractZipWithFallback(zipPath, extractRoot, logger)'; Explain = 'Archive extraction must occur only inside a temporary directory.' },
    @{ Text = 'var source = wrapper is null ? extractRoot : Path.Combine(extractRoot, wrapper)'; Explain = 'A versioned ZIP wrapper must not become an installation directory.' },
    @{ Text = 'if (!entries.Any(entry => entry.FullName.Replace'; Explain = 'The expected root-level marker must be checked before installation.' },
    @{ Text = 'Directory.Move(target, backup)'; Explain = 'The previous installation must be backed up before activating a new payload.' },
    @{ Text = 'Directory.Move(asset.PayloadRoot, target)'; Explain = 'The staged validated payload must be activated at the canonical path.' },
    @{ Text = 'Directory.Move(old.Backup, old.Target)'; Explain = 'Failed installer activation must restore the previous payload.' },
    @{ Text = 'FileAttributes.ReparsePoint'; Explain = 'Installer must reject links in install and cleanup paths.' },
    @{ Text = 'RemoveLegacyVersionedInstallFolders(targetPath, logger)'; Explain = 'Old versioned wrapper cleanup must run after both installations succeed.' },
    @{ Text = 'RemoveKnownLegacyFoldersInDirectory(previousRecovery, logger)'; Explain = 'Old releases quarantined by the earlier installer must also be cleaned.' },
    @{ Text = 'Directory.Delete(legacyPath, recursive: true)'; Explain = 'Confirmed obsolete wrapper directories must be deleted, not merely moved.' },
    @{ Text = 'TryIdentifyLegacyReleaseWrapper(name, out var requiredFile)'; Explain = 'Cleanup must recognize only exact versioned release folders.' },
    @{ Text = 'IsKnownReleasePayloadFolder(legacyPath, requiredFile, logger)'; Explain = 'Cleanup must protect unknown/user-owned data.' },
    @{ Text = 'Version.TryParse(versionText, out var version)'; Explain = 'Cleanup must validate all three version components.' },
    @{ Text = 'var cmdPath = Path.Combine(localGptRoot, "setupwinx64", cmdFileName)'; Explain = 'Shortcuts must target the canonical setup folder.' },
    @{ Text = 'throw new FileNotFoundException($"LocalGPT canonical setup shortcut target is missing:'; Explain = 'Missing canonical shortcut targets must fail closed.' }
)
foreach ($check in $checks) {
    if (-not $source.Contains([string]$check.Text)) {
        throw "INSTALLLAYOUT0002: $($check.Explain) See $installer."
    }
}
if (-not $builder.Contains('New-PortableReleaseArchive -SourceDirectory $appFolder -DestinationPath $appZip -RootFolderName $appRootFolderName') -or
    -not $builder.Contains('New-PortableReleaseArchive -SourceDirectory $setupFolder -DestinationPath $setupZip -RootFolderName $setupRootFolderName')) {
    throw "INSTALLLAYOUT0003: Build-Release archive layout unexpectedly changed: $release"
}
$installBegin = $source.IndexOf('private static async Task InstallLocalGptAsync(', [StringComparison]::Ordinal)
$installEnd = $source.IndexOf('private sealed record StagedPortableReleaseAsset(', $installBegin, [StringComparison]::Ordinal)
if ($installBegin -lt 0 -or $installEnd -le $installBegin) { throw 'INSTALLLAYOUT0004: Installer method boundary missing.' }
$section = $source.Substring($installBegin, $installEnd - $installBegin)
$setupDownload = $section.IndexOf('setupAsset: true', [StringComparison]::Ordinal)
$appStage = $section.IndexOf('appStage = PreparePortableReleaseAsset(', [StringComparison]::Ordinal)
$setupStage = $section.IndexOf('setupStage = PreparePortableReleaseAsset(', [StringComparison]::Ordinal)
$commit = $section.IndexOf('CommitPortableReleaseAssets(', [StringComparison]::Ordinal)
$cleanup = $section.IndexOf('RemoveLegacyVersionedInstallFolders(', [StringComparison]::Ordinal)
if ($setupDownload -lt 0 -or $appStage -le $setupDownload -or $setupStage -le $appStage -or $commit -le $setupStage -or $cleanup -le $commit) {
    throw 'INSTALLLAYOUT0005: Both ZIPs must be downloaded and staged before commit, with cleanup strictly after successful commit.'
}
if ($section.Contains('DeleteIfExists(targetPath, logger)') -or
    $section.Contains('ExtractZipWithFallback(zipPath, targetPath, logger)') -or
    $section.Contains('DeleteIfExists(Path.Combine(targetPath, "winx64"), logger)')) {
    throw 'INSTALLLAYOUT0006: Installation must never delete the user-data root or either canonical payload before staging and rollback.'
}
$commitBegin = $source.IndexOf('private static void CommitPortableReleaseAssets(', [StringComparison]::Ordinal)
$commitEnd = $source.IndexOf('private static void RemoveInstallerOwnedTemporaryDirectory(', $commitBegin, [StringComparison]::Ordinal)
$commitSection = $source.Substring($commitBegin, $commitEnd - $commitBegin)
if (-not $commitSection.Contains('catch (Exception commitException)') -or
    -not $commitSection.Contains('previous.AsEnumerable().Reverse()') -or
    -not $commitSection.Contains('if (rollbackFailed)')) {
    throw 'INSTALLLAYOUT0007: Transactional installer rollback is missing.'
}
$cleanupBegin = $source.IndexOf('private static void RemoveLegacyVersionedInstallFolders(', [StringComparison]::Ordinal)
$cleanupEnd = $source.IndexOf('private static bool TryIdentifyLegacyReleaseWrapper(', $cleanupBegin, [StringComparison]::Ordinal)
if ($cleanupBegin -lt 0 -or $cleanupEnd -le $cleanupBegin) { throw 'INSTALLLAYOUT0008: Legacy cleanup boundary missing.' }
$cleanupSection = $source.Substring($cleanupBegin, $cleanupEnd - $cleanupBegin)
if ($cleanupSection.Contains('Directory.Move(legacyPath') -or $cleanupSection.Contains('SearchOption.AllDirectories')) {
    throw 'INSTALLLAYOUT0009: Legacy wrappers must be deleted after validation; cleanup must only enumerate direct children.'
}
$exeBegin = $source.IndexOf('private static string? FindLocalGptExecutable(', [StringComparison]::Ordinal)
$exeEnd = $source.IndexOf('private static int GetRelativePathDepth(', $exeBegin, [StringComparison]::Ordinal)
if ($exeBegin -lt 0 -or $exeEnd -le $exeBegin) { throw 'INSTALLLAYOUT0010: Executable resolver boundary missing.' }
$exeSection = $source.Substring($exeBegin, $exeEnd - $exeBegin)
if ($exeSection.Contains('EnumerateFilesSafe(') -or $exeSection.Contains('Path.Combine(localGptRoot, "LocalGPT.exe")')) {
    throw 'INSTALLLAYOUT0011: Executable fallback must not select arbitrary old versioned installations.'
}
& (Join-Path $PSScriptRoot 'Assert-DatabaseRecoverySafety.ps1') -RepositoryRoot $RepositoryRoot
Write-Host 'INSTALLLAYOUT: atomic staged Windows installation, rollback, canonical shortcuts, safe deletion of obsolete wrappers and SQLite preservation guards verified.'
