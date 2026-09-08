# LocalGPT 3.9.4 source validation

This patch is prepared under the maintained source-only boundary: no `dotnet` build/publish and no PowerShell release build are run in this environment.

## Reproduced failure addressed

The supplied Windows build reaches a successful LocalGPT compile and successful DocFX generation/preflight, then fails in `Save-LocalGptDocumentationHtmlCache` with `PositionalParameterNotFound` for the argument `api`. The failing cache copy used `Join-Path $temporary 'site' $entry.Name`, which relies on the PowerShell 6+ `AdditionalChildPath` behavior even though `Directory.Build.targets` invokes Windows PowerShell 5.1 through `powershell.exe`.

## 3.9.4 regression checks

The source-specific validation verifies that:

- all three shipped executable projects report `3.9.4`, with one-digit version slots;
- `Save-LocalGptDocumentationHtmlCache` constructs each cached entry destination with nested two-argument `Join-Path` calls;
- the previous three-positional-argument `Join-Path $temporary 'site' $entry.Name` expression is absent;
- `Assert-PowerShellCompatibility.ps1` parses repository PowerShell scripts and rejects bare `Join-Path` commands with more than two positional arguments, explicitly covering Windows PowerShell 5.1's lack of `AdditionalChildPath`;
- all 3.9.3 Setup/CanIRun/Ollama/release-identity regression checks remain present through the inherited release audit;
- no InteractiveServer or hosted-service registration count changes are introduced.

## Static validation boundary

The final tree is run through the release-specific Python audit and the repository's maintained architecture, async-continuation, cross-platform, code-generation/DXFunction, configurable-behavior, provider-stream, X-Round, provider-qualified Council, SQL-seed, service-resilience, and XML/Razor documentation audits that can execute without .NET or PowerShell.

Passing those checks is source/static validation, not a claim that the Windows PowerShell 5.1 documentation pipeline was executed here. The next Windows build is the authoritative runtime confirmation of this exact compatibility repair.
