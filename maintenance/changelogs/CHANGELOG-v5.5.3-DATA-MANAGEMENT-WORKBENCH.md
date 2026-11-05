# LocalGPT 5.5.3 — Data Management DevExpress Workbench

## Fixed

- Replaced the sprawling table/column grids that rendered dozens of database entities and extremely wide records in one page with a compact workbench.
- Reused the established vertical business-area navigation and added a scrollable `DxTreeView` for entity tables and schema-derived child relationships. Selecting a node opens the table in the same interactive server circuit.
- Split record selection and editing with a resizable `DxSplitter`: a bounded `DxListBox` of human-readable record identities, a narrow optional two-column `DxGrid` preview, and a scrollable form editor in `DxFormLayout`.
- Used the existing typed DevExpress controls for long text, number, date/time, Boolean, secret and ordinary fields. Kept primary-key protection for existing records and EF migration-metadata write protection.
- Kept New, Save and confirmed Delete visible while editing. A changed field with a persisted `RequireHumanApproval` setting now requires an additional explicit confirmation before saving.
- Added Previous/Next navigation to all records rather than limiting record access to the first loaded page. The existing `ISqliteTableEditorService` validates table/column identities and clamps page sizes to the runtime limit; the new page methods use SQL-bound `LIMIT`/`OFFSET` parameters and preserve the original service entry points.
- Made the existing EF-backed `ISqliteEditorPreferenceService` editable from the workbench: field, control type, sensitive-value handling and protected-field confirmation. Changes are separately confirmed and retain the existing mask/format/null display settings.
- Preserved the Council model-health exclusion list and its existing `IHumanCollaborationService` release action, using a compact selection list rather than a broad grid.

## Preserved

- InteractiveServer without prerendering for `/data-management`, with the reviewed route registry intact.
- All 5.5.2 `/install` dedicated-VRAM and CanIRun.ai scoring fixes.
- Existing graphical thinking inspection, Human Collaboration approval ownership, and the per-file learning/checklist screen.
- Existing dynamic table catalog, relationship discovery, persistence service protections, logging, notification, six localization catalogs, strict component retention policy and source architecture checks.
- The 5.5.0 documentation PDF merge and recovery behavior.

## Source maintenance

- Advanced LocalGPT version from **5.5.2** to **5.5.3**, including UI asset version markers. The second and third version components remain single-digit.
- Introduced `build/audit_data_management_workspace_5_5_3.py` to defend compact layout, pagination, service-owned CRUD, user confirmation and locale parity.
- Kept the historical audit contracts active, updating version assertions only where required; registered new renderer-affine handlers explicitly in the existing async policy.
- **PublisherStudio unchanged; no PublisherStudio ZIP is needed.**

## Validation constraint

Static Python source audits were run; no .NET runtime, restore, build, DevExpress compilation, Windows PowerShell or GitHub access was used. User-side compilation and interactive verification remain necessary.
