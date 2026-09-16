# LocalGPT 4.3.3 source validation

Source-only validation was performed because this environment intentionally does not provide or use the .NET SDK. No restore, build, publish, package signing/notarization, GitHub access, or GitHub API operation was run.

## Passed static checks

- Current release audit `build/audit_release_4_3_3.py`: version identity, one-digit minor/patch rule, documentation PDF-default contract, Mermaid fences/recovery, non-tiled star/glass assets, in-app/Pages asset parity, render-mode ownership, game-session/close/shared-mode invariants, XML and JSON parseability.
- Application architecture audit: passed.
- Service resilience audit: passed; 2440 service methods own diagnostics/error boundaries, with only the audit's documented iterator/Program skips.
- Cross-platform boundary audit: passed (22 checks).
- Async continuation audit: passed across 265 source files.
- Chat ASCII-console audit: passed (17 checks).
- Configurable Council behavior-policy audit: passed.
- Documentation JavaScript syntax: `node --check` passed for the authored theme and both shipped in-app copies.
- Authored documentation CSS/JavaScript bytes match the shipped help copies and the tracked Pages archive.
- Source archive integrity is checked after packaging by reopening the ZIP and hashing every entry.

## Release-specific behavior reviewed

- Documentation fallback stars are fixed-position/non-tiled; the randomized layer creates 112 desktop or 48 compact stars with independent timing, nebulae, optional planet and satellites.
- Mermaid recovery uses DocFX's bundled Mermaid module and direct `mermaid.render(...)`; there is no `offsetParent` visibility gate.
- Normal documentation builds require `LocalGPT-4.3.3.pdf`; HTML-only output remains an explicit diagnostic opt-out through `RequireLocalGptDocumentationPdf=false`.
- `ChatGameConsole.CloseAsync()` returns to the Blazor dispatcher before invoking its render-affecting callback.
- New game sessions default to Shared direct control; autonomous autoplay is restricted to AI mode.
- `localgpt.game.session.close` ends the game runtime only. AI game/display functions prefer the invoking conversation game and fall back to the service's current-active-game convention when no conversation context is supplied.
- Complete frames, clipped text/cell/region mutations and bounded 2–12 frame browser-local ASCII animations remain exposed to AI participants.

Historical release-specific ASCII audits for 4.2.4/4.2.5 are not current release gates; they assert symbol/string layouts from those historical versions and are intentionally not used by `Invoke-RepositoryValidation.ps1`.
