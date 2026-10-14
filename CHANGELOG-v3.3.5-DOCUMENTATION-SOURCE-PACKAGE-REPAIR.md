# LocalGPT 3.3.5 — documentation source package and DocFX dependency repair

## Summary

LocalGPT 3.3.5 repairs the clean-source release path exposed after the cross-platform Node.js bootstrap started working on macOS. The release now carries the maintained DocFX source tree that the project and documentation pipeline already reference, fails fast when a source archive is incomplete, and extends unresolved DocFX assembly repair to the NuGet global package cache.

## Changes

- Restored the `docs/` source tree required by `LocalGPT.csproj` and `Build-Documentation.ps1` instead of shipping only the generated `wwwroot/help-docs` snapshot.
- Restored all 26 maintained conceptual guide/architecture/engineering/reference pages from the reviewed generated documentation snapshot, plus the DocFX root/category TOCs, dedicated complete-PDF TOC, API overview source, PDF cover, DocFX configuration and Kawaii theme source assets.
- The restored DocFX configuration keeps the modern template, nested namespace navigation and same-page member layout expected by the existing release guards.
- `Initialize-BuildPrerequisites.ps1` now validates the documentation source payload before DevExpress/Node provisioning or the long .NET build. An incomplete source ZIP therefore fails immediately with the exact missing files instead of reaching MSBuild `MSB3030` later.
- The existing Windows/macOS/Linux Node.js 20–22 bootstrap remains unchanged and is still shared by release/local-development documentation tooling.
- Extended `Repair-LocalGptDocfxAssemblyReferences` with a cross-platform NuGet-cache probe. After the normal build output and installed shared runtimes are checked, an unresolved assembly such as `System.Formats.Nrbf` can be resolved from `NUGET_PACKAGES` or the user's standard `.nuget/packages` cache and copied only into the temporary DocFX input directory.
- No direct `System.Formats.Nrbf` application package reference was added.
- The existing hard release rule remains: DocFX metadata must finish with zero unresolved assembly references.
- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are versioned **3.3.5**.

## Rationale

The cross-platform prerequisite bootstrap correctly provisioned Node.js on Apple Silicon, but a clean source package then failed before documentation generation because the project explicitly copies authored documentation files that were absent from the archive. The generated help site is not a substitute for those build inputs. The source payload is now complete and the prerequisite stage verifies that fact before doing expensive work.

The earlier `System.Formats.Nrbf` warning also showed that a DocFX-only metadata dependency may live in the NuGet global package cache rather than the application output or shared runtime directory. The repair path now checks that cache without changing LocalGPT runtime dependencies.

## Validation policy

No .NET restore/build/test and no PowerShell execution was performed while preparing this source archive. Static validation checks the restored documentation payload, DocFX configuration/TOCs/theme sources, early source preflight, NuGet-cache assembly probe, version bump, version-slot convention and preservation of existing Interactive Server render-mode declarations.
