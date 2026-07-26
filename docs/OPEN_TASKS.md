# Open architecture tasks

Canonical source: `CHANGELOG-v0.1.4-database-bootstrap-runtime-debug.md`, section **Open tasks carried forward**.

This file exists so agents and validation can find unresolved work without interpreting historical changelogs. Do not mark a task solved because code was drafted or a static scan passed. Close it only after implementation, compatibility review, validation, and user-visible verification. Copy every unresolved item into the next current changelog.

Current unresolved work:

1. Re-run Debug startup against the owner database and verify compatibility backup, migration-history adoption, all pending migrations, and initial data seeding complete.
2. Inspect and record `__EFMigrationsHistory` after the successful owner run.
3. Licensed Windows/DevExpress Release build.
4. Migration/recovery smoke tests for empty, logging-only, untracked-current, partial, and corrupt SQLite databases.
5. Repository-pull UI integration through safe text ingestion.
6. Rich SQLite custom mask/format/null-text editing UI.
7. Catalog-assisted requirement-link picker and validation.
8. Real-package integration tests.
9. Runtime theme smoke tests across Classic, Fluent, and external Bootstrap themes and all maintained DevExpress component families.
10. Decide whether to vendor Highlight.js theme stylesheets for completely offline switching.

## Latest runtime state

The owner environment compiled the root application, validated the service provider, configured middleware/endpoints, and entered EF migrations. The EF snapshot entity-order crash is closed. The latest observed failure was an existing `ApplicationLogs` table with empty migration history. The source now preserves that compatible table, creates an online SQLite backup, adopts only verified migration signatures, and runs normal EF migration for genuinely pending work. Item 1 remains open until the owner reruns startup.
