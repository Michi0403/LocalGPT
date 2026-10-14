# LocalGPT 4.7.1

## Build-policy repair

- Fixes the Windows build failure from `Assert-SystemVariableInitialization.ps1` in `WorkspaceVisionOcrService`.
- Preserves the same normalized Ollama base-address semantics while moving the string-bearing normalization outside the `new Uri(...)` constructor expression.
- Does not expand the system-variable baseline or weaken the initialization guard.
- Preserves all 4.7.0 repository ingestion, review-gated Knowledge/regex delta learning, OCR, PublisherStudio capability routing, drag/drop and controller-mode behavior.

## Validation scope

Source-only validation is used in this environment. No GitHub access, `dotnet`, restore, NuGet, MSBuild, build, publish or installer execution is used.
