# LocalGPT 5.0.9 — Razor layout and diagnostics repair

- Repaired the complete 5.0.8 Razor maintenance finding set across routable pages and reusable subcomponents.
- Added/retained the required typed `ILogger<Component>` plus `INotificationService` and `IComponentActivityService` component-safety injections.
- Wrapped maintained component output with approved DevExpress Blazor layout ownership and replaced every maintained `<section>` with a DevExpress layout-backed equivalent.
- Replaced the documentation native `<dialog>` with `DxPopup`; the architecture guard now also rejects native `role="dialog"` and manual modal/dialog backdrop ownership.
- Converted render-time expression properties to simple method-backed properties whose backing methods own structured logging.
- Added method-local try/catch + structured diagnostics to previously unguarded Razor and Razor code-behind methods without deleting operations or flattening DevExpress controls.
- Kept the framework reconnect host exception in `App.razor`; it remains circuit-independent because it must function while an InteractiveServer circuit is unavailable.
- Improved component-safety diagnostic messages so missing/duplicate/misplaced safety directives identify the source location and architectural repair path.
- Preserved existing `@rendermode InteractiveServer` boundaries and the 25.2.10 DevExpress dependency target.
