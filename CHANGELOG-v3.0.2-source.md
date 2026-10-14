# LocalGPT 3.0.2 source changelog

## Windows build-guard and compile repair

- Added the missing `using LocalGPT.Services;` to `StructuredTextController`, fixing the Windows compiler `CS0246` for `CouncilTextService`.
- Made `Assert-OperationalDiagnostics.ps1` logical-partial aware. Chat diagnostics are verified across `Chat.razor` plus its code-behind partials, and startup/controller registrations are verified across all maintained `Program*.cs` partials.
- Made `Assert-InteractiveServerRenderModes.ps1` verify InteractiveServer registration/mapping across the complete `Program*.cs` composition root while retaining all 19 required component render-mode checks.
- Made `Assert-IteratorExceptionPolicy.ps1` normalize responsibility-named partial filenames back to their maintained baseline owner before comparing historical iterator exceptions. New iterator violations remain blocked.
- Made `Assert-SystemVariableInitialization.ps1` use the same partial-owner normalization. Existing baseline literals remain controlled while new initialization literals remain forbidden.
- Preserved the 3.0.1 namespace, text-service, Minecraft/Datapack, partial-structure, EF startup, live-Council lane and lightweight rejoin repairs.

## Extracted-service API/DXFunction wiring

- Audited the services introduced by the 3.0.1 responsibility split.
- `MinecraftProjectService` and `MinecraftDatapackService` remain directly wired to `MinecraftDiagnosticController`.
- Added read-only `minecraft.dependency.version.resolve` and `minecraft.datapack.version.resolve` DI-backed DXAIFunction handlers. Existing `IDxAiFunctionHandler` discovery registers them automatically and `DxAiFunctionCatalogService` persists discovered descriptors as system-seed catalog entries.
- `CouncilKnowledgeContentService`, `RegexCompilationService`, `ProviderModelReviewerPolicyService`, and `JsonTextService` intentionally remain internal/subordinate service boundaries. Their externally meaningful capabilities continue through their owning Council Knowledge, Regex/Structured Text, Provider Benchmark, and service/filter APIs rather than duplicate controllers.

## Version

- LocalGPT: 3.0.2
- LocalGPTWebviewWrapper: 3.0.2
- LocalGPTInstallerConsole: 3.0.2
- Wire Protocol: 2.1.1 (unchanged)
- Council seed version: 25 (unchanged)
