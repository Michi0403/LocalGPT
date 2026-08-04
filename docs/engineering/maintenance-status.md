# Maintenance status

This is the small, honest maintenance corner of the documentation. 🐾 It keeps owner-only validation and unfinished integration work visible without mixing temporary pass notes into the architecture chapters.

A task is complete only after implementation, compatibility review, validation, and user-visible verification. Drafted code or a successful static scan is useful evidence, but not the finish line.

## Build and runtime gates

- Compile the root LocalGPT project and run startup against a backed-up owner database using the exact release candidate.
- Complete a licensed Windows/DevExpress Release build and the supported package matrix.
- Run real-package DevExpress and SQLite integration tests.
- Test supervised routes, collaboration refresh, and Chat autosave during rapid navigation, reconnect, and disposal.
- Review singleton registrations for thread safety; move circuit- or user-mutable state to scoped services where required.

## Persistence and recovery gates

- Verify compatibility-backup creation, migration-history adoption, pending migrations, and initial catalog seeding.
- Inspect and record `__EFMigrationsHistory` after a successful owner run.
- Run migration and recovery smoke tests for empty, logging-only, untracked-current, partial, locked, and corrupt SQLite databases.

## UI and theme gates

- Test runtime theme initialization, Light/Dark/Auto switching, rollback, reconnect, and disposal in the app and in the shipped DocFX site.
- Confirm the documentation theme preference persists independently on the local application origin and the GitHub Pages origin.
- Decide whether Highlight.js theme stylesheets should be vendored for completely offline switching.
- Complete rich SQLite custom mask, format, and null-text editing.
- Add a catalog-assisted requirement-link picker with validation.

## Remaining integration work

- Continue converting legacy logger helpers to injected logging and bounded service activity at high-level boundaries.
- Complete repository-pull UI integration through safe text ingestion.

## Carry-forward rule

Every unresolved item moves into the next current changelog, issue, or release-planning source. Historical pass manifests and superseded task lists remain available under `docs/internal-notes`, excluded from DocFX, so the public documentation stays focused without losing the trail that led here.
