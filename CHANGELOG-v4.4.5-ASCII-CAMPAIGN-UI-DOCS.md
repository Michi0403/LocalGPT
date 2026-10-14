# LocalGPT 4.4.5 — configurable ASCII campaign, renderer continuity and documentation meteors

## Scope

This maintenance release extends the directly playable ASCII corridor without changing its authoritative service-owned game model. It also repairs renderer-affinity and popup sizing around the DevExpress ASCII console and adds a restrained shooting-star effect to the Kawaii documentation.

## Configurable ten-level ASCII DOOM campaign

- Added `CouncilGameLevelProfile` as serializable campaign data containing level number/name, explicit difficulty, map width/height, room count, enemy density, hostile health/damage multipliers, and starting health/ammunition.
- Added the resettable `games.ascii.doom.campaign` runtime-class seed. Its supplied `levelProfilesJson` contains ten progressively harder starter levels and `autoAdvanceLevels=true`.
- The ten-level starter curve exists only in the visible runtime-class seed. `CouncilGameSessionService` does not contain a second hidden starter-difficulty fallback.
- The game service resolves an explicit `campaignRuntimeClassKey` override first, then campaign classes assigned to the selected Council team. If both a copied user class and the system seed are assigned, the copied non-system class wins.
- A copied campaign may change level count/order, map dimensions, room count, enemy density, health/damage scaling, starting resources, starting level, or automatic level progression without changing engine code.
- Campaign inputs are clamped to deterministic engine bounds. Malformed JSON or an empty level set is rejected instead of silently substituting model-invented difficulty.
- Clearing all hostiles and reaching extraction automatically initializes the next configured level when `autoAdvanceLevels` is enabled; the final configured extraction completes the campaign.
- Session snapshots/HUD expose the resolved campaign key, current/total level count, explicit difficulty, current level profile and campaign seed.
- `localgpt.game.session.start` now accepts optional `campaignRuntimeClassKey`, `startingLevel`, and `autoAdvanceLevels`; when no campaign override is supplied, the selected team assignment is authoritative.

## Human / Human + AI / AI ownership

- The supplied `ascii-doom-council-adventure` roles now use optional human participation instead of `None`, preventing the generic role instruction from being misread as "the game has no human input".
- The preset explicitly separates Council-role participation from runtime game control.
- New sessions remain `Shared`/Human + AI with autoplay disabled. Human and AI may submit explicit bounded controls in Shared mode; Human rejects AI controls; AI hunter is the only autonomous stepping mode.
- The preset tells models to report configured campaign difficulty rather than inventing another difficulty system.

## Renderer-affine DevExpress console repair

- Preserved explicit continuation annotations throughout `ChatGameConsole`; no implicit awaits were introduced.
- Renderer-affine component lifecycle, DevExpress selector callbacks, JS interop, EventCallbacks, state changes and human-input submissions now resume with explicit `ConfigureAwait(true)` where the Blazor renderer must remain authoritative.
- Background/service-owned delayed console rendering continues using explicit `ConfigureAwait(false)`.
- The popup game/conversation/operator stages now stretch through the popup grid with `height:auto` instead of forcing 100% stage height in addition to header/footer space.
- Removed the popup-only reserved scrollbar gutter and added bounded action padding so fit mode and DevExpress focus/selection visuals have room at the popup edge.

## Documentation sky refinement

- Added rare `.localgpt-kawaii-shooting-star` meteor passes to the existing animated documentation sky.
- Meteors use randomized position, trail length and travel, are intentionally infrequent, stay inside the fixed sky layer, and are skipped under `prefers-reduced-motion`.
- Authored DocFX JavaScript/CSS, embedded help assets, HTML cache references and the tracked Pages snapshot are synchronized.

## Regression protection

- Added `build/audit_ascii_doom_campaign.py` to enforce the runtime-class-only starter policy, ten configured levels, team-class resolution, explicit difficulty inputs, automatic progression, DX start schema, optional-human preset behavior and authoritative HUD/snapshot exposure.
- Extended `build/audit_chat_ascii_console.py` to require renderer-affine `ConfigureAwait(true)` for control/fullscreen/close paths and popup layout/gutter protections.
- Extended the 4.4.5 release audit to retain the 4.4.4/4.4.3 ASCII, DevExpress, macOS Installer, Mermaid, `InteractiveServer`, restored-shell and documentation asset contracts.

## Validation boundary

No `.NET` restore/build/publish, NuGet operation, GitHub access, GitHub API call, or online repository access was performed for this source release. Compiler/runtime success remains the responsibility of the normal Windows/DevExpress build performed by the user.
