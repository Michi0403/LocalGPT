# LocalGPT 3.5.8

LocalGPT 3.5.8 hardens the **macOS documentation/PDF release path** while retaining the optional WSL2 Linux backend introduced in 3.5.7.

The complete DocFX HTML site remains mandatory and is still generated once for all runtime packages. PDF generation now tries the normal tagged Chromium/Edge print first and, if the browser exits without a valid PDF or the large tagged render fails, retries the same complete HTML print book in a lower-overhead compatibility mode. Compatibility PDFs are recorded truthfully as `html-accessibility-fallback`; the already-strict generated HTML accessibility/link preflight remains mandatory.

The DocFX Playwright PDF plug-in remains a final fallback, but macOS no longer inherits an unavoidable 30-minute timeout floor. Its default fallback timeout is five minutes on macOS and thirty minutes elsewhere, and an explicitly supplied positive `DOCFX_PDF_TIMEOUT` value is respected instead of being silently raised.

Windows/WSL2, native Linux, macOS host-aware runtime selection, Linux packaging, DevExpress license bridging, LocalGPT.ReleasePackaging 1.0.1, and the explicit InteractiveServer boundaries are unchanged from 3.5.7.

See `CHANGELOG-v3.5.8-MACOS-DOCUMENTATION-PDF-RENDER-RECOVERY.md`, `VALIDATION-v3.5.8-source.md`, and `docs/engineering/wsl-linux-release.md`.
