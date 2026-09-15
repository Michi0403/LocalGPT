# LocalGPT 4.3.2 — Documentation PDF, Mermaid, and glass recovery

LocalGPT 4.3.2 repairs the documentation regressions visible in 4.3.1 without changing application architecture or established Blazor render-mode ownership.

- Normal documentation builds require the complete versioned PDF again. `RequireLocalGptDocumentationPdf` defaults to `true`; HTML-only output requires the explicit diagnostic override `RequireLocalGptDocumentationPdf=false`.
- The existing PDF generator, validation, runtime copy, `/api/documentation/pdf`, and Pages snapshot flow remain authoritative. A normal build now fails rather than publishing a dead PDF action.
- HTML-only diagnostic output no longer describes the PDF as a Release-only feature.
- Mermaid blocks skipped by DocFX while the embedded viewer is hidden are retried after visibility changes and rendered through DocFX's bundled Mermaid module, restoring the UML-like flow charts instead of leaving raw `flowchart` text.
- The old tiled pseudo-element star textures are disabled. The generated background now uses 112 independently randomized desktop stars (48 compact), stronger brightness variation, 3–5 drifting non-tiled nebula layers, 2–3 desktop satellites, and an occasional ringed planet.
- Article, navigation, API, card, details, and tab surfaces use materially translucent glass backgrounds with backdrop blur while keeping text itself fully opaque.
- The desktop shell receives a symmetric viewport gutter and explicit right-rail padding so the article TOC no longer crowds the browser edge.
- `prefers-reduced-motion` remains respected.
- Authored theme assets are synchronized into both checked-in DocFX asset locations and the tracked Pages snapshot; generated HTML cache keys were refreshed. The older generated API/PDF payload inside the checked-in snapshot was deliberately not relabeled as 4.3.2 because no .NET documentation build was run here.
- Existing routed `InteractiveServer` boundaries are preserved; child components still inherit their owning circuit and `Error.razor` remains intentionally static.

No .NET build, restore, publish, signing/notarization, GitHub access, or GitHub API operation was performed.
