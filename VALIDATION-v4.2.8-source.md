# LocalGPT 4.2.8 source validation

Validation for this handoff is source-only. No `dotnet`, MSBuild, restore, publish, application launch, release build, signing/notarization, GitHub access or GitHub API operation was performed.

## Hot-seat seed review

- `CreateDefaultTeamsCore` includes the new `ascii-hot-seat-showcase` seed.
- The seed defines two distinct `HumanOnly` roles so each seat creates a real Human Collaboration pause and no AI participant is assigned to either player role.
- The bounded `neon-hot-seat-match` loop alternates Player 1, Player 2, deterministic referee resolution and one display-director pass for at most ten iterations.
- The referee owns one machine-readable `HOTSEAT_STATE` and one `[[HOTSEAT_GAME_OVER]]` loop marker; the display role is explicitly forbidden from changing canonical multiplayer state.
- The display host is started with the supported `ascii-doom` session key only for fixed-cell presentation. Its game input overlay is disabled through `localgpt.game.input-gate.set`; the team never grants or requests `localgpt.game.control`.
- A single stable renderer identity is used for all frame mutations, matching the Council game display ownership rule.
- The renderer allow-list covers display readback, text, cell, fill, blit, complete-frame and bounded animation functions plus read-only regex/knowledge lookup.

## Static checks

- All automatic DXFunction names referenced by the new seed exist in the source DXFunction catalog.
- The new loop groups, completion marker, role references and single-member ASCII presentation steps satisfy the persisted Council-team validation shape.
- LocalGPT, installer-console and webview-wrapper application versions are 4.2.8 and retain single-digit semantic-version slots.
- Project XML and DocFX JSON parse successfully.
- Existing Blazor render-mode directives are unchanged by this release.
- Maintained browser JavaScript source was not changed by this feature, and `node --check` still accepts `localgpt-game-console.js`.
- The current general architecture, service-resilience, configurable-behavior, chat ASCII-console and provider-qualified Council audits passed.
- The historical version-specific `audit_ascii_experience_4_2_5.py` was not treated as a 4.2.8 gate; when sampled it reported three expected stale string-contract checks from the older 4.2.5 implementation. Historical audit source was left unchanged.

Compiler/runtime validation remains authoritative in the user's development environment.
