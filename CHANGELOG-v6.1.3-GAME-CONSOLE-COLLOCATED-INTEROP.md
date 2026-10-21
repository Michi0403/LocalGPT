# LocalGPT 6.1.3 — ChatGameConsole owned JavaScript module

- Turn the existing `localgpt-game-console.js` into the actual `ChatGameConsole.razor.js` ES module, collocated with its component. Its state machine and rendering functions are otherwise preserved.
- Stop loading that console script globally from `App.razor`; the component imports its own JavaScript module after interactive render and invokes exported functions through `IJSObjectReference`.
- Dispose the owned module in the existing `IAsyncDisposable` boundary with diagnostics. The existing shared `localgpt-game-studio.js` renderer remains global because the separate Game Studio page also calls it; no cross-component runtime is duplicated.
- Extend the existing PowerShell diagnostics hash coverage to collocated `.razor.js` modules instead of removing diagnostic protection with the relocation.
- This does not modify Council/model, session, persisted data or Service behavior, or claim a complete repository-wide architecture rewrite. No runtime compilation/DevExpress test is available in this container.

## Windows and cross-platform release version routing

- `Build-Release.ps1` derives the application version exclusively from `src/LocalGPT/LocalGPT.csproj` and now rejects minor/patch values outside the project's single-digit versioning convention.
- Windows application, setup, and optional WebView-wrapper profile publications override only the publish-output location to a version-scoped staging tree under the selected release root; Unix staging uses the same version-scoped convention. An inherited `FUTURE2_RELEASE_OUTPUT_ROOT` or `-ReleaseOutputRoot` that points to an old semantic-version directory fails immediately rather than silently nesting a newer release beneath the older version.
- Every Windows app/setup payload is stamped with `RELEASE-VERSION.txt` and source SHA-256; the application's managed assembly semantic version must match the declared application release. The separate installer executable retains its own independent component assembly version.
- ZIP root folders include the exact current application release and runtime (`LocalGPT-<version>-<rid>` / `LocalGPT-Setup-<version>-<rid>`). ZIP construction reads the archive back and rejects a missing/mismatched `RELEASE-VERSION.txt` entry before committing a filename. Existing outer archive asset names and versioned bundle directory convention remain unchanged for integration compatibility.
- The release script leaves prior completed version directories intact and refuses to misidentify them as output from a new build. It does not destructively rename or delete user archives from older releases.

Release numbering: 6.1.2 was released separately by the user, so this unchanged functional source is reissued as 6.1.3. No independent changes from that 6.1.2 checkout are claimed.
