# LocalGPT 4.9.0 — Release project-reference order repair

## Fixed

- Repaired the macOS/full-release RID-neutral documentation compile after repository-local `bin`/`obj` cleanup.
- Removed the `BuildProjectReferences=false` override from that LocalGPT build so MSBuild can rebuild freshly cleaned project references before compiling the application.
- `LocalGPT.PluginContracts` is now built through the normal LocalGPT project graph, preventing the observed missing `bin/Release/net10.0/LocalGPT.PluginContracts.dll` / `obj/Release/net10.0/ref/LocalGPT.PluginContracts.dll` failure.
- The source-backed LocalGPT wire-protocol reference remains part of the same ordered graph, including release modes where a previously packed/bundled protocol package would not have left a repository-local build output behind.
- Future LocalGPT project references inherit the same behavior without requiring a new hardcoded pre-build step in `Build-Release.ps1`.

## Preserved

- `BuildLocalGptDocumentation=false` remains in force for the RID-neutral application compile; documentation is still generated exactly once by the release coordinator after the assembly/XML build succeeds.
- Release packaging, signing/notarization, platform selection, documentation caching and the 4.8.9 toolchain/runtime-plugin behavior are otherwise unchanged.
- Version rollover follows the repository rule: 4.8.9 advances to 4.9.0 rather than 4.8.10.

## Validation scope

Source-only validation was performed in the handoff environment. No `dotnet`, NuGet restore, MSBuild, publish, native packaging, signing or notarization command was executed.
