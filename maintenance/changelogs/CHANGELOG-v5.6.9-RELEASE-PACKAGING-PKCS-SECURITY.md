# LocalGPT 5.6.9 – ReleasePackaging PKCS security dependency repair

Released: 2026-10-14. Source baseline: LocalGPT 5.6.8.

## Root cause and fix

- `LocalGPT.ReleasePackaging` references PDFsharp 6.2.4. PDFsharp declares `System.Security.Cryptography.Pkcs >= 8.0.1` even for `net10.0`. Without a direct version floor, a fresh restore can select vulnerable `8.0.1` and fail with NU1903 (GHSA-846f-f764-76mx).
- Add a direct `System.Security.Cryptography.Pkcs` **10.0.13** PackageReference to **`src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj`**. Retain PDFsharp 6.2.4, the shared PDF functionality, and existing `NuGetAudit` severity enforcement; do not suppress NU1903.
- Bump the independently packaged release helper from **1.0.4 to 1.0.5** in its `.csproj`, release entrypoint and ensure/publish scripts. This prevents stale 1.0.4 `.nupkg` contents from being selected by the release cache in a future packaging run.
- Roll the application source version to **5.6.9** and refresh JS URL version markers, without runtime behavior modifications.
- Add `build/audit_release_packaging_pkcs_5_6_9.py` to test the source-only security floor and tool-version handshake.

## Preserved contracts

- Keep `net10.0`, SDK `10.0.402`, all other .NET 10.0.13 package pins, PDFsharp version 6.2.4, `pdf-merge-input-list` capability validation and PDF size safety limits.
- DevExpress stays **25.2.10**. PublisherStudio stays **4.1.9**; no edits to its source.
- No changes to UI logic, database, AI functions, approvals, GIL handling, PDFs or any published runtime algorithms.

## Validation boundary

The source-only checks cannot prove NuGet's resulting dependency graph or successfully execute the release tool. Owner must install .NET SDK 10.0.402, run `dotnet restore`/`dotnet list package --vulnerable --include-transitive` and build ReleasePackaging on the target machine to confirm NU1903 is gone. An earlier 1.0.4 tool package may remain on disk but is no longer requested by the new release entrypoint.

Reference: https://www.nuget.org/packages/PDFsharp/6.2.4 and https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.13
