# LocalGPT 5.5.4 — Text-Service Ownership Repair

## Confirmed build blocker

Visual Studio reported `Assert-TextServiceOwnership.ps1` rejecting one direct string manipulation in `src/LocalGPT/Components/Pages/DataManagement.razor`:

`column.Type.Contains("INT", StringComparison.OrdinalIgnoreCase)`

The previous release's source-only audits had not executed that exact build-wired PowerShell text ownership contract.

## Fix

- Moved the entire read-only editor-column decision into the already injected `SqliteGridPresentationService.IsReadOnlyEditorColumn(...)`.
- Retained the original protection semantics: read-only tables stay protected; existing-row primary keys cannot be edited; integer-primary-key values cannot be manually entered when creating a row.
- Service-local error handling logs a diagnostic and returns **read-only** when classification unexpectedly fails.
- Left `Assert-TextServiceOwnership.ps1`, its baseline, all DevExpress controls, the data services and approval workflows intact. No suppression or architectural exception was introduced.
- Added a dedicated 5.5.4 source-only audit covering the service-owned classification and mirroring the Windows PowerShell gate's scanning rules across Components/Controllers.
- Advanced version and UI cache identifiers from **5.5.3** to **5.5.4**; both minor and patch components remain single digit.

## Unchanged functional scope

The 5.5.3 DevExpress Data Management workbench, SQLite editor and preference service CRUD/paging, Council model-health actions, model thinking inspection, InteractiveServer registration, Install/CanIRun.ai fixes, learning checklist and documentation recovery stay as delivered.

Only LocalGPT changed. PublisherStudio requires no new archive.
