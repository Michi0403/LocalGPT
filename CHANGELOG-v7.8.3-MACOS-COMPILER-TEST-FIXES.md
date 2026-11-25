# LocalGPT 7.8.3 · Underwhelming Urchin

The Undersuperiority Series. Fixes found while testing the Program Compiler team on a MacBook Neo (Apple A18 Pro, 8 GB) with qwen2.5-coder:3b.

## Fixed
- **Build repair no longer aborts the whole file work** when a model's repair answer is not a path→work-order object (for example when it echoes the input). The repair attempts end like an empty answer, with a visible notice; the persisted draft, approvals and build evidence stay available.
- **Repair packet**: the repair prompt now receives only `ManifestPaths` and `BuildEvidence`, not the path→hash map that small models echoed back.
- **Repair prompt seed** (`CouncilFileWorkRuntimeParametersJson.repairPrompt`): a project file that references a non-existent or excluded package (e.g. a dependency-free request, NU1102) is named as a source defect; includes a one-line answer example and asks not to repeat the input. Applies to new databases; a stored value is left unchanged.
- **`CompilerNotSelected` message** now points to Setup › Toolchains › "Discover and save toolchains" (a new installation has no registered compilers).
- **Provider host test** (Setup › AI providers) uses the existing `ServiceTimingRuntimeParametersJson.aiDiscoveryTimeoutSeconds` instead of the 100 s HttpClient default, so an offline host answers within seconds.

## Checks actually performed
- Reproduced on the installed macOS 7.8.2: `CompilerNotSelected` on a fresh install; after toolchain discovery a real `dotnet` build with NU1102 from a model-added `PackageReference System 4.7.2`; repair aborted with `JsonException` at `$.CompletedFiles`; host test waited ~100 s.
- Source edits reviewed by diff; seed JSON parsed. **No .NET build of 7.8.3 has been run by me.**
