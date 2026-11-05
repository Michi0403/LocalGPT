# LocalGPT 5.5.1 — Data Management & Council inspection and recovery

## User-facing additions

- Added a dedicated **Data Management** navigation item (`/data-management`) and an InteractiveServer **DevExpress** workbench. It groups the *actual* persisted SQLite tables into Council/models, human approvals/health, projects/workspaces, conversations/logs, knowledge/prompts and remaining system data. No hardcoded table allow-list hides an unrecognized entity.
- Reused the established vertical `ConfigurationWorkbenchNav`, `DxFormLayout`, `DxGrid`, `DxComboBox`, `DxSpinEdit`, `DxMemo`, `DxDateEdit`, `DxTextBox`, `DxCheckBox`, `DxButton`, `WorkbenchHeader` and form-layout route ownership. DevExpress retention now protects the new page with a **35-control minimum**, rather than overlooking new controls.
- Discovers real parent/child relationships using read-only SQLite `PRAGMA foreign_key_list`, displays outgoing and incoming links, and filters by the selected row's actual key. Matching rows are queried by the existing table-editor service with a validated column, parameterized value and bounded result count. The normal table editor remains available at `/database`.
- Allows inspection, insertion, update and explicitly confirmed deletion through the **existing** `ISqliteTableEditorService`, preserving SQLite constraints and the retained per-column editor-kind preferences. Database migration history is inspectable but not editable. Core-domain user actions can also use their owning services; model-health releases do so explicitly.
- Localized the new navigation and workbench in all six maintained UI cultures. The already-present durable **per-file learning checklist** remains reachable from Data Management through Project Maintenance.

## Council fixes

- Fixed the repeat appearance of “excluded from this Council run by a previously approved one-run model-health decision” when an older low-risk approval had been stored with non-consuming reuse. An approved `council.model.exclude-next-run.*` decision is **always consumed after one authorized use**, regardless of a legacy reuse flag; new health decisions enforce `ExactRequestOnce` and `ConsumeApproval=true`. Other Human Collaboration approval scopes are unchanged.
- Removed the general inbox's 200-request page limit from the Council's health exclusion lookup. The health service provides all active approved decisions and lets a trusted local operator release one exact decision without deleting its audit history. A release is not an automatic approval for future operations.
- Fixed graphical parallel Council lane inspection: completed cards no longer collapse over their own inspection action; their visible DevExpress button toggles lazy rendering of the completed answer and provider/thinking stream. Running lanes remain immediate, and the ordered transcript and ASCII presentation remain unchanged.

## Maintenance and compatibility

- Bumped **LocalGPT 5.5.0 → 5.5.1** under the single-digit minor/patch policy. PublisherStudio and the unchanged release-packaging helper retain their existing versions.
- Added regression contract `build/audit_data_management_5_5_1.py`. Explicit renderer-affine `ConfigureAwait(true)` helper method names are recorded in the existing policy without relaxing its general rule. Added one controlled retention baseline entry and raised the navigation's protected DevExpress count.
- Preserved the 5.5.0 ordered UTF-8 documentation PDF input-list merge, durability/recovery behavior and document-generation requirements. Existing PowerShell maintenance guards, DI boundaries, database migrations, authorization behavior and required PDF validation remain active.

## Verification boundary

Passed source-only checks for Data Management/Council, per-file checklist, documentation PDF merge, all-six-culture localization parity, DevExpress native-control guard, DevExpress retention tag counts, Razor layout, async continuation policy, async-only architecture, service resilience, runtime text ownership, application architecture and portable PowerShell paths. The maintained Windows PowerShell and .NET/DevExpress build guards cannot be executed in this environment; **build/runtime correctness must still be verified by the maintainer using the existing .NET 10 + DevExpress setup**. See `VALIDATION-v5.5.1-source.md`.
