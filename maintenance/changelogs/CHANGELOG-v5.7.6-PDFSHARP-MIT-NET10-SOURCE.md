# LocalGPT 5.7.6 – MIT PDFsharp 6.2.4 source integration (.NET 10)

## Fixed

- Replaced `LocalGPT.ReleasePackaging`'s external `PDFsharp` 6.5.0 package dependency, whose runtime license validation prevented merging the already-rendered documentation PDF chunks, with a **local project reference** to the user-supplied **MIT-licensed PDFsharp 6.2.4** source.
- Included three cooperating upstream source projects in the Visual Studio solution (`PdfSharp`, `PdfSharp.Shared`, `PdfSharp.System`), maintaining the upstream project-reference architecture and the original strong-name key.
- Restricted all three bundled PDFsharp projects to **`net10.0`** without touching their core PDF implementation. Pinned the logging abstraction to **10.0.13**; retained the helper's security-serviced `System.Security.Cryptography.Pkcs` **10.0.13**.
- Removed the vendored source's requirement for Git/GitVersion when building from a ZIP: fixed its generated version metadata to **6.2.4** and kept the original MIT license and copyright in `third_party/PDFsharp/LICENSE`.
- Preserved the **PowerShell/DocFX/Edge chunked documentation pipeline**, all chunk recovery behavior, and the existing `pdf-merge` implementation. No mandatory Python or paid DevExpress PDF dependency was introduced.
- Bumped the changed release-packaging helper version **1.0.5 → 1.0.6** consistently across the project, Release build entrypoint and PowerShell package-install/publish helpers, avoiding stale cached 1.0.5 packages.
- Updated third-party attribution and provenance. LocalGPT application version increased from **5.7.5 to 5.7.6**.

## Validation boundary

Only static archive, XML, path, project-graph, source/version/license and reference checks were performed here; **no .NET build, Windows/Visual Studio, NuGet restore, or runtime PDF merge was executed**. The owner must compile and verify the end-to-end PDF documentation build on Windows. Retained generated PDF parts should be reusable by the existing recovery logic. This is not a claim that compilation has been confirmed.

No PublisherStudio sources changed.
