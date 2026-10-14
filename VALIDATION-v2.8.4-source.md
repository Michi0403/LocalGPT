# LocalGPT 2.8.4 source validation

Source-only validation was performed without invoking dotnet, MSBuild, Visual Studio builds, or GitHub. The user's Windows build remains authoritative.

## Reported compiler diagnostics

- The malformed `await using` / local-declaration chain in `ChatUploadWorkspaceService` is removed.
- The malformed `await using` / local-declaration chain in `CodeGenerationWorkflowService` is removed.
- The corrected scopes dispose source and destination streams before subsequent destination-file reads/hashes.

## Static invariants

- LocalGPT strict async continuation Python audit: passed after repair.
- No `InvokeAsync.ConfigureAwait(... )<T>` generic-ordering pattern remains in maintained C#/Razor source.
- Existing Razor InteractiveServer render-mode directives were not changed.
- Project XML parses successfully.

## Versioning

- LocalGPT: 2.8.4.
- LocalGPTWebviewWrapper: 2.8.4.
- LocalGPTInstallerConsole: 2.8.4.
- 1-Wire protocol: 2.1.1 (unchanged).
