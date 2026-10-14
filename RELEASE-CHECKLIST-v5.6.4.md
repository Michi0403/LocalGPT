# Release checklist — LocalGPT 5.6.4

Baseline: complete LocalGPT 5.6.3 source archive.

- Original files: 3872
- Changed existing files: 27
- New files (including this checklist): 17
- Removed original source files: 0

## Modified files
- [x] `Directory.Build.targets`
- [x] `build/audit_ascii_console_memory_5_6_0.py`
- [x] `build/audit_data_management_5_5_1.py`
- [x] `build/audit_data_management_workspace_5_5_3.py`
- [x] `build/audit_game_revision_chat_5_5_8.py`
- [x] `build/audit_game_studio_5_5_5.py`
- [x] `build/audit_learning_checklist_5_4_9.py`
- [x] `build/audit_windows_setup_no_auto_models_5_6_2.py`
- [x] `src/LocalGPT/BusinessObjects/LocalAiRuntimeModels.cs`
- [x] `src/LocalGPT/BusinessObjects/PythonCoreOptions.cs`
- [x] `src/LocalGPT/BusinessObjects/RuntimePluginModels.cs`
- [x] `src/LocalGPT/Components/App.razor`
- [x] `src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs`
- [x] `src/LocalGPT/Components/Pages/Chat.razor`
- [x] `src/LocalGPT/Components/Pages/Install.LocalAiRuntime.razor.cs`
- [x] `src/LocalGPT/Components/Pages/Install.RuntimePlugins.razor.cs`
- [x] `src/LocalGPT/Components/Pages/Install.razor`
- [x] `src/LocalGPT/Components/Shared/ChatMicrophone.razor`
- [x] `src/LocalGPT/Controller/LocalAiRuntimeController.cs`
- [x] `src/LocalGPT/LocalGPT.csproj`
- [x] `src/LocalGPT/Program.ServiceRegistration.cs`
- [x] `src/LocalGPT/Runtime/Plugins/RuntimePluginDefinitionSupport.cs`
- [x] `src/LocalGPT/Runtime/Plugins/RuntimePluginExecutionHost.cs`
- [x] `src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py`
- [x] `src/LocalGPT/Services/LocalAiRuntimeDxAiFunctions.cs`
- [x] `src/LocalGPT/Services/LocalAiRuntimeService.cs`
- [x] `src/LocalGPT/Services/RuntimePluginService.cs`

## New files
- [x] `CHANGELOG-v5.6.4-PYTHON-PROJECT-REGEX-ORCHESTRATION.md`
- [x] `VALIDATION-v5.6.4-source.md`
- [x] `build/Assert-PythonProjectOrchestration.ps1`
- [x] `build/audit_python_project_orchestration_5_6_4.py`
- [x] `src/LocalGPT/BusinessObjects/ProjectSourceNavigationModels.cs`
- [x] `src/LocalGPT/BusinessObjects/PythonJobModels.cs`
- [x] `src/LocalGPT/BusinessObjects/PythonPluginFunctionRequests.cs`
- [x] `src/LocalGPT/Components/Pages/Install.ProjectSource.razor.cs`
- [x] `src/LocalGPT/Components/Pages/Install.PythonTasks.razor.cs`
- [x] `src/LocalGPT/Controller/ProjectSourceNavigationController.cs`
- [x] `src/LocalGPT/Interfaces/IProjectSourceNavigationService.cs`
- [x] `src/LocalGPT/Interfaces/IPythonJobOrchestrationService.cs`
- [x] `src/LocalGPT/Services/ProjectSourceNavigationDxAiFunctions.cs`
- [x] `src/LocalGPT/Services/ProjectSourceNavigationService.cs`
- [x] `src/LocalGPT/Services/PythonOrchestrationDxAiFunctions.cs`
- [x] `src/LocalGPT/Services/PythonTaskOrchestrationService.cs`
- [x] `RELEASE-CHECKLIST-v5.6.4.md`

## Release verification
- [x] Source-only retained and new audits recorded in `VALIDATION-v5.6.4-source.md`
- [x] Python bridge AST syntax checked
- [ ] Actual .NET compiler/Razor/Visual Studio publish — must run on the target development PC
- [ ] Interactive UI, multi-worker, and device-memory strategy tests — must run on the user machines
