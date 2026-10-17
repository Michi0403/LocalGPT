# LocalGPT 5.9.4 — same-version PDF reuse repair

## Reported regression

A Windows Debug build of LocalGPT 5.9.3 passed compilation, DocFX HTML generation, and HTML preflight but proceeded to print the complete handbook again despite an existing published PDF for that version.

## Root cause

`Build-Documentation.ps1` reused full PDFs only from a content-addressed cache keyed by the DLL, XML documentation, build script, and documentation sources. Rebuilding the DLL can invalidate that key without changing the public product version. More importantly, the early HTML-publication step removed the last published `wwwroot/help-docs` trees **before** a same-version PDF could be considered for recovery. The script had no fallback to an already published, verified PDF of the matching version.

## Fix

- Prefer the existing content-addressed PDF cache when a valid entry exists.
- On a cache miss, search already published documentation roots for the PDF whose name and `documentation-status.json` version match the current version.
- Require the same count of printable HTML pages, prior successful DocFX publication, complete-file markers, and a successful `LocalGPT.ReleasePackaging pdf-verify` invocation before recovery.
- Copy the validated PDF into the freshly generated site **before** the early HTML publication clears the old output directories. The second publication writes the normal status metadata.
- Skip browser printing, recompression, and recaching of an already validated same-version PDF.
- Support `FUTURE2_DOCUMENTATION_FORCE_PDF_REBUILD=1`, including bypassing cached PDF chunks for a genuinely fresh print.
- Keep the established source/release PowerShell architecture, PDFsharp 6.2.4 MIT vendoring, page-count verification, and failed-job chunk resume behavior unchanged.

## Publication policy

Same version and source-page count cannot prove identical document *content*. To avoid stale documents after semantic changes, increment the LocalGPT version or use the explicit force-rebuild option. First build of 5.9.4 prints once; subsequent unchanged-version builds can reuse its validated published PDF.
