# LocalGPT 4.2.5 — ASCII build ownership repair

LocalGPT 4.2.5 is the compile-gate repair release for the replayable ASCII chat experience introduced in 4.2.4. It preserves the same canonical `/chat` replay, Council participant lanes, DXFunction capability discovery, bounded ASCII sequences, games, and demand-driven gamepad support while aligning the implementation with LocalGPT's maintained architecture guards.

## Repairs

- Moved ASCII transcript parsing, Markdown/HTML projection, nickname derivation, animation extraction, and replay signature generation out of Razor components into the injected `AsciiChatTextService`.
- Uses DevExpress `ChatMessageRole` for DXAiChat messages, removing the invalid cross-type comparison with `Microsoft.Extensions.AI.ChatRole`.
- Routes the Chat Council-participant mirror signature and ASCII sequence signature through the same text service so the text-service ownership gate remains authoritative.
- Refreshed the reviewed JavaScript diagnostics manifest for `localgpt-game-console.js`; diagnostics coverage remains mandatory and unchanged.
- Retains `localgpt.ascii.surface.get`, all existing game DXFunctions, ASCII/game Council seed capabilities, opt-in contextual ASCII fun, 12-frame sequence bounds, and state-aware controller polling.
- Adds 4.2.5 release gates that explicitly verify the service-owned ASCII text boundary, DevExpress role mapping, JavaScript diagnostics manifest integrity, DXFunction/seed wiring, and the earlier char/StringComparison compiler guard.

No certificate validation, provider canonicalization, Council runtime identity, interactive render-mode, or normal chat behavior is weakened by this repair.
