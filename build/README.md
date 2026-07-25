# Build validation tools

The public source package intentionally contains only non-destructive validation helpers:

- `Assert-SourceFormatting.ps1`
- `Assert-SecurityPolicy.ps1`
- `Assert-ProtectedRepositoryFiles.ps1`
- `Protect-GovernanceFiles.ps1` (optional, owner-run local read-only hardening)
- `Audit-Dependencies.ps1`

Historical one-click scripts that downloaded software or repositories, changed user settings, generated certificates, deleted directories, started localhost services, pulled model collections, published releases, or pushed Git state are not shipped. Use the documented owner-side release process and enter each consequential command manually after review.

`Install-OllamaLocalGPTAndModels.ps1` remains only as a fail-closed compatibility notice and performs no installation.

The protected governance set is readable by repository tools but must not be edited by automated agents. Its reviewed contents are pinned in `protected-files.sha256`; the source-hygiene workflow verifies the manifest. The optional protection script changes filesystem write attributes only when the human owner runs it.
