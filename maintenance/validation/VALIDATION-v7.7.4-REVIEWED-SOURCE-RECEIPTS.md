# LocalGPT 7.7.4 validation / host test matrix

The source archive is not compiler-verified in this environment. On Windows, in the repository root:

```powershell
pwsh -NoProfile -File ./build/Assert-ReviewedArtifactReceipts.ps1
pwsh -NoProfile -File ./build/Assert-CouncilXRoundWiring.ps1
./Build-LocalDevelopment.ps1
```

1. Create an exact source-file review with `codegen.review.create`, approve via LocalGPT, and execute `codegen.review.execute` **without** a build. Verify the resulting ZIP is present and `council.development.artifact.verify` returns `SourceOnly`, `Succeeded=false` and the download URL.
2. Repeat with a coordinated source draft. Both direct and draft reviews must reject a ZIP whose reviewed file bytes differ from the immutable SHA-256 or whose path is missing or duplicated.
3. Repeat under a project-selected workspace different from the application-data directory with the CouncilArtifacts folder initially absent: packaging must create it automatically.
4. With an actually successful registered compiler and explicit build approval, the same verifier must return successful `ReviewedGeneration` with `BuildPassed`. Failed/no-toolchain builds must never report successful verification.
5. Re-run the TinyGreeter goal using the small team/prompt in the release summary. A reviewer-only run or model prose claiming a ZIP is **not** acceptance evidence.
