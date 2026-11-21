# LocalGPT 7.6.6 — Immutable Review Goal Build Repair

**Baseline:** LocalGPT 7.6.5 source candidate. **Type:** targeted compile-repair release; no other behavior redesign. **PublisherStudio:** unchanged.

## Owner-reported failure

On 2026-11-21 at 09:19:49, after the PowerShell architecture, ownership and source contracts passed, the owner's Debug build stopped with `CS1061` in `src/LocalGPT/Services/CodeGenerationWorkflowService.Execution.cs(71,73)`: `CodeGenerationReviewPayload` has no accessible `Goal` property. This is a source regression introduced by the 7.6.5 pre-approval source-target revalidation.

## Root cause and correction

`CodeGenerationReviewPayload` intentionally stores the immutable source manifest, source files, dependencies and inspection evidence, not the goal text. The authoritative user goal belongs to the persisted review entity loaded by `ExecuteReviewAsync`. The adjacent full-closure validation already uses `entity.Goal` correctly. The individual file-target revalidation mistakenly read `reviewedPayload.Goal`, causing the compiler error.

The call now passes `entity.Goal` to `ValidateDraftProjectTarget` for every reviewed source file. It retains the run-scoped consumed Human Collaboration decisions, structural and target validation, exact review hash check, user confirmation and approval-consumption ordering. No changes to persisted payload serialization, schema, database, artifact writing, build execution or saved configuration.

The existing `build/Assert-CouncilXRoundWiring.ps1` now asserts that both pre-approval validators use `entity.Goal` and rejects the invalid `reviewedPayload.Goal` expression, so this mistake is covered by the same PowerShell source-contract gate that is already used in the owner's build.

## Verification boundaries

- Source-level exact replacement, contract alignment and archive-integrity verification by the editor.
- No `.NET` compilation, PowerShell execution, network/GitHub operations, owner database access or runtime acceptance performed by the editor.
- Owner must rerun the existing source-contract checks and full build, then verify complete SQLite-backed source materialization and artifact correctness. A passing compilation does not itself establish successful application generation.

See the appended section of the cumulative `TASKS.md` for owner acceptance and still-open 7.6.5/NUTS work.
