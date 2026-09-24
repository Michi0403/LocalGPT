# LocalGPT 4.7.3 source validation

## Constraints

- LocalGPT 4.7.2 is the immediate supplied/user-tested source baseline for this repair.
- The supplied screenshots and previously supplied rendered HTML/logs were used only as local diagnostic evidence.
- No GitHub/network repository content was used.
- No `dotnet`, restore, NuGet, MSBuild, build, publish or installer command was invoked in this environment.
- Validation is static/source-level and does not claim a compiled runtime result.

## Verified source contracts

- Version-slot policy is respected and LocalGPT/WebView/installer source identities are synchronized at 4.7.3.
- The 4.7.2 Council provider/model identity reconciliation and explicit-team-binding protection remain present.
- The 4.7.2 Kernel Creature Tournament active-pair scheduling, bounded context, generated HUD/effect motifs, ASCII/Pixel rendering, greetings, attack/finisher animation phases and deterministic combat state remain present.
- Closed DevExpress dropdown popup cells are pointer-transparent; only a visible dropdown can re-enable pointer input above the ASCII modal. This removes the 4.7.2 full-page input/wheel shield while retaining dropdown usability.
- The ASCII popup uses responsive 98vw/96dvh sizing with a two-column header layout that stacks on narrower widths instead of crushing the title/control row.
- ASCII scaling no longer resets the fitted font to 16px during every resize pass. Scale is derived from glyph/line metrics and only updates the CSS variable when the target size actually changes.
- Resize observation is restricted to the ASCII wall itself; the former parent-observation feedback path is absent.
- Chat/Operator scrollback keeps vertical scrolling under fitted modes and does not attempt to shrink the complete scrollback history into one viewport.
- Follow-tail no longer installs whole-subtree MutationObservers; the existing render/layout synchronization path performs bounded follow-tail work.
- Unchanged Council participant terminal projections are cached, and very large mirrored lane payloads are bounded only in the ASCII presentation. Canonical Chat/Council data remains unchanged.
- Existing `@rendermode InteractiveServer` coverage remains present across the protected component surface.

## Static checks used before packaging

- `node --check` on `wwwroot/js/localgpt-game-console.js`.
- `python build/audit_release_4_7_3.py` for release/version, protected Council/tournament, popup, scroll/scale and prior-feature anchors.
- XML parsing of the three versioned project files and JSON parsing of `docs/docfx.json` through the release audit.
- CSS delimiter count checks on the modified scoped/global stylesheets.
- Version scan confirming no current 4.7.2 runtime identity remains outside historical 4.7.2 release/validation artifacts.
- ZIP CRC/path traversal checks and clean extraction comparison after packaging.
