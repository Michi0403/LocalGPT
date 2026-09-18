# LocalGPT 4.4.5 source validation

This is source-only validation. No `dotnet`, NuGet restore/build/publish, EF CLI, macOS `productbuild` execution, GitHub access, GitHub API operation, or other online repository operation was performed in this environment.

## Requested behavior addressed

The ASCII DOOM starter now uses the existing runtime-class/team configuration system rather than a second hardcoded gameplay preset. `games.ascii.doom.campaign` is a resettable runtime-class seed containing ten explicit starter levels. A copied non-system campaign class assigned to a copied team takes precedence over the system seed. The game engine consumes the configured map dimensions, room count, enemy density, health/damage scaling, starting resources, starting level and automatic progression, while rejecting malformed or empty campaign configuration rather than silently inventing another difficulty curve.

Council-role human participation and game control are now separate concepts. The three supplied ASCII DOOM Council roles are optional-human roles, while runtime control remains switchable between Human, Shared/Human + AI, and AI hunter. Shared remains non-autonomous; AI hunter remains the only autoplay mode.

The supplied runtime log showed historical Blazor circuit failures where `ChatGameConsole.CloseAsync` resumed off the renderer dispatcher. 4.4.5 keeps every continuation explicit and changes renderer-affine component paths to `ConfigureAwait(true)` while leaving service/background continuations explicit `ConfigureAwait(false)`. The non-fullscreen DevExpress popup also no longer forces its middle stage to 100% height in addition to its header/footer, and its viewport no longer reserves a popup-only scrollbar gutter.

The Kawaii documentation sky now schedules rare shooting-star/meteor passes with reduced-motion support and bounded viewport-only presentation.

## Release-specific contracts

- The three LocalGPT project files identify the release as `4.4.5`; the maintained single-digit minor/patch version policy is satisfied.
- `games.ascii.doom.campaign` is database-backed seed/template data. The runtime engine contains no `CouncilGameCampaignDefaults` or other duplicate starter table.
- The supplied campaign contains ten ordered profiles with difficulty 1 through 10 and progressively larger/denser/stronger starter settings.
- Copied campaign classes are resolved through the selected team and preferred over system seeds when both are assigned.
- `CouncilGameSessionService` remains singleton state but uses `IServiceScopeFactory` for scoped team/runtime-class lookups, avoiding a scoped-service capture.
- Campaign extraction can advance to the next configured level and snapshots/HUD expose the authoritative campaign state.
- `ChatGameConsole` retains explicit `ConfigureAwait(true/false)` annotations; renderer-affine selector, fullscreen, close, lifecycle and state paths are explicitly `true`.
- The popup game/chat/operator stages use grid stretching (`height:auto`) and the popup viewport uses `scrollbar-gutter:auto`.
- The three ASCII DOOM Council roles use optional human participation and the workflow explicitly states that absent Council-role input does not disable game input.
- Authored/embedded documentation JavaScript and CSS plus generated cache references and the tracked Pages snapshot remain synchronized.
- Routable Razor pages retain the maintained `@rendermode InteractiveServer` contract, excluding only the intentional static error fallback.
- `MainLayout.razor` and `Drawer.razor` remain byte-for-byte identical to the supplied 4.4.2 source.

## Source checks executed

- `python3 build/audit_release_4_4_5.py` — passed.
- `python3 build/audit_ascii_doom_campaign.py --root .` — passed; 32 checks.
- `python3 build/audit_chat_ascii_console.py --root .` — passed; 24 checks.
- `python3 build/audit_application_architecture.py --root . --product localgpt --mode all` — passed.
- `python3 build/audit_service_resilience.py --root . --product localgpt` — passed.
- `python3 build/audit_async_continuations.py --source-root src/LocalGPT` — passed.
- `python3 build/audit_configurable_behavior_policy.py` — passed.
- `python3 build/audit_xround_wiring.py` — passed.
- `python3 build/audit_powershell_variable_interpolation.py` — passed.
- `python3 build/audit_cross_platform_boundaries.py` — passed.
- `python3 build/audit_kawaii_documentation_layout.py` — passed.
- Final source ZIP CRC test and clean re-extraction/content comparison — passed.
- The same source-audit set above was rerun from the cleanly re-extracted final ZIP — passed.

## Build limitation

A compiler/runtime success result is not claimed because this environment does not run `dotnet`. The next authoritative compiler check is the user's normal Windows/DevExpress build. The source gates specifically protect the reported renderer-affinity failure mode, the earlier `RZ9986` DevExpress `CssClass` repair, and the new configurable campaign contracts.
