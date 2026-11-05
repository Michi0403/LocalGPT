# LocalGPT 2.3.12

## GitHub Pages build wiring

- Corrected the repository-relative Pages archive path used by the post-documentation MSBuild target.
- The previous expression could concatenate the repository directory and `.github` into `src.github`, causing an otherwise successful application/documentation build to fail only during snapshot seeding.
- Automatic Debug/Release snapshot seeding remains enabled and still validates the exact documentation output produced by the current build.

## Diagnostics cleanup

- Reused the service `_logger` field in the generated resilience catches for `GetFunctions` and `BuildPromptBriefing`, removing CS9124 without changing logging policy.
