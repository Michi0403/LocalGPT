# Build validation tools

The public source package intentionally contains only non-destructive validation helpers:

- `Assert-SourceFormatting.ps1`
- `Assert-SecurityPolicy.ps1`
- `Assert-ProtectedRepositoryFiles.ps1`
- `Assert-CSharpSyntax.ps1` (Roslyn grammar parse; no NuGet restore required)
- `Assert-ComponentSafety.ps1` (top-level logger/notifier/activity injection and global error-boundary contract)
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
