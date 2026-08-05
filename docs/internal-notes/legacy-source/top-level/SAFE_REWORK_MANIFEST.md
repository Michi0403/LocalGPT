# LocalGPT safe rework manifest

This manifest compares the delivered tree with the latest user-owned base used for this pass.

## Design constraints applied

- `Program.cs` remains a legal bootstrap/static boundary.
- Framework-required Blazor imports remain unchanged.
- PublisherStudio DI extension methods remain static extensions and accept `ILogger` with `try/catch` logging.
- No namespace declaration was renamed.
- No `.pubxml`, project file, installer project, or release lane was removed.
- Runtime regex source, collections, limits, identifiers, and security values are owned by typed/persisted policy services rather than static catalogs.
- DTOs, records, constructors, and pure calculations are not wrapped in artificial logging/exception boilerplate.
- Static, runtime-value, and maintained operational-method debt baselines are empty and unused.

Added files: 9
Removed files: 2
Changed files: 70

## Added

- `src/LocalGPT/Interfaces/IDxAiFunctionJsonService.cs`
- `src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs`
- `src/LocalGPT/Services/Persistence/LocalGptVocabularyService.cs`
- `src/LocalGPT/Services/RegexFunctionParameterService.cs`
- `build/Invoke-ArchitectureAudit.ps1`
- `build/audit_application_architecture.py`
- `build/tests/applicationArchitecturePolicy.test.mjs`
- `build/tests/test_architecture_audit.py`
- `docs/SAFE_STATIC_RUNTIME_AND_DIAGNOSTICS_POLICY.md`

## Removed

- `src/LocalGPT/Extensions/CollectionsExtensions.cs`
- `src/LocalGPT/Extensions/StringExtensions.cs`

## Changed

- `src/LocalGPT/BusinessObjects/AmbientLocalGptContextModels.cs`
- `src/LocalGPT/BusinessObjects/CommandPolicyDecision.cs`
- `src/LocalGPT/BusinessObjects/CouncilSpoolerModels.cs`
- `src/LocalGPT/BusinessObjects/DxAiFunctionCatalogModels.cs`
- `src/LocalGPT/BusinessObjects/HumanCollaborationModels.cs`
- `src/LocalGPT/BusinessObjects/LocalGptRuntimePolicyModels.cs`
- `src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor`
- `src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor`
- `src/LocalGPT/Components/Pages/Chat.razor`
- `src/LocalGPT/Components/Pages/Database.razor`
- `src/LocalGPT/Components/Pages/DxFunctionCatalog.razor`
- `src/LocalGPT/Components/Pages/ProjectMaintenance.razor`
- `src/LocalGPT/Components/Pages/Projects.razor`
- `src/LocalGPT/Components/Pages/TestLab.razor`
- `src/LocalGPT/Components/_Imports.razor`
- `src/LocalGPT/Controller/LocalGptDiagnosticController.cs`
- `src/LocalGPT/Controller/MinecraftDiagnosticController.cs`
- `src/LocalGPT/Interfaces/IChatResponseFormatter.cs`
- `src/LocalGPT/Interfaces/ILocalGptRuntimePolicyDataService.cs`
- `src/LocalGPT/Interfaces/IOneWireServices.cs`
- `src/LocalGPT/Interfaces/IRegexPatternService.cs`
- `src/LocalGPT/Logging/DatabaseLogger.cs`
- `src/LocalGPT/Logging/FileLogger.cs`
- `src/LocalGPT/Program.cs`
- `src/LocalGPT/Services/AiDiscoveryService.cs`
- `src/LocalGPT/Services/AmbientLocalGptContext.cs`
- `src/LocalGPT/Services/ArchitectureAndDebugDxAiFunctions.cs`
- `src/LocalGPT/Services/ChatClientFactory.cs`
- `src/LocalGPT/Services/ChatMemoryMessageMapper.cs`
- `src/LocalGPT/Services/ChatUploadWorkspaceService.cs`
- `src/LocalGPT/Services/Council/CouncilSpoolerService.cs`
- `src/LocalGPT/Services/Council/Skills/ModelCapabilitySelfAssessmentService.cs`
- `src/LocalGPT/Services/CouncilChatClient.cs`
- `src/LocalGPT/Services/CouncilDxFunctionOrchestrator.cs`
- `src/LocalGPT/Services/CouncilRuntimeService.cs`
- `src/LocalGPT/Services/CouncilTeamConfigurationService.cs`
- `src/LocalGPT/Services/CouncilTextService.cs`
- `src/LocalGPT/Services/DatabaseMaintenanceDxAiFunctions.cs`
- `src/LocalGPT/Services/DeferredDxAiInvocationService.cs`
- `src/LocalGPT/Services/DevExpressChatService.cs`
- `src/LocalGPT/Services/DxAiFunctionCatalogService.cs`
- `src/LocalGPT/Services/DxAiFunctionRegistry.cs`
- `src/LocalGPT/Services/Formatting/ChatProtocolProfiles.cs`
- `src/LocalGPT/Services/Formatting/ChatProtocolResolver.cs`
- `src/LocalGPT/Services/Formatting/ChatResponseFormatter.cs`
- `src/LocalGPT/Services/Helpers/DxAiFunctionJsonHelper.cs`
- `src/LocalGPT/Services/HumanCollaborationService.cs`
- `src/LocalGPT/Services/LearnBaseKnowledgeImporterService.cs`
- `src/LocalGPT/Services/LocalGptCatalogService.cs`
- `src/LocalGPT/Services/MinecraftModWorkspaceService.cs`
- `src/LocalGPT/Services/MultiModelCouncilService.cs`
- `src/LocalGPT/Services/NativeCommandRunner.cs`
- `src/LocalGPT/Services/OneWire/OneWireCapabilityCatalog.cs`
- `src/LocalGPT/Services/OneWire/OneWireExecutionServices.cs`
- `src/LocalGPT/Services/OneWire/OneWireTransportSecurityPolicy.cs`
- `src/LocalGPT/Services/OrganicCouncilBlueprintService.cs`
- `src/LocalGPT/Services/Persistence/LocalGptRuntimePolicyDataService.cs`
- `src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs`
- `src/LocalGPT/Services/Persistence/LocalGptRuntimePolicyStoreService.cs`
- `src/LocalGPT/Services/ProjectArchitectureDxAiFunctions.cs`
- `src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs`
- `src/LocalGPT/Services/PublicServiceMethodInvoker.cs`
- `src/LocalGPT/Services/RegexDxAiFunctions.cs`
- `src/LocalGPT/Services/SqliteUtilityService.cs`
- `build/Assert-ApplicationStaticPolicy.ps1`
- `build/Assert-MethodDiagnostics.ps1`
- `build/Assert-RuntimeValueOwnership.ps1`
- `build/application-static-baseline.json`
- `build/method-diagnostics-baseline.json`
- `build/runtime-value-ownership-baseline.json`

## Validation performed in this environment

- Python architecture audit: passed.
- Python architecture-audit unit tests: passed.
- Application-static scan: only `Program.cs`, the Blazor RenderMode import, and the two PublisherStudio DI extension boundaries remain.
- Namespace comparison against the base: no namespace declaration changed.
- LocalGPT Node architecture-policy contract: passed.

A .NET compile/publish was not possible in this environment; compiler confirmation remains a local merge gate.
