# Database migration bootstrap architecture

## Purpose

LocalGPT must support databases created by earlier development builds that wrote application tables before EF migration history was introduced or completed. It must preserve user data and must not blindly mark an unknown schema as current.

## Required sequence

1. Run bounded SQLite integrity and write probes.
2. Open the existing database directly through `Microsoft.Data.Sqlite`.
3. Inspect `__EFMigrationsLock`. Clear only a parseable lock older than ten minutes; reject recent or unreadable locks.
4. Ensure the EF history table exists.
5. Read the actual table and column schema.
6. Compare migrations in chronological order against stable table/column signatures.
7. Create an online SQLite compatibility backup before adopting any history row or migrating an untracked `ApplicationLogs` table.
8. Insert a history row only for a migration whose complete signature is already present.
9. Permit the special logging-only bootstrap only when every required `ApplicationLogs` column exists and no other initial-migration table exists.
10. Refuse partially applied ambiguous schemas and report missing markers plus the backup path.
11. Run normal EF `MigrateAsync` for every genuinely pending migration.
12. Seed catalog data only after migration succeeds.
13. Open the database-logger readiness gate only after the final deterministic seed stage; queued startup diagnostics must not write `ApplicationLogs` during migration or seed saves.

## Responsibility split

`DatabaseInitializationService` is the high-level hosted-operation boundary. It owns health checking, invokes compatibility preparation, runs EF migration, and seeds initial data. `DatabaseMigrationCompatibilityService` separately owns schema inspection, SQLite online backup, verified migration-history adoption, and stale-lock handling. Both are DI services with constructor-injected logging and bounded service-activity reporting; neither is static. `DatabaseLoggerReadiness` is a separate one-way DI gate: the database logger can enqueue startup entries immediately but cannot create its persistence context until initialization opens the gate.

## Security and data-preservation rules

- Never drop or recreate a compatible user table merely to satisfy migration history.
- Never adopt a migration from table names alone when the migration changes existing columns.
- Never clear a recent or unreadable migration lock; another LocalGPT process may own it.
- Never mark all migrations applied without verifying their signatures.
- Never copy a live WAL-mode database through ordinary file copying; use SQLite online backup.
- Migration-history adoption is local maintenance metadata, not AI authorization.
- Sensitive values and table contents must not be placed in compatibility log messages.

## Logging table exception

`ApplicationLogs` can predate EF history because logging may have been enabled by an earlier LocalGPT build. The initial migration therefore owns an idempotent table/index declaration. Existing compatible rows remain untouched.

## Validation

`build/Assert-DatabaseMigrationBootstrap.ps1` protects this sequence and rejects reintroduction of a non-idempotent logging-table migration, migration before legacy adoption, blind history stamping, or loss of the compatibility backup.
