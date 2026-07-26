# LocalGPT v0.1.4 — database bootstrap runtime debug candidate

Status: **debug candidate; owner startup validation required**.

This changelog is the current architecture ledger. Historical changelogs remain evidence and must not be rewritten as though later validation happened earlier.

## Closed in this iteration

- [x] Confirmed the owner Debug build, DI validation, middleware setup, endpoint registration, and EF migration entry all succeed up to the existing-database compatibility boundary.
- [x] Preserved the EF snapshot ordering repair; `LocalGptProject` is no longer materialized as a shared `Dictionary<string, object>` entity.
- [x] Made the `ApplicationLogs` part of the initial migration idempotent with `CREATE TABLE IF NOT EXISTS` and `CREATE INDEX IF NOT EXISTS` so a compatible pre-existing logging table is preserved.
- [x] Added verified legacy migration-history adoption before `MigrateAsync`. A migration is adopted only when its required tables and marker columns already exist.
- [x] Added a supported bootstrap path for the common legacy state where only a compatible `ApplicationLogs` table exists and `__EFMigrationsHistory` is empty.
- [x] Added an online SQLite compatibility backup through `SqliteConnection.BackupDatabase` before LocalGPT writes migration-history records or migrates an untracked logging table.
- [x] Added explicit partial-schema refusal. LocalGPT reports the missing table/column markers and backup path instead of guessing at destructive repairs.
- [x] Added bounded abandoned `__EFMigrationsLock` recovery: locks older than ten minutes are cleared with a warning; recent or unreadable locks fail clearly instead of waiting forever or risking another active instance.
- [x] Changed database-logger disposal to complete and drain its queue before cancellation, reducing disposed-service-provider noise during orderly shutdown while retaining the bounded timeout fallback.
- [x] Kept all migration, seeding, database-health, logging, project, collaboration, preset, knowledge-rating, and SQL-editor features intact.
- [x] Added migration-bootstrap architecture validation and integrated it into repository validation and source hygiene.
- [x] Added a regression test fixture that verifies an existing `ApplicationLogs` row survives the idempotent initial-migration SQL.

## Open tasks carried forward

- [ ] Re-run Debug startup against the owner database and verify the compatibility backup path is logged, all pending migrations complete, and initial catalog seeding completes.
- [ ] Inspect `__EFMigrationsHistory` after the successful run and record which verified legacy migrations were adopted versus newly applied.
- [ ] Run the licensed Windows/DevExpress Release build.
- [ ] Run migration and recovery smoke tests against copies representing: empty database, logging-table-only database, fully current schema without migration history, partially applied schema, and corrupt SQLite files.
- [ ] Add a repository-pull UI that feeds downloaded harmless text files through `ISafeTextDocumentService`.
- [ ] Add richer editable mask/format/null-text fields to the SQLite preference UI.
- [ ] Add a visual requirement-link browser with live catalog validation.
- [ ] Add real-package integration tests for DevExpress and SQLite.
- [ ] Runtime-smoke all Classic, Fluent, and external Bootstrap themes across maintained DevExpress component families.
- [ ] Decide whether to vendor Highlight.js theme stylesheets for fully offline switching.

A task may be marked closed only after implementation, compatibility review, validation coverage, and user-visible verification. Every unresolved item must be copied into the next current changelog until closed or explicitly rejected by the human maintainer.
