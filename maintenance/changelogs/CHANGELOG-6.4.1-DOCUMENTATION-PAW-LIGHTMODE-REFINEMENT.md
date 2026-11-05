# LocalGPT 6.4.1 — documentation light-theme paw and tear refinement

## Summary

Refined the maintained documentation pointer paw and the light-theme tear rendering without changing the scratch interaction model, content masking scope, or document layout. Dark theme behavior stays intentionally close to the previous release.

## What changed

- Light theme paw now uses a full all-around dark halo instead of a bottom-heavy dark blob.
- The paw fur silhouette is reduced to a subtle contour so all paw parts stay readable on pale backgrounds.
- Light theme tears now reveal a bright iridescent white/pastel crack instead of a comparatively flat dark void.
- Light theme tear bloom is stronger, giving the scratch path a more visible glow analogous to dark mode.
- No changes were made to pointer hotspots, scratch geometry, drag behavior, sound behavior, navigation reset, or documentation source content.

## Validation

- Static source regression checks updated for version 6.4.1.
- Targeted Python/CSS tests pass.
- Headless-browser fixture remains expected to show the paw and overlay tears without altering document flow.
