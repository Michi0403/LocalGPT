# LocalGPT 5.4.9 source validation and maintainer acceptance

## Static source audits

The following source-only Python audits completed with exit code 0 in the release-preparation workspace (no .NET SDK was used):

- `build/audit_learning_checklist_5_4_9.py` — model, migration, state-transition, DXFunction schema, Council seed, DevExpress checklist and six-language catalog parity.
- `build/audit_razor_maintenance_contract.py --root . --product localgpt` — Razor/DevExpress layout and component diagnostics.
- `build/audit_devexpress_blazor_controls.py` — no new native control debt.
- `build/audit_async_only_architecture.py --source-root src/LocalGPT --product localgpt` — async boundaries and no discarded work.
- `build/audit_service_resilience.py --root . --product localgpt` — service error-handling ownership.
- `build/audit_application_architecture.py --root . --product localgpt --mode all` — application statics, diagnostics and C# structural rules.
- `build/audit_runtime_text_ownership.py --root .` — no added hardcoded runtime prompt bodies/regex patterns outside maintained owners.
- `build/audit_transient_ui_state_ownership.py --root . --product localgpt` — InteractiveServer transient input ownership.

PowerShell-only guards could not be executed on this host and must run during the maintainer's .NET build.

## Required build/runtime checks on the maintainer PC

1. Build the unchanged .NET 10 solution using the version declared by `global.json`; run the build-wired PowerShell maintenance, localization, DI, async, Razor, DevExpress-retention and EF model/snapshot guards without bypassing them.
2. Apply the additive `20261010210000_AddTrackedFileLearningChecklist` EF migration on a copy of existing data; check that existing tracked rows become `Pending` without losing file identities, project relationships or pattern configuration.
3. Scan a project and synchronize a multi-ZIP upload workspace: verify one checklist row for each tracked file, preserved project/revision/archive-root identity, no duplicate rows on repeated scans, all source hashes retained and no false `Completed` rows.
4. Run the Learning Round Council against a reachable Ollama endpoint. Verify canonical `chat.upload_workspace_files` and transport `chat_upload_workspace_files` calls both resolve to the same allow-listed function, and that nonapproved functions remain rejected.
5. Submit `localgpt.learning.maintain` with a regex candidate containing `description`; verify JSON schema acceptance and persisted model-suggested/unapproved curator status. Test the regex on actual file text before accepting its usefulness.
6. Use `localgpt.learning.files`, `localgpt.learning.file.read` and `localgpt.learning.file.review`: verify multi-chunk EOF completion, cursor persistence after process restart, unchanged-hash retention, changed-hash reopen, missing/binary blocker, cancellation, rejected premature review, and absence of source-file writes.
7. Inspect the DevExpress file checklist in `/project-maintenance` with 5,000+ tracked rows, filter all statuses and inspect long evidence texts. Check that prior pattern editors and save/scan buttons still function.
8. Confirm the Human Collaboration decision/approval gate remains a single authority: no source-write, compiler, network or human decision can be approved merely through a learning-file function.

## Known environmental limitation

The source-only preparation host intentionally has no usable .NET build environment. None of the checks above that require .NET, the database runtime, browser circuit, DevExpress assemblies or Ollama have been represented as passed.
