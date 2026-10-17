# LocalGPT 6.0.0 — release tool NuGet notice packaging repair

## Root cause

The macOS release coordinator successfully built and packed `LocalGPT.ReleasePackaging` 1.1.0, but `Assert-ReleasePackagingPackageNotices.ps1` found that the .NET tool package did not contain `licenses/PDFsharp-6.2.4-MIT-LICENSE.txt`. An older included tool package also lacks the intended notice entries. The project listed notice files as generic `None Pack=true` items; the .NET SDK's tool packaging path gathers files through `PackTool` and `TfmSpecificPackageFile`, so ordinary content inclusion was not sufficient for the observed package.

## Repair

- In `src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj`, explicitly contribute both existing, unmodified source notices to `TfmSpecificPackageFile` with their mandated `licenses/` archive paths before `PackTool`. This preserves the normal .NET tool package layout and does not post-process the ZIP or bypass the integrity check.
- Use forward-slash, project-root-relative item paths compatible with MSBuild on Windows, macOS, and Linux.
- Update the packaging helper version from 1.1.0 to **1.1.1** because the helper package content changes; keep the release and developer helper-script defaults aligned. The release coordinator continues reading the version from the project.
- Roll the main LocalGPT version from 5.9.9 to **6.0.0** to keep the minor and patch fields single-digit. No feature or runtime-application change is intended.

## Required runtime acceptance

The locally generated `packages/LocalGPT.ReleasePackaging.1.1.1.nupkg` must contain the exact entries `licenses/THIRD-PARTY-NOTICES.txt` and `licenses/PDFsharp-6.2.4-MIT-LICENSE.txt`, each SHA-256 identical to their respective source files. Next, `dotnet tool install` must work and the macOS release must proceed into documentation and publish. The unchanged `Assert-ReleasePackagingPackageNotices.ps1` verifies the archive after `dotnet pack` and again before installation.

Static source/ZIP checks were performed, but **no .NET, PowerShell, NuGet pack, or macOS release was executed** in this environment. The fix remains dependent on validation of the actual generated .nupkg on macOS.

PDFsharp 6.2.4 original MIT source/license, release integrity gates, PublisherStudio, and application runtime remain unchanged.
