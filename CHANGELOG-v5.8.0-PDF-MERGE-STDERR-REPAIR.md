# LocalGPT 5.8.0 — Windows PowerShell PDF helper integration repair

## Fixed

- `LocalGPT.ReleasePackaging pdf-merge` now writes successful compression and merge verification messages to standard output, not standard error. Standard error remains reserved for actual failures or rejected optional optimization candidates.
- `Build-Documentation.ps1` captures the merge helper’s stdout/stderr under a temporary non-terminating error preference and evaluates `$LASTEXITCODE` and the final PDF independently. This avoids Windows PowerShell 5.1 treating redirected native diagnostic stderr as a terminating `NativeCommandError` when the helper has succeeded.
- The final `pdf-verify` invocation uses the same scoped handling, so a future helper diagnostic cannot trigger the same regression during documentation validation.
- PowerShell's prior error policy is restored in `finally`; failures from non-zero process exits or invalid/missing PDFs still fail the build, and durable chunks remain available for retries.

## Versions

- LocalGPT 5.7.9 → **5.8.0** (single-digit middle/patch version rule).
- LocalGPT.ReleasePackaging 1.0.9 → **1.1.0**.
- Vendored PDFsharp remains 6.2.4 MIT / .NET 10; PublisherStudio unchanged.

## Evidence and validation limitations

The supplied Windows Debug log shows successful helper output: `PDFsharp merge verified 134 parts, 5966 pages, 206952510 bytes.` The build stopped at the PowerShell invocation despite the successful merge, consistent with Windows PowerShell 5.1's redirected stderr behavior under `$ErrorActionPreference = 'Stop'`.

Static review and ZIP integrity were performed without invoking .NET or GitHub. Re-test the Debug build on Windows with the same documentation inputs and confirm the merge + final `pdf-verify` + documentation artifact publication all finish successfully.
