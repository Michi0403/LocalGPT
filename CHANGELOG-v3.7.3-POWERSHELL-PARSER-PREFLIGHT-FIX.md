# LocalGPT 3.7.3 — PowerShell parser preflight fix

Version advanced from 3.7.2 to 3.7.3 because release packaging scripts changed.

## Fix

- Fixed two invalid PowerShell interpolations in Apple notarization error paths by delimiting variables before a literal colon (`${SubmissionId}:` and `${ArtifactPath}:`).
- The existing repository-wide PowerShell parser preflight now accepts `NativeReleasePackaging.ps1` instead of aborting before the release build.
- Added static release-audit coverage that rejects future unbraced variable-plus-colon interpolation regressions in maintained PowerShell scripts.
- Replaced the release-complete message's hard-coded product version with the resolved project version to reduce future version-maintenance drift.
- No signing, notarization, PDF compression, self-contained Full packaging, cache/output-root, RPM, render-mode, or runtime behavior was otherwise changed.
