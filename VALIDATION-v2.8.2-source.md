# LocalGPT 2.8.2 source validation

- Supplied runtime log traced the browser failure to a Blazor/DevExpress render event-tracker collision after a Back-Forward Cache restore; server-owned Council execution continued independently.
- BFCache recovery now performs a clean reload/rejoin before Blazor can reuse preserved event registrations.
- Benchmark structured-output parsing now decodes entity-encoded JSON, accepts JSON comments/trailing commas and parses the first object without requiring trailing model prose to be JSON.
- Text-service ownership repair: human-visible entity decoding now belongs to `CouncilTextService`; affected Razor components no longer call `WebUtility.HtmlDecode` directly. The ownership guard was emulated against its maintained baseline with **0** new findings.
- Stale `GetHardwarePerformancePresetFunction` XML `presets` parameter documentation removed while the valid list-function `presets` documentation is retained.
- Architecture policy audit: passed.
- Service resilience: **1,843** service methods passed; 30 yield methods and 3 direct Program/Startup methods excluded by policy.
- Async continuation audit: **158** files, 2,336 await tokens, 2,126 `ConfigureAwait(false)`, 30 renderer-affine `ConfigureAwait(true)`, 2 preconfigured awaitables, 175 reviewed await-using disposals and 3 configured async streams.
- Provider-qualified Council audit: **280** checks passed.
- X-Rounds, codegen/DXFunction and documentation/1-Wire audits: passed.
- Human-visible entity and dedicated 2.8.2 benchmark/rejoin audits: passed.
- XML documentation coverage: **7,546** direct C# declarations across **414** maintained source files.
- JavaScript syntax: **24** maintained browser files passed Node syntax checking.
- JavaScript diagnostics manifest refreshed and hash-verified for all **24** maintained browser files.
- Localization: **1,862** matching unique en-US/de-DE keys; no case-insensitive duplicates.
- LocalGPT, WebView wrapper and installer versions are **2.8.2**. Wire protocol remains **2.1.1**.
- No dotnet/MSBuild compilation was performed in this source-only environment.
