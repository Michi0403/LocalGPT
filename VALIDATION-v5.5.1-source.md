# LocalGPT 5.5.1 — source validation and Windows acceptance

## Scope and non-executed work

This release was prepared from the supplied complete source ZIPs with **no GitHub access and no .NET SDK**. The source-only tests below do not execute a Blazor renderer, EF migration, database-backed service, DevExpress runtime, Ollama host or PDF merge. No Windows build or provider test result is claimed.

## Source-only checks

Run from the unpacked repository root:

```text
python3 build/audit_data_management_5_5_1.py
python3 build/audit_learning_checklist_5_4_9.py
python3 build/audit_documentation_pdf_merge_5_5_0.py
python3 build/audit_devexpress_blazor_controls.py
python3 build/audit_razor_maintenance_contract.py --root . --product localgpt
python3 build/audit_async_only_architecture.py --source-root src/LocalGPT --product localgpt
python3 build/audit_async_continuations.py --source-root src/LocalGPT
python3 build/audit_service_resilience.py --root . --product localgpt
python3 build/audit_application_architecture.py --root . --product localgpt --mode all
python3 build/audit_runtime_text_ownership.py --root .
python3 build/audit_powershell_portable_paths.py
```

All listed checks passed during release preparation. The DevExpress component-retention contract was additionally compared source-only across all 58 protected Razor components with no protected count/disclosure overrun. The Windows PowerShell script remains authoritative and has not been weakened.

## Maintainer's Windows/.NET 10 acceptance checklist

- [ ] Unpack the full source over a *separate, backed-up* checkout and build LocalGPT 5.5.1 with the existing .NET 10 SDK, DevExpress 25.2.10 and Windows PowerShell validation enabled. Keep all existing guards enabled; report precise errors if any.
- [ ] Navigate to **Data Management** from the main DevExpress menu. Verify all six vertical business-domain groups, live entity counts and all expected tables, including initially unknown table names, remain accessible.
- [ ] Select a project/conversation/knowledge entity with a declared SQLite foreign key, pick an existing record, follow parent and child buttons, confirm that the target records actually match its key, then clear the relation filter.
- [ ] Edit a safe non-critical record using the inferred/persisted DevExpress editor kind; save and refresh. Test a new record and one deliberately chosen disposable record deletion after explicitly checking the confirmation box. Verify foreign-key constraints and migration metadata protection; **make a database backup before any real deletion**.
- [ ] In Human Collaboration, approve a Council model-health proposal that excludes an eligible model once. Run the corresponding Council: verify the exclusion is consumed on that run, then the model becomes eligible for the next run. This also applies to historically approved non-consuming low-risk decisions. Do not confuse unreachable Ollama with a persisted exclusion; make the local host reachable during this test.
- [ ] In **Data Management → Approvals & health**, verify active model-health decisions are visible even with a long human-inbox history. Release one selection through the trusted human UI action and ensure it disappears from the *active* list while remaining in the Human Collaboration audit trail. Generic approval gates must still require their configured permission.
- [ ] Run a Council with thinking/provider streaming and verify every running graphical lane shows live content. After a lane completes, click **Inspect answer, thinking and provider evidence**, verify both completed answer and provider details, and click **Hide**. Confirm ASCII/default transcript still works and no reasoning segment is silently replaced.
- [ ] Confirm the Project Maintenance per-file learning checklist still creates one durable task per tracked file, keeps incomplete tasks visible and does not mark an unread file completed merely from a hash match.
- [ ] Produce the required documentation HTML/PDF; ensure the 5.5.0 ordered input-list repair still merges all bounded chunks without a Windows process-command-length error. Verify the versioned `LocalGPT-5.5.1.pdf` opens.

Any actual .NET/Razor build warning, DevExpress compile issue or runtime failure should be reported with its original diagnostic. Do not change build-guard minimums, remove DevExpress controls, bypass PDF generation, disable human approval, or discard features to make a failing build appear successful.
