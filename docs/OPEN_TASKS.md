# Open architecture tasks

Canonical source: `CHANGELOG-v0.1.4-database-first-debug.md`, section **Open tasks carried forward**.

This file exists so agents and validation can find unresolved work without interpreting old historical changelogs. Do not mark a task solved because code was drafted. Close it only after implementation, compatibility review, validation, and user-visible verification. Copy every unresolved item into the next current changelog.

Current unresolved work:

1. Licensed Windows/DevExpress Debug and Release builds.
2. Runtime service-provider and migration smoke tests.
3. Repository-pull UI integration through safe text ingestion.
4. Rich SQLite custom mask/format editing UI.
5. Catalog-assisted requirement-link picker and validation.
6. Real-package integration tests.


## Latest compiler-feedback state

The compiler errors and nullability warnings reported after the first database-first candidate are addressed in source. They remain **owner-verification pending** until the next Windows/DevExpress build confirms the root `LocalGPT` project and wrapper both compile. Do not close items 1 or 2 merely because static checks pass.
