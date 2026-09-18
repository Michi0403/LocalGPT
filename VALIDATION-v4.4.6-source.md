# LocalGPT 4.4.6 source validation

This is source-only validation. No `dotnet`, NuGet restore/build/publish, EF CLI, macOS `productbuild` execution, GitHub access, GitHub API operation, or other online repository operation was performed in this environment.

## Reported failures addressed

The supplied database snapshot was inspected separately and did not show SQLite corruption. The persisted `localgpt.game.*` catalog entries and 4.4.5 runtime-class seed existed, so this release does not destructively reset user data. The repair targets runtime ownership and interaction instead.

Council-started games can exist before a chat conversation id is persisted. 4.4.6 therefore resolves active games by explicit session id, conversation id, then the current Council run id. A required ASCII-game team is also prebootstrapped at the Council execution boundary from its persisted runtime-class family so the game exists before small models begin their workflow. The model-facing bootstrap step now reads the existing session first and uses `session.start` only as recovery.

The terminal JavaScript no longer focuses the game console for pointer/key events originating in interactive descendants, including DevExpress combobox/listbox/option/popup controls. This preserves Human / Human + AI / AI-hunter selection. Game-change events are filtered through the same conversation/Council-run ownership contract, preventing unrelated sessions from replacing the visible one.

The non-fullscreen DevExpress popup now has an explicit bounded height and its nested console/stage layout consumes that body without creating the previous avoidable sizing/scrollbar conflict.

## Configuration and architecture contracts

- The deterministic prebootstrap is triggered only by the persisted team's required-ASCII capability plus its assigned runtime-class namespace. It does not contain a second difficulty/map/enemy policy.
- `games.ascii.doom.campaign` remains the resettable/copyable source of the supplied ten-level starter progression; copied team/class assignments remain authoritative.
- System Council seed version 30 refreshes maintained unmodified seed rows; copied/user-modified rows are not silently replaced.
- Supplied ASCII-game workflow phases use exact automatic-function allow-lists and retain normal registry/safety filtering.
- Renderer-affine component continuations remain explicitly `ConfigureAwait(true)` and service/background continuations remain explicitly `ConfigureAwait(false)`.
- Routable Razor pages retain `@rendermode InteractiveServer`, excluding only the static error fallback.
- `MainLayout.razor` and `Drawer.razor` remain byte-for-byte identical to the supplied 4.4.2 restoration.

## Source checks executed

The final working tree and the cleanly re-extracted release ZIP are validated with the maintained release, ASCII console/campaign, architecture, service resilience, async continuation, configurable behavior, X-Round, DXFunction wiring, PowerShell interpolation, cross-platform and Kawaii documentation audits. ZIP CRC and byte-for-byte inventory/content comparison are also performed before release handoff.

## Build limitation

A compiler/runtime success result is not claimed because this environment does not run `dotnet`. The next authoritative compiler/runtime check is the user's Windows/DevExpress build. The source gates specifically protect the Council-run ownership path, required-game prebootstrap, interactive DevExpress-target exclusion, renderer-affine continuation declarations, popup sizing and all retained 4.4.x release contracts.
