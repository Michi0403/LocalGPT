# LocalGPT 4.9.2 — DevExpress HTML-editor compile repair

## Runtime extension editor

- Added the missing `DevExpress.Blazor` import for `HtmlEditorToolbarGroupNames` / `HtmlEditorToolbarItemNames`.
- Added the missing `DevExpress.Blazor.Office` import for the `IToolbar` HTML-editor toolbar contract.
- Kept the compact Undo/Redo + Insert Code Block toolbar from 4.9.1; no fallback to a plain HTML textarea was introduced.
- Named the `DxPopup.BodyContentTemplate` context explicitly so nested `DxFormLayoutItem` child content no longer produces Razor `RZ9999` context ambiguity.

## Scope

- No runtime-plugin persistence, build/load, function-registry, microphone, approval, Council, greenfield coding, or toolchain-discovery behavior was changed.
- Version identities/cache-busters were advanced to 4.9.2.

## Validation

- Verified the DevExpress API surface against the 25.2 documentation: `IToolbar` is in `DevExpress.Blazor.Office`; the HTML-editor toolbar group/item name classes are in `DevExpress.Blazor`.
- Repository source-level async/architecture/service/DevExpress audits were rerun where executable in the handoff environment.
- No .NET/MSBuild/NuGet build was run here; the user's host build remains authoritative.
