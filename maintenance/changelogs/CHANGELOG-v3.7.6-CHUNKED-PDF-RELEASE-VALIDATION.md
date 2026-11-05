# LocalGPT 3.7.6 — chunked PDF release validation

## Fixed

- Fixed the release failure reported after a successful 1,145-page adaptive documentation render: `Build-Documentation.ps1` correctly emitted `pdfMode: html-browser-chunked`, but `Build-Release.ps1` rejected that mode as incomplete. The release validator now recognizes the chunked browser/PDFsharp path as a supported complete HTML-backed PDF mode.
- Kept the safety checks intact for the chunked path. `pdfSourcePageCount` is now checked against both the minimum expected page set and the generated API HTML count, so accepting `html-browser-chunked` does not weaken completeness validation.
- Aligned `Build-LocalDevelopment.ps1` with the release validator so the same valid chunked/compatibility PDF outputs are not rejected in development builds.
- Aligned post-compression accessibility handling with the chunked browser mode; any rewritten browser-backed PDF keeps the generated HTML site as its accessibility fallback.
- Fixed cached browser-PDF accessibility validation: a previously validated browser-native tagged PDF can be reused from the durable cache without being misclassified merely because cache reuse is recorded as `cached-validated-pdf`; unknown accessibility states are still rejected.
- Strengthened `Assert-InteractiveServerRenderModes.ps1`: every routed LocalGPT page except the intentional static fatal-error fallback must remain a prerendered `InteractiveServer` page, preventing future routes from silently losing interactivity.

## Preserved

- The adaptive bounded browser rendering, PDFsharp merge, optional qpdf/Ghostscript optimization, 256 MiB sane-size ceiling, durable documentation cache, embedded offline help PDF, Full/self-contained packaging, signing/notarization resume behavior, and LocalGPT-owned `LocalGPT.ReleasePackaging` 1.0.2 package model are unchanged.
- Existing page-level and reviewed island render boundaries remain unchanged; nested child components continue to inherit their owning circuit instead of creating competing nested render-mode boundaries.

## Validation

Static release/version, documentation-mode, render-boundary, architecture, cross-platform, async/service and XML-documentation audits are retained. This source package was not built with .NET in the editing environment; the macOS/Windows release hosts remain the authoritative execution test.
