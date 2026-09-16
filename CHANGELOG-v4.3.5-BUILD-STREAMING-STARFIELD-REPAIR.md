# LocalGPT 4.3.5 — build, streaming and starfield repair

## Fixed

- Repair the `DxButton.CssClass` Razor expression used by the top-level **Contextual ASCII fun** action. The complete CSS class now comes from one C# property instead of mixed markup/C# inside a component attribute, addressing the `RZ9986` build failure reported from 4.3.4.
- Move the two newly introduced hot-seat/scenario whitespace-normalization operations back behind `CouncilTextService.TrimForPrompt(...)`, satisfying LocalGPT's component/controller text-service ownership boundary instead of calling `Split`/`string.Join` directly in `ChatGameConsole`.
- Keep the 4.3.4 canonical chat/provider/game fixes intact: provider switching retains the shared transcript, the selected primary Ollama host wins, Contextual ASCII fun remains directly reachable, Council hot-seat input stays on the game surface, distinct runtime roles remain distinct when possible, and completed corridor runs retain restart/new-map/scenario controls.

## Streaming responsiveness

- Coalesce very small Ollama text deltas into bounded presentation batches (96 characters or about 45 ms, whichever comes first) before DXAIChat receives them.
- The provider stream, cancellation, function-call recovery and stored transcript remain lossless. The change is presentation-side back-pressure: it reduces full accumulated-Markdown rerenders during long answers so the Blazor circuit is less likely to build a render backlog and then display a large tail all at once.
- Function-call/status boundaries still flush immediately; the batching applies only to normal visible/thinking text fragments.

## Documentation sky

- Remove the remaining fixed nebula pseudo-layer from the final theme and disable documentation `backdrop-filter` entirely. The glass look is retained through translucent surfaces, borders and shadows instead of GPU backdrop tiles.
- Replace the purple haze with a smooth deep-space gradient behind the JavaScript star field. Once the dynamic sky is ready, only the animated sky remains over that gradient; the fallback star field still exists for JavaScript failure/reduced functionality.
- Remove per-star brightness filters and keep twinkle through opacity. This lowers compositor work while retaining independent star motion.
- Keep satellites guaranteed, make them larger/brighter, shorten their travel periods, and give each side-gutter satellite a visibly longer flight path so the space objects are no longer technically present but easy to miss.

## Validation boundary

This repository copy is validated without `dotnet` in this environment. The user-supplied compiler/build diagnostics were used to identify the 4.3.4 Razor and text-ownership failures; 4.3.5 verifies those source patterns are removed and runs the maintained static architecture, resilience, async, cross-platform, ASCII-console, JavaScript and release audits. No GitHub or GitHub API access is used.
