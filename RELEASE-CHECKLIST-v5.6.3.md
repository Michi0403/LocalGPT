# LocalGPT 5.6.3 – Per-file release checklist

Base: exact uploaded `LocalGPT-5.6.2-source.zip`; all original files retained.

## Modified files (22)

- [x] `Directory.Build.targets`
- [x] `build/async-continuation-policy.json`
- [x] `build/audit_ascii_console_memory_5_6_0.py`
- [x] `build/audit_data_management_5_5_1.py`
- [x] `build/audit_data_management_workspace_5_5_3.py`
- [x] `build/audit_game_revision_chat_5_5_8.py`
- [x] `build/audit_game_studio_5_5_5.py`
- [x] `build/audit_learning_checklist_5_4_9.py`
- [x] `build/audit_windows_setup_no_auto_models_5_6_2.py`
- [x] `src/LocalGPT/BusinessObjects/CodeGenerationWorkflowModels.cs`
- [x] `src/LocalGPT/Components/App.razor`
- [x] `src/LocalGPT/Components/Pages/DxFunctionCatalog.razor`
- [x] `src/LocalGPT/Components/Pages/DxFunctionCatalog.razor.css`
- [x] `src/LocalGPT/Components/Shared/ChatMicrophone.razor`
- [x] `src/LocalGPT/Controller/DxAiFunctionCatalogController.cs`
- [x] `src/LocalGPT/Controller/DxAiFunctionsController.cs`
- [x] `src/LocalGPT/Interfaces/IDxAiFunctionCatalogService.cs`
- [x] `src/LocalGPT/LocalGPT.csproj`
- [x] `src/LocalGPT/Program.ServiceRegistration.cs`
- [x] `src/LocalGPT/Services/DxAiFunctionRegistry.cs`
- [x] `src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs`
- [x] `src/LocalGPT/Services/OllamaThinkingChatClient.cs`

## Added files (12)

- [x] `CHANGELOG-v5.6.3-DX-FUNCTION-WORKBENCH.md`
- [x] `VALIDATION-v5.6.3-source.md`
- [x] `build/Assert-DxFunctionWorkbench.ps1`
- [x] `build/audit_dx_function_workbench_5_6_3.py`
- [x] `src/LocalGPT/BusinessObjects/DxAiFunctionWorkbenchModels.cs`
- [x] `src/LocalGPT/Interfaces/IDxAiFunctionInvocationAuditService.cs`
- [x] `src/LocalGPT/Interfaces/IDxAiFunctionWorkbenchService.cs`
- [x] `src/LocalGPT/Services/DxAiFunctionCatalogService.DiscoveryWorkbench.cs`
- [x] `src/LocalGPT/Services/DxAiFunctionInvocationAuditService.cs`
- [x] `src/LocalGPT/Services/DxAiFunctionWorkbenchDxAiFunctions.cs`
- [x] `src/LocalGPT/Services/DxAiFunctionWorkbenchService.cs`
- [x] `RELEASE-CHECKLIST-v5.6.3.md`

## Removed files

None.

## Verification

- [x] Full source tree differs only by the changed/added paths listed above.
- [x] New preview/approval/order build guard and 18 other source checks passed.
- [x] Existing LocalGPT game/data/installer functions maintained.
- [ ] Windows C# compilation, browser and real Ollama tests – deliberately not run here.
