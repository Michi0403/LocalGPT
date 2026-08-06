# LocalGPT 2.3.4 validation

LocalGPT 2.3.4 is a source-validated Kawaii documentation-layout milestone. The owner-side Windows build, application runtime, publish, installer, browser, and GitHub Actions runs remain authoritative.

## Completed checks

- Architecture policy audit: passed.
- Explicit async-continuation audit: passed for **146 source files**.
- Provider-qualified Council audit: **85 checks passed**.
- Removable Chat ASCII-console audit: **17 checks passed**.
- Symmetric Kawaii documentation-layout audit: passed.
- The maintained theme source, generated DocFX site, in-app help site, and pinned GitHub Pages snapshot use byte-identical layout CSS.
- Both desktop rails use the same responsive width and both center gaps use the same spacing variable.
- The center article consumes the remaining width, while short pages fill one viewport and longer pages use normal document scrolling.
- The tracked Pages artifact preparation validator passed for **885 HTML pages** and **856 API pages**.
- Kawaii Auto, Light, and Dark persistence, paw branding, cache-busted assets, relative links, and snapshot hashes remain validated.
- Application, installer, desktop wrapper, documentation metadata, and PDF naming are aligned to **2.3.4**.
- Wire protocol versioning remains independent.

## Required owner-side checks

1. Run the complete maintained Windows validation and build chain.
2. Open the generated in-app documentation and GitHub Pages output at 100% zoom.
3. Confirm the left and right rails have equal width and equal distance from the article.
4. Confirm short pages do not create an unnecessary vertical scrollbar.
5. Confirm long pages use normal document scrolling without nested panel scrolling.
6. Confirm Auto, Light, and Dark modes retain identical geometry.
7. Publish the release and run the pinned-snapshot GitHub Pages workflow.
