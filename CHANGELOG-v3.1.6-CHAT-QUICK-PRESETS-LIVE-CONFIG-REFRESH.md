# LocalGPT 3.1.6 — Chat quick presets and live configuration refresh

LocalGPT 3.1.6 is a forward-only Chat usability and state-freshness release built on 3.1.5. It preserves the provider-stream repetition watchdog, Council recovery/cancellation behavior, benchmark evidence and coverage truth guards, XML documentation completeness, database compatibility, provider-qualified identity rules and 1-Wire protocol behavior.

## Prompt-line quick selectors

The `/chat` composer now exposes three compact DevExpress selectors immediately to the left of the existing attachment/send/stop actions:

- **Team** — enabled Council teams read through `ICouncilTeamConfigurationService`.
- **Models** — Council model presets read through `IModelPresetService`.
- **Performance** — hardware performance presets read through `IHardwarePerformancePresetService`.

The quick selectors intentionally reuse the same selection/application handlers as the detailed Chat configuration surface. They do not introduce duplicate configuration state or a second preset implementation.

The selectors are rendered as normal Blazor-owned markup and are visually anchored over the prompt action line with scoped CSS. No JavaScript DOM transplant is used, avoiding the renderer churn/flicker risk that occurs when Blazor-owned elements are moved into DevExpress' internal DOM.

Detailed model membership, hardware roads, token limits, workflow details, save/archive/delete actions and other advanced controls remain in **Chat configuration**.

## Chat configuration data freshness

Opening the Chat configuration ribbon now starts an independent service-backed refresh pass for the non-provider datasets instead of relying on page-initialized copies. The refresh includes:

- Council team definitions;
- Council model presets;
- hardware performance presets;
- persistent Council prompt starters;
- saved chat/project choices and selected project details;
- saved conversations, recent thoughts and feedback metadata.

Provider/Ollama discovery keeps its existing refresh path and runs independently. A slow provider discovery therefore no longer prevents the rest of the modal data from refreshing.

The refresh pass preserves current manual runtime values and current selections whenever the corresponding stored row still exists. Deleted/archived rows disappear on the next configuration open instead of requiring `/chat` to be reopened. An interlocked gate prevents overlapping refresh passes if the ribbon is opened repeatedly.

## Compatibility

- LocalGPT / installer console / webview wrapper version: **3.1.6**.
- .NET SDK policy remains **10.0.400** / `net10.0`.
- Existing DevExpress **25.2.*** package lane is unchanged.
- 1-Wire protocol remains **2.1.1**.
- BenchmarkEvidence schema remains **1**.
- No EF migration was added or modified.
- `DatabaseMigrationCompatibilityService.cs` is unchanged.

## Validation boundary

This archive is source-only. The preparation environment does not contain the .NET/DevExpress build toolchain, so no successful compilation or runtime execution is claimed. Applicable static audits and package re-validation are recorded in `VALIDATION-v3.1.6-source.md` and the external static validation log.
