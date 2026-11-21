# LocalGPT 7.6.5 — Transactional Source Amendments (source candidate)

**Baseline:** 7.6.4. **PublisherStudio:** unchanged. **Validation:** source contract assertions added; no PowerShell execution, .NET compilation, runtime tests, direct online GitHub, or owner database mutation by the editor.

## Why this maintenance release exists

On 20 November, the previously broken project-generation pipeline reached real source materialization but produced only four reviewed files, missing the selected SQLite implementation and the user-specified `DotnetTwitchDashboard` solution name. The prior candidate established early manifest and final-source guards, but a draft that reached the final closure with newly discovered omissions could not *safely add or reopen* work, and a completed old review could not remove obsolete files in a successor without rebuilding unrelated source. LocalGPT 7.6.5 focuses on these precise lifecycle gaps and does not claim completed software compilation.

## Changes

- New `CodeGenerationSourceRequirement` business object with origin, requirement, mapped manifest paths, accepted source paths and explicit evidence states. Persist requirement graph on begin, submit and successor repair. This is not a proof of runtime behavior.
- New `amend` operation in registered `codegen.review.create` function schema. The active-draft transaction requires exact draft ID and manifest SHA256 and optional exact source hash for each reopened path. Only additional manifest paths or hash-fenced existing-path reopen are allowed; no model-provided source bytes or approvals.
- Enforce current consumed human decisions, path uniqueness/containment, no compiler-output paths, no path-as-directory conflicts, manifest file limits, dependency DAG checks, protected requirement coverage and a persisted bounded amendment count. Optimistic compare-and-swap persists the draft; previous accepted source receipts/history remain intact.
- Coordinator requests **only a delta** from eligible architect routes after all currently declared bytes are present but closure fails. Validated amendment restarts the existing sequential author/inspect loop. Unknown tool outcomes are not replayed; amendment retry count is bounded by source-draft policy and recorded in the draft.
- Extend explicit successor `repair` for already executed immutable reviews with exact hash-fenced obsolete path removal, retaining other reviewed bytes and insisting on a fresh successor approval. Pending/unconsumed approvals cannot be silently revised.
- Enforce exact manifest-to-reviewed-files equality, duplicate file-path rejection, solution/project-reference structural validation, selected provider in actual project declaration and source configuration, and explicit non-compiler statuses through pre-approval/execute paths.
- Fix a 7.6.4 source-level interface mismatch: immutable `CodeGenerationReviewPayload` requires its original `entity.Goal` when passed into the completed-source validation overload.
- Expand maintained `build/Assert-CouncilXRoundWiring.ps1` source assertions for the exact new enum and saved-source permission boundaries. Existing architectural/DevExpress/TextService/PowerShell guards are not relaxed.
- Extend `TASKS.md` without dropping any earlier unresolved items; add the detailed workflow contract at `maintenance/workflows/COUNCIL-SOURCE-AMENDMENT-AND-REPAIR-7.6.5.md`.

## Limitations / next work

No automatic post-approval mutation, no model-supplied build-verification claims, and no general natural-language semantic proof. Existing provider/solution-name heuristics handle the demonstrated error and selected known providers, not arbitrary natural-language requirements. The user must compile and run guards, then verify a real end-to-end approved deliverable. Parallel file generation and neural learning remain subsequent work.
