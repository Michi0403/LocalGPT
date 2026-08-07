# LocalGPT 2.3.7 validation

This repair keeps the 2.3.6 application feature set and restores the proven 2.3.2 Kawaii documentation shell and publishing behavior.

Validation completed in the source-packaging environment:

- Restored Kawaii CSS and JavaScript are byte-identical to the 2.3.2 maintained theme source.
- JavaScript syntax check passed for 137 `.js`/`.mjs` files.
- Python syntax check passed for all 8 maintained `.py` files.
- XML project/target/profile files and JSON configuration files parse successfully.
- Documentation/1-Wire contract audit passed.
- Kawaii layout audit passed: equal rails, symmetric gaps, full-width articles, synchronized site assets, and the tracked Pages snapshot matches the maintained theme source.
- GitHub Pages snapshot validation passed with 884 HTML files, 855 API HTML files, valid local links, required accessibility landmarks/metadata, version 2.3.7, synchronized Kawaii assets, and a tagged PDF.
- The tracked source handbook is `LocalGPT-2.3.7.pdf`: 73 pages, 565,067 bytes, A4 portrait, tagged PDF. It prints all maintained conceptual chapters in full and a complete inventory of all 853 generated API detail pages; the full generated API bodies remain in the shipped HTML site. Representative rendered pages (cover, contents, conceptual body, migration/reference body, final API inventory) were visually checked for clipping, overlap, or broken glyphs.
- The tracked-source Pages gate rejects obsolete/tiny fallback PDFs and correctly detects PDF structure tags even when the catalog lives in a Flate-compressed object stream.
- Owner-side release checks remain stricter: complete release PDFs must be at least 1 MiB and HTML-browser PDFs must cover at least the generated API page count.

The packaging environment intentionally has no .NET SDK and no PowerShell runtime. No claim of .NET compilation is made here. The normal Windows .NET 10 owner build remains the compilation and current assembly/XML DocFX release authority.
