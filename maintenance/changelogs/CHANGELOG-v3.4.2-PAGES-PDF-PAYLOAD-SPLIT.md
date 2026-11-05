# LocalGPT 3.4.2 — Pages PDF Payload Split

- Fixed the 3.4.1 release failure where the pinned GitHub Pages ZIP expanded to more than 4.5 GB because the 2.25 GB handbook was present both at the DocFX `pdf/` path and at the canonical documentation root.
- Removes the nested DocFX PDF candidate after the canonical root copy is validated, preventing duplicate multi-gigabyte payloads.
- GitHub Pages now publishes the fully validated HTML/API documentation without embedding the release PDF in the tracked Pages ZIP. The Pages snapshot preserves release-PDF metadata and rewrites the handbook link to the latest GitHub release.
- The full release documentation still validates the PDF before the Pages snapshot is prepared.
- Large DocFX fallback PDFs are no longer read wholesale into Python memory just to probe a structure token when the strict HTML-accessibility fallback is already in force.
