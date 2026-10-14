# LocalGPT 2.7.9 source validation

This package was reviewed as source only. No GitHub/network repository access was used. No `dotnet`, MSBuild, restore, compile, test, publish, PowerShell build, or DocFX command was executed. The owner Windows build remains authoritative for compiler/runtime validation.

## Hardware performance preset feature

- Durable `HardwarePerformancePreset` business object, DbSet/model mapping, migration, snapshot, service interface/implementation, API controller, Chat Hardware spooler selector, and benchmark persistence paths are present.
- Five profile DXFunctions (list/get/save/delete/apply) plus the provider-qualified benchmark function are registered through the maintained DI-backed handler discovery/catalog synchronization path.
- Adaptive Model Benchmark Council seed revision 20 names the new profile functions as preferred capabilities and treats the provider-qualified benchmark as the maintained execution path.
- Profile application matches exact provider-qualified model selection keys and leaves Council membership unchanged.
- Profile application carries session token ceilings large enough for every stored route's maximum output/context range.

## Static application audits

- Provider-qualified Council feature audit: **280 checks passed**.
- Council X-Round/heartbeat/live-result source audit: **passed**.
- Architecture/static policy audit: **passed**.
- Async continuation audit: **158 source files; 2,336 await tokens; 2,126 `ConfigureAwait(false)`; 30 renderer-affine `ConfigureAwait(true)`; 2 preconfigured awaitables; 175 reviewed await-using disposals; 3 configured async streams**.
- Service resilience audit: **1,841 service methods passed**; 30 yield methods and 3 direct Program/Startup methods remain intentionally excluded by policy.
- Chat ASCII-console audit: **17 checks passed**.
- Code-generation/DXFunction audit: **passed**.
- Documentation/1-Wire contract audit: **passed**.
- Kawaii documentation layout audit: **passed**.
- XML documentation coverage/quality: **7,544 direct maintained C# declarations across 414 source files**.
- Documentation enhancer second pass: **0 missing blocks added, 0 existing blocks enriched**.
- Localization key parity: **1,862 en-US / 1,862 de-DE keys; 0 missing on either side**.
- InteractiveServer topology remains **19 explicit islands/pages**; the existing inherited nested components are unchanged.

## Version contract

- LocalGPT application/WebView wrapper/installer: **2.7.9**.
- Wire protocol: **2.1.1**.
