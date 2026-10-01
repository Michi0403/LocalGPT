# LocalGPT 5.2.0 source validation

## Validation boundary

This handoff was validated at source level only. No `dotnet`, MSBuild, NuGet restore/publish, installer execution, GitHub, or online repository access was used.

## Runtime evidence addressed

The supplied 5.1.9 runtime output establishes a narrower failure boundary than the earlier 5.1.8 trace:

- The isolated MCP host starts successfully.
- The primary LocalGPT Kestrel listener starts on `http://127.0.0.1:5000`.
- Blazor negotiation succeeds, the server circuit opens, and the interactive Index page initializes.
- A subsequent `GET /chat?toggledSidebar=False` enters the `/chat` endpoint and renders the application shell/router state, but the trace ends without `LocalGPT.Components.Pages.Chat` initialization and without `Request finished` for that navigation.

`Chat.razor` directly injects `IDxAiFunctionRegistry`. In 5.1.9 that resolution used a transient alias which immediately initialized all `IDxAiFunctionHandler` services. It avoided the earlier duplicate scoped-cache insertion, but it still placed complete DXFunction graph construction on the Chat component activation path.

5.2.0 replaces that alias with a normal scoped `IDxAiFunctionRegistry -> DxAiFunctionRegistry` registration. The registry exposes a lazy handler map. `DxAiFunctionHandlerResolver`, itself scoped, owns the deferred `GetServices<IDxAiFunctionHandler>()` call and therefore resolves the handler collection from the same request/Blazor-circuit scope only when the function directory is first needed. The resolver does not create a child scope and the registry does not retain `IServiceProvider`.

## Source checks

The following source-only checks were run after the 5.2.0 repair:

- Application architecture audit: passed.
- Razor maintenance architecture audit: passed for 54 Razor components.
- Async continuation audit: passed for 332 source files.
- DevExpress Blazor control audit: passed; only the two circuit-independent reconnect controls remain native.
- Cross-platform boundary audit: passed with 22 checks.
- Code-generation / DXFunction wiring audit: passed with the normal scoped registry plus lazy same-scope resolver contract.
- Service resilience audit: passed for 2,852 service methods; 29 iterator methods and 3 direct Program/Startup methods remain intentional exclusions.
- Custom DI/render/MCP/version check: passed; all 20 explicit Blazor render-mode directives match 5.1.9 exactly and the `src/LocalGPT/Mcp` source is byte-for-byte unchanged.
- Version is `5.2.0`; no `5.1.10` version is introduced.
- `IDxAiFunctionRegistry` has one normal scoped registration and no transient/scoped factory alias.
- `DxAiFunctionRegistry` does not contain `InitializeHandlers`, re-entrant initialization flags, or an injected `IServiceProvider`.
- The handler map is lazy and built through the scoped `DxAiFunctionHandlerResolver`.
- The resolver contains no `CreateScope` / `CreateAsyncScope`; deferred handler resolution stays inside the active DI scope.
- PublisherStudio is not modified.

PowerShell itself is not installed in the handoff environment, so PowerShell guard scripts were updated but not executed here. Their Python-backed counterparts and the directly affected source contracts were validated as listed above.

## Result

The source architecture no longer performs eager 202-handler DXFunction graph construction merely to activate a component or service that needs `IDxAiFunctionRegistry`. A real target-machine run remains the authority for confirming that `/chat` now completes and interactive controls remain responsive.
