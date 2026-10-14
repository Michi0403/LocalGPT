# LocalGPT 4.3.6 source validation

## Scope

Source-only validation of the documentation pointer-overlay scroll repair. No .NET build, restore, publish, signing/notarization, GitHub access, or GitHub API operation was performed in this environment.

## Checks performed

- `node --check docs/templates/localgpt/public/main.js` — passed.
- `python build/audit_release_4_3_6.py` — passed.
- Maintained LocalGPT documentation CSS/JavaScript are byte-identical to the embedded `public` and `styles` copies.
- Tracked Pages snapshot theme assets are byte-identical to the maintained theme source and the ZIP passes `testzip()`.
- Help-document cache keys match the SHA-256 prefixes of the current theme assets.
- `Directory.Build.targets` remains well-formed XML after wiring the pointer-overlay build guard.

## Browser geometry regression check

Chromium headless was used with the full maintained 4.3.6 documentation CSS and a synthetic viewport overlay containing 80 paw-trail elements spread across a 1200×800 viewport. Before and after inserting the trails, document scroll geometry stayed unchanged (`documentElement`: 1200×1170; body: 1184×1154). Computed styles confirmed the overlay was `position: fixed`, `overflow: clip`, `contain: strict`, 1200×800, and the trail elements were `position: absolute`.

A minimal reproduction of the former selector confirmed the defect mechanism: the old `body > :not(.localgpt-kawaii-sky)` stacking rule changed direct paw-trail children to `position: relative`, and synthetic trails expanded a 1200×900 document to 2475×2326. The overlay/exclusion form kept the same test at 1200×900.
