# LocalGPT 3.9.1 source validation

This release is prepared under the requested source-only constraint: no `dotnet` build/publish and no PowerShell release build are run in this environment. The user's supplied macOS release log is treated as authoritative evidence for the 3.9.0 failure; local validation below is deterministic source/static validation only.

## Build failure reproduced from source

The 3.9.0 release log reaches the RID-neutral LocalGPT build and fails because `build/Assert-TextServiceOwnership.ps1` exits with code 1. A source-parity implementation of that guard identifies exactly two new 3.9.0 violations, both in `Components/Shared/InitialSetupAssistantPanel.razor`:

- `profile.Key.StartsWith("ollama-", StringComparison.OrdinalIgnoreCase)`
- `item.Key.StartsWith("ollama-", StringComparison.OrdinalIgnoreCase)`

3.9.1 removes both component-owned string operations and delegates classification to `IAiProviderBootstrapService.IsOllamaProfile(...)`, using the existing implementation in `AiProviderBootstrapService`. The guard baseline itself is not changed.

The same follow-up parity review found one next-stage `Assert-SystemVariableInitialization.ps1` violation introduced by 3.9.0: the literal `"application/json"` appeared inside the new `StringContent` constructor. 3.9.1 stores that fixed protocol media type as `JsonMediaType` and passes the constant to the constructor; that guard and its baseline are also unchanged.

## Executed source checks

The final 3.9.1 tree was checked without .NET/PowerShell execution:

- application architecture audit: passed;
- cross-platform boundary audit: passed with 22 checks;
- async continuation audit: passed for 259 source files, 3,019 await tokens, 2,663 `ConfigureAwait(false)` uses, 135 renderer-affine `ConfigureAwait(true)` uses, 216 explicitly configured async disposals, and 5 configured async streams;
- code-generation/DXFunction wiring audit: passed;
- provider-qualified Council audit: passed with 282 checks;
- provider stream repetition policy audit: passed;
- X-Round wiring audit: passed;
- Council SQL seed audit: passed with 60 executable current-schema `INSERT OR IGNORE` rows and verified second-run idempotency;
- service resilience audit: passed for 2,209 service methods;
- XML documentation coverage: passed for 10,330 direct C# declarations across 656 maintained source files and 784 direct Razor members across 45 component types;
- text-service ownership PowerShell-guard parity scan: zero unbaselined direct component/controller string/regex operations;
- system-variable initialization PowerShell-guard parity scan: zero unbaselined constructor/system-variable initialization violations;
- direct source count: 15 established `@rendermode InteractiveServer` boundaries remain present;
- direct registration count: 8 `AddHostedService<T>` registrations remain present.

## Version and packaging policy

- LocalGPT, LocalGPTInstallerConsole, and LocalGPTWebviewWrapper are `3.9.1`.
- Documentation metadata, browser asset cache query, and CanIRun user agent are `3.9.1`.
- The one-digit minor/patch rule is respected.
- Source packaging excludes `bin`, `obj`, `__pycache__`, `.pytest_cache`, `.pyc`, and `.pyo` content.

Passing these checks is source/static validation and is not a claim that 3.9.1 was compiled in this environment. The next authoritative build result remains the user's normal `pwsh Build-Release.ps1` run.
