# Open architecture tasks

Canonical source: `CHANGELOG-v0.1.4-service-lifecycle-debug.md`, section **Open tasks carried forward**.

This file exists so agents and validation can find unresolved work without interpreting historical changelogs. Do not mark a task solved because code was drafted or a static scan passed. Close it only after implementation, compatibility review, validation, and user-visible verification. Copy every unresolved item into the next current changelog.

Current unresolved work:

1. Compile the root LocalGPT project and run startup against a backed-up owner database with this candidate.
2. Verify compatibility backup creation, migration-history adoption, pending migrations, and initial catalog seeding.
3. Inspect and record `__EFMigrationsHistory` after a successful owner run.
4. Licensed Windows/DevExpress Release build.
5. Runtime theme initialization/switch/rollback/reconnect/disposal testing.
6. Runtime supervised route, collaboration refresh, and Chat autosave testing during rapid navigation and disposal.
7. Continue converting legacy method-parameter logger utilities to injected logging and bounded service activity at high-level boundaries.
8. Review singleton registrations for thread safety and move circuit/user mutable state to scoped services where required.
9. Migration/recovery smoke tests for empty, logging-only, untracked-current, partial, locked, and corrupt SQLite databases.
10. Repository-pull UI integration through safe text ingestion.
11. Rich SQLite custom mask/format/null-text editing UI.
12. Catalog-assisted requirement-link picker and validation.
13. Real-package DevExpress and SQLite integration tests.
14. Decide whether to vendor Highlight.js theme stylesheets for completely offline switching.

## Latest runtime state

The owner environment previously compiled the root application, validated the service provider, configured middleware/endpoints, and entered EF migrations. The snapshot-order crash was repaired. The later existing-table/migration-history mismatch received compatibility backup/adoption logic. This iteration fixes the reported migration-signature compiler error, awaits every DevExpress theme change, and removes discarded component tasks through supervised execution. Owner compile and startup validation remain open for this exact source fingerprint.
