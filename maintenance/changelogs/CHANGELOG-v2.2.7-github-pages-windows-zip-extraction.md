# LocalGPT 2.2.7

## GitHub Pages release extraction

- Fixed the Pages extractor for application ZIPs created on Windows whose member names use backslashes.
- Portable path matching now uses normalized forward-slash names while reads use the exact stored `ZipInfo` entry.
- Added duplicate-normalized-member rejection so mixed slash spellings cannot create an ambiguous extraction path.
- Preserved traversal protection, bounded reads, Kawaii theme markers, generated API checks, PDF checks and project-relative asset validation.
- Kept `actions/checkout@v6` and the existing Node.js 24 Pages workflow.

## Scope

No frontend, localization, Council, game, persistence, installed-documentation or Kawaii website behavior was removed. The separately versioned 1-Wire protocol remains at 2.1.0.
