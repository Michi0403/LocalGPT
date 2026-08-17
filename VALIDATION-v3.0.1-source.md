# LocalGPT 3.0.1 source validation

This package was validated source-only. No `dotnet`, MSBuild, Visual Studio build, GitHub, or online repository access was used. The Windows build, startup, live-Council execution and browser rejoin remain the authoritative compiled/runtime validation.

## Passed source audits

- `build/audit_release_3_0_1.py` — 138 namespace/wiring/structure/rejoin checks.
- Historical `build/audit_release_2_8_5.py` through `build/audit_release_3_0_0.py` all pass after being made logical-partial aware where required.
- `build/audit_application_architecture.py --root . --product localgpt --mode all`.
- `build/audit_service_resilience.py --root . --product localgpt` — 1,907 service methods own logged try/catch boundaries; 30 iterator methods and 3 direct Program/Startup methods are treated by their dedicated policies.
- `build/audit_async_continuations.py --source-root src/LocalGPT` — 220 source files, 2,437 await tokens, 2,222 `ConfigureAwait(false)`, exactly 29 renderer-affine `ConfigureAwait(true)`, 182 explicitly configured async disposals and 4 configured async streams.
- `build/audit_provider_qualified_council.py --root .` — 282 checks.
- `build/audit_xround_wiring.py`.
- `build/audit_codegen_dxfunction_wiring.py`.
- `build/audit_configurable_behavior_policy.py`.
- `build/audit_benchmark_rejoin_repair.py`.
- `build/audit_async_responsiveness_2_8_3.py`.
- `build/audit_chat_ascii_console.py --root .` — 17 checks.
- `build/audit_human_entity_formatting.py`.
- `build/audit_documentation_onewire_contracts.py`.
- `build/audit_kawaii_documentation_layout.py`.
- `build/Assert-XmlDocumentationCoverage.py .` — 8,001 direct declarations across 537 maintained C# source files.
- Python-equivalent execution of `Assert-TextServiceOwnership.ps1` — zero direct component/controller text/regex ownership violations and an empty baseline.
- LocalGPT 3.0.0 EF consistency regression audit — 41 `DbSet` entity types and 571 persisted scalar properties covered.

## Structural validation

- 545 maintained C# files were lexically masked for strings/comments and checked for balanced type/member braces: zero failures.
- The new 3.0.1 structure gate reports no maintained productive C# declaration spanning 1,000 or more source lines.
- All Razor code-behind partial files are below 1,000 lines; the formerly exact-1,000-line Chat configuration partial was split again before packaging.
- `MultiModelCouncilService` retains all 96 method names present in 3.0.0.
- `ProjectMaintenanceService` retains all 56 method names present in 3.0.0 while its Regex compilation implementation now routes through `IRegexCompilationService`.
- `CouncilRuntimeService` has 18 fewer methods than 3.0.0; all 18 are the intentionally extracted Minecraft/Datapack dependency/version/reference/validation/write responsibilities.
- `CouncilTextService` removed Minecraft/Datapack/project-generation responsibilities and gained only the service-owned general text-filter/format helpers required by the zero-baseline UI/API ownership rule.
- `ProviderModelBenchmarkService` moved only `GetReviewerPriority` to the shared `ProviderModelReviewerPolicyService`.

## Live Council rejoin validation

- The attach path uses `GetAttachmentSnapshot(runId)` instead of materializing `CouncilLiveSessions.Get(runId)`.
- While the Council is active, the persisted Chat surface carries the stable live-Council marker rather than a duplicate full transcript.
- The DevExpress message collection is rebound exactly once in the attach method when the initial/rejoin bind is required.
- Browser draft capture/restore is preserved.
- `JSDisconnectedException` and component-lifetime cancellation return attach failure rather than reporting successful rejoin.
- Component/message mutation occurs under `InvokeAsync`.
- A completed run resolves the full transcript from the live-session service and forces conversation persistence, preserving restart durability.

## Release invariants against 3.0.0

- 19/19 explicit `@rendermode` directives are identical by relative file and directive text.
- 137/137 JavaScript files under `src/LocalGPT` are byte-identical.
- LocalGPT Wire Protocol source tree: 3/3 files byte-identical; version remains 2.1.1.
- EF migration/model-snapshot tree: 25/25 files byte-identical; no schema migration is introduced in 3.0.1.
- Council seed version remains 25.

## Windows guard compatibility

The Windows build invokes PowerShell wrappers. `Invoke-ArchitectureAudit.ps1` was updated so its no-Python fallback recognizes Program partials, narrowly permits service-only extension methods, and recognizes `RegexCompilationService` as the approved Regex compilation boundary. The async wrapper continues to execute the syntax-aware Python policy when Python is available. PowerShell itself is not installed in this inspection environment, so the wrappers were source-reviewed rather than executed here.
