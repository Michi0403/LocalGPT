# LocalGPT 2.1.23 feature persistence map

LocalGPT does not map every business-object class to EF Core. Request DTOs, generated projections, UI snapshots and runtime predictions are intentionally transient. Durable user or product state is stored through aggregate entities.

| Feature | Durable EF Core entity | Transient details carried by the aggregate |
|---|---|---|
| Chat/Council quick starts | `CouncilPromptStarterConfiguration` | Rendered prompt suggestion objects |
| User languages | `LocalizationCatalogRegistration` | Parsed localization dictionaries and request upload buffers |
| Generated help | `DocumentationBuildRecord` | Current filesystem status projection |
| ESP32/Arduino planning | `EmbeddedFirmwarePlanRecord` | Board, pin, wiring, telemetry and artifact plan JSON |
| GameDirector runtime | `CouncilGameSessionRecord` | Actor predictions, controller proposals and authoritative snapshot JSON |
| Compiler/toolchain setup | existing `ProjectCompilerInstallation` | Discovery candidates and version-probe results before approval |
| Council runtime classes | existing `CouncilRuntimeClassConfiguration` | Calculated factory/runtime descriptors |
| Council team workflows | existing `CouncilTeamConfiguration` | Seed blueprint definitions before persistence |

The 2.1.23 migration, DbContext mappings, services and controllers form the authoritative database boundary for the five newly durable aggregates.
