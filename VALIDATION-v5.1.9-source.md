# LocalGPT 5.1.9 source validation

## Validation boundary

This handoff was validated at source level only. No `dotnet`, MSBuild, NuGet restore/publish, installer execution, GitHub, or online repository access was used.

## Runtime evidence addressed

The supplied 5.1.8 runtime output shows that the primary web host and isolated MCP listener both start, but scoped service resolution later fails with `ArgumentException: An item with the same key has already been added. Key: Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceCacheKey`. The Chat page trace reaches `Program.ServiceRegistration.cs` at the scoped `IChatClient` factory while resolving `IChatClientFactory`; background hosted services fail through the same scoped service graph.

The maintained DXAIFunction registration had one scoped concrete registry plus a second scoped interface factory. That interface factory resolves the concrete registry and then synchronously resolves all scoped handlers. Several handler dependency graphs re-enter `IDxAiFunctionRegistry` by design. Because the outer scoped interface factory had not returned yet, the nested resolution attempted to cache the same interface service key again.

5.1.9 keeps the concrete `DxAiFunctionRegistry` scoped and changes only the public interface alias to transient. Every alias resolution still returns the same scoped concrete object. Nested handler resolution can therefore re-enter the alias without creating a second scoped-cache insertion, while `handlerInitializationInProgress` prevents recursive handler-map construction.

## Source checks run

- Application architecture audit: passed.
- Razor maintenance architecture audit: passed for 54 Razor components.
- Async continuation audit: passed for 332 source files.
- DevExpress Blazor control audit: passed; only the two circuit-independent reconnect controls remain native.
- Cross-platform boundary audit: passed with 22 checks.
- Code-generation / DXFunction wiring audit: passed and now rejects the former scoped re-entrant interface alias.
- `DxAiFunctionRegistry` remains scoped and `IDxAiFunctionRegistry` is a transient non-owning alias returning the scoped registry.
- Same-scope synchronous `InitializeHandlers(() => provider.GetServices<IDxAiFunctionHandler>())` wiring is retained.
- No provider-backed lazy registry, child DI scope, `DispatchProxy`, or second registry implementation was introduced.
- Explicit `@rendermode` / `InteractiveServer` declarations match 5.1.8 exactly.
- The 5.1.8 isolated MCP listener implementation and registration remain unchanged.

## Result

The source-level DI contract now matches the intended re-entrant registry architecture without caching the re-entered alias as a scoped service. A real .NET 10 run on the target Windows machine remains the runtime authority for proving the duplicate `ServiceCacheKey` exception is gone end-to-end.
