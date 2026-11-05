# LocalGPT 6.8.3 — PowerShell release-version resolution repair

## Failure reproduced from source

The macOS `pwsh Build-Release.ps1` log for 6.8.2 reaches compilation preparation and then stops in `Build-Release.ps1:177` with `The property 'Version' cannot be found on this object.` `src/LocalGPT/LocalGPT.csproj` has several `<PropertyGroup>` elements, but only the first declares `<Version>`. The former pipeline dereferenced `$_.Version` on *every* group while `Set-StrictMode -Version Latest` was enabled. The development build contained the same resolver.

## Source changes

- Added `build/Get-ProjectVersion.ps1` as the single source-owned version reader for both full release and local-development entrypoints. It selects existing `/Project/PropertyGroup/Version` XML nodes directly, ignores empty entries, preserves first-declared-version behavior, and reports missing versions explicitly. It does not require .NET, package installation, or online access.
- `Build-Release.ps1`: replaced the unsafe version traversal with the shared reader. The release packaging tool's independent version remains unchanged.
- `Build-LocalDevelopment.ps1`: replaced both copies of the unsafe version lookup (application and packaging-tool version) with the same reader.
- Added `build/Assert-ProjectVersionResolution.ps1`, executed before prerequisite provisioning and compilation in full release, development and release `-CompileOnly`. It validates the actual two project versions plus six valid and two invalid XML fixture shapes, including multiple property groups without a `Version` element, blank values, and version precedence.
- `src/LocalGPT/LocalGPT.csproj`: bumped `6.8.2` to **6.8.3**. Minor and patch segments remain single-digit.

No application, sandbox, cross-platform boundary, DX, Council, or PublisherStudio code was modified.

## Verification boundary

Static offline checks validate the XML node-selection logic against the supplied project and synthetic fixture shapes, inspect guard wiring and version declarations, and verify archive integrity and unchanged unrelated source. Windows PowerShell 5.1, modern `pwsh`, .NET compilation, macOS packaging, and signing were not executed here. A full release is **not** claimed successful until it runs on the target host.
