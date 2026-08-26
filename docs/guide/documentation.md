# Documentation inside LocalGPT

## One documentation tree

The conceptual documentation and compiler-generated API reference are built into one DocFX site. That exact static tree is:

- copied into `wwwroot/help-docs` for the running application;
- packaged inside release ZIPs;
- extracted by the GitHub Pages workflow;
- used to generate the versioned PDF or packaged handbook.

GitHub Pages does not rebuild LocalGPT or require DevExpress packages. It publishes the already reviewed documentation shipped in a release.

## Important routes

- `/help-docs/index.html` — documentation home in the local application.
- `/help-docs/api/index.html` — generated API reference.
- `/api/documentation/status` — build mode, counts, PDF state, and theme hashes.
- `/api/documentation/comments` — bounded XML-comment search.
- `/api/documentation/pdf` — current versioned PDF.

On GitHub Pages, the equivalent root is `/LocalGPT/`.

## Theme selector

The navigation theme menu supports **Light**, **Dark**, and **Auto**. The selected value is written to:

- DocFX's `theme` local-storage key for compatibility;
- LocalGPT's `localgpt-docs-theme` local-storage key;
- the first-party `localgpt-docs-theme` cookie.

The cookie uses `Path=/`, `SameSite=Lax`, and a one-year lifetime. HTTPS pages add the `Secure` attribute. A tiny bootstrap script applies the stored value in the document head before the body is painted, reducing light/dark flicker.

The GitHub Pages origin and the local application origin have separate browser storage. A selection persists on each host, but browsers correctly do not share it across those origins.

## Kawaii shell

The Kawaii layer is deliberately separate from DocFX's generated content. It provides the gradient shell, paw/cat branding, cards, subtle motion, cursor trail on fine pointers, and light/dark palettes. Reduced-motion and coarse-pointer users keep normal cursor and animation behavior.

## PDF

The owner-side documentation build can assemble a complete PDF from generated conceptual and API HTML pages. It requires meaningful source-page and API-page counts and rejects a tiny fallback shell. Website-only motion and navigation are removed in print styles.

A prebuilt source package may include a curated handbook: all maintained conceptual chapters are printed in full, followed by a linked inventory of every generated API page. The full API reference remains available in HTML, and the regular LocalGPT build can regenerate the complete HTML-backed PDF when .NET, DocFX, and a supported browser are present.

## Publishing verification

The Pages extractor verifies:

- `index.html` and `api/index.html`;
- current Kawaii CSS and JavaScript markers;
- `documentation-status.json` (including UTF-8 BOM compatibility);
- project-relative assets;
- a nonzero API count;
- the versioned PDF when available.

The Node deprecation messages emitted by GitHub's own actions are warnings; deployment success is determined by artifact upload and the Pages deployment result.
