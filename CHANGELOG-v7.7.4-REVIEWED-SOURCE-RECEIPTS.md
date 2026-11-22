# LocalGPT 7.7.4 — verified source-only CouncilArtifacts receipts

Baseline: supplied 7.7.3 repository. No GitHub or .NET SDK used in this revision.

## Fixed

- `council.development.artifact.verify` now requires consumed human approval for **every** reviewed generation, not only source-draft reviews. Its immutable per-file SHA-256 check covers both direct `codegen.review.create` reviews and coordinated source drafts. Missing, duplicated and modified file entries are rejected.
- Non-compiled source archives now return a distinct `SourceOnly` **non-success** receipt after the verified file checks, including direct reviews. `Generated`, `BuildFailed`, `BuildDisabled`, and `BuildNotAttempted` cannot be mistaken for a successful .NET build. A real `BuildPassed` remains required for successful development verification.
- Approved project-bound code generation creates the application-owned `CouncilArtifacts` directory before writing its ZIP, even when the working tree is stored under a different project root.
- Add a PowerShell architecture regression guard and wire it into the existing Council X-round validation chain.

## Not changed

- No compiler was invoked, no AI model routes, provider timeouts, human approvals, tool schemas, code templates or Council scheduling policies were silently changed.
- Existing verified-build and repository-staging flows remain separate from source-only receipts.
- Failed/never-invoked generation cannot be repaired by verification alone: `codegen.review.create`, explicit user approval, and `codegen.review.execute` must still occur before a ZIP exists.

## Validation

- Source-focused checks and packaging inspection only; this environment has no `dotnet` or `pwsh`. Execute `pwsh -NoProfile -File build/Assert-ReviewedArtifactReceipts.ps1` and the normal repository validation on the maintainer's Windows machine before classifying this release as compiler-verified.
