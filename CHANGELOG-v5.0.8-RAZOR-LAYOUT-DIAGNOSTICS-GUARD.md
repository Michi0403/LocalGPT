# LocalGPT 5.0.8 — Razor layout and diagnostics guard

- Added a build-breaking aggregate Razor maintenance audit that reports **all findings from the audit** with exact file/line/column diagnostics and architectural repair choices.
- Approved component-level layout owners are exactly `DxGridLayout`, `DxCarousel`, `DxDrawer`, `DxFormLayout`, `DxSplitter`, `DxStackLayout`, and `DxTabs`. Native `div`/`span` may remain only inside the DevExpress-owned layout.
- `<section>` and `<dialog>` are forbidden in maintained Razor source. Modal workflows use `DxPopup`/reviewed DevExpress window-prompt controls; ordinary conditional UI uses an approved DevExpress layout.
- Every Razor component must own `@inject ILogger<ComponentName> Logger`; Razor and code-behind methods require method-local diagnostics and structured logging.
- Expression-bodied computed Razor properties must delegate to a named instance method. Async/service/JS work belongs in the correct Blazor lifecycle/event and stores render state rather than creating an async property.
- Restored the existing LocalGPT component-safety target to the normal build chain.
- Layered the new guard behind DevExpress component retention so removing the maintenance target/script is itself a build violation.
- Hardened the localization identical-text baseline and Razor component-root discovery for Windows PowerShell 5.1 scalar/collection behavior.
- Updated repository instructions with the non-negotiable layout, logging, forbidden-tag, and computed-property contracts.

This release deliberately does not mass-edit the existing Razor violations. The purpose is to let a real build enumerate the current findings first, so the subsequent repair can address them together without hiding or deleting features.
