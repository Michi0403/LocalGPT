# LocalGPT 5.1.9 — DXAI scoped alias re-entrancy repair

## Fixed

- Fixed the remaining `.NET 10` dependency-injection failure that prevented the Chat page and other scoped consumers from resolving `IChatClient`: `ArgumentException: An item with the same key has already been added. Key: Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceCacheKey`.
- The failure was not caused by `IChatClientFactory.Build()` itself. That registration was the visible outer resolution boundary. The conflicting cache key came from the maintained DXAIFunction bootstrap contract: `IDxAiFunctionRegistry` was registered as a scoped factory while that factory synchronously resolved all scoped `IDxAiFunctionHandler` instances, and handler dependency graphs are intentionally allowed to re-enter `IDxAiFunctionRegistry`.
- Kept `DxAiFunctionRegistry` as the single scoped owner for the Blazor/request scope, but changed the public `IDxAiFunctionRegistry` factory to a non-owning transient alias over that scoped concrete instance. Re-entrant interface resolution therefore returns the already-created scoped registry without trying to cache the still-running interface factory under the same `ServiceCacheKey` a second time.
- Preserved same-scope synchronous handler initialization through `InitializeHandlers`. No child scope, retained `IServiceProvider`, `Lazy<T>` provider capture, `DispatchProxy`, singleton promotion, or second registry instance was introduced.
- Strengthened the service architecture guard and DXFunction source audit so a scoped re-entrant `IDxAiFunctionRegistry` alias is rejected and the scoped-owner/transient-alias contract is required.
- Retained the 5.1.8 isolated MCP host architecture unchanged. The MCP listener remains optional and cannot own or replace the primary LocalGPT application DI graph.
- Explicit Blazor `@rendermode` / `InteractiveServer` declarations are unchanged from 5.1.8.
- Advanced LocalGPT browser asset cache-busters to 5.1.9.

## Validation

- Source architecture audit: passed.
- Razor maintenance architecture audit: passed for 54 Razor components.
- Async continuation audit: passed.
- DevExpress Blazor control audit: passed.
- Cross-platform boundary audit: passed with 22 checks.
- Code-generation / DXFunction wiring audit: passed with the new re-entrant alias lifetime contract.
- No `dotnet`, MSBuild, NuGet restore/publish, GitHub, or online repository access was used for this source-only repair.
