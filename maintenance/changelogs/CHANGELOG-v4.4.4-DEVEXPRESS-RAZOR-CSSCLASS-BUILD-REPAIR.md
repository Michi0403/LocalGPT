# LocalGPT 4.4.4 — DevExpress Razor CssClass build repair

## Scope

This maintenance release corrects the Razor compiler regression reported from the 4.4.3 `ChatGameConsole` DevExpress conversion. It intentionally keeps the 4.4.3 ASCII-surface, popup, installer-identity and documentation behavior unchanged.

## Build repair

- Corrected the Chat, Game and Operator `DxButton.CssClass` bindings in `ChatGameConsole.razor`.
- The 4.4.3 markup mixed literal attribute text with an inline Razor conditional, which Razor component attributes reject with `RZ9986`.
- Each affected `CssClass` is now one explicit Razor expression that concatenates the stable base classes with the conditional `is-active` suffix.
- The resulting CSS class names and mode-selection behavior are unchanged; only the Razor representation was corrected.

## Regression protection

- Extended `build/audit_chat_ascii_console.py` so the mixed-content `CssClass` form is rejected and all three mode buttons must use the pure-expression form.
- Added the 4.4.4 release audit while retaining all 4.4.3 release contracts: required ASCII-surface state, DevExpress popup lifetime, ordinary DevExpress console controls, macOS Installer title metadata, Mermaid diagram repair, `InteractiveServer` routes, and the untouched restored 4.4.2 shell/menu.
- `MainLayout.razor` and `Drawer.razor` remain byte-for-byte identical to the supplied 4.4.2 source.

## Validation boundary

No `.NET` restore/build/publish, NuGet operation, GitHub access, GitHub API call, or online repository access was performed for this source-only correction. The reported Windows compiler output was used as the concrete failure signal, and the source was repaired and statically audited accordingly.
