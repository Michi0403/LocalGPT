# LocalGPT 4.1.3 source validation

This is a source-only validation record. No `dotnet build`, Visual Studio build, PowerShell execution, Apple signing/notarization, or GitHub action is claimed from this environment.

Validated source contracts include:

- active release identity is 4.1.3 and preserves the one-digit minor/patch rule;
- static emulation of `Assert-SystemVariableInitialization.ps1` reports zero new constructor-initialization or direct variable-store-key findings;
- `ProviderRuntimeManagementService` owns the Ollama generate/delete API paths as named immutable protocol constants, and `HttpRequestMessage` construction no longer embeds those route string literals;
- no system-variable baseline exception was added and the guard script remains unchanged;
- Ollama unload and permanent-delete semantics remain unchanged;
- the 4.1.2 adaptive low-memory documentation policy remains present, including 8-page constrained-host chunks, 100-page 64 GiB-class chunks, heap limits, DocFX parallelism limits, heavyweight-stage serialization, and transient-memory cleanup;
- the 4.1.1 provider-management and 4.1.0 Matrix operator contracts remain guarded;
- renderer-affinity, service-resilience, cross-platform, Council/provider, signing/PDF, Pages PDF hygiene, and installer guards remain enabled.

The user’s Visual Studio/macOS `pwsh Build-Release.ps1` remains the authoritative compiler/runtime validation.
