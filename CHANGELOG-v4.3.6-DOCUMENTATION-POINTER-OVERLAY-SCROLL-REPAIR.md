# LocalGPT 4.3.6 — documentation pointer-overlay scroll repair

## Scope

This release intentionally changes only the documentation pointer-decoration containment contract and the release metadata required for the new version. It does not redesign the documentation shell, star field, glass surfaces, navigation, diagrams, game UI, provider behavior, or application runtime.

## Fixed

- Fixed the documentation paw trail and custom paw cursor expanding the page and drifting away from the pointer.
- Root cause: the later documentation stacking selector applied `position: relative` to every direct `body` child except the star sky. Paw/cursor/sparkle elements were appended directly to `body`, so that selector overrode their earlier fixed positioning, returned them to normal flow, and allowed their `left`/`top` offsets to enlarge the scrollable document.
- Added one dedicated fixed, viewport-sized, clipped, strictly contained pointer overlay. Paw cursor, paw trails, hover sparkles, click paw/scratch effects, and click pop decorations now render inside that overlay instead of normal document flow.
- The content-stacking selector explicitly excludes the pointer overlay. Pointer decorations use viewport `clientX`/`clientY` coordinates inside that overlay.
- Removed the custom-paw transform transition so the replacement cursor no longer visibly lags behind the actual pointer.

## Regression protection

- Added repository guidance stating that decorative/background work must not alter article, rail, footer, scroll, sizing, or stacking behavior unless explicitly requested.
- Added `build/Assert-DocumentationPointerOverlay.ps1` and wired it into normal LocalGPT builds. The guard rejects direct body insertion of the maintained transient pointer decorations and requires viewport containment plus the stacking exclusion.
- Added `build/audit_release_4_3_6.py` to verify the maintained theme, embedded help copies, cache keys, and tracked Pages snapshot stay synchronized.
