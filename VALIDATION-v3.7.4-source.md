# LocalGPT 3.7.4 source validation

Static validation covers version consistency, `LocalGPT.ReleasePackaging` 1.0.2 PDFsharp integration, adaptive memory-based browser PDF chunking, preservation of every rendered chunk until the managed merge completes, optional qpdf/Ghostscript optimization, the compressed embedded documentation PDF contract, Full/self-contained Unix/macOS packaging, resumable notarization/reuse, Homebrew RPM support, architecture/cross-platform boundaries, async/service resilience, and the 15 reviewed InteractiveServer boundaries.

The repository-owned release audit also guards against deleting the shared browser-chunk directory from an individual print-book render and against unbraced PowerShell variable-plus-colon interpolation. A local Linux Ghostscript smoke test using the same pdfwrite/downsampling options reduced a synthetic image-heavy PDF from 9,803,633 bytes to 1,819,343 bytes while retaining a valid `%PDF-` header.

This environment does not contain PowerShell or .NET, so `pwsh Build-Release.ps1`, .NET restore/build/pack, PDFsharp execution, macOS signing, Homebrew provisioning, and Apple notarization are not claimed here. The Mac/Windows release hosts remain the authoritative execution tests; their early PowerShell parser preflight runs before the expensive release stages.
