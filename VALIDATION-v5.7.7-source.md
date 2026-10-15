# LocalGPT 5.7.7 — static validation and required owner checks

## Verified without .NET or GitHub

- Repository preserves original PDFsharp 6.2.4 MIT license and upstream source unchanged.
- Consolidated release notice includes the complete PDFsharp MIT source license, full standard Apache-2.0 text, Open Iconic MIT and OFL source texts, and original repository attribution (with Bootswatch, Bootstrap, QR Generator references).
- XML project syntax, project-relative notice paths and version references inspected.
- Application and setup project MSBuild content items copy one notice into publish output; setup console embeds the notice in the assembly.
- Release PowerShell checks for identical notice hashes in application, setup and versioned output, and checks the release SHA256SUMS manifest contains the file.
- .NET tool project packs `licenses/THIRD-PARTY-NOTICES.txt` and `licenses/PDFsharp-6.2.4-MIT-LICENSE.txt`; package creation and cached package reuse both validate those two embedded files against the source with SHA-256.

## Required on Windows / macOS / Linux (not executed here)

1. Build LocalGPT 5.7.7 with your normal .NET 10 and DevExpress environment; inspect the Debug documentation build and PDF merge.
2. Publish the release and inspect `artifacts/release/5.7.7/THIRD-PARTY-NOTICES.txt`, plus its SHA256SUMS entry.
3. Open each Windows application/setup ZIP and verify the single notice beside the executable. For Linux TAR.GZ, DEB, RPM and AppImage, and macOS app/DMG/PKG, verify the notice inside the packaged runtime payload.
4. Open `LocalGPT.ReleasePackaging.1.0.7.nupkg` and verify both `licenses/` entries.
5. Verify the standalone setup executable still works. The text is also embedded as `LocalGPT.ThirdPartyNotices.txt` in the setup assembly.
6. Review the final actual distributed DevExpress binaries/assets, transitive packages and any optional models or external assets for their own license and NOTICE obligations. The consolidated text is a safeguard, not a universal legal clearance.

.NET compilation, NuGet packaging, actual native artifact contents, and installer behavior are not verified in this environment.
