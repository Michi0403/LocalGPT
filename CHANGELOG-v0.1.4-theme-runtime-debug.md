# LocalGPT v0.1.4 — DevExpress theme-runtime debug candidate

Status: **debug candidate; owner compile and runtime validation required**.

This changelog is the current architecture ledger. Historical changelogs remain evidence and must not be rewritten as though later validation happened earlier.

## Closed in this iteration

- [x] Added the missing `LocalGPT.Interfaces` import to `RegexPatternService` for `IDatabaseInitializationService`.
- [x] Removed the manual `new ThemeService()` fallback from `ThemeJsChangeDispatcher`; one scoped DI instance is now the source of truth for a Blazor circuit.
- [x] Rewired startup theme loading through `DxResourceManager.RegisterTheme(ITheme)`.
- [x] Rewired runtime theme changes through DevExpress `IThemeChangeService.SetTheme(ITheme)`.
- [x] Registered external Bootstrap theme stylesheets through `Themes.BootstrapExternal.Clone(...AddFilePaths...)` instead of replacing DevExpress links from JavaScript.
- [x] Registered Fluent themes with their actual light/dark mode and enabled their supported Bootstrap/page-element styles so LocalGPT layout CSS receives stable Bootstrap variables.
- [x] Reduced `theme-controller.js` to cookie persistence, `data-bs-theme`, LocalGPT theme metadata, and bounded Highlight.js stylesheet switching. It no longer owns DevExpress or Bootstrap component-theme links.
- [x] Added `css/localgpt-theme-contract.css`, with Bootstrap-backed variables and explicit fallbacks for custom LocalGPT surfaces and native inputs. DevExpress internal selectors remain owned by DevExpress.
- [x] Replaced fixed colors in the main custom CSS paths with theme-aware variables where they represented application surfaces, borders, statuses, or shadows.
- [x] Restored the selectable **Blazing Berry** theme that was recognized by the old component model but omitted from the newer theme list.
- [x] Hardened theme restoration across prerender and the interactive circuit by using cookie state plus `PersistentComponentState` and always persisting the effective validated theme name.
- [x] Made theme failure behavior reversible: a failed runtime change restores the previous theme, logs the technical failure, records bounded component activity, and sends a sanitized notification.
- [x] Converted the Minecraft diagnostic command result from a nullable dereference into an explicit failed-workflow exception when an approved command produces no result.
- [x] Compared the supplied older component set against the current component tree. All older JavaScript/theme resources are present; all older routed UI surfaces and DevExpress component families remain. The former manual layout error renderer is superseded by `SafeErrorBoundary`, and the removed drawer disposal methods contained no resources or behavior.
- [x] Added a theme-architecture guard that rejects manual ThemeService construction, raw DevExpress stylesheet swapping, missing resource-manager registration, missing Bootstrap/Fluent theme contracts, missing Blazing Berry, and reintroduction of nullable Minecraft build-result use.

## Open tasks carried forward

- [ ] Run the licensed Windows/DevExpress Debug and Release builds and record the resulting compiler logs.
- [ ] Execute startup service-provider validation and confirm no additional runtime DI cycles.
- [ ] Smoke-test migration on a copy of an existing LocalGPT SQLite database, including downgrade/backup behavior.
- [ ] Add a repository-pull UI that feeds downloaded harmless text files through `ISafeTextDocumentService`; the ingestion service and approved DXAI import function exist, but no safe pull-UI integration point has yet been owner-tested.
- [ ] Add richer editable mask/format/null-text fields to the SQLite preference UI. Editor-kind persistence and automatic inference are complete; custom mask strings are persisted and displayed but not yet edited from the page.
- [ ] Add a visual requirement-link browser with validation against live business-object/function catalogs. Stable named links are saved now; catalog-assisted pickers remain future polish.
- [ ] Add integration tests using the real DevExpress packages and SQLite provider after the owner build environment restores licensed dependencies.
- [ ] Runtime-smoke every selectable theme family—Classic, Fluent light/dark, and external Bootstrap—across DXChat, Grid/database editing, PivotTable, PDF Viewer, RichEdit, drawers, toasts, approval inbox, and native fallback inputs.
- [ ] Decide whether to vendor the Highlight.js theme stylesheets for fully offline theme switching. The current optional CDN behavior is preserved and bounded so it cannot block a theme change indefinitely.

A task may be marked closed only after implementation, compatibility review, validation coverage, and user-visible verification. Every unresolved item must be copied into the next current changelog until closed or explicitly rejected by the human maintainer.
