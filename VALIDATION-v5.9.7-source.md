# LocalGPT 5.9.7 — static source verification

**Baseline:** complete uploaded `LocalGPT-5.9.6-source.zip`.

**Defect reproduced statically:** `Build-Release.ps1` line 66 and
`Build-LocalDevelopment.ps1` line 26 invoke
`build/Assert-CrossPlatformBoundaries.ps1`; the file is absent from all source
snapshots beginning with 5.7.1, although present in 5.5.0 and 5.6.1. The old
script delegated to `build/audit_cross_platform_boundaries.py` and is not
compatible with the maintained PowerShell-only architecture.

**Repair:** restored the full audit in PowerShell rather than bypassing the
preflight. Added early source-archive dependency checks to
`Assert-RepositoryPowerShellBoundary.ps1`. No build entrypoint was disabled.

**Offline static checks:**

- Release entrypoint: 10 direct `build/*.ps1` invocations; 0 missing.
- Local-development entrypoint: 5 direct `build/*.ps1` invocations; 0 missing.
- Reviewed historical OS branching and Windows-specific executable/environment
  patterns across 846 application C# files; 0 unapproved occurrences.
- Historical GDI/Windows-only reference checks: 0 matches.
- Unix case-sensitive path comparisons and Ollama executable comparator contract
  snippets are present.
- Eight required platform DI registrations and platform service/interface source
  contracts remain present.
- Previously forbidden hard-coded case-insensitive path containment snippets
  do not occur in their named owners.
- PowerShell build scripts preserve the project's no-Python-execution policy.
- Application project XML is well-formed and version is `5.9.7`.
- Full original source archive content is retained; vendor/third-party sources
  and notices were not changed.

**Limitations:** source and ZIP checks only. No PowerShell parser/runtime, .NET
compilation, macOS publish, PDF printing, or platform execution ran here.
`pwsh ./build/Assert-CrossPlatformBoundaries.ps1` and
`pwsh ./Build-Release.ps1` on macOS are the next runtime checks.
