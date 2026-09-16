# LocalGPT 4.3.6

LocalGPT 4.3.6 is a deliberately narrow documentation maintenance release. It fixes the paw-trail/custom-cursor scroll-growth bug without changing the current star background, glass design, documentation layout, diagrams, game behavior, provider behavior, or chat runtime.

The concrete defect was a CSS stacking rule that changed every direct documentation `body` child except the star sky to `position: relative`. Because the paw cursor and transient paw/sparkle elements were direct body children, that later rule overrode their fixed positioning. Their pointer coordinates then became layout offsets and expanded the document. 4.3.6 moves all maintained transient pointer decorations into one fixed, viewport-sized, clipped and contained overlay and excludes that overlay from the content-stacking rule.

The repository now carries a build-time pointer-overlay guard and contributor guidance that decorative/background work must remain scroll- and layout-neutral unless a task explicitly requests a layout change.

This is source-only validation in this environment. No .NET build, restore, publish, signing/notarization, GitHub access, or GitHub API operation was performed. See `CHANGELOG-v4.3.6-DOCUMENTATION-POINTER-OVERLAY-SCROLL-REPAIR.md` and `VALIDATION-v4.3.6-source.md`.
