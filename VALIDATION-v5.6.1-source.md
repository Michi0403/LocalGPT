# LocalGPT 5.6.1 — source verification record

Input: user-attached `LocalGPT-5.6.0-source(1).zip`. Output: `LocalGPT-5.6.1-source.zip`.

## Source-only checks executed successfully

The following existing scripts passed against the edited 5.6.1 source without .NET, NuGet, external services or GitHub:

- `build/audit_release_packaging_5_6_1.py` — helper version agreement, cached-tool capability handshake, early documentation preflight, ordered PDF manifest, preserved compression and size limit.
- `build/audit_documentation_pdf_merge_5_5_0.py` — retained durable PDF input ordering and recovery behavior.
- `build/audit_ascii_console_memory_5_6_0.py` — console CRLF, shell selection, fullscreen, DevExpress game selection and attributed memory display.
- `build/audit_game_studio_5_5_5.py` and `build/audit_game_revision_chat_5_5_8.py` — Godot, game state/revisions and Chat wiring.
- `build/audit_data_management_5_5_1.py`, `build/audit_data_management_workspace_5_5_3.py`, `build/audit_data_management_text_service_5_5_4.py` — BusinessObject CRUD/editor boundaries.
- `build/audit_learning_checklist_5_4_9.py` — retained persistent/UI-visible learning checklist.
- `build/audit_powershell_portable_paths.py` and `build/audit_powershell_variable_interpolation.py` — script portability.
- `build/audit_cross_platform_boundaries.py` — 26 platform boundary checks.
- `build/audit_application_architecture.py --root . --product localgpt --mode all`.
- `build/audit_async_continuations.py --source-root src/LocalGPT`.
- `build/audit_async_only_architecture.py --source-root src/LocalGPT --product localgpt`.
- `build/audit_devexpress_blazor_controls.py`.
- `build/audit_razor_maintenance_contract.py --root . --product localgpt`.
- `build/audit_transient_ui_state_ownership.py --root . --product localgpt`.
- `build/audit_repository_build_storage.py`.
- `build/audit_runtime_text_ownership.py --root .`.
- `build/audit_service_resilience.py --root . --product localgpt`.

Direct byte-by-byte comparison with the attached original archive found **16 modified files, 4 added files, 0 removed files**. Each of the 20 resulting differences has an individual row in `RELEASE-CHECKLIST-v5.6.1.md`.

## Historical standalone audits

Three historical scripts that were already stale against unmodified 5.6.0 source still contain old source/UI/seed assumptions and therefore did not pass when separately invoked: `audit_configurable_behavior_policy.py`, `audit_interactive_server_registry_5_5_2.py`, `audit_documentation_onewire_contracts.py`. No production code or mandatory maintenance policy was changed to satisfy obsolete assumptions; the corresponding current, build-relevant architecture, runtime text, render-state and DevExpress checks above passed.

## Not executable here

- .NET 10 compile and `LocalGPT.ReleasePackaging 1.0.4` runtime behavior.
- Full cross-platform publish and final DocFX/PDF merge/compression on the user's host.
- Native PowerShell execution and installer testing.

The checks above are **source validation**, not a claim that the native release build has been executed.
