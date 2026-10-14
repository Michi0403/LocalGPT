# LocalGPT 4.9.7 changelog — MCP architecture/build repair

## Build-policy regressions repaired

- Moved `/install` MCP draft cloning and route normalization out of the Razor component into an injected `LocalGptMcpConfigurationPolicy`, preserving the existing MCP controls while restoring the repository's text-service ownership boundary.
- Replaced the MCP project-ID iterator with a materialized read-only list. The project allow-list behavior is unchanged, but the implementation no longer violates LocalGPT's iterator exception/logging contract.
- Centralized MCP text-content array creation in `LocalGptMcpEndpoint.CreateTextContent`, avoiding constructor-embedded protocol literals that were incorrectly crossing the repository's system-variable initialization guard.
- Kept all 4.9.6 MCP gateway features, approval behavior, data-domain exposure controls, microphone changes, and existing LocalGPT behavior intact.

## MCP gateway architecture retained

- The MCP gateway remains opt-in and hosted by the existing ASP.NET Core/Kestrel application.
- Dedicated MCP listener defaults remain TCP `51142`; `/install` still exposes dedicated/primary endpoint topology, address, port, path, TLS, admission/authentication, Host/Origin allowlists, protocol controls, tool/resource/prompt exposure, project/tool filters, data-domain gates, and request/result limits.
- Existing DXAIFunctions and LocalGPT domain services remain the implementation authority; the MCP layer stays an adapter and does not duplicate project, knowledge, toolchain, artifact, Council, or approval business logic.
- Human Collaboration remains authoritative for confirmation-gated operations and stable operation IDs continue to support approval retries.

## Validation constraints

- Re-ran the repository-owned Python architecture, async-continuation, service-resilience, cross-platform, DevExpress-control, ConfigurationRoot and code-generation wiring audits.
- Reproduced the text-service ownership, iterator exception policy and system-variable initialization guards against the maintained baselines; all three reported 4.9.6 failures are cleared.
- Per the requested workflow, no `dotnet`, MSBuild, NuGet restore/build/publish, GitHub access, installer execution, signing or runtime deployment was performed.
