# LocalGPT 3.2.3 changelog

## User-defined AI Functions

- Added a simple JSON/OData mode to the existing `UserDxFunctionEditor`.
- The form captures runtime/display names, source kind, URL, optional selector, headers, timeout, payload bound and HTTP policy alongside the existing AI/automatic-invocation policy.
- Source functions are persisted through deterministic `user-source.*` Remote Control connector/pipeline adapters. This reuses existing REST/OData pull, host allow-list, selector, network-policy and persistence behavior.
- A zero-action source pipeline returns its connector payload directly when invoked as a user DXFunction.
- Existing generated source functions reopen in the simple editor; generated adapters are cleaned up when no longer owned.
- The existing advanced pipeline-backed user-function workflow remains available.

## Automated X Functions frontend

- Added direct **X Functions & automation** navigation from the DX Functions catalog.
- Council Teams now gives the existing workflow/X-Round controls a stable `x-functions-automation` target and clearer user-facing naming.
- The existing X-Round engine remains authoritative for enablement, automatic/native policy, transition budget, revisit, text return, one-model/child-Council work and human approval.

## Learning Round project persistence

- Added `ILearningProjectWorkspaceSyncService` / `LearningProjectWorkspaceSyncService`.
- Learning maintenance synchronizes repository-shaped content from the current or selected chat upload workspace by default.
- Source synchronization creates/updates existing LocalGPT project records instead of generic Learning Round placeholders and persists exact repository version, current version record, source-backed revision, source root, solution path and repository snapshot hash.
- The existing project-scoped workspace-root record is prefilled with the extracted chat repository root as read-only source evidence.
- Every maintained repository file is represented as a tracked file for the synchronized revision, with normalized path, absolute path, role, hash, size and source metadata.
- Revision `ProjectStructureJson` records the complete source manifest plus SDK/framework/workspace metadata for transparent review.
- Source requirements are derived from `global.json` and project target frameworks; obsolete repository-derived requirements are marked superseded/historical and no longer approved as current.
- Learning snapshots/results now expose project counts and synchronized project/version/workspace/file-count information so the Council can state exactly what was persisted.

## Requirement grounding and recovery warning

- Project briefings now explicitly ground SDK/runtime/framework/version/structure in selected revision, tracked files and inspected chat uploads; absent facts must remain absent rather than being guessed.
- Repository policy for Learning Rounds explicitly rejects invented .NET 7/8 requirements when the source declares .NET 10.
- Normalized nullable `xRoundCause` at the recovery call boundary, resolving the reported CS8604 warning without changing X-Round semantics.

## Localization and stability

- Added/updated LocalGPT localization entries across de-DE, en-US, es-ES, fr-FR, ja-JP and uk-UA for the new user-function/X-automation surfaces and repaired remaining User DXFunction English fallbacks.
- `Chat.razor` and `Chat.razor.css` are unchanged from 3.2.2.
- No EF migration/schema change was added.
- No GitHub access and no .NET build were used while preparing this source archive.
