# Bundled PDFsharp 6.2.4 sources

- **Source provided by user:** `PDFsharp-9c3863a71b12067ff36e68a92c1a1f260114e2d1.zip`.
- **Upstream source version:** PDFsharp **6.2.4**, as indicated by the source README and associated 6.2.4 changelog.
- **Upstream copyright and license:** empira Software GmbH; MIT. Full license reproduced without alteration at [`LICENSE`](LICENSE).
- **Source modifications for LocalGPT:** Build only the upstream cross-platform PDFsharp core, `PdfSharp.Shared`, and `PdfSharp.System` projects against **net10.0**; reference modern `Microsoft.Extensions.Logging.Abstractions` **10.0.13** instead of inheriting upstream central versions; replace generated GitVersion metadata with fixed 6.2.4 metadata so ZIP builds do not need a Git checkout. For LocalGPT 5.7.8, `PdfDocumentOptions.cs` additionally defaults to compact PDF serialization and compressed content streams in Debug as well as Release. The release-packaging merger selects these settings explicitly. Other upstream PDFsharp source code remains unchanged.
- **Strong-name key:** The upstream source archive contains `src/StrongnameKey.snk`, preserved for assembly signing compatibility.
- **Build dependency:** `LocalGPT.ReleasePackaging` references the local `PdfSharp.csproj`, never PDFsharp 6.5+ NuGet. The resulting PDFsharp assemblies belong to the build helper, not to the LocalGPT runtime application.
- **Scope:** Only the dependencies needed to merge the documentation PDFs are bundled. Upstream samples, tests, optional GDI/WPF variants and MigraDoc are deliberately not vendored.

The MIT terms apply to these sources regardless of the license of surrounding LocalGPT project-owned code. Keep `LICENSE` and this notice with source distributions. This is an engineering provenance record, not a legal opinion about unrelated assets.
