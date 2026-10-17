# LocalGPT 5.9.7 — Restored cross-platform release boundary audit

## Release regression

The macOS `pwsh ./Build-Release.ps1` path stopped after its initial notice,
PowerShell compatibility, documentation, DevExpress license, and Node.js
preflights because its next call targeted a nonexistent
`build/Assert-CrossPlatformBoundaries.ps1` (line 66). `Build-LocalDevelopment.ps1`
also invokes that missing script. The guard was present in the historical 5.5.0
and 5.6.1 archives, but absent from the available source snapshots beginning
with 5.7.1; the entrypoint invocations were never removed or replaced.

The historical 10-line wrapper invoked a Python audit. Reinstating it verbatim
would violate the current PowerShell-only maintenance and publishing policy.

## Repair

- Restored `build/Assert-CrossPlatformBoundaries.ps1` as a **native PowerShell**,
  source-only audit, with no `python`, `dotnet`, or platform-specific tooling
  dependency. It works as a release or local-development guard on Windows,
  Linux, and macOS PowerShell hosts.
- Retained the historical audit's guarantees for forbidden platform-specific
  app dependencies and GDI usage, permitted OS/platform composition boundaries,
  Windows executable/environment selection, sensitive physical path containment,
  Unix path comparer semantics, Ollama executable comparers, and platform DI
  contracts. The approved exceptions are explicit source-file allowlists.
- Strengthened `build/Assert-RepositoryPowerShellBoundary.ps1` (run by ordinary
  Visual Studio builds) to ensure the **directly invoked** build guard scripts
  in `Build-Release.ps1` and `Build-LocalDevelopment.ps1` exist. This prevents
  another missing release dependency from surviving Windows Debug builds.
- Advanced only the LocalGPT application version to **5.9.7**. Vendor PDFsharp,
  DevExpress integration, wire-protocol and ReleasePackaging versions, service
  lifetimes, PDF cache behavior, Council, and PublisherStudio are unchanged.

## Source and validation

The archive preserves the entire 5.9.6 source tree. Static checks compared
all historical audit requirements against the current 846 application C#
source files, verified all directly invoked scripts in both entrypoints, and
validated the application version, documentation provenance, and complete ZIP.
No .NET, PowerShell, or release publish command was executed in this environment.
The user-side macOS publish remains the required runtime acceptance test.

To avoid a new PDF version when validating an existing 5.9.6 checkout, you may
copy only `build/Assert-CrossPlatformBoundaries.ps1` into that checkout and run
`pwsh ./build/Assert-CrossPlatformBoundaries.ps1`, followed by
`pwsh ./Build-Release.ps1`. For the permanent versioned source release, use 5.9.7.
