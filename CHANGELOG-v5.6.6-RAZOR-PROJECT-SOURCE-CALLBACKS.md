# LocalGPT 5.6.6 — DevExpress Razor callback and project editing context repair

## Root cause
Windows Visual Studio compilation of 5.6.5 failed in Install.razor lines 1316 and 1324: `DxComboBox.ValueChanged` was assigned a `Task` method group with insufficiently resolved generic EventCallback type information. Razor's generated `Install_razor.g.cs` reported additional CS1662 cascades. The reported NuGet restore issue is **not** part of this source error; the latest log shows package restore succeeded.

## Source changes
- `src/LocalGPT/Components/Pages/Install.razor`: Three project/revision/file `DxComboBox` editors now bind a render-local `RuntimePluginSourceEditContext`, use explicitly typed `Guid` lambdas and `ValueExpression`, and disable selection during active publication. This directly repairs the method-group CS1503 and its Razor-generated CS1662 cascades.
- `src/LocalGPT/Components/Pages/Install.ProjectSource.razor.cs`: Project selection resets dependent state; revision changes reset file identity; asynchronous source/navigation loads check the editing context and selection generation before applying results. All post-service component-state mutations are marshalled back through `InvokeAsync` to the InteractiveServer renderer; background service continuations are not permitted to mutate the render tree directly. Publication checks plugin identity and list membership before calling the existing service, which rechecks file review/hash.
- `src/LocalGPT/Components/Pages/Install.RuntimePlugins.razor.cs`: Creating/editing a plugin provisions a separate draft-owned source-selection context; successful saves preserve that draft identity.
- `src/LocalGPT/BusinessObjects/RuntimePluginSourceEditContext.cs`: New transient Business Object holding the active PluginId, ProjectId, RevisionId, TrackedFileId, and SelectionVersion (not an additional database entity or source-of-truth).
- `build/audit_runtime_plugin_source_callbacks_5_6_6.py` and `build/Assert-RuntimePluginSourceCallbacks.ps1`: Regression tests for explicit typed callbacks, editor retention, selection scopes, generation checks and publish controls.
- `Directory.Build.targets`: Wires the regression guard into LocalGPT's regular BeforeBuild checks without disabling existing guards.
- `src/LocalGPT/LocalGPT.csproj`, `src/LocalGPT/Components/App.razor`: Release version and script-cache identity 5.6.6.
- `build/audit_ascii_console_memory_5_6_0.py`, `build/audit_data_management_5_5_1.py`, `build/audit_data_management_workspace_5_5_3.py`, `build/audit_game_revision_chat_5_5_8.py`, `build/audit_game_studio_5_5_5.py`, `build/audit_learning_checklist_5_4_9.py`, `build/audit_python_project_orchestration_5_6_4.py`, `build/audit_windows_setup_no_auto_models_5_6_2.py`: Retained source audit version assertions updated for the new 5.6.6 release.

## Data ownership and render model
The popup's `BodyContentTemplate` supplies a DevExpress popup context, not a `LocalGptProject` or plugin draft. The Razor renderer therefore captures the current component-owned `RuntimePluginSourceEditContext` and passes it explicitly to its typed callbacks. This transient context never replaces versioned project/tracked-file/revision entities or the published project artifact. Business source validation and hash/review checks remain in `ProjectSourceNavigationService`. Switching between plugin drafts recreates the context so an older async callback cannot change another draft. Saving a plugin increments its selection generation, invalidating outstanding navigation results even when the draft identity is retained.

## Retained functionality
No changes to Python.NET GIL ownership, subprocess orchestration, DXAiFunction registry, controller exposure, HumanApproval, DevExpress HTML editor, Game Studio, model installation, PublisherStudio, or persistence schema. No stored project files were changed.

## Validation constraints
Static source checks pass; no .NET build, Razor generator run, Windows interactive test, or GitHub access was performed. A Windows Visual Studio build is required to confirm CS1503/CS1662 no longer appear.
