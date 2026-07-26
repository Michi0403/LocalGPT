param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$initializationPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseInitializationService.cs'
$compatibilityPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
$initialMigrationPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Migrations/20260616222639_Initial.cs'
$architecturePath = Join-Path $RepositoryRoot 'docs/DATABASE_MIGRATION_BOOTSTRAP.md'

foreach ($path in @($initializationPath, $compatibilityPath, $initialMigrationPath, $architecturePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required migration-bootstrap file is missing: $path"
    }
}

$initialization = Get-Content -LiteralPath $initializationPath -Raw
$compatibility = Get-Content -LiteralPath $compatibilityPath -Raw
$initial = Get-Content -LiteralPath $initialMigrationPath -Raw
$architecture = Get-Content -LiteralPath $architecturePath -Raw
$errors = [System.Collections.Generic.List[string]]::new()

foreach ($token in @(
    'IDatabaseMigrationCompatibilityService migrationCompatibility',
    'await databaseFileHealth.EnsureHealthyOrRecoverAsync',
    'await migrationCompatibility.PrepareAsync',
    'await db.Database.MigrateAsync',
    'IServiceActivityService serviceActivity')) {
    if (-not $initialization.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Database initialization must retain '$token'.")
    }
}

foreach ($token in @(
    'public sealed class DatabaseMigrationCompatibilityService',
    'await ClearAbandonedMigrationLockAsync',
    'abandonedMigrationLockAge = TimeSpan.FromMinutes(10)',
    'sourceConnection.BackupDatabase(destinationConnection)',
    'INSERT OR IGNORE INTO \"__EFMigrationsHistory\"',
    'IsSupportedApplicationLogsBootstrap',
    'SignatureState.Partial',
    'signature.Requirements.Length',
    'did not guess at destructive repairs')) {
    if (-not $compatibility.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Database migration compatibility service must retain '$token'.")
    }
}

if ($compatibility.Contains('signature.Requirements.Count`n', [StringComparison]::Ordinal) -or
    $compatibility -match 'signature\.Requirements\.Count\s*(\r?\n|\?|:)') {
    $errors.Add('Migration signature cardinality must use the array Length property, not the Enumerable.Count method group.')
}

$healthPosition = $initialization.IndexOf('await databaseFileHealth.EnsureHealthyOrRecoverAsync', [StringComparison]::Ordinal)
$adoptionPosition = $initialization.IndexOf('await migrationCompatibility.PrepareAsync', [StringComparison]::Ordinal)
$migrationPosition = $initialization.IndexOf('await db.Database.MigrateAsync', [StringComparison]::Ordinal)
if (-not ($healthPosition -ge 0 -and $healthPosition -lt $adoptionPosition -and $adoptionPosition -lt $migrationPosition)) {
    $errors.Add('Database initialization must run health checks, compatibility reconciliation, and EF migration in that order.')
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
    if (-not $compatibility.Contains('"' + $migrationId + '"', [StringComparison]::Ordinal)) {
        $errors.Add("Database migration compatibility is missing a verified signature for migration '$migrationId'.")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Database migration bootstrap and compatibility-service contracts verified.'
