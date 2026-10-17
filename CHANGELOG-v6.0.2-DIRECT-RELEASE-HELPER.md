# LocalGPT 6.0.2 — Repository-owned release helper execution

- Removes mandatory `dotnet tool install` from `build/Ensure-ReleasePackagingPackage.ps1`; macOS reported access denied extracting the already-verified PDFsharp license to `.store/.stage` on an external build volume.
- The normal `Build-Release.ps1` and `Build-LocalDevelopment.ps1` prepare and run `LocalGPT.ReleasePackaging` directly from a fresh, host-specific `dotnet publish` of the existing project, preserving the executable path contract for documentation and native packaging.
- The distributable NuGet package still builds automatically on cache miss or integrity failure and must pass the existing full source-license SHA-256 verification before running the helper.
- Checks the real published executable for `pdf-merge-input-list` before the expensive documentation process.
- Extends the early PowerShell boundary guard to preserve one-command direct execution and reject an explicit tool-install dependency.
- Documents the updated build sequence in the DocFX engineering chapter with a Mermaid sequence diagram.

## Boundaries

No .NET, PowerShell, native package signing, Apple notarization, or tool installation was executed in this environment. The underlying macOS file-permission error is not proven to be a NuGet bug or a filesystem bug; it is removed from the required *build* path, not claimed fixed for external NuGet tool consumers. PublisherStudio and vendored PDFsharp source unchanged. Helper version stays 1.1.2 because helper project and binary semantics were not changed.
