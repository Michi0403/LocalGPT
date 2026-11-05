# LocalGPT 6.4.0 — Documentation Paw Visibility

## Changed

- Refined the existing documentation paw cursor using the same inline SVG, with a darker miniature fur silhouette behind the original iridescent paw pads, subtle fur-tip accents, and clearer pastel pink–cyan highlights.
- Increased the cursor artwork from 3.15rem to 3.4rem without moving its maintained pointer hotspot.
- Dark theme: thin bright silhouette rim with a restrained cyan, blue and violet glow, readable against deep-purple backgrounds.
- Light theme: deliberately shadow-led treatment (plum/charcoal soft drop shadows and only a small cool-color highlight) so the paw contrasts on pale documentation surfaces.
- No changes to DocFX or PDF generation, existing documentation content, theme selection, claw/scratch geometry, masks, navigation reset, sounds, or the pointer overlay lifecycle.
- Followed the project version rule: 6.3.9 → 6.4.0, never 6.3.10.

## Offline verification

- 69/69 Python source-regression tests passed, including new theme and scratch-isolation checks.
- Browser fixture: light and dark paw render, pointer position, theme-resolved color/shadow, and click/drag content masking checked in Chromium for both product styles.
- JavaScript syntax validated with `node --check`.
- .NET and the installed DocFX runtime were not available and were not built/tested. Installed behavior requires acceptance testing after regenerating documentation assets.
