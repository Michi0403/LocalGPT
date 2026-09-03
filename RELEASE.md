# LocalGPT 3.6.6

LocalGPT 3.6.6 repairs the documentation release path exposed by a clean/retry build on macOS. The application assembly itself built successfully, the durable DocFX HTML cache restored successfully, and the failure occurred only when the large browser print job failed and the script attempted the DocFX PDF fallback without having restored a DocFX command for that cached-HTML path.

The release script now resolves DocFX lazily when the PDF plug-in fallback is required. This keeps durable HTML-cache reuse fast while preventing a null PowerShell invocation target. LocalGPT manuals above 1000 printable HTML pages also bypass the monolithic browser print-book attempt and go directly to the DocFX PDF plug-in; the current 1100+ page manual has already demonstrated that Edge can fail the one-shot print path on macOS.

The 3.6.5 Apple-Silicon/Rosetta architecture diagnostics, exact Mach-O package manifest, dynamic macOS port handling, Future2 positioning, DevExpress licensing clarification, visible console, user-data permissions, optional local-AI setup helpers, and cross-platform packaging work remain in place.

See `CHANGELOG-v3.6.6-DOCFX-PDF-CACHE-FALLBACK-REPAIR.md` and `VALIDATION-v3.6.6-source.md`.
