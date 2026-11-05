# LocalGPT 2.2.6

## GitHub Actions Node.js 24 maintenance

- Updates the GitHub Pages workflow from `actions/checkout@v4` to `actions/checkout@v6`.
- Removes the Node.js 20 deprecation warning emitted by the old checkout action on current GitHub-hosted runners.
- Keeps `actions/configure-pages@v5`, `actions/upload-pages-artifact@v4`, and `actions/deploy-pages@v4`; their current releases use Node.js 24-compatible implementations.
- Preserves the exact shipped Kawaii DocFX extraction and deployment flow, including theme validation, PDF/API verification, `.nojekyll`, Pages source checks, light/dark mode, paw cursor and click-scratch animation.
- Does not change application features or the separately versioned 1-Wire protocol.

The package remains unverified until the workflow is executed on GitHub-hosted Actions and the Windows build is run.
