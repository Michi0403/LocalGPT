# LocalGPT v0.1.4 workflow-safety revision

This candidate keeps the v0.1.4 feature set and fixes the compile/workflow diagnostics reported after the compile-fix archive.

## Compile and contract fixes

- Qualified `NavigationUrlService.ToggleSidebarName` in Drawer, MainLayout, and Index without changing sidebar behavior.
- Moved `DxaichatFunctionInfo` into `LocalGPT.BusinessObjects` as the shared interface/service contract.
- Removed the duplicate database-controller using.
- Aligned reported implementation nullability with interfaces while preserving meaningful optional artifact results.
- Replaced silent `null` workflow failures with explicit safe result objects, user-visible AI failure responses, or logged exception propagation according to each contract.
- Preserved cancellation as cancellation.

## Component safety and short-term awareness

- Added top-directive `ILogger<T>`, `INotificationService`, and `IComponentActivityService` injection to every maintained Razor component.
- Added global and layout-level error boundaries plus a shared toast provider.
- Routed error boundaries now recover after navigation, so one failed page cannot trap the rest of the UI.
- Added bounded process-local UI activity memory and included a sanitized recent briefing in AI bootstrap context.
- Added a read-only `/__diag/component-activity` verification route for the same sanitized bounded context.
- Connected notifications to bounded operational awareness without copying prompts, responses, uploads, generated source, secrets, or full exception text.
- Hardened database, project, model-council, test-lab, Minecraft, chat autosave, navigation, startup, and theme workflows with consistent start/success/cancel/failure reporting.

## Workflow integrity

- Minecraft workspace creation now awaits the selected loader workflow and cannot report success after required file generation fails.
- Chat streaming returns explicit safe failure updates rather than nullable streams.
- Required workspace/import/table/artifact operations no longer hide failure behind `null` while callers continue with stale state.
- Optional DLL artifact creation remains nullable by design because “not produced” is a valid, explicitly handled outcome.

## Future-session guards

- Added `Assert-ComponentSafety.ps1` and `Assert-WorkflowContracts.ps1`.
- Repository validation and CI now check component dependency placement, notification/activity integration, shared DTO ownership, navigation constant qualification, interface nullability, streaming contracts, and workflow failure propagation before Roslyn and Debug/Release builds.
- Governance instructions require feature/data behavior to be preserved even when visual composition changes.
