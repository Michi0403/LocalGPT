# LocalGPT 2.7.8 changelog

## Windows build/documentation repair

- Repaired the source defects exposed by the owner Windows build of 2.7.7 instead of treating the documentation build as clean merely because the application assembly was eventually produced.
- Removed **272 compiler-confirmed misplaced XML documentation blocks** that had been generated inside executable code, argument lists, object/collection initializers, logger calls, throws, and similar non-declaration locations. These blocks were the source of the broad `CS1587` warning wave.
- Corrected generic XML references for `DxAiFunctionParameterBinding<T>` and `SaveFeatureRecordRequest<TRecord>` so the documented generic types can be resolved by the compiler/DocFX rather than emitting `CS1574`.
- Corrected `OneWireController.Validate(OneWireEnvelope)` documentation so its parameter tag matches the actual `envelope` parameter.
- Fixed the documentation enricher's `inheritdoc` handling. It no longer adds local `<param>`, `<typeparam>`, `<returns>`, or `<value>` tags on top of inherited contract documentation, eliminating the generator-side cause of the DocFX duplicate-parameter warnings seen in the 2.7.7 build.
- Hardened expression-bodied member parsing for object/switch initializers (`=> new() { ... };` and related forms) and tightened type-declaration detection so executable constructs such as `if (... is null)` cannot be misclassified as declaration sites.
- Re-ran the repaired documentation pass. Coverage/quality now validates **7,453 direct maintained declarations across 408 C# source files**, and a second pass performs **0 additions / 0 enrichments**.

## JavaScript diagnostics build guard

- Repaired `wwwroot/js/localgpt-bounded-number-editor.js`, the concrete blocker from the earlier 2.7.7 Windows build: the maintained asset now carries the required `javascript-diagnostics: guarded` marker and guarded initialization with explicit diagnostic logging.
- Regenerated `build/javascript-diagnostics-files.sha256` so the bounded-number editor and all maintained browser scripts are represented by their current normalized SHA-256 values.
- The bounded-number editor keeps the 2.7.7 viewport clamping behavior; this repair adds the repository-required diagnostics contract rather than reverting the popup fix.

## 2.7.7 functional repairs preserved

- Revalidated the provider-qualified benchmark stepping/selection path, reviewer-pool behavior, provider benchmark DXFunction delegation, exact sage/member pool invocation counts with intentional repeats, non-blocking Human Collaboration decision handling, duplicate-question identity, and global bounded-number viewport clamping introduced in 2.7.7.
- Revalidated the maintained InteractiveServer boundary: **19 explicit islands/pages and 3 intentionally inherited Theme child components**. No broad render-mode rewrite was performed and no competing nested circuits were introduced.
- Existing X-Rounds, foreground heartbeat behavior, attachment restoration, 1-Wire capability synchronization, text-service ownership, code-generation workspace, and deployment/build policy sources are otherwise preserved.

## Version

- LocalGPT application, WebView wrapper, and installer: **2.7.8**.
- `LocalGPT.WireProtocolVersion` remains **2.1.1** because no 1-Wire message shape changed.
- This is a source-only repair package. No .NET/MSBuild/PowerShell/DocFX build was executed in this environment; the owner Windows build remains authoritative.
