# LocalGPT 5.5.3 — Source-only Validation

The complete source archive was prepared offline. No .NET SDK/runtime is assumed or used, and no GitHub network operations or Windows builds were attempted. Source checks do not prove that the Windows Razor/DevExpress solution compiles; perform a normal local Visual Studio build to confirm that.

## Completed static checks

- `python3 build/audit_data_management_workspace_5_5_3.py` — compact DevExpress navigation/editor, safe CRUD service calls, relational and table paging, protected-field confirmation, six locale catalogs and version.
- `python3 build/audit_data_management_5_5_1.py` — retained Council/Data Management/graphical evidence/checklist source contract.
- `python3 build/audit_interactive_server_registry_5_5_2.py` — approved InteractiveServer route with no prerender.
- `python3 build/audit_install_hardware_scores_5_5_2.py` — retained `/install` VRAM and CanIRun.ai score repairs.
- `python3 build/audit_learning_checklist_5_4_9.py` — retained per-file checklist and seeded workflow.
- `python3 build/audit_devexpress_blazor_controls.py` — DevExpress control/native-tag ownership.
- `python3 build/audit_razor_maintenance_contract.py --root . --product localgpt` — Razor maintenance layout, logging and component ownership.
- `python3 build/audit_async_continuations.py --source-root src/LocalGPT` — configured async continuation boundaries.
- `python3 build/audit_async_only_architecture.py --source-root src/LocalGPT --product localgpt` — async-only service architecture.
- `python3 build/audit_service_resilience.py --root . --product localgpt` — service diagnostics and resilience.
- `python3 build/audit_application_architecture.py --root . --product localgpt --mode all` — architectural policies.
- `python3 build/audit_documentation_pdf_merge_5_5_0.py` — previously repaired PDF merge flow.
- `python3 build/audit_powershell_portable_paths.py` — existing script path handling.

## Suggested Windows acceptance walkthrough

1. Build locally in Visual Studio with the licensed DevExpress packages available; confirm `Assert-InteractiveServerRenderModes.ps1` and retention/architecture gates pass.
2. Open `/data-management` and verify group navigation, nested foreign-key tree nodes, bounded splitter panes, record list selection and the narrow optional preview; the page should not grow into a many-column/one-row screen.
3. Open a table with more than one page and use Next/Previous to reach older records. Follow a selected row's parent/child relation and clear the filter.
4. On safe test data, create, modify and delete a record; verify deletion requires confirmation, EF migration metadata remains read-only, and protected preference fields require confirmation on changes.
5. Edit an editor-field preference and reload the page to verify service persistence; release a test one-run model exclusion and verify the eligibility warning clears without removing the audit record.
6. Recheck `/install` with 24 GiB VRAM and retained CanIRun.ai scores, graphical thinking, the human approval box and the per-file checklist.

## Scope

Changed product: **LocalGPT 5.5.3**. PublisherStudio sources were not changed and are not repackaged. This is a full source deliverable, not a compiled binary.
