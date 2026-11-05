# LocalGPT 5.0.7 — Windows PowerShell 5.1 maintenance-guard repair

## Fixed

- Fixed the reviewed German identical-text baseline loader. `ConvertFrom-Json` is now assigned directly before enumeration instead of being wrapped in `@(...)`, avoiding the Windows PowerShell 5.1 nested-array shape that caused `Common.Name = "Name"` to be falsely rejected even though the key was already reviewed in `build/localization-identical-german-baseline.json`.
- Fixed `Assert-RazorComponentAttributeExpressions.ps1` so zero/one/many maintained component roots are always materialized as a collection before `.Count` is used. This removes the observed `PropertyNotFoundStrict` failure on the one-root LocalGPT repository.
- Hardened publish-profile output discovery against the same one-result pipeline/`.Count` behavior.
- Extended `Assert-PowerShellCompatibility.ps1` with source-line diagnostics and architectural repair guidance for both PowerShell 5.1 regression shapes.
- Added the guard-authoring rule to `AGENTS.md`: maintenance scripts are repository production code and must be correct for zero/single/multiple results under StrictMode, not merely parser-compatible.

## Preserved

- DevExpress Blazor remains the owner of ordinary interactive Razor controls.
- Existing InteractiveServer/prerender boundaries are unchanged.
- Razor `RZ9986` prevention remains property/state based; no DevExpress component was replaced by native HTML.
- Localization still requires every maintained culture to keep key parity and German text to be translated or explicitly reviewed as language-neutral.

## Version

- LocalGPT: `5.0.6` → `5.0.7`.
