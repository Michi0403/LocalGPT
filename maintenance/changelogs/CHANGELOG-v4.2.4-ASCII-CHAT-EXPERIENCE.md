# LocalGPT 4.2.4 — replayable ASCII chat experience

LocalGPT 4.2.4 turns the existing removable ASCII game/operator surface into a third presentation of the canonical `/chat` conversation without creating a parallel message store.

## ASCII conversation replay

- Opening the ASCII terminal now mirrors the current DXAiChat history instead of showing only process output when no game is active.
- Switching between normal chat and ASCII does not move or duplicate the conversation. A bounded 700 ms mirror captures the canonical DXAiChat state while the terminal is open and only rerenders when the message signature changed.
- User, assistant, system and tool messages receive compact terminal nicknames (`YOU:`, model nickname, `SYS:`, `TOOL:`).
- Provider-visible thinking, function-call and function-result traces already emitted into the normal chat are normalized into `[THINK]`, `[CALL ...]`, and `[RESULT ...]` transcript entries so old and new events replay when the user opens the terminal later.
- When a Council run belongs to the current chat, the terminal also reads the existing server-owned participant activity lanes. Each member receives a compact model-derived nickname and is projected as its phase/role plus live trace/thinking/function activity and `[SAY]` final content when available. This is presentation-only; it does not create a second Council history.
- The existing game and Matrix operator planes remain available as explicit tabs/modes on the same surface. Their state remains alive when returning to the chat transcript.

## Contextual ASCII fun capability

- Chat configuration now exposes **ASCII fun mode (contextual)**.
- A new circuit-scoped `IChatAsciiExperienceState` tells individual providers and Council participants whether the ASCII terminal is open and whether the opt-in fun mode is enabled. Models do not scrape or depend on browser DOM state.
- With the terminal open and fun mode enabled, models may occasionally use a bounded fenced `ascii` block for a picture or a fenced `ascii-sequence` block for a small animation when context makes that useful or fun. The guidance explicitly says this is optional, not required for every response.
- Small animation sequences are bounded to 2–12 rendered frames separated by `--- frame ---`. Playback is browser-local, uses a minimum 250 ms delay (650 ms by default), pauses when the document/window is inactive, and does not cause Blazor rerenders per frame.
- A new read-only `localgpt.ascii.surface.get` DXFunction exposes the circuit-scoped terminal/fun-mode capability through the normal DXFunction registry. It is automatically cataloged as a system seed, so individual models and user-built Council teams can explicitly inspect the ASCII surface without scraping browser state.
- The shipped ASCII DOOM and Green Dragon Council blueprints now prefer `localgpt.ascii.surface.get` alongside their existing game/runtime functions. Existing `localgpt.game.*` functions remain intact for stateful interactive sessions.
- Real interactive games remain routed through LocalGPT's existing Council game/runtime functions rather than allowing arbitrary executable JavaScript or shell code in chat messages. This preserves the foundation for Steam Deck/controller-driven DOS-like team games.

## Compile follow-up

- Corrected the two .NET string-overload regressions reported by the 4.2.3 user build in `ProviderModelModels.cs` and `OllamaProcessService.cs`. IPv6 detection now uses string operands for the `Contains(..., StringComparison.Ordinal)` and `StartsWith(..., StringComparison.Ordinal)` checks accepted by the targeted compiler surface.
- A repository-wide follow-up scan found the same char-plus-`StringComparison` call shape in `MinecraftDatapackService` and `ProjectArchitectureService`; those two pre-existing sites now use compiler-safe string operands as well, and the release audit rejects this overload pattern anywhere under `src/LocalGPT`.

## Compatibility and safety

- The regular DevExpress chat remains unchanged and authoritative.
- Existing gamepad support remains demand-driven and is not removed by the new transcript/animation presentation.
- Function calling, Council runs, saved conversation memory, provider switching, game sessions and operator jobs retain their existing ownership and persistence behavior.
