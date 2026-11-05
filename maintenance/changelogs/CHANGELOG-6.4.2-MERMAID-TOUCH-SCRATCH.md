# LocalGPT 6.4.2 — Mermaid label recovery and mobile cat scratching

## User-visible defects

- Desktop documentation diagrams could retain truncated or horizontally overrun node labels while Mermaid was considered successfully rendered.
- Touch/mobile devices could not create page tears because coarse pointers were rejected in JavaScript and the tear overlay was hidden in CSS.

## Changes

- Verify actual SVG label bounds against node outlines rather than assuming Mermaid completion means usable labels.
- Preserve original Mermaid source before rendering; when measured desktop labels overflow, attempt a targeted HTML-label rerender of affected diagrams.
- For DocFX-rendered SVGs without a source to rerender, fit overflowing node text in place. The remaining SVG is preserved on a failed attempt.
- Perform two bounded delayed post-render audits and recheck when crossing the desktop/mobile breakpoint, avoiding late-rendered regressions.
- Add an opt-in mobile **🐾 Scratch / ↕ Scroll** control. Scroll is always the default. Scratch mode sets `touch-action:none` only on the documentation content surface.
- Touch paths support the existing narrow tear mask, SVG curled-paper edges, audio effects, pointer-following paw and reset-on-navigation behavior. The same scene remains available to mouse and pen.
- Preserve reduced-motion preference, interactive controls, real documentation text/DOM, DocFX content and generation scripts, and all maintenance rules.

## Validation

- JavaScript syntax checks for maintained documentation scripts.
- Source regression suite and isolated JavaScript runtime tests for label overflow detection/fitting and mobile mode toggling.
- Not .NET/DocFX-compiled or accepted on a physical phone or installed Windows documentation page.
