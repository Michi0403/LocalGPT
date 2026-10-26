# LocalGPT 6.5.5 — PowerShell-only source validation and version independence

Base: complete LocalGPT 6.5.4 source archive. PublisherStudio and existing LocalGPT runtime features are unchanged.

## Corrected repository maintenance policy

- Removed **all 29 Python `tests/static/test_*.py` files** and **29 cached `.pyc` files**, together with their now-unused `__pycache__` directory. The previously uploaded `test_embedded_resources_633.zip` contains examples of this legacy version-suffixed structure; it is not included as repository content.
- Removed the version-named source/Node test `tests/static/documentation-mermaid-touch-behavior-642.cjs`. The remaining `tests/static` audit is a `.ps1` file.
- Kept the **application runtime Python bridge** `src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py` and other non-test application requirements intact. The change targets tests/architecture maintenance, not the user-facing Python runtime feature.
- Extended the existing **build-wired** `build/Assert-RepositoryPowerShellBoundary.ps1` to reject non-PowerShell tests and Python build architecture checks, and to validate current project `<Version>` values by reading the `.csproj` files on each invocation. Versions follow `major.minor.patch` with **single-digit minor and patch**; **no expected version literals or per-release test tables** exist in the new guards.
- Added `build/Assert-EmbeddedResourceArchitecture.ps1`, wired into the normal LocalGPT build through `Directory.Build.targets`. It checks **all current physical EmbeddedResource paths and logical names** for duplicates, validates file existence, and derives seed-document resource names from source paths rather than a hand-maintained list or exact release string. This replaces the corresponding legacy Python test functionality without weakening existing build gates.
- Updated `AGENTS.md` and `docs/engineering/build-validation.md` so new maintenance work follows the PowerShell-only contract rather than repeating Python/test-suite and version-hardcoding errors. Removed obsolete AGENTS references to nonexistent Python audit scripts.
- Bumped `src/LocalGPT/LocalGPT.csproj` from **6.5.4** to **6.5.5** (only the application version changed).

## Architectural integrity

- All existing mandatory PowerShell guards remain in place. The new embedded-resource gate is additive, not a replacement for existing validation.
- No LocalGPT C# implementation, Blazor component, JavaScript runtime, 1-Wire, Council logic, installer or model configuration was changed.
- The old Python source tests are **removed rather than converted line-for-line**; behavior-specific assertions not already covered by an existing PowerShell guard should be implemented as proper version-independent PowerShell checks when needed. Do not claim that deleting 125 Python assertions proves identical test coverage.

## Verification limits

- Archive structure, source-only XML inspection, embedded resources, and absence of Python/test bytecode files in `tests` can be checked without .NET.
- **Neither Windows PowerShell 5.1 nor `pwsh` is installed in this execution environment**, so the new `.ps1` scripts have not been executed on a PowerShell host.
- **No .NET/Razor build or live application test** was run. Verify the normal Windows build before treating this as a working binary release.
