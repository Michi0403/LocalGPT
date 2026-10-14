# LocalGPT 4.1.2 source validation

This is a source-only validation record. No `dotnet build`, Visual Studio build, PowerShell execution, Apple signing/notarization, or GitHub action is claimed from this environment.

Validated source contracts include:

- active release identity is 4.1.2 and preserves the one-digit minor/patch rule;
- 8–10 GiB documentation hosts select an 8-page browser-PDF chunk, 768 MiB browser JS heap, 1024 MiB Node heap, and DocFX max parallelism 1;
- 64 GiB-class hosts select 100-page chunks while retaining the existing high-memory heap budget;
- explicit low-memory and per-resource environment overrides remain available;
- DocFX build is invoked with `--maxParallelism` rather than relying on unrestricted core-count parallelism;
- front-matter generation does not retain page bodies and normal print chunks no longer retain duplicate full HTML strings;
- low-memory mode deletes temporary chunk HTML promptly and trims managed transient memory between chunks;
- low-memory builds serialize LocalGPT/PublisherStudio heavyweight documentation stages through the same temp-file lock;
- the 4.1.1 provider runtime model JSON helper is materialized rather than implemented with `yield`, so the maintained iterator-exception policy no longer reports the release-blocking `EnumerateModelObjects` violation;
- existing 4.1.1 provider-management, 4.1.0 Matrix operator, 4.0.9 signing/PDF latency, 4.0.8 Pages PDF hygiene, installer, renderer-affinity, service-resilience, cross-platform, and Council/provider guards remain enabled.

The user’s Visual Studio/macOS build remains the authoritative compiler/runtime validation.
