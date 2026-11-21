# LocalGPT 7.7.1 — Council token and hardware-route default audit

This audit distinguishes **implicit runtime restrictions** from **persisted policy seeds, saved roads, transport defaults and explicitly chosen test/preset budgets**. It is not a claim of complete compiled-code or host execution coverage.

| Source/workflow | 7.7.0 finding | 7.7.1 disposition |
| --- | --- | --- |
| `MultiModelCouncilService.ParticipantSelection.cs` | Missing provider-qualified route assigned 256–4096 output and 2048–32768 context regardless of current Council run | Removed implicit fallback; creates route from run and `ICouncilHardwareRoadConfigurationService` |
| `CouncilHardwareRoadPlanner.cs` | Missing road constructed with independent 256/2048 minimums | Uses shared configuration factory and caller run budget |
| `CouncilHardwareRoadConfigurationService.cs` | Sync fallback used static minimums; normalization independently clipped at 262144 output / 1048576 context | One route factory and configured maximums; saved road ranges retained |
| Chat preparation model selection | Synchronized missing auto roads with policy default, ignoring user-selected run budget | Selected run budgets supplied to synchronization; exact unchanged automatic template can refresh when the user changes the ceiling |
| Built-in preset seed | Unconditionally clipped newly generated templates to 4096 output and 16384/32768 context | Preserves authored templates; evolves only unchanged shipped bounded presets |
| `DatabaseInitializationService.SeedCore.cs` | Existing Council token row `262144` overwritten on seed | Removed destructive inference; keep operator's stored value |
| OneWire screenreader Council caller | 1200 output and 16384 context regardless of runtime policy | Uses persisted Council defaults |
| Benchmark fallback route minima | Hardcoded 256 output / 2048 context even when policy differs | Policy-backed minimums; successful benchmark measurements remain authoritative |
| Configuration Steward Council-load health check | Displayed stale independent 32k/4k reference road | Reads actual default route and interpolation from configuration service |
| Native Ollama ChatOptions fallback | Hardcoded 2048 output without runtime policy key | Uses seeded configurable `DefaultOllamaRequestOutputTokens` (2048 initial value) |
| Chat, standalone Council, provider benchmark and runtime management editor maxima | 262144/1048576/8192 widget-only ceilings | Maxima delegated to catalog/runtime-policy limits; wide-range number arithmetic made safe |
| Diagnostic workstation string | Assumed specific local GPU/CPU across distributed provider hosts | Replaced with host-specific discovery/benchmark wording |

## Hardcoded token numbers deliberately retained (not a newly generated runtime hardware route)

- `LocalGptRuntimePolicySeedDataService`: 256/4096/2048/32768 are *initial persisted Council policy defaults*. They are editable; source consumers now read policy rather than duplicating them in execution paths.
- `LocalGPT.WireProtocolVersion/OneWireProtocolContracts.cs`, request/preset BusinessObjects: legacy serializer/transport initializer values remain for compatibility with existing 1-Wire data and PublisherStudio, not as the source of newly generated automatic route limits. Removing these requires a coordinated protocol-default migration and should not be disguised as a safe one-project patch.
- Built-in Council team model-specific authored presets, benchmarking fixtures and explicitly selected low/balanced/high profiles: intentional use-case inputs, **not** Ollama capability discovery. Applying a saved preset can still impose a lower per-model limit than a new run's global maximum.
- `CouncilTeams` final-answer recovery stage's 8192 baseline/32768 editing range is a separate, explicit workflow-step setting, not the normal participant context allocator; runtime final-recovery policy can impose its own ceiling.
- Provider/model context advertised by Ollama remains authoritative. A request requiring 24066 tokens cannot fit a model whose provider advertises 16384, even when its run-level target is 262144.

## Required checks on a host with toolchains

`pwsh ./build/Assert-CouncilRoadRuntimePolicy.ps1`, repository validation, compiler tests and real Ollama/Blazor run observations. No runtime tests were executed in this source-only editing environment.
