# LocalGPT 6.5.7 — OCR XML documentation and one-time test cleanup

## Build fix

- Fix the C# XML-documentation contract for `ResolveAvailableProviderAsync(string?, CancellationToken)` by documenting `cancellationToken`. This addresses compiler diagnostic CS1573 observed in Visual Studio after the 6.5.6 OCR provider-routing change. No provider-routing behavior is changed.

## One-time cleanup for installations upgraded by copying files over an older checkout

- Add `build/Remove-ObsoleteSourceTestsOnce.ps1`, a **manual, opt-in** cleanup tool.
- It previews by default. `-Apply` removes Python/Node source-test files and bytecode artifacts only from the repository's `tests` tree, matching the existing `Assert-RepositoryPowerShellBoundary.ps1` forbidden extensions.
- It keeps all PowerShell tests/guards, the application Python runtime bridge, other application sources, and runtime/user data untouched.
- The script does not run during build or installation. It exists to remove abandoned older test files that copying a newer ZIP over an old folder does not delete.
- There are no hard-coded release versions or test filenames in the cleanup logic.

## Validation limits

- Source review, project-XML validation, archive CRC, and file-retention checks can be performed without the .NET SDK. A .NET 10 compiler, PowerShell 5.1 or modern pwsh runtime is not available in the authoring environment; no successful Visual Studio or live OCR execution is claimed.
