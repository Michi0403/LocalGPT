# LocalGPT 4.3.4 — chat, ASCII game, provider and documentation recovery

## Changed

- Keep one canonical conversation transcript while switching configured AI/provider sessions. Provider selection no longer blanks the visible chat when context reuse is enabled, and clearing a chat clears the provider mirrors together with the canonical conversation.
- Surface **Contextual ASCII fun** directly in the Chat header while retaining the advanced provider-workbench control.
- Respect the primary Ollama host selected in the detached installer/editor draft before falling back to an older persisted primary endpoint.
- Project pending `Player 1 Hot Seat` / `Player 2 Hot Seat` Human Collaboration requests directly into the ASCII game surface with MOVE, DASH, PASS and bounded EMOTE controls. The normal Approvals & Team inbox remains an alternate view of the same request.
- Keep the Council hot-seat presentation distinct from standalone **AI hunter** controls, so changing deterministic corridor autoplay does not hijack a Council multiplayer presentation.
- Assign the hot-seat Arena Referee and ASCII Display Director through one distinct AI-assignment group. When two suitable selected Council models exist, the two active runtime roles use different members instead of both silently selecting the same model.
- Add a discoverable **Reactive ASCII Gameplay** quick action beside the existing team/model/hardware selectors. It reuses the maintained low-latency model preset and leaves the selected hardware-performance profile authoritative for matching provider roads.
- Extend ASCII corridor start state with a bounded deterministic `mapSeed` and optional `scenarioPrompt`. Completed standalone corridor sessions can restart the same map, start a fresh map, or start a newly themed deterministic ASCII scenario without replacing the owning chat.
- Preserve the stable game viewport while frames/animations change; scrolling remains contained to the ASCII surface when native/width scaling genuinely requires it.
- Documentation sky now uses a viewport-contained randomized 112-star desktop field with stronger independent drift, guaranteed side-gutter satellites, optional planet detail, and a CSS fallback that disappears once the JavaScript sky is ready.
- Remove the large GPU-blurred nebula DOM layers that could show Chromium compositor tiles. Nebula depth now comes from compositor-safe radial gradients while glass panels retain a moderate backdrop blur.
- Mermaid recovery now converts legacy fenced blocks to attached Mermaid nodes and uses Mermaid's attached-node `run` path with SVG labels. Recovery is bounded instead of retrying continuously, avoiding detached-label `getBoundingClientRect` failures and page growth.
- Maintained architecture diagrams are emitted as Mermaid HTML blocks rather than syntax-highlighted `lang-mermaid` code, preventing Highlight.js from treating Mermaid as a missing programming language.

## Validation boundary

This is a source-only release preparation. Static architecture, resilience, cross-platform, async, game/ASCII, documentation parity, JavaScript syntax, JSON/XML, render-mode ownership and archive-integrity checks are recorded in `VALIDATION-v4.3.4-source.md`. No .NET build/restore/publish was run and no GitHub/GitHub API access was used.
