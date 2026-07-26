# Open architecture tasks

Canonical source: `CHANGELOG-v0.1.4-theme-runtime-debug.md`, section **Open tasks carried forward**.

This file exists so agents and validation can find unresolved work without interpreting historical changelogs. Do not mark a task solved because code was drafted or a static scan passed. Close it only after implementation, compatibility review, validation, and user-visible verification. Copy every unresolved item into the next current changelog.

Current unresolved work:

1. Licensed Windows/DevExpress Debug and Release builds.
2. Runtime service-provider and migration smoke tests.
3. Repository-pull UI integration through safe text ingestion.
4. Rich SQLite custom mask/format/null-text editing UI.
5. Catalog-assisted requirement-link picker and validation.
6. Real-package integration tests.
7. Runtime theme smoke tests across Classic, Fluent, and external Bootstrap themes and all maintained DevExpress component families.
8. Decide whether to vendor Highlight.js theme stylesheets for completely offline switching.

## Latest compiler and theme state

The missing `IDatabaseInitializationService` namespace import, manual `ThemeService` construction, and nullable Minecraft command-result dereference are addressed in source. Theme startup and runtime switching now use DevExpress `ITheme` resource APIs. These items remain owner-verification pending until the next Windows/DevExpress build and runtime theme pass confirm the root `LocalGPT` project and wrapper both work. Do not close items 1, 2, 6, or 7 merely because static checks pass.
