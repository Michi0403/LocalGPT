# LocalGPT 4.5.3 source validation

## Scope

This release starts from packaged LocalGPT 4.5.2 and addresses the concrete build diagnostics supplied from Windows: two `LGDX0001` reconnect-button violations, two DevExpress `ValueChanged` method-group conversion errors, two mixed Razor/C# `CssClass` parser errors, and four `DxCheckBox.CheckedChanged` generic callback conversion errors. The related mixed `CssClass` pattern is also normalized in the other DevExpress-converted components so the same Razor parser failure does not simply move to the next file.

## Repairs verified statically

- `Assert-DevExpressBlazorControls.ps1` converts Windows backslashes to slash-separated repository-relative paths before applying the `App.razor` reconnect/reload exception.
- `MainLayout.razor` and `CouncilSpoolerPanel.razor` use explicit typed lambdas for the reported DevExpress `ValueChanged` bindings.
- The four Council Teams `DxCheckBox.CheckedChanged` bindings use explicit `bool` callback parameters and preserve their existing handlers.
- Dynamic DevExpress `CssClass` values in Human Collaboration, Council Teams, Configuration Workbench navigation, Project Maintenance, Projects and Remote Control no longer mix literal markup with inline C# in a component attribute.
- The two native `App.razor` reconnect/reload buttons remain the only intentional native controls, because they must operate while the InteractiveServer circuit is unavailable.
- All maintained render-mode declarations, seed versions and wire-protocol behavior remain unchanged.

## Validation boundary

The environment intentionally does not execute `dotnet`, so this package does not claim a local compiler/build pass. Validation consists of the maintained Python source audits, targeted source-pattern checks for the supplied compiler diagnostics, JavaScript syntax and diagnostics-manifest verification, JSON/XML parsing, version-slot checks, render-mode preservation, ZIP CRC/path-safety checks, clean extraction and byte-for-byte comparison against the packaged working tree.
