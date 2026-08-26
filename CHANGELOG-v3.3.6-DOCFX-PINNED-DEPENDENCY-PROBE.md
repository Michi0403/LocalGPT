# LocalGPT 3.3.6 — DocFX pinned dependency probe

## Fixed

- Added `docs/DocfxDependencies.csproj` as an isolated documentation-tooling dependency graph. It pins `System.Formats.Nrbf` 10.0.11 for DocFX metadata reflection without adding that package to the LocalGPT application/runtime dependency graph.
- `Build-Documentation.ps1` now restores the documentation-only probe package when the assembly is not already present in the user NuGet cache, copies `System.Formats.Nrbf.dll` into the temporary DocFX input probe directory before the first metadata pass, and keeps the existing generic unresolved-reference repair loop for any additional references.
- Unresolved DocFX metadata references now fail immediately with their assembly names before the PDF stage. This replaces the misleading later `Complete documentation PDF generation failed. No additional PDF diagnostic was emitted.` message when metadata had already failed.
- The cross-platform prerequisite preflight now validates that `docs/DocfxDependencies.csproj` is present in clean source archives.
- Updated all current LocalGPT version markers to 3.3.6 while preserving the existing interactive render-mode boundaries.

## Why

On the verified macOS arm64 release path, LocalGPT 3.3.5 successfully completed the SDK, DevExpress, Node.js, protocol, and application build preflights, but DocFX 2.78.5 still reported `InvalidAssemblyReference` for `System.Formats.Nrbf, Version=10.0.0.0`. `CopyLocalLockFileAssemblies=true` was insufficient because the assembly is required by the reflection/documentation graph rather than by LocalGPT as a direct runtime dependency.
