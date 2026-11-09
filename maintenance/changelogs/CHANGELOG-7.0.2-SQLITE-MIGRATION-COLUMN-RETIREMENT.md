# LocalGPT 7.0.2 — SQLite-safe column retirement for the 7.0.0 game-engine migration

7.0.2 is 7.0.1 plus one startup fix. Nothing else changed.

**Not compiled.** No .NET SDK was available. The new SQL was run against SQLite 3.53 (see Verification).

## Fixed: the 7.0.0 migration stopped startup on SQLite

`20261108200000_AddGameEngineRules` called `DropColumn("RuntimeProfile")` in `Up`. The EF SQLite generator cannot execute that, so startup failed with `NotSupportedException: SQLite does not support this migration operation ('DropColumnOperation')`. The exception is thrown while the SQL is generated, before any statement of that migration runs, so a database that hit it is still in its pre-7.0.0 schema.

- **Migration.** `Up` keeps the `RuntimeProfile` column. It still adds the engine columns, creates the rule tables and maps `RuntimeProfile` to `ViewKey` and `RulesetTemplateKey`.
- **Post-migration drop.** New `IDatabaseMigrationCompatibilityService.ReconcileAfterMigrationAsync` runs right after `MigrateAsync` and before the seed stages. It drops the obsolete column only after checking the real database:
  - the column still exists;
  - the migration is recorded;
  - `ViewKey` and `RulesetTemplateKey` exist;
  - no index, key, trigger or view uses the column;
  - SQLite is 3.35 or newer.

  It takes the usual compatibility backup first. Then, in one transaction, it maps any row that is still unmapped, runs `ALTER TABLE … DROP COLUMN` and verifies that the column is gone and no foreign-key violation exists. The drop is required: the old column is `NOT NULL` without a default, so it would reject every new game profile row.
- The obsolete column is listed in `SqliteColumnRetirement`, so later retirements follow the same path.
- **Build guard.** `build/Assert-EfModelSnapshotConsistency.ps1` now fails when a migration's `Up` uses `DropColumn`, `AlterColumn` or the key and constraint operations SQLite cannot run. It flags the 7.0.0 migration and passes the fixed one.
- **Verification.** The SQL sequence was tested against SQLite 3.53 with a pre-7.0.0-shaped table, a child table with a foreign key, a pre-existing unmapped row and the new index. Inserts failed before the drop and succeeded after it, child rows were kept, and `integrity_check` returned `ok`.

## Changed files

- `src/LocalGPT/Migrations/20261108200000_AddGameEngineRules.cs`: `DropColumn` removed from `Up`.
- `src/LocalGPT/BusinessObjects/SqliteColumnRetirement.cs` (new).
- `src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.ColumnRetirement.cs` (new).
- `src/LocalGPT/Interfaces/IDatabaseMigrationCompatibilityService.cs`: adds `ReconcileAfterMigrationAsync`.
- `src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs`: calls it after migration.
- `build/Assert-EfModelSnapshotConsistency.ps1`: rejects SQLite-unsupported operations in `Up`.
- `docs/architecture/project-data.md`: new section "Dropping columns on SQLite".
- `src/LocalGPT/LocalGPT.csproj`: version 7.0.2.

## What to expect on first start

A Warning says that `LocalGptGameProjectProfiles.RuntimeProfile` was dropped, with the compatibility backup path. Later starts log nothing about it.

If startup stopped in the debugger during the failed 7.0.0/7.0.1 migration, EF may have left a row in `__EFMigrationsLock`. For up to 10 minutes LocalGPT refuses to migrate and says why. After that it clears the lock itself.
