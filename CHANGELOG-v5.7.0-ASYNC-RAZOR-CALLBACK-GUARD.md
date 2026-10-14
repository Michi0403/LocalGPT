# LocalGPT 5.7.0 — awaited Razor source callbacks and version-independent build guards

## Cause of the 5.6.9 Windows build failure
`Assert-RuntimePluginSourceCallbacks.ps1` delegated to the old Python audit `audit_runtime_plugin_source_callbacks_5_6_6.py`. That audit compared the application to a former release number and could block builds even when the callback contract was intact. It also required exact non-`async` lambda source text instead of verifying an awaited callback.

## Correction
- Project, revision, and curated-file `DxComboBox.ValueChanged` selectors now use explicitly typed **`async (Guid value) => await ...`** lambdas. They receive the selected value and the render-captured `RuntimePluginSourceEditContext`.
- The curated-file handler was converted from `void` into a genuine awaited `Task` method; its state changes and failure messages run via the InteractiveServer renderer's `InvokeAsync` and are guarded against stale draft identities.
- Replaced the Python callback audit with a **PowerShell-only** maintained `build/Assert-RuntimePluginSourceCallbacks.ps1`. It checks the actual three DevExpress selectors, asynchronous handler signatures, context identity, stale-result generation, compiler-friendly `ValueExpression`, disabled-while-busy state, editor retention, and absence of direct method groups. No hardcoded LocalGPT version and no Python prerequisite for this guard.
- Removed `build/audit_runtime_plugin_source_callbacks_5_6_6.py` because it asserted the incorrect contract.
- Removed stale release-identity pinning from the existing Python/project and release-packaging Python audits. Both still verify their substantive Python.NET/GIL/project and PKCS/package security invariants.
- Advanced LocalGPT to 5.7.0 (repository's one-digit minor/patch scheme) and refreshed browser script cache tokens. No changes to .NET SDK 10.0.402, DevExpress 25.2.10, packaging helper 1.0.5, or security pins.

## Validation limitations
Source-based contract checks and nine negative callback mutations were exercised in the packaging environment. Windows PowerShell 5.1 and the .NET/Razor compiler were not available, so the release **does not claim** a successful Visual Studio build or runtime UI test. Run the included guard on Windows before the full build to verify PowerShell execution.
