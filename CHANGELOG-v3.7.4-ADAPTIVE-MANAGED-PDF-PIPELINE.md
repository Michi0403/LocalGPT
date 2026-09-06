# LocalGPT 3.7.4 — adaptive managed PDF pipeline

- Preserved every already-rendered browser PDF chunk until the managed merge completes; print-book generation now replaces only its own HTML file instead of clearing the shared chunk directory.
- Upgraded `LocalGPT.ReleasePackaging` to 1.0.2 and added the MIT-licensed PDFsharp 6.2.4 dependency for cross-platform PDF merging.
- Large DocFX HTML documentation is now browser-printed in memory-adaptive bounded chunks and merged by the shared packaging tool, avoiding the multi-hour single-browser print and multi-gigabyte DocFX-PDF path in normal release builds.
- Browser chunk size adapts to available memory and can be overridden with `FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_PAGES`.
- A complete cover/table-of-contents print is rendered separately before body chunks so chunking does not truncate the document index.
- The packaging helper also exposes PDF optimization that can use qpdf and Ghostscript when present; Ghostscript remains a fallback rather than a mandatory prerequisite for the normal managed browser path.
- Release builds prepare the packaging helper on Windows, macOS, and Linux because documentation assembly now uses it on every host.
- The compressed/merged PDF remains embedded in `wwwroot/help-docs` for the offline documentation viewer.
- Existing macOS notarization resume/reuse and Full self-contained release behavior are preserved.
