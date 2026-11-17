# LocalGPT 7.4.2 — Review first

**Exact baseline:** GitHub `main` commit `cce9574c7f73749dba1b72cc7cad97821cad3fd9` (already `7.4.1`).
**Destination version:** `7.4.2` (review only; no commits or pushes were made).

## Purpose

Codex added a persisted per-file Council source draft in 7.4.1. Its Council loop still accepted `SOURCE_DRAFT_READY` in generated model text as completion without verifying that the source draft had all files. The reverse also occurred: a fully persisted draft could keep running if the model omitted the marker. Both paths risk extra tokens, incomplete approval attempts, and false progress.

This patch uses the existing scoped `ICodeGenerationWorkflowService`, already injected into the scoped `MultiModelCouncilService`, to perform a read-only database inspection. The check is triggered for existing configured loops that expose `codegen.review.create`; it is not tied to a hard-coded team name.

- **Draft exists and is incomplete:** ignore the unverified model marker; continue the bounded existing workflow.
- **Draft exists and is complete:** finish the loop from stored file receipts even if the model omitted the marker.
- **No draft exists:** retain the previous loop behavior (for example a repository-staging workflow that does not create a greenfield source draft).
- **Database/read error:** treat completion as *unverified*, log the error, and do not claim the draft is ready.

The service method does **not** create source files, execute tools, approve anything, alter permissions, or change a model's selected team/hardware context. No new service registrations, coordinator layers or static helper methods.

## Files

- `src/LocalGPT/Interfaces/ICodeGenerationWorkflowService.cs`: one read-only interface method.
- `src/LocalGPT/Services/CodeGenerationWorkflowService.DraftReadiness.cs`: new partial of the **existing** DI-owned workflow service.
- `src/LocalGPT/Services/MultiModelCouncilService.ConfiguredWorkflowExecution.cs`: direct injected call in existing loop, with informative Chat progress.
- `src/LocalGPT/LocalGPT.csproj` and the four maintained documentation metadata files: 7.4.2 version.

The binary `.github/pages/localgpt-kawaii-docs.zip` has **not** been regenerated. Produce it through the repository documentation workflow, not a hand-edited ZIP.

## Applying

Review `review/7.4.1-to-7.4.2.diff` first. On a clean checkout of the exact baseline, from the repository root:

```powershell
git apply --check .\review\7.4.1-to-7.4.2.diff
git apply .\review\7.4.1-to-7.4.2.diff
```

**Do not apply both the patch and the complete changed files.** Those files are alternative ways to review/apply the same change. No other branches, commits or files are touched by this package.

## Local build gate

After applying the patch, run `powershell -NoProfile -File .\review\Invoke-7.4.2-Checks.ps1` in the repository root (or `pwsh -File ...` on macOS). This validates the direct DI source contract and invokes the actual .NET Debug build plus repository MSBuild architecture targets. Use `-SkipBuild` for a source-only check. The script was not executable in the packaging environment (PowerShell and .NET SDK unavailable).

## Explicit limitations

Source integrity and patch application were tested against baseline-matched files; **no .NET SDK is available in the packaging environment**, so C# compilation, provider inference, approvals and end-to-end HelloMath delivery are **not verified**. The prior large-context/GPU and Chat-rejoin defects require separate runtime testing; this change specifically repairs the persisted file-loop completion boundary.
