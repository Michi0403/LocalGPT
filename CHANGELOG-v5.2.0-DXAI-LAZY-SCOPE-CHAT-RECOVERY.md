# LocalGPT 5.2.0 — DXAI lazy-scope Chat recovery

## Fixed

- Fixed the remaining frontend activation stall that remained after 5.1.9 stopped the duplicate `ServiceCacheKey` exception.
- The supplied 5.1.9 trace reaches a healthy Kestrel startup, a connected Blazor circuit, and an initialized interactive Index page. The later `GET /chat?toggledSidebar=False` starts and reaches `/chat` routing, but never logs Chat initialization or request completion.
- `Chat.razor` directly injects `IDxAiFunctionRegistry`. In 5.1.9 that interface was a transient factory alias that still called `InitializeHandlers(() => provider.GetServices<IDxAiFunctionHandler>())` synchronously. Resolving the page therefore remained coupled to eager construction of the complete DI-backed DXFunction handler graph.
- Removed the transient/scoped factory-alias pattern entirely. `IDxAiFunctionRegistry` is again a normal scoped interface-to-implementation registration: `AddScoped<IDxAiFunctionRegistry, DxAiFunctionRegistry>()`.
- Restored lazy handler-map materialization with `LazyThreadSafetyMode.ExecutionAndPublication`, so ordinary Chat/page construction can resolve the registry without constructing every DXFunction handler.
- Added scoped `DxAiFunctionHandlerResolver` as the single deferred handler-resolution boundary. It uses the active request/circuit scope, creates no child scope, and leaves the central registry itself free of retained `IServiceProvider` state.
- Preserved the intentional handler-to-registry relationship: when the lazy map is materialized, handlers that depend on `IDxAiFunctionRegistry` receive the already-created scoped registry instance.
- Updated both the service architecture guard and DXFunction source audit to reject the disproven 5.1.9 eager transient alias as well as the earlier re-entrant scoped factory alias.
- Kept the isolated MCP listener architecture from 5.1.8 unchanged.
- Kept all explicit Blazor `@rendermode` / `InteractiveServer` declarations unchanged from 5.1.9.
- Advanced LocalGPT browser asset cache-busters to 5.2.0.

## Validation boundary

- Source-only validation; no `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online repository access.
- Runtime proof must come from the target Windows/.NET 10 environment; this source package does not claim a local .NET execution.
