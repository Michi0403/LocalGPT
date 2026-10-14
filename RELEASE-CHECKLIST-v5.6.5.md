# Release checklist — LocalGPT 5.6.5

- [x] Derived from exact LocalGPT 5.6.4 tree.
- [x] CS0104 ambiguous ConfigurationRoot resolved by explicitly qualified business-object type.
- [x] Missing documented public-service API route implemented against approved existing DI invoker.
- [x] User-approved access separated from AI/1-Wire exposure policy.
- [x] Purpose-specific service controllers and exact-service exposure operation added.
- [x] Maintained HumanApprovalRequired attribute used on consequential HTTP actions.
- [x] New controller regression guard wired into regular LocalGPT pre-build checks.
- [x] No previously existing source files removed.
- [x] Archive integrity check performed.
- [ ] Windows/.NET compilation and Visual Studio restore (not executed here).
- [ ] Interactive approval, catalog discovery and actual controller calls (device test pending).
- [ ] WebviewWrapper NuGet restore diagnosis (separate user-environment report).

## Changed existing source files relative to 5.6.4
- [x] `Directory.Build.targets`
- [x] `build/audit_ascii_console_memory_5_6_0.py`
- [x] `build/audit_data_management_5_5_1.py`
- [x] `build/audit_data_management_workspace_5_5_3.py`
- [x] `build/audit_game_revision_chat_5_5_8.py`
- [x] `build/audit_game_studio_5_5_5.py`
- [x] `build/audit_learning_checklist_5_4_9.py`
- [x] `build/audit_python_project_orchestration_5_6_4.py`
- [x] `build/audit_windows_setup_no_auto_models_5_6_2.py`
- [x] `src/LocalGPT/Components/App.razor`
- [x] `src/LocalGPT/Controller/DxAiFunctionCatalogController.cs`
- [x] `src/LocalGPT/Controller/RegexCuratorController.cs`
- [x] `src/LocalGPT/Interfaces/IDxAiFunctionCatalogService.cs`
- [x] `src/LocalGPT/LocalGPT.csproj`
- [x] `src/LocalGPT/Program.ServiceRegistration.cs`
- [x] `src/LocalGPT/Services/PublicServiceMethodInvoker.cs`
- [x] `src/LocalGPT/Services/PythonTaskOrchestrationService.cs`

## Added source files relative to 5.6.4
- [x] `build/Assert-PublicServiceControllers.ps1`
- [x] `build/audit_public_service_controllers_5_6_5.py`
- [x] `src/LocalGPT/BusinessObjects/DiscoverPublicServiceMethodsRequest.cs`
- [x] `src/LocalGPT/BusinessObjects/KnowledgeRegexRecognitionRequest.cs`
- [x] `src/LocalGPT/BusinessObjects/PublicServiceControllerModels.cs`
- [x] `src/LocalGPT/Controller/CouncilTeamsController.cs`
- [x] `src/LocalGPT/Controller/KnowledgeRegexLinksController.cs`
- [x] `src/LocalGPT/Controller/PublicServiceMethodsController.cs`
- [x] `src/LocalGPT/Controller/RuntimePluginsController.cs`
- [x] `src/LocalGPT/Interfaces/IPublicServiceMethodDirectoryService.cs`
- [x] `src/LocalGPT/Services/PublicServiceDirectoryDxAiFunctions.cs`
- [x] `src/LocalGPT/Services/PublicServiceMethodDirectoryService.cs`

## Release documents (not part of the pre-documentation diff)
- [x] `CHANGELOG-v5.6.5-PUBLIC-SERVICE-CONTROLLER-COVERAGE.md`
- [x] `RELEASE-CHECKLIST-v5.6.5.md`
- [x] `VALIDATION-v5.6.5-source.md`
