# LocalGPT v0.1.4 — service lifecycle and async supervision debug candidate

Status: **debug candidate; owner compile and runtime validation required**.

This changelog is the current architecture ledger. Historical changelogs remain evidence and must not be rewritten as though later validation happened earlier.

## Closed in this iteration

- [x] Fixed the migration-signature compiler regression by using the array `Length` property instead of referencing the LINQ `Count` method group.
- [x] Awaited all three DevExpress `IThemeChangeService.SetTheme` calls in `ThemeJsChangeDispatcher`.
- [x] Added guarded theme rollback. A failed selected theme no longer allows a second rollback exception to erase the original diagnostic.
- [x] Removed every explicit discarded asynchronous call from maintained LocalGPT source.
- [x] Added `ISupervisedTaskRunner` for intentionally concurrent component work. It tracks completion, records cancellation/failure, and prevents unobserved task exceptions.
- [x] Moved route recovery, Human Collaboration refresh, Chat collaboration refresh, and Chat autosave onto the supervised-task runner with component-lifetime cancellation.
- [x] Added disposal-race guards so late navigation/collaboration events cannot read a disposed cancellation source; Chat autosave is linked to the owning component lifetime.
- [x] Added `IServiceActivityService` and reused the bounded application-activity queue for sanitized service start/success/cancel/failure awareness. Failures are rethrown; the activity layer is not an exception-swallowing mechanism.
- [x] Extracted legacy SQLite migration-history adoption from `DatabaseInitializationService` into the injected `DatabaseMigrationCompatibilityService`.
- [x] Removed static runtime state and static construction methods from `ThemeService`; the scoped service instance now owns its theme catalog and Highlight.js mapping.
- [x] Moved the pure DXAI JSON helper into `Services/Helpers/DxAiFunctionJsonHelper.cs`, making the permitted static-helper boundary explicit.
- [x] Added service-lifecycle validation that rejects static service classes, manually constructed core services, discarded async calls, unawaited DevExpress theme changes, and the migration-signature method-group regression.
- [x] Integrated the new guard into owner validation and GitHub source hygiene.
- [x] Replaced mutable extension catalogs in `LocalGptCatalogService` with case-insensitive `FrozenSet<string>` catalogs; generated regex accessors and immutable constants remain valid static helpers.
- [x] Expanded the protected architecture set and source-hygiene checks so service-lifecycle rules, supervised async work, immutable catalogs, and the migration responsibility split cannot disappear silently.
- [x] Preserved all database-first, project, council, human-collaboration, theme, SQL-editor, model-preset, safe-import, and DXAI features.

## Open tasks carried forward

- [ ] Compile the root LocalGPT project and run startup against a backed-up owner database with this candidate.
- [ ] Verify compatibility backup creation, migration-history adoption, all pending migrations, and initial catalog seeding complete.
- [ ] Inspect and record `__EFMigrationsHistory` after the successful owner run.
- [ ] Run the licensed Windows/DevExpress Release build.
- [ ] Runtime-test theme initialization, switch, rollback, reconnect, and disposal across Classic, Fluent, and external Bootstrap themes.
- [ ] Runtime-test supervised route/collaboration refresh and Chat autosave during rapid navigation and circuit disposal.
- [ ] Continue converting legacy services that accept an `ILogger` method parameter into constructor-injected logging and bounded service activity. Do not force this into EF materializers, pure mappers, or very low-level hot paths where the boundary caller already logs and records failure.
- [ ] Review existing singleton registrations for proven thread safety and move any circuit/user mutable state to scoped services.
- [ ] Add migration/recovery smoke tests for empty, logging-only, untracked-current, partial, locked, and corrupt SQLite databases.
- [ ] Add a repository-pull UI that feeds downloaded harmless text files through `ISafeTextDocumentService`.
- [ ] Add richer editable mask/format/null-text fields to the SQLite preference UI.
- [ ] Add a visual requirement-link browser with live catalog validation.
- [ ] Add real-package integration tests for DevExpress and SQLite.
- [ ] Decide whether to vendor Highlight.js theme stylesheets for fully offline switching.

A task may be marked closed only after implementation, compatibility review, validation coverage, and user-visible verification. Every unresolved item must be copied into the next current changelog until closed or explicitly rejected by the human maintainer.
