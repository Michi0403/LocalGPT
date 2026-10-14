# LocalGPT 3.9.0 source validation

This release was prepared under the requested source-only constraint: no .NET build/publish and no PowerShell release build were attempted in this environment. Validation is therefore deterministic static/source validation plus parser/data checks that do not require `dotnet`.

Validation targets:
- all three application/package projects, current documentation metadata, browser asset cache query, and CanIRun user agent report 3.9.0;
- the one-digit minor/patch rule is respected (`3.9.0`, not a two-digit patch/minor segment);
- `docs/reference/ai-provider-installation.md` contains six `localgpt-provider-profile` fenced JSON blocks and every block parses as JSON;
- the new additive `builtin.ai-provider-bootstrap-block-v2` seed exists and captures full fenced bodies, while the previous provider-block regex remains present as a compatibility fallback;
- the current-platform provider loader asks for v2 before v1 and still deduplicates profile keys;
- Ollama provider start routes through the existing `IOllamaProcessService`; Setup exposes Start, Stop, Restart, Refresh, status, and resolved executable state and performs no automatic daemon start;
- Setup exposes a manual first-model ID path through `ResolveModelIdAsync` and `InstallModelAsync`; no default `gpt-oss:20b` value is introduced in the Setup component;
- CanIRun setup lookup uses HTTPS `POST https://canirun.ai/api/recommend`, JSON content, explicit opt-in, hardware name + RAM and optional VRAM, and does not use `/device/<slug>/` HTML scraping;
- total system memory flows through `IHardwarePlatformProbeService`, `IHardwareInventoryService`, configured-host detection, Setup rows, and reviewed persistence;
- legacy confirmed local profiles with no stored RAM receive a read-only RAM value in the Setup snapshot, while explicit local detection can persist only the missing `SystemMemoryBytes` field without overwriting existing confirmed hardware/provenance;
- the Windows/macOS/Linux memory probes remain read-only and use their existing platform-service boundary;
- all 15 established `@rendermode InteractiveServer` boundaries remain present;
- the original eight `AddHostedService<T>` registrations remain present;
- `docs/COUNCIL_KNOWLEDGE_SEED.sql` still passes the executable/idempotent Council seed audit;
- source ZIP contents exclude `bin`, `obj`, Python caches, pytest caches, and compiled Python bytecode.

## Executed source checks

The following checks were executed against the final 3.9.0 source tree in this environment before packaging:

- `build/audit_release_3_9_0.py`: passed after repository cache cleanup.
- `build/audit_application_architecture.py --root . --product localgpt --mode all`: passed.
- `build/audit_cross_platform_boundaries.py`: passed with 22 checks and no detected platform leaks.
- `build/audit_async_continuations.py --source-root src/LocalGPT`: passed for 259 source files, 3,019 await tokens, 2,663 `ConfigureAwait(false)` uses, 135 renderer-affine `ConfigureAwait(true)` uses, 216 explicitly configured async disposals, and 5 configured async streams.
- `build/audit_codegen_dxfunction_wiring.py`: passed.
- `build/audit_provider_qualified_council.py --root .`: passed with 282 checks.
- `build/audit_provider_stream_repetition_policy.py`: passed.
- `build/audit_xround_wiring.py`: passed.
- `build/audit_council_sql_seed.py`: passed with 60 executable current-schema `INSERT OR IGNORE` rows and verified second-run idempotency.
- `build/Assert-XmlDocumentationCoverage.py .`: passed for 10,328 direct C# declarations across 656 maintained source files and 784 direct Razor `@code` members across 45 component types.
- direct source count: 15 `@rendermode InteractiveServer` component boundaries remain present.
- direct registration count: 8 `AddHostedService<T>` registrations remain present.

No `dotnet` build/publish, PowerShell release build, or GitHub access was used. Passing these checks is source/static validation, not a claim that the repository was compiled in this environment.
