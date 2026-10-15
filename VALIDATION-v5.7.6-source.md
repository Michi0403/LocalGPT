# LocalGPT 5.7.6 static verification and owner-side smoke tests

## Verified by static inspection

- Supplied upstream archive README says PDFsharp **6.2.4**; source `LICENSE` contains original **MIT License** and copyright.
- Bundled three PDFsharp source projects, all target **net10.0**; upstream original key and sRGB ICC embedded resource are included.
- LocalGPT.ReleasePackaging's `PDFsharp` **6.5.0** package reference removed and replaced with a `PdfSharp.csproj` project reference; solution contains all three source projects.
- PDFsharp product version fields read fixed 6.2.4 metadata without GitVersion, so ZIP builds require no `.git` history.
- Release-packaging helper version references agree on **1.0.6**.
- Original PowerShell documentation script, chunked browser rendering, PDF merge code and security-serviced PKCS dependency retained.

## Requires owner-side testing (not executed here)

1. In Windows Visual Studio, reload `src/LocalGPTWebviewWrapper.sln`, restore and build `LocalGPT.ReleasePackaging` **Debug** first. Check the three PDFsharp projects compile under net10.0 and the merged tool output contains PdfSharp, PdfSharp.Shared and PdfSharp.System assemblies.
2. Execute ordinary `LocalGPT` Debug build. DocFX HTML should be generated, then retained browser-generated PDF chunks merged by the helper **without `PdfSharp.License.PdfSharpLicenseException`**. Verify `help-docs` PDF and `/help` links.
3. Check Release build and any PublisherStudio pipeline consuming the separately packaged `LocalGPT.ReleasePackaging` tool.
4. If build fails, provide the **first compiler diagnostic** and affected path; do not replace this MIT source with a licensed PDFsharp NuGet version to bypass it.
