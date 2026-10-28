# LocalGPT 6.6.1 — restore PowerShell-only Council validation

Base: complete LocalGPT 6.6.0 source archive. PublisherStudio is unchanged.

## Build-blocking corrections

- **Removed** `tests/static/test_detachable_council_backend_660.py`. It violated the repository's PowerShell-only source-test policy. The existing build-wired `build/Assert-RepositoryPowerShellBoundary.ps1` would reject this Python test during Windows and other full builds. The removed test never belonged in the 6.6.0 source package.
- Extended the existing **version-independent** `build/Assert-CouncilXRoundWiring.ps1` to check Council backend launch before the first observer yield, observer-only cancellation, bounded observer buffers, live-session persistence/completion, explicit native Stop forwarding to server cancellation, all seven supported Windows/Linux/macOS runtime IDs, and retention of the classic and declarative game files. No duplicate or version-suffixed test script was added.
- Wired that existing Council guard into the mandatory `AssertLocalGptNativeArchitectureMaintenance` target in `Directory.Build.targets`, so normal builds execute it; `build/Invoke-RepositoryValidation.ps1` already invokes that guard and is unchanged. The PowerShell-only repository boundary gate remains enabled and unchanged.
- Reconciled the existing `build/javascript-diagnostics-files.sha256` with **two** already-modified frontend files: `localgpt-chat-ui.js` and `localgpt-game-studio.js`. The stale hashes in 6.6.0 would cause a second independent Windows build validation failure. Both browser files themselves are **unchanged** by this repair.
- Bumped the LocalGPT application version **6.6.0 → 6.6.1**; single-digit minor/patch versioning preserved.

## No application behavior changes

The Council backend/observer separation, team configuration, project/knowledge/regex engine, native stop handler, game scenes/renderers, and Windows/Linux/macOS runtime code are **identical to 6.6.0**. No packaging paths, installer logic, native platform integrations, or PublisherStudio files changed.

## Verification and limitations

- ZIP contents and CRC, XML parse, absence of Python source tests/bytecode under `tests`, all mandatory script path references, unchanged runtime sources, project runtime identifiers, and normalized JavaScript SHA-256 manifest are checked in the offline packaging environment.
- JavaScript syntax is checked using available Node.js, without changing any browser JavaScript.
- **PowerShell is not installed in this environment**, so the new PowerShell assertions were not executed here. **No dotnet build, real browser runtime test, GitHub operation, or Windows/Linux/macOS installer run** was performed. Source-level checks do not establish a successful Windows build.
