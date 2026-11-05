# LocalGPT 5.7.9 — PDF size limits removed, structural merge verification

## Fixed

- Removed the artificial documentation PDF output-size rejection from `build/Build-Documentation.ps1`. PDF generation no longer fails solely because the final PDF exceeds 256 MiB, 384 MiB, or another fixed cap.
- Removed the matching downstream size-ceiling check from `Build-Release.ps1`. Release packaging still checks that the embedded versioned PDF exists, has non-trivial content and agrees with its recorded byte count.
- Kept chunked browser rendering, durable chunk/cache recovery, documentation-on-Debug-build and PowerShell orchestration unchanged. Python is not a build dependency.

## PDF merge / packaging validation

- `LocalGPT.ReleasePackaging pdf-merge` now checks every source PDF is present and non-empty, tallies imported pages, reopens the complete merged output, and checks its page count before atomically replacing the final output. Failed validation preserves the previous destination.
- Added `pdf-verify --input FILE [--expected-pages N]` for explicit structural and page-tree validation. Documentation generation uses it before caching and publishing the final PDF, including when an older cached document is reused.
- `pdf-optimize` validates the original input and accepts qpdf/Ghostscript candidates only when they remain readable with the same page count. External optimizers remain optional; no size reduction is required for a valid PDF to be published.
- PDF header/EOF checks read bounded segments from the beginning and end of files rather than loading huge PDF contents into memory.

## Versions and licensing

- LocalGPT 5.7.8 → **5.7.9**
- LocalGPT.ReleasePackaging 1.0.8 → **1.0.9**
- Vendored PDFsharp remains **6.2.4**, locally maintained for .NET 10. Original MIT terms, notices and copyright remain in place.
- PublisherStudio unchanged.

## Limitations

- No .NET/Windows build or runtime PDF merge was executed here. Static source checks cannot establish that all PDFsharp operations scale to arbitrarily large PDFs; actual limits are disk, RAM, file-format/implementation boundaries, and document validity. No byte-size policy is used to reject an otherwise valid document.
