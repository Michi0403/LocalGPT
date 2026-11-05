# LocalGPT 5.1.7 — DI diagnostics runtime recovery

## Fixed

- Removed the runtime DI-graph mutation performed by `ServiceMethodDiagnosticsRegistration.Apply`. The previous DispatchProxy replacement of scoped/transient interface registrations could recursively re-enter `ActivatorUtilities` while .NET 10 was materializing the same scoped call-site graph, producing `ArgumentException: An item with the same key has already been added. Key: Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceCacheKey`.
- The diagnostics registration pass now reviews eligible registrations but leaves the original `ServiceDescriptor` instances untouched. Existing method-local service diagnostics remain authoritative, preserving service lifetime, identity, factory ownership and disposal semantics.
- This specifically restores routed Blazor component injection and menu/page rendering that were failing after the host had already started successfully.
- Retained the 5.1.6 loopback/MCP recovery and frontend repairs. No business-service implementation or database model was rewritten for this correction.
- Advanced LocalGPT browser asset cache-busters to 5.1.7.

## Validation

- Source architecture, Razor maintenance, strict async-continuation, service-resilience, DevExpress control, cross-platform boundary and DXFunction wiring audits pass.
- No `dotnet`, MSBuild, NuGet restore/publish, GitHub or online repository access was used for this source-only handoff.
