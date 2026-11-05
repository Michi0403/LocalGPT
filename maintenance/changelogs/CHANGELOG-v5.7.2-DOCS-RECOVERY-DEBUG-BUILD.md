# LocalGPT 5.7.2 – documentation source recovery and optional Debug docs

- Based on the user's repaired LocalGPT 5.7.1 source archive.
- Restored the ten supplied historical reference Markdown documents verbatim under docs/reference/.
- Recovered remaining maintained DocFX source tree from the earlier complete LocalGPT 5.6.9 source archive; did not create placeholder pages. Review DocFX content for any later changes absent from that archive.
- Added Exists conditions to 23 docs-related Content/EmbeddedResource entries; missing optional docs no longer cause CS1566 during the C# resource compilation.
- The default Debug build no longer invokes the expensive documentation/PDF generator. An explicit BuildLocalGptDocumentation=true still enables it.
- Release defaults continue to generate documentation, and the existing publish/source checks remain active.
- The documentation pointer-overlay build gate only runs when both its maintained JS and CSS sources are present. It still validates them when present.
- Removed the misplaced XML comment inside ProjectIngestionController.Get that caused CS1587.
- No change to the Python application runtime, DevExpress packages or the architecture's PowerShell-only tests.

Known limits: no Windows PowerShell, MSBuild, C# compilation, DocFX or publish execution in this environment. When docs are missing, their corresponding embedded initial-data fallbacks will not exist (the existing service logs warnings); this release ships the restored documents. Windows Debug and Release/DocFX runs remain necessary.
