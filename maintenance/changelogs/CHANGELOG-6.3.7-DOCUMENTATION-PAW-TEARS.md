# LocalGPT 6.3.7 — documentation paw and ephemeral paper tears

## User-facing changes

- Upgrade the **documentation-only** floating cat paw to a ~2.5× larger glossy lilac/violet/blue/cyan vector with separate light/dark glow, following the user's chosen Option 2 paw.
- Add the requested Option 3-inspired **interactive claw tear** inside the current DocFX `.content` surface. Primary-button pointer-down makes three short cuts; holding and dragging extends those cuts along the pointer trajectory. Repeated scratches accumulate.
- Use an **alpha SVG mask on the existing content element**, cutting through the *actual rendered text, images and document background*, together with a separate noninteractive SVG depicting a dark/light underlayer, jagged paper edges, peeled fiber tabs, soft bloom and shadow.
- Maintain the original click-burst animation, paw trail, sparkles, theme selector and navigation; no DocFX generation, C#, site templates or renderer ownership were changed.
- Vary subtle paper-scratch audio with Web Audio on actual user gestures; no external media files or autoplay. Touch/reduced-motion visitors retain the original accessible pointer behavior.
- The damage lives only in document memory and clears on navigation/hash change, page unload or refresh. Shift+drag still selects text; interaction controls are exempt from tearing.

## Scope and safety

- Changes limited to the documentation theme's `public/main.js` and `public/main.css`, app semantic version, and focused source tests. No document nodes or saved Markdown are modified; no masks are applied to navigation, menus, document generation, application UI, or the existing scratch overlay.
- Browser rendering was tested in an **isolated synthetic page**, not the full generated/installed documentation; visual fidelity of the real DocFX pages and audio output still need Windows acceptance testing.
- No GitHub, .NET SDK, NuGet restore, DocFX rebuild, or PowerShell maintenance guard was run.

## Verification

- 63/63 LocalGPT Python source tests passed.
- Browser-driven Chromium test: drag creates three transparent text cuts and corresponding jagged SVG contour; no JavaScript errors; no text selection; hash change and reload remove the damage. Repeated for dark and light modes in the synthetic fixture.
- The production documentation will receive the changes the next time its maintained documentation build copies the updated source theme assets; existing prebuilt snapshots are not silently rewritten.
