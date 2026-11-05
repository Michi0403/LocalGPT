# LocalGPT 5.7.8 — compact managed documentation PDFs

## Fixed

- PDFsharp 6.2.4's vendored .NET 10 build no longer emits its oversized verbose/debug PDF format by default. Debug and Release both default to compact layout and compressed content streams; callers may still explicitly opt into verbose format for debugging.
- The LocalGPT.ReleasePackaging PDF merger explicitly selects compact layout, best Flate compression, and content-stream compression independent of build configuration.
- The merger losslessly compresses imported streams that have no existing filters and only if the compressed bytes are actually shorter. Already filtered image streams and PDF decode parameters remain untouched; no external executable or Python is required.
- The generated-documentation PDF size gate now has a bounded source-page allowance: 256 MiB plus 64 KiB per DocFX printable page, never more than 384 MiB without the pre-existing `FUTURE2_DOCUMENTATION_PDF_MAX_BYTES` operator override. This prevents a legitimate ~1,600-page reference exceeding a fixed 256 MiB threshold from failing while continuing to reject pathological output.
- The no-native-compressor diagnostic no longer incorrectly implies that Ghostscript is mandatory on every host. Optional qpdf/Ghostscript post-optimization remains supported.
- The PDF size warning remains visible when an output exceeds the preferred 256 MiB budget. Durable HTML/PDF chunk reuse and the existing PowerShell pipeline are unchanged.

## Versions

- LocalGPT: 5.7.7 → **5.7.8**
- LocalGPT.ReleasePackaging: 1.0.7 → **1.0.8**
- PDFsharp upstream identity and license: **6.2.4 MIT**, unchanged; these are LocalGPT-specific modifications to the vendored copy.
- PublisherStudio and the installer project: unchanged.

## Verification scope

Static source inspection, C# brace matching, XML parsing, PowerShell syntax-oriented inspection, and ZIP integrity checks. The 280,373,424-byte user PDF and .NET SDK are not available here; a Windows Debug build and actual merge output remain necessary to verify resulting PDF size and readability. No .NET build, GitHub, or online repository operations were performed.
