# LocalGPT 4.3.3 — Documentation and ASCII game session recovery

LocalGPT 4.3.3 repairs the remaining documentation presentation regressions and the conversation/session faults exposed by the shared ASCII game console, without changing the established application architecture or Blazor render-mode ownership.

- Restores a visible non-tiled deep-space documentation background even when the dynamic sky layer cannot initialize immediately. A CSS fallback now provides uniquely positioned stars while the JavaScript layer adds 112 randomized desktop stars (48 compact), independent twinkle/drift timing, colored points and cross-stars, drifting nebula haze, small satellites, and an occasional ringed planet.
- Keeps the article, navigation rails, cards, API panels, details and tabs as translucent glass rather than flattening the documentation into opaque purple surfaces. The right article rail retains a symmetric viewport gutter and internal padding.
- Reworks Mermaid recovery around direct `mermaid.render(...)` calls using DocFX's bundled Mermaid module. Rendering no longer depends on `offsetParent`, so diagrams can be prepared while the in-app documentation iframe/dialog is hidden. Authored architecture blocks are explicitly fenced as Mermaid and the rendered SVG receives full-width glass styling.
- Preserves reduced-motion and print behavior: motion stops for reduced-motion users and decorative sky layers do not enter printed/PDF output.
- Carries the current `IChatSessionContext` into provider-native Ollama DXFunction invocations. Automatic game/display functions therefore resolve the same conversation/project context as the visible chat instead of losing the conversation identity at the tool boundary.
- Makes new ASCII game sessions Shared by default with background autoplay disabled. Human and AI participants can take bounded turns against the same conversation-bound session; continuous autonomous stepping is limited to explicit AI mode so Shared mode no longer races ordinary controls.
- Makes session/turn identifiers optional for AI frame/display mutation calls. LocalGPT prefers the invoking conversation game and, when no conversation context is available, follows the existing service convention of using the current active game rather than imposing an artificial context-only block. Models can submit complete ASCII frames, bounded cell/text/fill/blit updates, and 2–12 pregenerated animation frames for browser-local playback.
- Adds `localgpt.game.session.close` and a distinct **End game** action. Ending a game stops its game/autoplay lifetime only; it does not terminate the chat, Council run, provider connection, or ASCII terminal surface.
- Fixes the ASCII console close path by marshalling its rendering `EventCallback` back through the Blazor dispatcher after asynchronous fullscreen teardown, preventing the off-Dispatcher circuit failure shown in the runtime log.
- Updates the shipped ASCII DOOM Council seed to Shared/no-autoplay semantics and exposes the ASCII surface capability alongside the game/display capabilities.
- Keeps the PDF-required-by-default documentation contract introduced in 4.3.2 and synchronizes the authored theme assets with the in-app help copies and tracked Pages snapshot.
- Retains the existing routed `InteractiveServer` boundary set and intentionally static error page.

Visual frontend checklist: expected appearance is the moving varied deep-space field behind readable glass panels, with full Mermaid SVG diagrams and comfortable rail gutters; reduced-motion disables motion without hiding the sky; print hides decorative layers; game-console close and End-game remain distinct controls across the normal responsive console modes.

No .NET build, restore, publish, signing/notarization, GitHub access, or GitHub API operation was performed for this source-only release.
