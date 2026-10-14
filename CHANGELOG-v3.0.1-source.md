# LocalGPT 3.0.1 source changelog

## Live Council rejoin and renderer synchronization

- Reworked live Council browser rejoin so an active run no longer copies the complete Council transcript and all participant buffers into the DevExpress chat message collection.
- Added the lightweight `CouncilLiveSessionAttachmentSnapshot`. While a Council is running, Chat keeps a stable `localgpt-live-council` marker and reads transcript/participant lanes directly from the server-owned live-session service.
- The complete transcript is materialized into the persistent conversation only after the run has completed, preserving restart durability without making reconnect/rejoin a multi-megabyte `LoadMessages` operation.
- `AttachToLiveCouncilSessionAsync` now returns an explicit success value, handles disconnected/cancelled browser circuits as retryable attach failures, preserves the composer draft, and performs component/message mutation through `InvokeAsync`.
- Renderer-affine continuations remain explicitly scoped. The async gate still reports exactly 29 intentional `ConfigureAwait(true)` continuations; service/orchestration work remains on `ConfigureAwait(false)` by default.

## Namespace and generated-documentation ownership

- Corrected `MinecraftDiagnosticController` from the stale `LocalGPT.Endpoints` namespace to `LocalGPT.Controller`.
- Corrected the notification service from the legacy `TacosPortal.Services` namespace to `LocalGPT.Services`.
- Moved `ChatHub` from the Services folder to `Hubs/` while retaining the correct `LocalGPT.Hubs` namespace.
- Removed stale `LocalGPT.Endpoints`, `TacosPortal.Services`, and corresponding imports from maintained source.
- Removed the documentation build rewrite that could preserve the obsolete `TacosPortal.Services` namespace. A normal documentation build now derives the corrected namespaces from source/assembly metadata.

## Text, regex and policy service boundaries

- Reduced the text-ownership compatibility baseline to an empty list. Razor components and controllers no longer own direct Regex/`Split`/`Replace`/`Join`/substring-style filtering covered by the maintained ownership guard.
- Internal static string helpers remain allowed only as implementation details behind injected services; Razor and controller code may not import/use the extension layer directly.
- Added `JsonTextService` as the DI-owned JSON text/escaping boundary.
- Added `RegexCompilationService` as the single bounded regex option/timeout compiler used by persisted/runtime pattern services and project-maintenance regex policy.
- Added `ProviderModelReviewerPolicyService` so benchmark reviewer-selection heuristics are not duplicated between UI and benchmark runtime.
- Added semantic `CouncilUserPollOptionKind` handling so UI behavior does not depend on an English display label such as `Exclude`.

## Minecraft, Datapack and knowledge domain extraction

- Extracted Minecraft project/loader/dependency/version generation into `MinecraftProjectService`.
- Extracted datapack content, pack-format/version catalog, validation, comparison and artifact writing into `MinecraftDatapackService`.
- Rewired Minecraft workspace generation, diagnostics and Council artifact flows to those domain services instead of routing Minecraft behavior through the general Council text/runtime services.
- Extracted Council knowledge projection, trust/review/status and source-hash behavior from `SqliteUtilityService` into `CouncilKnowledgeContentService`. The SQLite utility is again database-focused.

## Large-type and Razor component structure

- Split the largest Council/runtime/services into responsibility-named partial files after service extraction, without changing their external DI contracts.
- Split `Chat`, `Install`, `CouncilTeams` and `ModelCouncil` code-behind away from Razor markup; the Razor files retain their existing render-mode declarations.
- The maintained 3.0.1 structure gate rejects individual productive C# declarations spanning 1,000 or more source lines and rejects Razor code-behind partial files at or above 1,000 lines.
- Historical release audits were made logical-type/component aware so splitting a maintained type no longer makes existing feature gates falsely report removed functionality.

## Regression guards

- Added `build/audit_release_3_0_1.py` covering namespace ownership, text-service ownership, extension-layer isolation, new DI service boundaries, Minecraft/Datapack extraction, lightweight live rejoin and source-size limits.
- Updated the architecture audit and its Windows PowerShell fallback for Program partials, internal service-only extensions and the centralized regex compiler.
- Updated the async audit so markup-only `.razor` files remain counted after code-behind extraction.
- Updated provider, X-Round, Codegen, configurable-policy and historical release audits to inspect logical partial types/components without removing their feature assertions.

## Preserved invariants

- All 19 explicit `@rendermode` directives are exactly unchanged from 3.0.0.
- All 137 maintained JavaScript files under `src/LocalGPT` are byte-identical to 3.0.0.
- LocalGPT Wire Protocol remains 2.1.1; all 3 maintained source files are byte-identical to 3.0.0.
- The EF migration/model-snapshot tree is byte-identical to 3.0.0; 3.0.1 introduces no database schema change.
- Council seed version remains 25.

## Versioning

- LocalGPT: 3.0.1
- LocalGPTWebviewWrapper: 3.0.1
- LocalGPTInstallerConsole: 3.0.1
- LocalGPT Wire Protocol: 2.1.1

## Build status

This package is source-only and intentionally not compiled in the repair environment. No GitHub access and no .NET/MSBuild invocation were used. The Windows build/runtime remains authoritative.
