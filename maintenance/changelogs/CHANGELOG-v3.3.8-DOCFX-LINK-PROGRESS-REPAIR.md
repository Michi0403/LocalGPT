# LocalGPT 3.3.8 — DocFX link and progress repair

## Fixed

- Changed the conceptual API link from `../api/index.html` to the authored `../api/index.md`, allowing DocFX to validate the source link and rewrite it to the generated HTML target.
- Added the build-time `LocalGPT-*.pdf` validation stub to DocFX resources so the handbook link is valid during the site build before the real PDF replaces the stub.
- Changed `Invoke-LocalGptDocfx` from buffered native-process output to live streamed output while retaining the complete captured diagnostics for retry and failure reporting.
- Runs the long `docfx pdf` phase with verbose diagnostics and explicitly reports that four-digit page sets can take several minutes to render.
- Preserved the 3.3.7 parser preflight and the isolated `System.Formats.Nrbf` dependency probe that now resolves DocFX metadata with zero assembly-reference warnings.
- Updated active LocalGPT version, documentation, cache, PDF and outbound markers to 3.3.8 while preserving the existing interactive render-mode boundaries.

## Why

The macOS arm64 3.3.7 release run proved that the NRBF probe works: metadata generation completed with zero warnings and zero errors. The subsequent site build still emitted two link warnings, and the PDF phase appeared frozen because the wrapper collected all native DocFX output in memory until the process exited. A 1,132-page PDF render is expected to be materially slower than the HTML build, so the release pipeline now both removes the avoidable link warnings and makes the long-running renderer visibly observable.
