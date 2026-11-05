# LocalGPT 3.9.5 — Debug documentation PDF contract repair

## Fixed

- Fixed the reproduced normal `Debug` build failure after successful DocFX HTML generation. `Directory.Build.targets` intentionally enables `-RequirePdf` only for Release builds, but `Build-Documentation.ps1` still performed an unconditional final source-tree PDF assertion. That contradictory contract made an otherwise valid HTML-only Debug build fail with `Embedded LocalGPT documentation PDF was not published into the runtime help-docs tree`.
- The embedded PDF validation now runs only when `-RequirePdf` was requested or a complete PDF was actually generated. Debug builds therefore publish the validated HTML/API documentation and accurately report that the standalone handbook is unavailable, while Release builds still require, validate, size-check, publish, and record the complete PDF.
- The repair uses only Windows PowerShell 5.1-compatible language/cmdlet constructs and is platform-neutral for Windows, macOS, and Linux. No PowerShell 7-only syntax or Windows-specific path assumption was introduced.

## Preserved

- The 3.9.3 `/install` runtime setup, CanIRun.ai redirect/model mapping, Ollama lifecycle/model-install workflow, packaged macOS runtime/logging repair, and release source-fingerprint contract remain unchanged.
- The 3.9.4 nested two-argument `Join-Path` cache repair and its PowerShell compatibility guard remain in place.
- Release documentation continues to require the complete embedded versioned PDF; this change does not weaken Release packaging validation.
