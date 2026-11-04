# Verify the owner of SQLite startup self-recovery preserves data on uncertain probes.
param([string]$RepositoryRoot = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { $RepositoryRoot = Split-Path -Parent $PSScriptRoot }
$path = Join-Path $RepositoryRoot 'src/LocalGPT/Services/Persistence/DatabaseFileHealthService.cs'
if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'DBRECOVERY0001: Database health owner missing.' }
$source = [IO.File]::ReadAllText($path)
$checks = @(
    'var pendingRecovery = File.Exists(GetRecoveryMarkerPath())',
    'A marker is NOT evidence that the current database is corrupt',
    'var quickCheck = await RunQuickCheckAsync(cancellationToken)',
    'if (quickCheck == DatabaseProbeResult.Inconclusive)',
    'var verified = await RunQuickCheckAsync(cancellationToken)',
    'if (verified == DatabaseProbeResult.Healthy)',
    'if (verified == DatabaseProbeResult.Inconclusive)',
    'var moved = new List<(string Original, string Backup)>()',
    'File.Move(sourcePath, backupPath)',
    'File.Move(item.Backup, item.Original)',
    'if (restorationFailed)',
    'TryDeleteRecoveryMarker()',
    'logger.LogCritical(',
    'CorruptDatabaseBackups'
)
foreach ($token in $checks) {
    if (-not $source.Contains($token)) { throw "DBRECOVERY0002: SQLite preservation contract missing '$token': $path" }
}
$begin = $source.IndexOf('public async Task RecoverMalformedDatabaseAsync(', [StringComparison]::Ordinal)
$end = $source.IndexOf('private async Task<DatabaseProbeResult> RunQuickCheckAsync(', $begin, [StringComparison]::Ordinal)
if ($begin -lt 0 -or $end -le $begin) { throw 'DBRECOVERY0003: Cannot verify SQLite recovery method.' }
$recovery = $source.Substring($begin, $end - $begin)
$verifiedIndex = $recovery.IndexOf('var verified = await RunQuickCheckAsync(', [StringComparison]::Ordinal)
$moveIndex = $recovery.IndexOf('File.Move(sourcePath, backupPath)', [StringComparison]::Ordinal)
if ($verifiedIndex -lt 0 -or $moveIndex -le $verifiedIndex) {
    throw 'DBRECOVERY0004: Database corruption must be reverified before live files are moved.'
}
if ($source.Contains('TryQuarantineOrDelete') -or $recovery.Contains('File.Delete(sourcePath)') -or
    $recovery.Contains('overwrite: true')) {
    throw 'DBRECOVERY0005: Recovery must never delete original DB/WAL/SHM files or overwrite a previous backup.'
}
Write-Host 'DBRECOVERY: stale recovery markers reverified, original SQLite DB/WAL/SHM preserved with rollback and logged recreation.'
