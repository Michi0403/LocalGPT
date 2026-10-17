# LocalGPT 6.0.2 — Static validation / Windows and macOS acceptance

- Source baseline: complete provided `LocalGPT-6.0.1-source.zip`.
- Updated one-command helper path, early PowerShell boundary guard, application version, engineering build documentation.
- The NuGet distribution package is still mandatory and validated with source-byte SHA-256; using the project executable does **not** demonstrate installability of the NuGet package.
- `dotnet publish` generates a platform-specific executable with `UseAppHost=true`; expected names: `LocalGPT.ReleasePackaging.exe` on Windows, `LocalGPT.ReleasePackaging` on macOS/Linux.
- Executable `capabilities` must include `pdf-merge-input-list`, or the top-level build stops before DocFX.
- These are static source-contract checks only; no PowerShell parser, .NET build/publish, PDF verification, runtime tool installation, signing or notarization was run in this container.

## Required macOS acceptance

From a clean 6.0.2 checkout, invoke **only** `pwsh ./Build-Release.ps1`. It must automatically repair/check the package, print `Publishing the repository-owned release-packaging executable directly`, validate capabilities, and proceed beyond prior `.store/.stage` access-denied exception. Separately verify full documentation and native packaging output; neither is established by the current static review.

## Required local development acceptance

Run `pwsh ./Build-LocalDevelopment.ps1` as normal. The same helper preparation should complete without an external `dotnet tool install` prerequisite.
