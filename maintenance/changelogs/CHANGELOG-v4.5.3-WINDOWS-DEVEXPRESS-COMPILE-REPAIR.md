# LocalGPT 4.5.3 — Windows DevExpress compile repair

## Windows DevExpress guard repair

- Fixed the DevExpress Razor control guard's Windows path normalization so the two intentional native reconnect/reload controls in `App.razor` are correctly recognized as the circuit-independent exception.
- The reconnect and reload controls remain native by design because they must function while the InteractiveServer circuit is unavailable; ordinary interactive Razor controls remain DevExpress-first.
- The guard still reports exact file/line diagnostics for any ordinary native or legacy Razor control introduced elsewhere.

## Razor/DevExpress compile-surface fixes

- Replaced ambiguous DevExpress `ValueChanged` method-group bindings in the main language selector and Council spooler selector with explicit typed callbacks.
- Replaced the four Council Teams checkbox callback expressions reported by the compiler with explicit `bool` callbacks while preserving the existing change-handler behavior.
- Rewrote mixed static/C# `CssClass` component attributes as single C# expressions. This includes the reported Human Collaboration and Council Teams failures plus the same fragile pattern in Configuration Workbench navigation, Project Maintenance, Projects, and Remote Control.
- No render-mode declaration, Council seed version, runtime-class seed version, database migration, wire protocol, or deployment flow is changed.

## Release identity and validation boundary

- Advanced LocalGPT from 4.5.2 to 4.5.3; the single-digit second/third version-slot policy remains satisfied.
- Updated application/installer/webview-wrapper versions, browser cache keys, user-agent strings and documentation release identity.
- Validation in this environment remains source/static only: no `.NET` restore/build/publish and no GitHub/online repository access is used.
