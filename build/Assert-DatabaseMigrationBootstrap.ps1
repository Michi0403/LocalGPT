param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
$initializationPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseInitializationService.cs'
$compatibilityPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
$initialMigrationPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Migrations/20260616222639_Initial.cs'
$architecturePath = Join-Path $RepositoryRoot 'docs/architecture/project-data.md'
$chatMemoryPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/EfChatMemoryService.cs'
$databaseLoggerPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Logging/DatabaseLoggerProvider.cs'
$databaseLoggerReadinessPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/DatabaseLoggerReadiness.cs'
$programPath = Join-Path $RepositoryRoot 'LocalGPTWebviewWrapper/LocalGPT/Program.cs'

foreach ($path in @($initializationPath, $compatibilityPath, $initialMigrationPath, $architecturePath, $chatMemoryPath, $databaseLoggerPath, $databaseLoggerReadinessPath, $programPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required migration-bootstrap file is missing: $path"
    }
}

$initialization = Get-Content -LiteralPath $initializationPath -Raw
$compatibility = Get-Content -LiteralPath $compatibilityPath -Raw
$initial = Get-Content -LiteralPath $initialMigrationPath -Raw
$architecture = Get-Content -LiteralPath $architecturePath -Raw
$chatMemory = Get-Content -LiteralPath $chatMemoryPath -Raw
$databaseLogger = Get-Content -LiteralPath $databaseLoggerPath -Raw
$databaseLoggerReadiness = Get-Content -LiteralPath $databaseLoggerReadinessPath -Raw
$program = Get-Content -LiteralPath $programPath -Raw
$errors = [System.Collections.Generic.List[string]]::new()

foreach ($token in @(
    'IDatabaseMigrationCompatibilityService migrationCompatibility',
    'await databaseFileHealth.EnsureHealthyOrRecoverAsync',
    'await migrationCompatibility.PrepareAsync',
    'await db.Database.MigrateAsync',
    'IServiceActivityService serviceActivity',
    'IDatabaseLoggerReadiness databaseLoggerReadiness',
    'databaseLoggerReadiness.MarkReady()')) {
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


foreach ($token in @(
    'IDatabaseLoggerReadiness databaseLoggerReadiness',
    'await databaseLoggerReadiness.WaitUntilReadyAsync(stop.Token).ConfigureAwait(false)',
    'if (!databaseLoggerReadiness.IsReady)',
    'await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false)')) {
    if (-not $databaseLogger.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Database logger startup isolation must retain '$token'.")
    }
}

foreach ($token in @(
    'TaskCompletionSource<bool>',
    'TaskCreationOptions.RunContinuationsAsynchronously',
    'return ready.Task.WaitAsync(cancellationToken)',
    'ready.TrySetResult(true)')) {
    if (-not $databaseLoggerReadiness.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Database logger readiness gate must retain '$token'.")
    }
}

if (-not $program.Contains('AddSingleton<IDatabaseLoggerReadiness, DatabaseLoggerReadiness>()', [StringComparison]::Ordinal)) {
    $errors.Add('Program.cs must retain the singleton database logger readiness registration.')
}

$loggerWaitPosition = $databaseLogger.IndexOf('await databaseLoggerReadiness.WaitUntilReadyAsync', [StringComparison]::Ordinal)
$loggerContextPosition = $databaseLogger.IndexOf('await dbContextFactory.CreateDbContextAsync', [StringComparison]::Ordinal)
if (-not ($loggerWaitPosition -ge 0 -and $loggerWaitPosition -lt $loggerContextPosition)) {
    $errors.Add('DatabaseLoggerProvider must wait for database readiness before creating its persistence DbContext.')
}

$finalSeedPosition = $initialization.IndexOf('await RunSeedStageAsync("Council model presets"', [StringComparison]::Ordinal)
$loggerReadyPosition = $initialization.IndexOf('databaseLoggerReadiness.MarkReady()', [StringComparison]::Ordinal)
if (-not ($finalSeedPosition -ge 0 -and $finalSeedPosition -lt $loggerReadyPosition)) {
    $errors.Add('Database logger readiness must open only after the final deterministic seed stage.')
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


foreach ($token in @(
    '20260726133000_AddOrganicSkillsAndHardwareRoutes',
    '20260726150000_AddCouncilTeamScripting',
    'TryRepairKnownMigrationAsync',
    'AddColumnIfMissingAsync',
    'ArchiveMalformedIdentityTableAsync',
    'sourceConnection.BackupDatabase(destinationConnection)')) {
    if (-not $compatibility.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Database compatibility must retain lossless organic/council repair token '$token'.")
    }
}

foreach ($token in @(
    'BeginTransactionAsync',
    '.AsNoTracking()',
    '.ExecuteDeleteAsync(cancellationToken)',
    'ConversationId = conversation.Id',
    'transaction.CommitAsync')) {
    if (-not $chatMemory.Contains($token, [StringComparison]::Ordinal)) {
        $errors.Add("Chat memory snapshot replacement must retain '$token'.")
    }
}
if ($chatMemory.Contains('conversation.Messages.Clear()', [StringComparison]::Ordinal)) {
    $errors.Add('Chat memory autosave must not clear a tracked required Messages collection; that causes conceptual-null failures with the required foreign key.')
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
