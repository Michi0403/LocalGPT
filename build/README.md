# Build validation tools

The public source package intentionally contains only non-destructive validation helpers:

- `Assert-SourceFormatting.ps1`
- `Assert-SecurityPolicy.ps1`
- `Assert-ProtectedRepositoryFiles.ps1`
- `Assert-CSharpSyntax.ps1` (Roslyn grammar parse; no NuGet restore required)
- `Assert-ComponentSafety.ps1` (top-level logger/notifier/activity injection and global error-boundary contract)
- `Assert-InteractiveServerRenderModes.ps1` (reviewed InteractiveServer page/island boundaries and App shell composition)
- `Assert-AsyncContinuationPolicy.ps1` (monotonic ConfigureAwait(false) coverage with reviewed renderer-affine exceptions)
- `Invoke-RepositoryValidation.ps1` (guards, restore, Debug build, Release build, fingerprinted success stamp)
- `New-VerifiedSourcePackage.ps1` (refuses stale or missing compiler evidence)
- `RepositoryValidation.Common.ps1`
- `Protect-GovernanceFiles.ps1` (optional, owner-run local read-only hardening)
- `Audit-Dependencies.ps1`

Historical one-click scripts that downloaded software or repositories, changed user settings, generated certificates, deleted directories, started localhost services, pulled model collections, published releases, or pushed Git state are not shipped. Use the documented owner-side release process and enter each consequential command manually after review.

`Install-OllamaLocalGPTAndModels.ps1` remains only as a fail-closed compatibility notice and performs no installation.

The protected governance set is readable by repository tools but must not be edited by automated agents. Its reviewed contents are pinned in `protected-files.sha256`; the source-hygiene workflow verifies the manifest. The optional protection script changes filesystem write attributes only when the human owner runs it.

## Required build/package path

```powershell
./build/Invoke-RepositoryValidation.ps1
./build/New-VerifiedSourcePackage.ps1 -Version "0.1.4"
```

Do not make release ZIPs by hand. A missing SDK, licensed feed, or workload is a failed release gate, not permission to replace compilation with structural checks.
- `Assert-ComponentSafety.ps1` verifies top-level component safety injection, routed error boundaries, notification-to-memory wiring, UI-operation safeguards, and bounded AI UI awareness.
- `Assert-WorkflowContracts.ps1` rejects known navigation, shared-contract, nullability, streaming, and swallowed-workflow regressions.

- `Assert-HumanCollaboration.ps1` verifies the ambient identity/authority split, main-frame inbox, exact controller and DXAI gates, persistence migration/snapshot, non-blocking council heartbeat, and restricted trusted-capability usage.

## Database-first iteration ledger

- The current `CHANGELOG-v0.1.4-service-lifecycle-debug.md` and `docs/OPEN_TASKS.md` are the canonical unresolved-work ledger.
- Never remove or silently mark an open item complete. Close it only after implementation, compatibility review, validation coverage, and user-visible verification.
- Carry every unresolved item into the next current changelog.
- Preserve the `IChatMemoryMessageMapper` seam: persistence must not depend on `DevExpressChatService`, because that recreates the memory/function-registry DI cycle.
- Project revisions, requirements, requirement links, artifacts, presets, editor preferences, safe imports, and knowledge ratings are database-first contracts. Do not replace them with prewired generation strings.

Theme or application-shell changes must also run `Assert-ThemeArchitecture.ps1`. It verifies DevExpress resource-manager registration, runtime `IThemeChangeService` use, external Bootstrap/Fluent contracts, CSS-variable defaults, theme-state persistence, feature-resource preservation, and the absence of manual `ThemeService` construction or JavaScript-managed DevExpress links.

EF entity, relationship, migration, or snapshot changes must also run `Assert-EfSnapshotArchitecture.ps1`. It verifies that project entities are declared before dependent relationships and collection navigations are declared only after relationships.
## Service lifecycle and supervised asynchronous work

The detailed contract is in `docs/SERVICE_LIFECYCLE_AND_ASYNC_ARCHITECTURE.md`. Runtime services are DI-owned instances; static code is restricted to pure helpers/extensions, immutable constants, generated regex accessors, framework entry points, and security invariants.

High-level service boundaries use injected loggers and bounded service activity while rethrowing failures. Intentionally concurrent component work uses `ISupervisedTaskRunner`; discarded tasks are forbidden. Every DevExpress theme change is awaited. Database migration compatibility is isolated from migration/seeding orchestration.


## Interactive rendering and async continuation policy

LocalGPT intentionally uses reviewed `InteractiveServer` boundaries on routed pages and UI islands. `App.razor` hosts the static router shell and must not replace those boundaries with one persistent root render mode. The render-mode guard fails direct Visual Studio/MSBuild builds if a reviewed directive disappears or the root boundary is reintroduced.

`ConfigureAwait(false)` is the default continuation policy. The async baseline records only the existing renderer-affine exceptions. Removing `ConfigureAwait(false)`, adding an unconfigured await, or adding a new `ConfigureAwait(true)` beyond the reviewed per-file allowance fails the build until the human owner explicitly reviews the baseline.
