# LocalGPT Kawaii documentation rework

This source package replaces the flat DocFX reader experience with a maintained product, architecture, engineering, and reference structure while preserving the original notes for repository archaeology.

## Public documentation

The public DocFX input is now limited to:

- `docs/index.md`
- `docs/guide/**`
- `docs/architecture/**`
- `docs/engineering/**`
- `docs/reference/**`
- `docs/api/index.md` plus the XML-generated API metadata

Superseded top-level documentation pages and patch-specific `docs/articles` pages were removed from the public input. Their durable content was integrated into the maintained sections.

## Information preservation

The 46 superseded top-level pages and 9 former release articles are retained byte-for-byte under `docs/internal-notes/legacy-source`. `docs/reference/documentation-migration.md` maps every former public page and article to its maintained destination and original preserved copy.

## Theme and branding

- Replaced the unstable DocFX/Bootstrap theme dropdown interaction with an accessible native `details` control for Light, Dark, and Auto.
- Persists the choice in the DocFX `theme` key, the LocalGPT `localgpt-docs-theme` local-storage key, and a one-year `localgpt-docs-theme` cookie.
- Applies the stored theme in the document head to reduce repaint flicker.
- Keeps storage correctly separate between the local application origin and GitHub Pages.
- Adds a cat-paw SVG/ICO favicon and matching navigation logo.
- Keeps reduced-motion and coarse-pointer fallbacks.

## GitHub Pages publishing

The release extractor still accepts Windows ZIP separators and UTF-8 BOMs. Its theme markers now validate the current stable selector and persistence implementation rather than the obsolete hidden-toggle functions.

## Static validation completed

- JavaScript syntax checked with Node.js.
- CSS parsed without syntax errors.
- DocFX JSON and maintained TOC YAML parsed successfully.
- 885 shipped HTML files checked, including 856 API pages.
- All full HTML pages contain the Kawaii activation, favicon, early theme bootstrap, CSS, and JavaScript markers.
- All relative links and local assets in the shipped site resolve.
- The in-app `wwwroot/help-docs` tree matches `docs/_site`.
- A synthetic release ZIP was accepted and extracted by `.github/scripts/extract-shipped-docs.py`, with matching CSS and JavaScript hashes.
- All 55 legacy source pages were compared byte-for-byte with the supplied pre-rework documentation.

## Not executed in this environment

No .NET/DevExpress build or owner-database startup was claimed. Headless Chromium could not complete a browser session in this container, so the selector interaction should still receive a normal owner-side browser/WebView smoke test before the next release.
