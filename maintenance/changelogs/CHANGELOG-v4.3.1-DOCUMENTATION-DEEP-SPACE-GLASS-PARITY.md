# LocalGPT 4.3.1 — Documentation deep-space glass parity

LocalGPT 4.3.1 refines the Kawaii documentation shell shared with PublisherStudio.

- Replaces the visually repeating floating-decoration cadence with a bounded, randomized star field generated per page load.
- Gives stars independent brightness cycles, drift vectors, delays, sizes, and durations so the background does not pulse or move in lock-step.
- Adds mostly white/lavender stars plus sparse warm-yellow, pale-blue, and pink stars, including a small number of cross-shaped sparkles.
- Adds one or two tiny CSS-built satellites with soft motion and a subtle heart detail, plus a rare ringed planet accent on desktop; no remote image asset is required.
- Makes navigation rails, article cards, API panels, cards, details panels, and tab surfaces more translucent while keeping text fully opaque and using backdrop blur for readability.
- Increases the desktop outer gutter so the right-hand table-of-contents rail no longer sits too close to the viewport edge.
- Preserves `prefers-reduced-motion`: stars and satellites remain decorative but stop animating when reduced motion is requested.
- Keeps authored theme source, shipped in-app help assets, and the tracked Pages snapshot CSS/JavaScript synchronized with cache-busting hashes.

The application behavior outside documentation styling is unchanged from 4.3.0.
