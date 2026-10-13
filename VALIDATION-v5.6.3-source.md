# LocalGPT 5.6.3 – Source validation and Windows acceptance plan

## Source audit results

The following **19** source-only checks were rerun against this source tree and returned exit code 0:

1. `python build/audit_dx_function_workbench_5_6_3.py --root .`
2. `python build/audit_razor_maintenance_contract.py --root . --product localgpt`
3. `python build/audit_devexpress_blazor_controls.py --root . --product localgpt`
4. `python build/audit_async_continuations.py --source-root src/LocalGPT`
5. `python build/audit_async_only_architecture.py --source-root src/LocalGPT --product localgpt`
6. `python build/audit_service_resilience.py --root . --product localgpt`
7. `python build/audit_application_architecture.py --root . --product localgpt --mode all`
8. `python build/audit_cross_platform_boundaries.py --root .`
9. `python build/audit_powershell_portable_paths.py --root .`
10. `python build/audit_game_studio_5_5_5.py --root .`
11. `python build/audit_game_revision_chat_5_5_8.py --root .`
12. `python build/audit_data_management_5_5_1.py --root .`
13. `python build/audit_data_management_workspace_5_5_3.py --root .`
14. `python build/audit_learning_checklist_5_4_9.py --root .`
15. `python build/audit_ascii_console_memory_5_6_0.py --root .`
16. `python build/audit_windows_setup_no_auto_models_5_6_2.py --root .`
17. `python build/audit_human_collaboration_unification_5_4_6.py --root .`
18. `python build/audit_codegen_dxfunction_wiring.py --root .`
19. `python build/audit_data_management_text_service_5_5_4.py --root .`

The maintained configurable runtime-text ownership check passed earlier in the final source review. An older standalone `audit_interactive_server_registry_5_5_2.py` expects an outdated Game Studio render-mode spelling, and `audit_configurable_behavior_policy.py` expects an old Council seed revision; they are not changed to bypass their obsolete expectations. The normal active render-mode and architecture policies remain unchanged.

## Acceptance tests on Windows/.NET (not performed here)

- Open the complete 5.6.3 source root in Visual Studio; run **Build/Rebuild** with standard build targets. The new DX workbench guard must run and finish before compilation; investigate **all** C# and generated Razor errors (source audits cannot prove C# compiles).
- Start LocalGPT; open `/dx-functions`, enter **Function workbench**, confirm only enabled and AI-exposed entries are shown.
- Choose a safe read-only function; send invalid JSON and then schema-invalid valid JSON; verify readable `ValidationIssues` and **no execution**. Request a valid preview, verify `PreviewReady`, see current approval requirement and `PreviewFingerprint`.
- Change the parameters after preview: execution must be disabled until re-preview. Change server-side policy/descriptor between preview and execute: registry must return `StalePreview` without running the handler.
- Execute an ordinary read-only function: verify returned status and a payload-free entry in the history grid/API. Audit storage errors must never cause repeated side effects.
- Test an action requiring Human Collaboration: preview **does not** create an approval or execute; an execution request creates the usual approval/pending status; only explicit user approval may continue it.
- Disable a function and remove `ExposeToAiChat` through existing catalog UI. Confirm discovery hides it and direct registry execution remains denied by stored policy.
- Run a small Ollama Council/chat session without a custom tool allow-list: native tool offerings must be limited to `dxai.tools.discover`, `dxai.tools.preview`, `dxai.tools.invoke`; run discover → schema → preview → approved invoke on a harmless test handler.
- Confirm an explicit team/step automatic-function allow-list still preserves the previous exact native-tool set and existing Council workflows.
- Confirm `SystemVariables` contains at most 250 metadata-only audit entries, and secrets/arguments/results are never persisted in those entries.
- Regression-check game revisions, documentation publish, Windows model-free setup, PublisherStudio 1-Wire integration and existing `/api/dxai/functions/{functionName}/invoke` clients.

## Limitations

No `.NET` build, NuGet restore, published Windows package, real browser interaction, actual model inference or PublisherStudio build was executed in this environment, as agreed. No source audit can replace those tests.
