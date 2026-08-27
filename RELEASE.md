# LocalGPT 3.4.2

LocalGPT 3.4.2 is the **Pages PDF Payload Split** release.

It keeps the 3.4.x cross-platform backend boundaries and fixes the post-documentation release failure caused by duplicating a multi-gigabyte PDF into the tracked GitHub Pages snapshot. GitHub Pages now receives the validated HTML/API site, while the complete PDF remains part of release documentation rather than the tracked Pages archive.

This handoff is source-only and was not built with .NET or executed with PowerShell in the packaging environment. See `CHANGELOG-v3.4.2-PAGES-PDF-PAYLOAD-SPLIT.md` and `VALIDATION-v3.4.2-source.md`.
