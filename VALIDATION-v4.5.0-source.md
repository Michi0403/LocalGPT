# LocalGPT 4.5.0 source validation

## Validation boundary

This release was validated from source without invoking `dotnet`, MSBuild, restore, compile, test, publish, installer execution, GitHub, or any online repository. The checks below are static repository audits, parsers, JavaScript syntax validation, archive integrity checks, clean re-extraction checks and baseline comparisons. Runtime/compile validation remains the responsibility of the normal LocalGPT development environment after extraction.

## Color and renderer contract

Source audits verify that:

- `CouncilAsciiColorMode` exposes stable persisted values for terminal-default (`0`), ANSI-16 (`1`) and indexed-256 (`2`), with terminal-default as the compatibility fallback.
- Foreground/background/bold/dim/invert presentation metadata is separate from canonical frame text.
- Palette indexes and fixed-cell style coordinates are bounded and clipped by the server-side game service.
- Text/cell/fill/blit, full-frame submission, animation submission and display readback carry the color contract consistently.
- The browser converts ANSI-16/xterm-256 indexes locally and renders text nodes/spans instead of parsing raw ANSI or injecting model-authored HTML.
- Static frames and one-shot animation frames use the same style metadata, including held subtitle styling.

## Existing game regression coverage

Static audits preserve and additionally inspect:

- ASCII corridor/DOOM deterministic rendering and campaign configuration.
- Green Dragon/story rendering.
- Kernel Creature Tournament deterministic micro-turn state, bracket progression, one-shot movies/subtitles and semantic style runs.
- Two-human hot-seat display ownership and browser-local animation flow.
- Human, Shared and AI controller/session boundaries.
- Chat ASCII-console behavior and the maintained InteractiveServer route contract.

## Custom Game-project coverage

Static validation checks that:

- Project-owned Game profiles persist color mode/default indexes, and the `/projects` Game workbench edits/reloads the same fields.
- The new EF migration and model snapshot agree on those columns.
- `project.game.build` copies palette values into `ProjectGameDefinition`.
- Build-artifact readback validates the enum.
- `project.game.start` passes the compiled palette into the normal Council game session.
- No alternate custom-game rendering/state engine is introduced.

## Small-model evidence routing

The maintained Game Project Discovery, Game Project Development, Game Engine Extension, Game Project Playtest and ASCII hot-seat workflows are checked for compact palette, Project, knowledge and database-backed regex functions. `docs/reference/ascii-game-authoring.md` is both source-backed approved knowledge and an embedded publish fallback. Its guidance explicitly targets 0.8B-2B models and orders evidence as Project facts -> ASCII/palette contract -> narrow knowledge -> exact/tested regex evidence.

## Static commands used

The release validation set includes the maintained compatibility audits plus the 4.5.0 gates. Representative commands are:

```text
python3 build/audit_release_4_5_0.py
python3 build/audit_ascii_color_system.py
python3 build/audit_kernel_creature_tournament_ascii.py
python3 build/audit_council_role_context_isolation.py
python3 build/audit_chat_ascii_console.py --root .
python3 build/audit_ascii_doom_campaign.py
python3 build/audit_application_architecture.py --root . --product localgpt --mode all
python3 build/audit_service_resilience.py
python3 build/audit_async_continuations.py --source-root src/LocalGPT
python3 build/audit_configurable_behavior_policy.py
python3 build/audit_xround_wiring.py
python3 build/audit_codegen_dxfunction_wiring.py
python3 build/audit_powershell_variable_interpolation.py
python3 build/audit_cross_platform_boundaries.py
python3 build/audit_kawaii_documentation_layout.py
node --check src/LocalGPT/wwwroot/js/localgpt-game-console.js
```

JSON files and MSBuild/XML project files are parsed separately, DXFunction JSON schemas touched by this release are extracted and parsed, and `@rendermode` declarations are compared with the 4.4.9 release baseline before packaging.

## Packaging validation

The final ZIP is checked for CRC errors, unsafe member paths, clean re-extraction and byte-for-byte equality with the validated working tree. The 4.5.0 release/color audits and selected compatibility audits are rerun from the clean extraction so the downloadable bytes, rather than only the working directory, are covered.

## Observed source-audit results

The final working-tree pass reported: 62 ASCII color/game-authoring checks, 43 Kernel Creature Tournament checks, 24 Chat ASCII-console checks, 32 ASCII DOOM checks, architecture-policy success, 2518 service methods with owned diagnostics/error handling, async-continuation validation across 272 source files, configurable Council/X-Round/codegen/PowerShell/cross-platform/documentation checks, and JavaScript syntax success. A separate baseline comparison confirmed all 20 existing Razor `@rendermode` declarations are unchanged from 4.4.9. Repository-wide literal DXFunction schema validation parsed 133 JSON contracts; 38 JSON files and 10 MSBuild/XML files parsed without a new failure.
