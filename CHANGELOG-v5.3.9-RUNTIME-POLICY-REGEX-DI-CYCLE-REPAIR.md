# LocalGPT 5.3.9 — runtime-policy/regex DI-cycle repair

LocalGPT 5.3.9 repairs the startup regression reported after the database-backed runtime-parameter expansion. The source compiled in 5.3.8, but application startup failed during dependency-injection validation because the runtime-policy service and policy-aware regex compiler directly depended on each other.

## Fixed

- Removed `IRegexCompilationService` from `LocalGptRuntimePolicyDataService` bootstrap ownership.
- Added `IRegexEngineService` / `RegexEngineService` as the policy-independent framework-regex construction boundary. It receives `RegexRuntimeParameters` explicitly and therefore owns no runtime-policy lookup or service-locator behavior.
- `LocalGptRuntimePolicyDataService` now reads `RegexRuntimeParametersJson` from the same seed/persisted runtime-policy definition it is currently building, then passes those explicit database-backed bounds to the regex engine. This keeps regex size/timeout configuration database-owned without a circular dependency.
- `RegexCompilationService` remains the application-facing policy-aware regex service. It reads the active database-backed `RegexRuntimeParameters` and delegates construction to the engine. Existing callers do not need a new configuration path.
- Runtime-policy JSON deserialization now reuses the existing `IJsonTextService` instead of maintaining a second serializer configuration inside `LocalGptRuntimePolicyDataService`.
- The maintained application architecture audit now rejects a future `LocalGptRuntimePolicyDataService -> IRegexCompilationService` bootstrap edge and rejects runtime-policy/policy-aware dependencies inside `RegexEngineService`.
- The configurable runtime-text/regex ownership audit remains strict and passes without exempting `RegexEngineService`; hardcoded regex patterns outside the existing database-backed owners remain rejected.

## Reported startup failure

The supplied startup log contained 104 failing service descriptors. Every circular-dependency trace terminated in the same underlying loop between `IRegexCompilationService` and `ILocalGptRuntimePolicyDataService`; the many affected DXFunctions and services were downstream validation failures rather than independent dependency defects.

## Validation boundary

Source-only validation was performed. `dotnet`, MSBuild, NuGet restore, publish and GitHub were not invoked. Application architecture (including the new bootstrap-cycle guard), configurable prompt/regex ownership, service resilience, async continuation/async-boundary architecture, provider-qualified Council and CodeGeneration/DXFunction wiring audits pass after the repair.
