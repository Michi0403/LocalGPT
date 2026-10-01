# LocalGPT 5.1.8 source validation

## Validation boundary

This handoff was validated at source level only. No `dotnet`, MSBuild, NuGet restore/publish, installer execution, GitHub, or online repository access was used.

## Runtime evidence addressed

The supplied runtime history contains host-start failures in the listener/bootstrap path and later 5.1.7 request failures involving duplicate `Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceCacheKey` entries. The MCP route can appear in request stack traces because it is middleware on the main host, while the duplicate-key trace identifies the obsolete diagnostics proxy path as the service-resolution boundary.

5.1.8 therefore separates the two failure domains instead of rewriting application services: the dedicated MCP transport is isolated from the main Kestrel host, while method-diagnostics registration remains descriptor-preserving and its obsolete `CreateProxy` implementation is removed from source.

## Source checks run

- Application architecture audit: passed.
- Razor maintenance architecture audit: passed for 54 Razor components.
- Async continuation audit: passed after the MCP host isolation change.
- Service resilience audit: passed after the MCP host isolation change.
- DevExpress Blazor control audit: passed; only the two circuit-independent reconnect controls remain native.
- Cross-platform boundary audit: passed with 22 checks.
- Code-generation / DXFunction wiring audit: passed.
- Explicit `@rendermode` / `InteractiveServer` declaration locations and text match 5.1.7 exactly.
- The primary LocalGPT Kestrel configuration no longer attaches a dedicated MCP listener.
- The isolated MCP host resolves `LocalGptMcpEndpoint` from a scope created by the parent LocalGPT `IServiceScopeFactory`.
- No active `ServiceMethodDiagnosticsRegistration.CreateProxy` / DispatchProxy construction path remains.

## Result

The source-level contracts required for the 5.1.8 MCP startup containment are satisfied. A real .NET runtime test on the target Windows installation remains required to prove listener binding, TLS behavior, wrapper reuse and end-to-end MCP traffic.
