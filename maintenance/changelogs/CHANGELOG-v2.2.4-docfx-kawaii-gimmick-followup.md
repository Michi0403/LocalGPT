# LocalGPT 2.2.4

This patch advances the application after the 2.2.2 package so the completed documentation-site work is not republished under an already used version.

- Applies the full pink/lavender Kawaii DocFX shell to the generated HTML website instead of styling only selected text.
- Preserves functional DocFX Light, Dark and Auto modes with matching Kawaii palettes and readable navigation, article, API, table, code and footer surfaces.
- Replaces the stock DocFX `D` branding with a LocalGPT cat badge.
- Keeps decorative cats, dogs, paws, stars, shimmer, sparkles and gentle motion while honoring `prefers-reduced-motion`.
- Activates and cache-busts the custom CSS and JavaScript on every generated page for the application WebView, local HTTP host and GitHub Pages.
- Validates generated API and PDF links through build-time source resources without shipping a placeholder PDF.
- Publishes the exact `wwwroot/help-docs` release payload to GitHub Pages, including `.nojekyll`, the API tree, theme assets and versioned PDF.
- Retains the working full-request localization switch, recursive installed-documentation discovery, Council provider selector, centered ASCII-game guide, DX catalog reconciliation and all frontend features.
- Leaves `LocalGPT.WireProtocolVersion` independently versioned at 2.1.0.


## Follow-up refinements in 2.2.4

- Removed the broken documentation-site moon/theme toggle button while preserving a cute animated heart glimmer in the navbar.
- Added a playful paw-following cursor, fading paw trail, and cat-paw scratch click effect that run only inside the documentation website.
- Strengthened Kawaii shell styling so sidebars, TOCs, panels, and supporting chrome use the same pink/lilac themed surfaces as the PDF-inspired design.
- Light/dark modes continue to work automatically through system preference and existing theme state.
