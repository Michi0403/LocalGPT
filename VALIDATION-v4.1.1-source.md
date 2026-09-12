# LocalGPT 4.1.1 source validation

This release was reviewed with repository-maintained source/static audits only. No GitHub checkout, `dotnet build`, `dotnet publish`, Apple signing/notarization, or PowerShell execution is claimed. The user's Visual Studio/macOS release build remains authoritative for compilation and runtime packaging.

## Provider-management contracts

- DevExpress runtime/model workbench is embedded in the existing AI-guided setup surface.
- Ollama storage supports reviewed external paths, environment-variable/home expansion, provider-default paths, disk/free-space visibility, unload, and explicit permanent delete through documented HTTP endpoints.
- Ollama runtime options cover bind/port, context, keep-alive, loaded-model/concurrency/queue limits, and local-only cloud mode.
- LM Studio inventory/load lifecycle uses direct `lms` arguments rather than shell interpolation and covers detailed list, loaded list, estimate, load, unload, context, GPU offload, TTL, bind/port, and CORS.
- LM Studio model-directory relocation and permanent downloaded-model deletion are not fabricated; the UI directs those provider-owned operations to LM Studio → My Models.
- Managed server bind choices are loopback or LAN/all-IPv4 only; provider HTTP lifecycle calls remain loopback-scoped.
- Managed port changes synchronize matching LocalGPT provider endpoints.
- Existing provider API-key configuration remains the authentication surface.

## Maintained checks

The maintained validation set for this source revision includes XML documentation coverage/quality, application architecture, async continuation policy, service resilience, cross-platform boundaries, configurable-behavior policy, provider-qualified Council routing, provider stream repetition policy, codegen/DXFunction wiring, X-round wiring, localization coverage, and the 4.1.1 release-specific guard.

The release-specific guard also preserves the 4.1.0 Matrix operator/cancellation contracts plus the earlier macOS entitlement, PDF-render latency, stale-documentation-PDF, Pages strictness, and installer compile-surface protections.
