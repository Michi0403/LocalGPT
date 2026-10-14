# LocalGPT 2.8.6 source-only validation

Validation is intentionally source-only: no GitHub access, `dotnet`, MSBuild, or .NET compiler was used.

Passed source audits:

- LocalGPT 2.8.6 reasoning/function trace and Council durability release audit.
- LocalGPT 2.8.5 localization regression audit.
- Strict async continuation audit: 158 source files, 2,349 await tokens, 2,140 `ConfigureAwait(false)`, 31 renderer-affine `ConfigureAwait(true)`, 175 configured async disposals, and 3 configured async streams.
- Provider-qualified Council audit: 280 checks.
- Human-visible entity formatting, benchmark/rejoin/build-guard, X-Round/heartbeat, and code-generation/DXFunction audits.
- Application architecture and service-resilience audits; 1,850 service methods own try/catch plus diagnostics.
- XML documentation coverage: 7,111 direct C# declarations across 406 maintained source files.
- Documentation/1-Wire contract audit.

Reviewed release invariants:

- application/wrapper/installer versions are 2.8.6 while the 1-Wire protocol stays 2.1.1;
- native Ollama requests explicitly request provider-supplied `think` output, with conservative compatibility fallback;
- Ollama and generic-provider reasoning/tool metadata becomes normal transcript content rather than transient-only status;
- function-call arguments and bounded function results are expanded by default and persist with `/chat` history;
- every provider-supplied thinking block is retained, completed thought panels remain expanded in live chat and restored sessions, and only an unfinished block carries the live indicator;
- Council participant traces include reasoning/function metadata across teams and rounds;
- PublisherStudio/1-Wire Council runs register in the LocalGPT live-session path and force normal session persistence;
- Council markdown logging creates an early checkpoint and atomically refreshes it independently of UI/transport cancellation, including failed/partial runs;
- all 19 existing `@rendermode` directives exactly match the supplied 2.8.5 source baseline.

Provider limitation: LocalGPT can display reasoning only when the configured provider/model actually returns reasoning/thinking metadata. This release does not fabricate or claim access to provider-internal hidden reasoning.

The user's Windows .NET 10 build remains authoritative for compile/runtime confirmation.
