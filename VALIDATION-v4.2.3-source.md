# LocalGPT 4.2.3 source validation

Validation is source/static only in this handoff environment. No `dotnet` build, publish, signing, notarization or GitHub operation was performed.

Required checks include the 4.2.3 release audit, application architecture, service resilience, provider-qualified Council policy, async continuation policy, cross-platform boundaries, ASCII-console policy, configuration policy, DXFunction/X-Round wiring, PowerShell interpolation and XML/Razor documentation coverage. The delivered ZIP must also pass archive integrity and a fresh-extraction rerun of the maintained release gates.

4.2.3-specific assertions cover protocol-independent host+port Council runtime identity, preservation of 4.2.2 Ollama `/v1` canonicalization, bounded Ollama `/api/tags` health/recovery, non-bypassed TLS catalog fallback, coalesced console rendering, and demand-driven/focus-aware ASCII gamepad polling with preserved controller mappings and lifecycle cleanup.
