# Validation — LocalGPT v0.1.4 database bootstrap runtime debug candidate

Status: **source-validated debug candidate; owner Windows/DevExpress startup rerun required**.

## Owner evidence incorporated

The owner build produced `LocalGPT.dll`, passed startup service-provider validation, configured middleware/endpoints, and entered EF migration execution. The observed failure was an existing compatible `ApplicationLogs` table while `__EFMigrationsHistory` did not record the initial migration. EF therefore replayed `20260616222639_Initial` and attempted to create the table again.

## Implemented repair

- The `ApplicationLogs` part of the initial migration is idempotent and preserves compatible existing rows.
- Existing SQLite schemas are inspected before `MigrateAsync`.
- Migration-history rows are adopted only when all required tables and marker columns for that migration already exist.
- A logging-table-only legacy database is supported without falsely adopting the complete initial migration.
- A SQLite online backup is created before history adoption or compatibility migration.
- Partial or ambiguous schemas stop with an exact missing-marker report and backup path.
- Abandoned migration locks older than ten minutes are cleared; recent or unreadable locks stop safely.
- Database logger disposal drains its bounded channel before cancellation to reduce shutdown flush errors.

## Checks completed in this environment

- 42 protected governance hashes verified after line-ending normalization.
- All 8 migration source files have matching bootstrap signatures.
- Bootstrap order verified: health check, compatibility preparation, EF context creation, `MigrateAsync`, then seeding.
- Existing `ApplicationLogs` row preservation tested against the exact initial-migration SQL.
- All three logging indexes were created by the compatibility SQL.
- JSON files parsed successfully.
- project/props/targets/XML files parsed successfully.
- Changed C# files passed focused string/comment/delimiter checks.
- No database files, logs, credentials, keys, `bin`, `obj`, `.git`, or `.vs` content is included.

## Not claimed here

- No licensed Windows/DevExpress compilation was performed after this bootstrap change.
- No mutation was run against the owner's real SQLite database.
- Successful migration and seeding remain owner-runtime checks.

## Owner test

Back up the current database or test on a copy. Build `LocalGPT`, start it once, and verify:

1. A path under `CompatibilityBackups` is logged when adoption/compatibility work is needed.
2. `__EFMigrationsHistory` contains the adopted and newly applied migration IDs.
3. All pending migrations finish.
4. Initial regex, prompt, variable, and knowledge seeding finishes.
5. A second startup performs no compatibility adoption and starts normally.
