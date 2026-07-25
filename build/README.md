# Build validation tools

The public source package intentionally contains only non-destructive validation helpers:

- `Assert-SourceFormatting.ps1`
- `Assert-SecurityPolicy.ps1`
- `Audit-Dependencies.ps1`

Historical one-click scripts that downloaded software or repositories, changed user settings, generated certificates, deleted directories, started localhost services, pulled model collections, published releases, or pushed Git state are not shipped. Use the documented owner-side release process and enter each consequential command manually after review.

`Install-OllamaLocalGPTAndModels.ps1` remains only as a fail-closed compatibility notice and performs no installation.
