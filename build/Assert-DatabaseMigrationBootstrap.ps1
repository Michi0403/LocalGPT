param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$servicePath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseInitializationService.cs'
$initialMigrationPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Migrations/20260616222639_Initial.cs'
$architecturePath = Join-Path $RepositoryRoot 'docs/DATABASE_MIGRATION_BOOTSTRAP.md'

foreach ($path in @($servicePath, $initialMigrationPath, $architecturePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required migration-bootstrap file is missing: $path"
    }
}

$service = Get-Content -LiteralPath $servicePath -Raw
$initial = Get-Content -LiteralPath $initialMigrationPath -Raw
$architecture = Get-Content -LiteralPath $architecturePath -Raw
$errors = [System.Collections.Generic.List[string]]::new()

foreach ($token in @(
    'await databaseFileHealth.EnsureHealthyOrRecoverAsync',
    'await PrepareLegacyMigrationHistoryAsync',
    'await ClearAbandonedMigrationLockAsync',
    'AbandonedMigrationLockAge = TimeSpan.FromMinutes(10)',
    'await db.Database.MigrateAsync',
    'sourceConnection.BackupDatabase(destinationConnection)',
    'INSERT OR IGNORE INTO \"__EFMigrationsHistory\"',
    'IsSupportedApplicationLogsBootstrap',
    'SignatureState.Partial',
    'did not guess at destructive repairs')) {
    if (-not $service.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Database initialization must retain '$token'.")
    }
}

$healthPosition = $service.IndexOf('await databaseFileHealth.EnsureHealthyOrRecoverAsync', [StringComparison]::Ordinal)
$adoptionPosition = $service.IndexOf('await PrepareLegacyMigrationHistoryAsync', [StringComparison]::Ordinal)
$migrationPosition = $service.IndexOf('await db.Database.MigrateAsync', [StringComparison]::Ordinal)
if (-not ($healthPosition -ge 0 -and $healthPosition -lt $adoptionPosition -and $adoptionPosition -lt $migrationPosition)) {
    $errors.Add('Database initialization must run health checks, legacy adoption, and EF migration in that order.')
}

foreach ($token in @(
    'CREATE TABLE IF NOT EXISTS "ApplicationLogs"',
    'CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_LogLevelValue"',
    'CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_LogLevelValue_TimestampUtc"',
    'CREATE INDEX IF NOT EXISTS "IX_ApplicationLogs_TimestampUtc"')) {
    if (-not $initial.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("The initial migration must retain '$token'.")
    }
}

if ($initial -match 'migrationBuilder\.CreateTable\(\s*name:\s*"ApplicationLogs"') {
    $errors.Add('ApplicationLogs must not return to a non-idempotent CreateTable migration operation.')
}

foreach ($token in @(
    'Insert a history row only for a migration whose complete signature is already present',
    'SQLite online backup',
    'Refuse partially applied ambiguous schemas',
    'Clear only a parseable lock older than ten minutes')) {
    if (-not $architecture.Contains($token, [StringComparison]::OrdinalIgnoreCase)) {
        $errors.Add("Database migration architecture documentation must retain '$token'.")
    }
}


$migrationDirectory = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Migrations'
$migrationFiles = Get-ChildItem -LiteralPath $migrationDirectory -File -Filter '*.cs' |
    Where-Object { $_.Name -notlike '*.Designer.cs' -and $_.Name -ne 'LocalGptMemoryDbContextModelSnapshot.cs' }
foreach ($migrationFile in $migrationFiles) {
    $migrationId = [IO.Path]::GetFileNameWithoutExtension($migrationFile.Name)
    if (-not $service.Contains('"' + $migrationId + '"', [StringComparison]::Ordinal)) {
        $errors.Add("Database initialization is missing a legacy-adoption signature for migration '$migrationId'.")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Database migration bootstrap and legacy-schema adoption contracts verified.'
