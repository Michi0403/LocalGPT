# LocalGPT 5.3.6 — compile repair and initial data feed unification

LocalGPT 5.3.6 is a corrective follow-up to 5.3.5. It fixes the compiler errors reported from the 5.3.5 runtime-parameter ownership pass and continues the same ownership direction by centralizing repository-backed initial Markdown/SQL data ingestion behind one initial-data feed service.

## Compiler repairs

- `LocalGptMcpConfigurationPolicy.NormalizePath` now uses the correct string overload when evaluating the database-backed MCP root path.
- `AdaptiveOllamaBenchmarkWiring.BindOptions` no longer shadows its `JsonElement parameters` argument with a runtime-policy local. The database-backed benchmark parameter set is loaded once as `runtimeParameters` and reused for bounds and model-count truncation.
- `CreatureCouncilGameSubdirector` and `CouncilGameActorRuntimeFactory` now receive `ILocalGptRuntimePolicyDataService` explicitly and resolve `CouncilGameRuntimeParameters` from the database-backed runtime-policy owner before applying creature-director bounds.

## Initial data feed ownership

- Added `IInitialDataFeedService` / `InitialDataFeedService` as the single source-reading boundary for configured initial repository knowledge.
- `InitialDataCatalog.LoadKnowledgeAsync` no longer owns a second hardcoded Markdown file list or its own repository/embedded file reader. It consumes the centralized feed and projects the returned source-backed entries into `CouncilKnowledgeEntry` records.
- The feed is driven by the existing `LocalGptRuntimeCollection.KnowledgeFiles` seed collection, so the already-maintained `.md` and `docs/COUNCIL_KNOWLEDGE_SEED.sql` sources use the same catalogued path. Existing source files remain in the repository; their ingestion mechanism is unified.
- Approved embedded Markdown fallbacks are also read through the same service rather than through a private parallel path in `InitialDataCatalog`.
- SQL seed/reference text is treated as source-backed knowledge content, not executed as arbitrary bootstrap SQL by the feed. Database schema/data migrations remain owned by EF migrations and database initialization.

## Compatibility

The 5.3.5 runtime-parameter BusinessObjects, migration seeds, workspace progression repair, database/runtime policy architecture, user-customizable values, and existing knowledge records are preserved.

No .NET build, restore, publish, or GitHub access was used while preparing this source release.
