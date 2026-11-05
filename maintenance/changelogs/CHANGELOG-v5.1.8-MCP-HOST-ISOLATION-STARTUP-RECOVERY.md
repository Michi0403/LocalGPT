# LocalGPT 5.1.8 — MCP host isolation and startup recovery

## Fixed

- Removed the dedicated MCP socket from the primary LocalGPT Kestrel endpoint collection. A dedicated MCP bind/TLS/address failure can no longer fail startup of the Blazor, installer, API or normal remote-web host.
- Added `LocalGptMcpListenerHostedService`, an optional `BackgroundService` that owns a small isolated Kestrel `WebApplication` for the configured MCP-only address, port and TLS certificate.
- Kept LocalGPT as the single authoritative application service graph. Each isolated MCP request creates a scope from the parent LocalGPT `IServiceScopeFactory` and resolves the existing `LocalGptMcpEndpoint`; no duplicate business-service container is created.
- Replaced the previous race-prone MCP port bind preflight with failure containment around the isolated listener itself. A real bind failure is logged without terminating LocalGPT.
- Loopback-only MCP configurations may fall back to the already existing primary `/mcp` route for the current run when the dedicated listener cannot start. Remote-capable MCP configurations do not silently weaken exposure policy; MCP is disabled for that run instead.
- Invalid MCP-only configuration or a dedicated MCP port collision now disables MCP for the current run rather than terminating the main LocalGPT host.
- Removed the obsolete private `CreateProxy` implementation from `ServiceMethodDiagnosticsRegistration`. The active diagnostics pass remains descriptor-preserving, so 5.1.8 cannot accidentally rebuild the stale DispatchProxy/`ActivatorUtilities` path implicated by the supplied runtime trace.
- Advanced LocalGPT browser asset cache-busters to 5.1.8.
- Explicit Blazor `@rendermode` / `InteractiveServer` declarations are unchanged from 5.1.7.

## Validation

- Source architecture, Razor maintenance, strict async-continuation, service-resilience, DevExpress control, cross-platform boundary and DXFunction wiring audits pass.
- No `dotnet`, MSBuild, NuGet restore/publish, GitHub or online repository access was used for this source-only handoff.
