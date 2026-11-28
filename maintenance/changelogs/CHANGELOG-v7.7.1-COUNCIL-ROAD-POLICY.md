# LocalGPT 7.7.1 — Council hardware-road runtime-policy consistency

Base: complete user-provided LocalGPT 7.7.0 source ZIP. PublisherStudio 4.9.1 is unchanged.

## Fixed

- **New provider-qualified Council models**: `MultiModelCouncilService.QualifyModelRoutes` no longer invents 256/4096 output or 2048/32768 context ranges. It constructs missing routes through `ICouncilHardwareRoadConfigurationService` using the current run budget and persisted Council minimum/maximum policy values.
- **Missing planner routes**: `CouncilHardwareRoadPlanner` uses the same factory rather than an independent compiled fallback. Provider matching continues to prefer exact qualified model identities; manually configured routes, load overrides and measured hardware ranges remain authoritative.
- **Chat model selection**: automatic hardware roads now inherit the preparation session's selected output/context budgets. Changing a Council ceiling selectively refreshes roads that still exactly match the generated automatic template, while leaving custom hardware, capability, concurrency, per-road budget and benchmark settings untouched.
- **Preset model expansion**: new missing routes use the selected preset's own run ceilings; preserved per-model saved ranges are not rewritten. Newly seeded built-in presets use their authored ranges rather than the unintended 4096-output/16k-or-32k secondary clamp. Unmodified previously shipped bounded presets are migrated; user-edited or archived presets are preserved.
- **Database default preservation**: removed startup-seeding rewrite that treated a saved Council 262144 budget as an obsolete seed and silently replaced it with the default.
- **Hardware-road editor and Chat budgets**: UI token maximums and server normalization share the persisted Council policy limits; shared number editor uses overflow-safe step arithmetic for large ranges. Standalone Model Council, provider benchmarking, and provider runtime management token editors use their existing respective catalog ceilings instead of independent 262144/1048576/8192 UI maxima.
- **Other Council execution entrypoints**: OneWire screenreader Council requests use persisted Council defaults rather than a local 1200-output/16384-context ceiling. Model benchmarking fallback minima use the configured Council minimums without replacing measured recommendations. The Configuration Steward's automatic-road health finding reports the live configuration service's effective values, not stale reference numbers.
- **Ordinary native Ollama chat**: missing `ChatOptions.MaxOutputTokens` uses the seeded/editable `DefaultOllamaRequestOutputTokens` setting; existing 2048 behavior remains the initial default.
- **Diagnostics**: removed a fixed workstation hardware claim from the diagnostics response. The intentional diagnostic test matrix is unchanged.

## Source regression guard

Added `build/Assert-CouncilRoadRuntimePolicy.ps1`, wired into the existing mandatory PowerShell-only architecture check in `Directory.Build.targets`. It prevents reintroduction of the specific hardcoded Council fallbacks, UI ceilings, seed rewrite and missing run-budget propagation.

## Deliberate boundaries

- A **manually configured or benchmark-measured** route remains an explicit per-model ceiling even when the global Council budget is larger. Raising the global setting does not erase independently saved hardware limits.
- The **provider-advertised model context maximum** remains a hard physical/model constraint for the Ollama request budget policy. A model advertising 16384 cannot safely accept a 24066-token request merely because the Council global limit is 262144; the existing workload-recovery path must handle that case.
- Seed defaults and authored use-case presets are policy/template data, not provider-detected performance measurements.
- No new transport fields, services with duplicate ownership, GitHub dependencies or PublisherStudio changes.

## Validation status

This is an **uncompiled source candidate**. No `dotnet`, PowerShell execution or real Ollama/Blazor runtime validation was available in the editing environment. See `VALIDATION-v7.7.1-COUNCIL-ROADS.md` for source checks and required host acceptance tests.
