# LocalGPT 5.3.5 — service runtime-parameter ownership and upload-workspace progression

## Scope

This release continues from 5.3.4 and repairs two runtime-ownership regressions reported against the supplied source: operational services that still authored their own configuration defaults/bounds, and Learning Round project synchronization that could remain anchored to an older upload workspace after later ZIP uploads.

No replacement configuration subsystem was introduced. The repair uses the existing `SystemVariable`/runtime-policy persistence path, typed BusinessObjects, `LocalGptRuntimePolicySeedDataService`, and the existing `InitialDataCatalog` feed into database initialization.

## Database-owned service parameters

Added typed BusinessObjects for service-level operational settings and seeded their defaults through the existing runtime-policy/InitialDataCatalog path. A forward migration (`20261008231500_AddServiceRuntimePolicyParameters`) uses `INSERT OR IGNORE`, so an existing user-edited value is not overwritten.

The migration/seed covers 20 typed parameter documents:

- MCP gateway defaults, protocol versions, cache policy, host policy and request/result bounds;
- chat upload workspace stream/list/read bounds;
- Learning project workspace scanning and progression policy;
- provider discovery/session defaults and provider endpoints/routes;
- model benchmark bounds;
- Council execution/recovery limits;
- Council game bounds;
- shared read/query limits;
- LearningBase import bounds;
- Local AI media/runtime bounds;
- model preset bounds;
- 1-Wire timing/result bounds;
- project-maintenance bounds;
- Python.NET queue bounds;
- regex compile/store bounds;
- remote-knowledge import bounds;
- toolchain discovery/runtime bounds;
- Hugging Face catalog bounds;
- embedded-firmware telemetry bounds; and
- shared service timing defaults.

`LocalGptMcpConfigurationPolicy.NormalizeDraft` no longer owns the reported address/header/protocol/cache/host/body/list/result magic values. `LocalGptMcpEndpoint` and `LocalGptMcpGatewayAccessor` consume the same persisted MCP policy, so preview, endpoint behavior and execution no longer have independent constants.

The same ownership pass was applied across the affected service families rather than only the reported MCP class. Operational defaults and bounds now resolve through typed database-backed parameter objects in upload, provider, Council, benchmark, Learning, Local AI, regex, project-maintenance, 1-Wire, remote-knowledge, toolchain, Hugging Face, firmware and timing paths. Existing mathematical invariants, protocol parsing syntax, authored document values and algorithmic calculations remain code-owned instead of being incorrectly turned into user configuration.

## ZIP/upload progression

`LearningProjectWorkspaceSyncService` now treats an explicitly remembered workspace as the lower bound of the current Learning Round evidence set. When newer upload workspaces exist, it synchronizes the requested workspace plus the newer workspaces in creation order and discovers every repository-shaped root under each extracted workspace.

The Learning maintenance result now advances its remembered workspace to the last/newest synchronized workspace instead of preserving the first result forever. This prevents repeated rounds from continually treating the first ZIP as the canonical upload after subsequent ZIPs arrive.

The existing upload service still extracts every ZIP in a workspace; this release repairs the downstream project synchronization/continuation behavior rather than adding a second archive extractor.

## Provider defaults

Provider discovery/session fallback values that were still embedded in operational methods now resolve from `ProviderModelRuntimeParameters`, including local OpenAI-compatible endpoint/key, public OpenAI endpoint, API path/model path, session keep-alive/context/timeout and discovery timing. The existing database-backed default Ollama endpoint is reused rather than duplicated.

## Regression guards and preservation

- Runtime model-prompt/regex ownership remains green with zero findings.
- Existing database, BusinessObject, provider, Council, approval, execution, DevExpress and DI architecture is preserved.
- DevExpress remains 25.2.10 and the maintained .NET 10 dependency baseline remains 10.0.12.
- No .NET/MSBuild restore/build/publish and no GitHub/online repository access were used for this source repair.
