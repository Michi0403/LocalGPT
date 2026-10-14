# LocalGPT 5.6.2 source-only validation and Windows acceptance

## Source verification

- `python build/audit_windows_setup_no_auto_models_5_6_2.py` — passed.
- `python build/audit_cross_platform_boundaries.py` — passed (26 checks).
- `python build/audit_devexpress_blazor_controls.py` — passed.
- `python build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `python build/audit_razor_maintenance_contract.py --root . --product localgpt` — passed.
- `python build/audit_async_continuations.py --source-root src/LocalGPT` — passed.
- `python build/audit_async_only_architecture.py --source-root src/LocalGPT --product localgpt` — passed.
- `python build/audit_service_resilience.py --root . --product localgpt` — passed.
- `python build/audit_runtime_text_ownership.py --root .` — passed.
- `python build/audit_game_studio_5_5_5.py`, `python build/audit_game_revision_chat_5_5_8.py`, `python build/audit_data_management_5_5_1.py`, `python build/audit_data_management_workspace_5_5_3.py`, `python build/audit_data_management_text_service_5_5_4.py`, `python build/audit_learning_checklist_5_4_9.py`, `python build/audit_ascii_console_memory_5_6_0.py` — passed.

The original 5.6.1 packaging-release audit is deliberately pinned to its historical version; it was not weakened to pass an unrelated 5.6.2 change. No .NET/Windows runtime compilation, installation or live model pull occurred. The source-only checks cannot substitute for these execution tests.

## Windows acceptance checklist

1. Publish/build `LocalGPTInstallerConsole` on Windows. **No-argument** installer: Ollama is detected/installed; no `Pulling gpt-oss:20b`, `--pull-models` or unexpected network model downloads; LocalGPT and shortcuts remain as before.
2. `LocalGPTInstallerConsole.exe --all`: combined setup does not pull models; other `--all` functions still run as previously configured (avoid unwanted repository downloads by not selecting `--all` unnecessarily).
3. Start LocalGPT; from first-run guide choose `/install`. Verify Ollama/LM Studio are detectable and that choosing and downloading a specific model works only after an explicit action.
4. Existing installed models are not removed or changed. Confirm previous chat/model binding data persists.
5. If deliberately testing `--pull-models --range Slim`, verify only that opt-in mode starts model downloads; cancel early rather than downloading a large model unintentionally.
6. Verify Ctrl+C cancellation remains responsive and setup transcript writes to LocalGPT.Setup.log.
7. Check Game Studio, Data Management and ASCII console for prior feature regressions.

## Release archive

The full repository is included. `RELEASE-CHECKLIST-v5.6.2.md` inventories changed/new files against the previous source ZIP; no files from 5.6.1 should be lost. The ZIP integrity check is performed separately during packaging.
