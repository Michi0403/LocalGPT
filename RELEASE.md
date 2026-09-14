# LocalGPT 4.2.8

LocalGPT 4.2.8 adds the preseeded **ASCII Hot Seat — Neon Relay Arena** Council team. It is a real two-seat local workflow: Player 1 and Player 2 are separate `HumanOnly` roles, the keyboard is passed between them, one AI referee owns deterministic multiplayer state, and one AI display director owns the ASCII presentation surface.

The team is designed as an ASCII feature showcase rather than a second hidden game engine. It boots a bounded 100×32 Council game session as the supported display host, hides that host's single-player input overlay, and then demonstrates full-frame rendering, display readback, cell/text/fill/blit updates, score/HUD composition and pregenerated browser-local animations. The canonical hot-seat state remains the referee's `HOTSEAT_STATE`; presentation functions never become game authority.

No `dotnet` build, restore, publish, release packaging, signing/notarization or GitHub operation was performed. See `CHANGELOG-v4.2.8-ASCII-HOT-SEAT-SHOWCASE.md` and `VALIDATION-v4.2.8-source.md`.
