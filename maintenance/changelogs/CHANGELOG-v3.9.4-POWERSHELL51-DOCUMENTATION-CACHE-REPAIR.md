# LocalGPT 3.9.4 — Windows PowerShell 5.1 documentation-cache repair

## Fixed

- Repairs the reproduced Windows build failure in `Save-LocalGptDocumentationHtmlCache`. The documentation cache copied each generated DocFX entry with a three-positional-argument `Join-Path` expression. Modern PowerShell accepts that through `AdditionalChildPath`, but the maintained build invokes Windows PowerShell 5.1, where only `Path` and `ChildPath` are available positionally. The first generated `api` directory therefore became an unsupported third positional argument after the complete DocFX HTML preflight had already succeeded.
- Rewrites the cache destination with nested two-argument `Join-Path` calls, preserving the same target path while remaining compatible with Windows PowerShell 5.1 and modern pwsh.
- Extends `Assert-PowerShellCompatibility.ps1` to inspect parsed PowerShell command ASTs and reject bare `Join-Path` calls with more than two positional arguments. This moves the same regression from the end of the long documentation build into the early compatibility guard.

## Preserved

- All LocalGPT 3.9.3 `/install` Setup, CanIRun.ai redirect/mapping, Ollama lifecycle, model-installation, packaged-runtime path, logger containment, and release-source-identity repairs remain unchanged.
- DocFX HTML generation, accessibility/local-link preflight, durable HTML caching, PDF generation/resume, and publication behavior remain unchanged apart from the PowerShell 5.1-compatible cache path construction.
- `@rendermode InteractiveServer` placement and hosted-service ownership are unchanged.

## Version

- LocalGPT: `3.9.4`
- LocalGPTInstallerConsole: `3.9.4`
- LocalGPTWebviewWrapper: `3.9.4`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)
- LocalGPT.ReleasePackaging: unchanged (`1.0.2`)

## Validation boundary

The supplied Windows build log is the authoritative reproduction: the C# project compiled and DocFX produced 1,163 models with zero warnings/errors, then Windows PowerShell 5.1 failed while saving the validated HTML cache on the generated `api` entry. This environment does not provide `powershell`, `pwsh`, or `dotnet`, so 3.9.4 is validated here with source/static regression checks rather than a claimed Windows runtime build.
