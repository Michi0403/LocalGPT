# LocalGPT 5.5.0 — Windows documentation PDF merge repair

## Confirmed failure

- The 5.4.9 Windows build completed the DocFX HTML build and rendered all 1505 documentation pages in one PDF cover/index plus 126 bounded PDF chunks. It failed at the subsequent `LocalGPT.ReleasePackaging.exe pdf-merge` process launch with `Der Dateiname oder die Erweiterung ist zu lang`.
- The failure was caused by passing 127 long absolute PDF paths as individual native command-line arguments, exceeding the process-launch command-line limit. It was **not** a change in repository paths, a storage-capacity problem or a PDF rendering failure.

## Repair

- The documentation script writes the ordered absolute PDF input paths to one UTF-8 manifest in the existing repository-owned temporary documentation workspace. The merge process now receives the manifest path with `pdf-merge --input-list` instead of hundreds of `--input` arguments.
- `LocalGPT.ReleasePackaging` reads that manifest in order, validates non-empty input paths and file existence, and uses the same PDFsharp import, merged-PDF validation and atomic output commit as before. Existing direct `--input` calls remain supported. Mixing both input modes is rejected explicitly to prevent accidental reordering.
- A process-launch exception is reported as a managed PDF merge error, while the previously rendered durable chunks are retained for the next attempt. Temporary manifest files are cleaned with the existing work directory after the merge.
- The PDF requirement, bounded browser rendering, memory policy, durable chunk reuse, DocFX HTML validation, PDF completion validation, versioned output, storage locations, and monolithic-fallback safety policy are unchanged. No architecture guard was disabled.

## Version and scope

- LocalGPT advances from **5.4.9 to 5.5.0**, honoring the one-digit minor/patch policy; the edited release-packaging helper advances from **1.0.2 to 1.0.3**.
- Retains the 5.4.9 per-file learning checklist, Council and tool-call repairs, Human Collaboration permission gate, DevExpress UI, all prior maintenance constraints, and both existing source-level checks and new PDF-merge manifest audit.
- PublisherStudio source is unchanged; only the LocalGPT source ZIP is distributed for this release.
- No .NET restore/build, browser PDF render, Windows PowerShell execution or live merge was performed in the source-only preparation environment. The maintainer's Windows build remains the runtime acceptance test.
