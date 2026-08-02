# LocalGPT 2.1.12 — Compiler and renderer-continuation corrections

## Fixed compiler and maintenance failures

- Fully qualifies `LocalGPT.BusinessObjects.ConfigurationRoot` in `AdaptiveOllamaBenchmarkWiring` and the Install component to avoid the Microsoft configuration type-name collision.
- Restores all project architecture, debug-artifact and project maintenance DXFunction binders to the canonical `DxAiFunctionInvocationRequest.Parameters` JSON payload.
- Fixes the PowerShell async-policy fallback interpolation (`${relative}:$line`) so Windows PowerShell does not parse the path as a drive-qualified variable.
- Keeps the OneWire Security lifecycle diagnostics check aligned with its renderer-affine QR rendering continuations.

## Fine-grained continuation policy

- Every ordinary `await` in C# and Razor code must explicitly choose `ConfigureAwait(false)` or `ConfigureAwait(true)`.
- Services, controllers, persistence, diagnostics, networking infrastructure and background workflows use `ConfigureAwait(false)`.
- Blazor lifecycle entry points (`OnInitializedAsync`, `OnParametersSetAsync`, `OnAfterRenderAsync`) may retain the renderer context.
- Renderer-affine loading helpers are declared by exact file and method name in `build/async-continuation-baseline.json`; a broad per-file allowance is not accepted.
- HTTP, SignalR and object-domain loading helpers retain the renderer only when their continuation directly applies loaded state to component fields. Background probes that marshal only their final assignment through `InvokeAsync` remain context-free.
- The audit extracts Razor `@code`/`@functions` blocks instead of tokenizing markup as C#, detects unconfigured Razor awaits, and rejects `ConfigureAwait(true)` outside Components or outside the exact reviewed methods.
- `await foreach` remains explicitly configured with `ConfigureAwait(false)`; `await using` remains the language-level asynchronous-disposal construct.

## Versioning

- LocalGPT application and organic 1-Wire application advertisement: `2.1.12`.
- The separately versioned `LocalGPT.WireProtocolVersion` package is unchanged.
