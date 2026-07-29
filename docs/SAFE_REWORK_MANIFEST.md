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

- `LocalGPTWebviewWrapper/LocalGPT/Interfaces/IDxAiFunctionJsonService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/LocalGptVocabularyService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/RegexFunctionParameterService.cs`
- `build/Invoke-ArchitectureAudit.ps1`
- `build/audit_application_architecture.py`
- `build/tests/applicationArchitecturePolicy.test.mjs`
- `build/tests/test_architecture_audit.py`
- `docs/SAFE_STATIC_RUNTIME_AND_DIAGNOSTICS_POLICY.md`

## Removed

- `LocalGPTWebviewWrapper/LocalGPT/Extensions/CollectionsExtensions.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Extensions/StringExtensions.cs`

## Changed

- `LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/AmbientLocalGptContextModels.cs`
- `LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/CommandPolicyDecision.cs`
- `LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/CouncilSpoolerModels.cs`
- `LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/DxAiFunctionCatalogModels.cs`
- `LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/HumanCollaborationModels.cs`
- `LocalGPTWebviewWrapper/LocalGPT/BusinessObjects/LocalGptRuntimePolicyModels.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Layout/HumanCollaborationInbox.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Database.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Pages/DxFunctionCatalog.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Pages/ProjectMaintenance.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Projects.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/Pages/TestLab.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Components/_Imports.razor`
- `LocalGPTWebviewWrapper/LocalGPT/Controller/LocalGptDiagnosticController.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Controller/MinecraftDiagnosticController.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Interfaces/IChatResponseFormatter.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Interfaces/ILocalGptRuntimePolicyDataService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Interfaces/IOneWireServices.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Interfaces/IRegexPatternService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Logging/DatabaseLogger.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Logging/FileLogger.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Program.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/AiDiscoveryService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/AmbientLocalGptContext.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/ArchitectureAndDebugDxAiFunctions.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/ChatClientFactory.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/ChatMemoryMessageMapper.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/ChatUploadWorkspaceService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Council/CouncilSpoolerService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Council/Skills/ModelCapabilitySelfAssessmentService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/CouncilChatClient.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/CouncilDxFunctionOrchestrator.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/CouncilRuntimeService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/CouncilTeamConfigurationService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/CouncilTextService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/DatabaseMaintenanceDxAiFunctions.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/DeferredDxAiInvocationService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/DevExpressChatService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/DxAiFunctionCatalogService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/DxAiFunctionRegistry.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Formatting/ChatProtocolProfiles.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Formatting/ChatProtocolResolver.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Formatting/ChatResponseFormatter.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Helpers/DxAiFunctionJsonHelper.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/HumanCollaborationService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/LearnBaseKnowledgeImporterService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/LocalGptCatalogService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/MinecraftModWorkspaceService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/MultiModelCouncilService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/NativeCommandRunner.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OneWireCapabilityCatalog.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OneWireExecutionServices.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/OneWire/OneWireTransportSecurityPolicy.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/OrganicCouncilBlueprintService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/LocalGptRuntimePolicyDataService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/LocalGptRuntimePolicyStoreService.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/ProjectArchitectureDxAiFunctions.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/PublicServiceMethodInvoker.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/RegexDxAiFunctions.cs`
- `LocalGPTWebviewWrapper/LocalGPT/Services/SqliteUtilityService.cs`
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
