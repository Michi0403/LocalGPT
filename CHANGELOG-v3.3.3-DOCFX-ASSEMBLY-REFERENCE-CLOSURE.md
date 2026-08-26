# LocalGPT 3.3.3 — DocFX assembly-reference closure

## Summary

LocalGPT 3.3.3 hardens the cross-platform documentation build after a successful macOS release build exposed a DocFX `InvalidAssemblyReference` warning for `System.Formats.Nrbf`.

## Changes

- The RID-neutral LocalGPT documentation build now sets `CopyLocalLockFileAssemblies=true`, so direct and transitive NuGet assemblies are materialized beside `LocalGPT.dll` before DocFX metadata analysis.
- The development build uses the same copy-local dependency policy for its documentation source assembly.
- `Build-Documentation.ps1` now treats unresolved DocFX assembly references as an incomplete API graph rather than accepting a warning-only metadata run.
- DocFX metadata output is inspected for `Unable to resolve assembly reference ...` diagnostics.
- Missing framework assemblies are resolved from installed `Microsoft.NETCore.App` / `Microsoft.AspNetCore.App` shared-runtime directories and copied into the temporary DocFX input probe directory, then metadata extraction is retried.
- The repair is bounded to three dependency-resolution passes and never changes LocalGPT's runtime target or adds artificial application package references.
- `documentation-status.json` now records unresolved-reference count/list and how many dependency assemblies were repaired.
- Release and local-development validation reject documentation with any remaining unresolved assembly reference.
- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper are versioned **3.3.3**.

## Rationale

`System.Formats.Nrbf` is part of the .NET runtime dependency graph on current .NET 10 installations. DocFX metadata analysis uses its own assembly resolver and may not probe the installed shared framework automatically. The correct repair is therefore to provide DocFX with the complete dependency probe closure, not to add an unrelated direct `PackageReference` to the LocalGPT application merely to silence documentation tooling.

## Validation policy

No .NET build was executed while preparing this source archive. Static source validation verifies the version bump, copy-local documentation property, runtime-probe repair logic, unresolved-reference release guards, changelog wiring and preservation of the existing interactive render-mode declarations.
