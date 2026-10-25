# LocalGPT 6.3.9 — Fine documentation scratches

## Scope

Refined the *existing* DocFX documentation cat-scratch Easter egg in the maintained theme source. The same left-mouse drag still scratches through rendered documentation text without mutating document files. Damage accumulates until navigation/refresh. The selected iridescent cursor paw, click scratch animation, synthesized scratch audio, text-selection override (Shift), and page navigation remain unchanged.

## Changes

- Three parallel claw cuts now use 16px spacing rather than 25px spacing.
- Cut borders use a narrow ~2-3px half-width per side, rather than the previous broad 12px half-width; irregular contour nicks are under ~1px.
- A single geometry feeds the alpha mask, paper lip, tear outline and occasional torn fibers, removing spatial disagreement between the effects.
- Tapered ends and 6px path resampling prevent two-point diamonds and reduce angular/polygonal tearing.
- Reduced folded paper flaps to sparse 3-6px features and kept fibers delicate.
- Glow/outline strokes are thinner and weaker. Separate light and dark appearances retained.
- Removed duplicated prior tear CSS declarations; existing unrelated documentation CSS is untouched.
- Documentation source and generation scripts, DevExpress/PowerShell maintenance gates, popup/layout code, and packaged documentation snapshots were not modified.

## Verification

- Browser synthetic fixture: three actual alpha-masked cuts via held left-button drag, 30 small corner flaps in one long stroke, identical DOM text after scratch, and route-reset on hash change. Tested using Chromium in both light and dark themes for both projects. Fixture runs the tear installer separately: the about:blank origin rejects cookies, so full documentation startup is *not* browser-certified by this fixture.
- JavaScript syntax: `node --check` succeeds in both projects.
- Offline tests: LocalGPT 67 Python tests; PublisherStudio 16 JavaScript tests. Historic version-locked regression expectations were updated to the exact current release version. No production architecture checks were disabled or loosened.
- ZIP CRC/integrity verified.

.NET/DocFX and Windows live builds were **not** run. The generated docs must be rebuilt from theme sources for end users to receive this effect.
