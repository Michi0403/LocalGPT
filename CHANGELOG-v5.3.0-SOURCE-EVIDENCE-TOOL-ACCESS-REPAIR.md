# LocalGPT 5.3.0 — source evidence and Council tool-access repair

## Why this release exists

The 2026-10-04 Learning Round exposed three cooperating regressions: uploaded ZIPs stayed as quarantined originals with an empty `extracted/` tree until a later promotion path; attachment DXFunctions encouraged tiny prefix reads instead of complete/progressive evidence consumption; and ordinary Council workflow function lists were interpreted as exclusive runtime tool catalogs. Together those behaviors made models discuss source that was already available locally instead of inspecting it.

## Upload and archive evidence

- A user-supplied ZIP is still written to `original/` as quarantined evidence, but LocalGPT now immediately performs bounded **read-only safe extraction** into `extracted/<archive>/` during upload-workspace creation.
- ZIP extraction preserves the existing entry-count, per-entry byte, total extracted-byte, sanitized-relative-path and workspace-root containment checks. Unsafe entries are skipped and recorded as warnings.
- Extraction uses create-new semantics and skips duplicate archive destinations instead of overwriting an earlier extracted path.
- Read-only extraction is explicitly not promotion or trust: no extracted content is executed, built, installed, published or written back to the repository. Existing promotion and human-approval policy remains separate.
- Extracted source files are represented as source-backed file evidence rather than duplicated wholesale into `context.md`; large files stay directly readable through the workspace file function.
- Workspace manifests report `QuarantinedReadOnlyExtractionReady` only when an archive actually opened and its bounded safe extraction completed.

## Complete and progressive file reading

- `chat.upload_workspace_file` now reads text progressively using a character offset and returns `CharacterOffset`, `CharactersReturned`, `HasMore` and `NextOffsetCharacters`.
- `chat.upload_workspace_context` now has the same continuation contract instead of being permanently tied to its first prefix.
- Both DXFunction schemas use substantial reads: 64,000 characters minimum, 250,000 default and up to 1,000,000 characters per call. A model-requested 2,000-character read therefore cannot silently become the completion contract.
- Learning instructions explicitly require continuation until `HasMore == false` when the requested file must be read completely. A partial segment cannot support a whole-file claim.
- For repository exports that also have a ZIP, the safely extracted file tree is preferred over repeatedly forcing a 100+ MB flattened source dump through model context, while the large text upload remains directly and progressively readable.
- The old Council DXFunction result default of 32,000 characters would have truncated the larger reads before the model could consume them. The maintained default is now 1,100,000 characters, and database initialization upgrades only the untouched historical `32000` value; user-edited values remain authoritative.
- Text-gateway continuation evidence now scales with the selected model's effective context instead of using one fixed 24,000-character ceiling.

## Source-backed learning

- Safely extracted repository-shaped content is explicitly classified as **source-backed local evidence** while it remains quarantined and non-executable.
- The Learning Round inventories original uploads and safely extracted files before interpretation and tells members to use the preserved source tree when it represents the same repository as a flattened export.
- Study and verification can use that source-backed workspace evidence immediately. During the final learning-maintenance stage, repository-shaped evidence is passed to `localgpt.learning.maintain` with project-structure synchronization enabled so the existing database-first project/version/revision/tracked-file model can be refreshed without requiring project promotion or execution.
- Existing LearningBase/project/Knowledge/regex/log/memory functions are expected to be used when relevant; imported local project knowledge is no longer hidden merely because a Council template named a narrower preferred function set.
- The maintained Council seed version is advanced so unchanged built-in teams receive the corrected Learning Round instructions while existing user-customized teams remain governed by the normal seed-preservation behavior.

## Council DXFunction policy

- Outside live game runtimes, `TeamAllowList` and `ExactAllowList` are now **guidance/preferences**, not capability starvation. When a workflow step enables automatic/native functions, the complete registered policy-approved LocalGPT DXFunction catalog remains available and the model may select the function required by context or an explicit user request.
- Existing per-function safety policy, availability, automatic-invocation eligibility, deferred approval, human confirmation and consequential-action gates remain authoritative.
- Teams with any `games.*` runtime class retain strict team/step allow-list semantics. Game sessions therefore remain intentionally constrained while ordinary productivity, learning, maintenance and research Councils regain the LocalGPT function surface.
- Council Teams UI wording now reflects the guidance-vs-game-restriction distinction instead of presenting every configured function list as an exclusive runtime policy.

## Additional consistency repairs

- Upload-processing UI/advisor text now acknowledges safe read-only extraction without confusing it with promotion.
- Project-ingestion DXFunction descriptions no longer claim that quarantined evidence cannot already have a safe read-only extraction.
- Progressive read result models/interfaces document continuation ownership.
- No maintenance/build script was changed for this repair.

## Unchanged boundaries

- No uploaded code is executed merely because it was extracted.
- Promotion, command execution, builds, installs, publishing, external writes and other consequential operations keep their existing safety/approval requirements.
- No live game runtime restriction was relaxed.
- No GitHub/online repository access or .NET build was used while preparing this source release.
