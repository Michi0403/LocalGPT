# LocalGPT v0.1.4 — EF snapshot runtime debug candidate

Status: **debug candidate; owner migration/runtime verification required**.

This changelog is the current architecture ledger. Historical changelogs remain evidence and must not be rewritten as though later validation happened earlier.

## Closed in this iteration

- [x] Confirmed the root `LocalGPT` project compiles and the host reaches `BuildWebApp`, endpoint registration, and startup service-provider validation in the owner environment.
- [x] Diagnosed the migration crash as snapshot ordering, not a missing CLR navigation or missing database-first feature.
- [x] Moved the `LocalGptProject` scalar/property declaration before every relationship that targets it.
- [x] Removed premature and duplicate project topic/version relationship blocks.
- [x] Moved `Artifacts`, `Requirements`, `Revisions`, `Topics`, `Versions`, `ChildRevisions`, `Links`, and `KnowledgeLinks` collection-navigation declarations to the final snapshot navigation section.
- [x] Preserved all project artifacts, requirements, revisions, topics, versions, document imports, model presets, SQLite editor preferences, and knowledge-rating relationships.
- [x] Added `build/Assert-EfSnapshotArchitecture.ps1` to reject navigation-before-relationship and relationship-before-entity ordering.
- [x] Added `docs/EF_MIGRATION_SNAPSHOT_ARCHITECTURE.md` and integrated the guard into local validation and source hygiene.

## Open tasks carried forward

- [ ] Re-run the owner Debug startup against a disposable or backed-up database and confirm all migrations complete after the snapshot repair.
- [ ] Run the licensed Windows/DevExpress Release build and record the compiler log. The owner Debug build is confirmed; Release is not.
- [ ] Smoke-test migration on a copy of an existing LocalGPT SQLite database, including backup and recovery behavior.
- [ ] Add a repository-pull UI that feeds downloaded harmless text files through `ISafeTextDocumentService`; the ingestion service and approved DXAI import function exist, but no safe pull-UI integration point has yet been owner-tested.
- [ ] Add richer editable mask/format/null-text fields to the SQLite preference UI. Editor-kind persistence and automatic inference are complete; custom mask strings are persisted and displayed but not yet edited from the page.
- [ ] Add a visual requirement-link browser with validation against live business-object/function catalogs. Stable named links are saved now; catalog-assisted pickers remain future polish.
- [ ] Add integration tests using the real DevExpress packages and SQLite provider after the owner build environment restores licensed dependencies.
- [ ] Runtime-smoke every selectable theme family—Classic, Fluent light/dark, and external Bootstrap—across DXChat, Grid/database editing, PivotTable, PDF Viewer, RichEdit, drawers, toasts, approval inbox, and native fallback inputs.
- [ ] Decide whether to vendor the Highlight.js theme stylesheets for fully offline theme switching. The current optional CDN behavior is preserved and bounded so it cannot block a theme change indefinitely.

A task may be marked closed only after implementation, compatibility review, validation coverage, and user-visible verification. Every unresolved item must be copied into the next current changelog until closed or explicitly rejected by the human maintainer.
