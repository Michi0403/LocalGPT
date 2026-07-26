# Open architecture tasks

Canonical source: `CHANGELOG-v0.1.4-ef-snapshot-runtime-debug.md`, section **Open tasks carried forward**.

This file exists so agents and validation can find unresolved work without interpreting historical changelogs. Do not mark a task solved because code was drafted or a static scan passed. Close it only after implementation, compatibility review, validation, and user-visible verification. Copy every unresolved item into the next current changelog.

Current unresolved work:

1. Re-run Debug startup and complete migrations after the EF snapshot ordering repair.
2. Licensed Windows/DevExpress Release build. The owner Debug build and service-provider construction are confirmed.
3. Existing-database migration, backup, and recovery smoke test.
4. Repository-pull UI integration through safe text ingestion.
5. Rich SQLite custom mask/format/null-text editing UI.
6. Catalog-assisted requirement-link picker and validation.
7. Real-package integration tests.
8. Runtime theme smoke tests across Classic, Fluent, and external Bootstrap themes and all maintained DevExpress component families.
9. Decide whether to vendor Highlight.js theme stylesheets for completely offline switching.

## Latest runtime state

The owner environment compiled the root application, constructed all registered services, configured endpoints, and reached EF migration validation. The remaining observed startup failure was caused by premature snapshot navigation/relationship ordering that created a shared `Dictionary<string, object>` entity under the `LocalGptProject` name. Source ordering is repaired and statically guarded, but item 1 remains open until the owner reruns startup and migrations complete.
