# LocalGPT 5.7.1 — full user-upload delta checklist

The authoritative base is the files in the attached `LocalGPT.zip`, **not** any assistant archive. Machine-specific generated contents are not source changes.

- Existing relevant files modified: **14**.
- Relevant source/build files introduced: **10** (before these three release documents).
- Obsolete Python audit implementation retired: **1**.

## Modified

- `.github/workflows/publish-shipped-docs.yml`
- `Directory.Build.targets`
- `build/Assert-ArchitectureTasks.ps1`
- `build/Assert-CodeGenerationDxFunctionWiring.ps1`
- `build/Assert-CouncilXRoundWiring.ps1`
- `build/Build-Documentation.ps1`
- `build/Initialize-BuildPrerequisites.ps1`
- `build/Invoke-ArchitectureAudit.ps1`
- `build/Update-GitHubPagesSnapshot.ps1`
- `build/WslRelease.Common.ps1`
- `src/LocalGPT/Components/App.razor`
- `src/LocalGPT/Components/Pages/Install.ProjectSource.razor.cs`
- `src/LocalGPT/Components/Pages/Install.razor`
- `src/LocalGPT/LocalGPT.csproj`

## Added

- `build/Assert-AsyncContinuationPolicy.ps1`
- `build/Assert-AsyncOnlyArchitecture.ps1`
- `build/Assert-DevExpressComponentRetention.ps1`
- `build/Assert-MethodDiagnostics.ps1`
- `build/Assert-PagesArtifact.ps1`
- `build/Assert-ProtectedRepositoryFiles.ps1`
- `build/Assert-RazorMaintenanceArchitecture.ps1`
- `build/Assert-RepositoryPowerShellBoundary.ps1`
- `build/Assert-RuntimePluginSourceCallbacks.ps1`
- `build/Assert-RuntimeTextOwnership.ps1`

## Retired

- `.github/scripts/prepare-pages-artifact.py`

## Verification constraints

- [x] Source ZIP CRC verified before editing.
- [x] No mandatory Python invocation in maintained build, debug, release or GitHub Pages entrypoints.
- [x] Callback implementation and PowerShell check version-independent.
- [x] MSBuild target XML parses.
- [ ] Real Windows PowerShell 5.1 validation.
- [ ] Real Visual Studio .NET / Razor compiler build.
- [ ] Real DocFX/PDF release, Pages ZIP generation and hosted workflow.
