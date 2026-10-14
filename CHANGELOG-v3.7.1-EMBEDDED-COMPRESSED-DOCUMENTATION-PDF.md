# LocalGPT 3.7.1 — embedded compressed documentation PDF

Version advanced from 3.7.0 to 3.7.1 because release/documentation scripts changed.

## Fix

- Restored the generated `LocalGPT-3.7.1.pdf` to the runtime `wwwroot/help-docs` payload used by the embedded documentation mechanism.
- The PDF is embedded only after the existing release PDF size-control stage. Browser-generated PDFs can remain native when already sane; oversized or DocFX-fallback PDFs are optimized through Ghostscript and the release fails if the final handbook exceeds the configured ceiling.
- Runtime documentation status now truthfully reports `pdfAvailable=true` and `runtimePdfPublished=true`.
- Runtime documentation copies preserve the local handbook link instead of rewriting it to a GitHub release URL.
- Runtime validation requires exactly one current embedded PDF, verifies its physical byte size against `documentation-status.json`, and enforces the configured sane-size ceiling.
- The source/runtime `help-docs` snapshot keeps the size-controlled PDF instead of deleting it after documentation generation.
- Full/self-contained-only Unix/macOS packaging, resumable notarization, configurable cache/output roots, Homebrew RPM support, and failed-build cleanup from 3.7.0 remain unchanged.

No Light runtime lane was reintroduced.
