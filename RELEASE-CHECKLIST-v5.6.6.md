# LocalGPT 5.6.6 — complete file-by-file release checklist

All files compared byte-for-byte with the delivered LocalGPT 5.6.5 source tree. No original file is removed. This is a complete source archive, not a patch.

## Changed existing files (14)

- [x] `Directory.Build.targets` — Build wiring for the strict Razor callback regression check.
- [x] `build/audit_ascii_console_memory_5_6_0.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `build/audit_data_management_5_5_1.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `build/audit_data_management_workspace_5_5_3.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `build/audit_game_revision_chat_5_5_8.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `build/audit_game_studio_5_5_5.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `build/audit_learning_checklist_5_4_9.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `build/audit_python_project_orchestration_5_6_4.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `build/audit_windows_setup_no_auto_models_5_6_2.py` — Retained source regression test version checks aligned with application version 5.6.6.
- [x] `src/LocalGPT/Components/App.razor` — Versioned browser script-cache identity.
- [x] `src/LocalGPT/Components/Pages/Install.ProjectSource.razor.cs` — Project/revision/file editor event handlers, race checks, and renderer-dispatched UI state commits.
- [x] `src/LocalGPT/Components/Pages/Install.RuntimePlugins.razor.cs` — One new source selection context on each plugin draft; save identity and selection-generation invalidation.
- [x] `src/LocalGPT/Components/Pages/Install.razor` — Explicit typed Guid callbacks; data-bound context; separate editor selection.
- [x] `src/LocalGPT/LocalGPT.csproj` — Application version bump to 5.6.6.

## Added files (6)

- [x] `CHANGELOG-v5.6.6-RAZOR-PROJECT-SOURCE-CALLBACKS.md` — Added for the 5.6.6 release; see corresponding release notes or validation report.
- [x] `RELEASE-CHECKLIST-v5.6.6.md` — Added for the 5.6.6 release; see corresponding release notes or validation report.
- [x] `VALIDATION-v5.6.6-source.md` — Added for the 5.6.6 release; see corresponding release notes or validation report.
- [x] `build/Assert-RuntimePluginSourceCallbacks.ps1` — Build-wired PowerShell entrypoint for callback contract.
- [x] `build/audit_runtime_plugin_source_callbacks_5_6_6.py` — Source-only regression for explicitly typed DevExpress callbacks and context.
- [x] `src/LocalGPT/BusinessObjects/RuntimePluginSourceEditContext.cs` — Business Object for per-plugin source selection and async generation.

## Verification

- [x] Byte-level base-version comparison; zero removed original files.
- [x] Existing service/controller/Python and Razor source audits completed, details in `VALIDATION-v5.6.6-source.md`.
- [x] XML project/target files parse successfully.
- [ ] Windows Visual Studio restore/build, Razor code generation and interactive selector test — intentionally not run in this environment.

PublisherStudio and its source were not modified. No schema migrations were necessary. No precompiled binary artifacts are included.
