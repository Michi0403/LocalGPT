# LocalGPT 2.6.8 source validation

This package is source-only and was **not compiled** in the packaging environment.

## Static checks performed

- No `dotnet`, MSBuild, restore, build, test, publish, or DocFX build was executed.
- Provider-qualified Council Python audit passed: **210 checks**.
- EN/DE localization catalogs parsed as JSON with duplicate-preserving top-level inspection: **1856 keys each**.
- EN/DE localization key sets compared for equality.
- Case-insensitive localization duplicate scan performed: **0 duplicates** in both catalogs.
- Project XML files parsed with Python XML tooling: **6 files, 0 parse failures**.
- Version fields checked in LocalGPT, installer console, and WebView wrapper projects.
- Application architecture audit passed.
- Async continuation audit passed: **2250 await tokens**, **2044 `ConfigureAwait(false)`**, **30 renderer-affine `ConfigureAwait(true)`**.
- Service resilience audit passed: **1754 service methods** with owned error boundaries/diagnostics.
- Chat ASCII-console audit passed: **17 checks**.
- Documentation/1-Wire contract audit passed.
- Kawaii documentation layout audit passed.
- Source scans verified:
  - new `AllMembersSequentialOnEachAIHostParallel` support in configuration, UI, runtime normalization, and runtime execution;
  - strict `AllMembersSequential` remains supported;
  - new custom workflow steps use the host-parallel sequential default;
  - untouched source-controlled sequential workflow steps use the new strategy;
  - AI-host queue scheduling uses one deterministic queue per host;
  - the per-host strategy still uses the existing phase completion barrier;
  - runtime lane keys include the AI host so disabling extra hardware-road parallelism does not collapse separate PCs into one global lane;
  - participant streaming still uses isolated channels and intact-member presentation.

## Semantics checked in source

`AllMembersSequentialOnEachAIHostParallel`:
1. groups provider-qualified members by canonical AI host;
2. starts one asynchronous worker per AI host;
3. executes each worker's members sequentially;
4. lets host workers run concurrently;
5. waits for all host workers before adding the phase results and advancing;
6. uses one request at a time per host regardless of the configured multi-model lane count.

`AllMembersParallel`:
- remains bounded by the configured per-host model limit.

`AllMembersSequential`:
- remains a strict global member-by-member chain.

## Build authority

A Windows/.NET build remains required to validate compilation, generated documentation, and runtime behavior.
