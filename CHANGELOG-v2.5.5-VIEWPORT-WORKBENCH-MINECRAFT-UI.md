# LocalGPT 2.5.5 — Viewport workbench and Minecraft UI repair

## Scope

This release intentionally stays focused on the three UI problems reported after 2.5.4: the squeezed `/install` workbench, the Chat configuration workspace not using the available modal height, and the Minecraft Mod Builder still looking like a raw utility form.

## Fixed

### `/install` now uses the full available application width

- Removed the effective conflict between the older 2.5.3 outer desktop grid and the 2.5.4 nested `install-workbench-layout` grid.
- The outer `install-workbench` now has one full-width column; the navigation/stage pair is the only desktop two-column owner.
- The workbench header, navigation/stage layout, stage and active panel explicitly fill the available route width.
- The responsive breakpoint still collapses the navigation and stage to one column below 860px.
- No setup controls, provider discovery behavior, persisted host configuration, certificate controls, toolchain controls or onboarding actions were removed.

### Chat configuration now consumes the full modal viewport

- The open Chat configuration surface now uses the browser viewport minus a small safe inset in both width and height.
- The modal body, workbench, active stage and active `ConfigurationWorkbenchPanel` share the same available block size instead of stopping at natural content height.
- The active panel is the scroll owner, preventing the navigation rail and sibling sections from competing for page scroll.
- The old Council `max-height` and model-list caps are overridden inside the configuration modal so the Council surface can use the full workspace before scrolling.
- Provider, Council, Memory & Projects and Architecture remain mutually exclusive rendered workbench sections from 2.5.4.

### Minecraft Mod Builder now follows the LocalGPT workbench visual language

- Replaced the oversized standalone heading with the shared `WorkbenchHeader` used by the newer configuration and diagnostic surfaces.
- Promoted Create Workspace / Create Datapack ZIP into the workbench action area.
- Reworked the project form into responsive LocalGPT cards with theme-aware borders, surfaces, focus treatment and orange accent hierarchy.
- Grouped project/target fields, the implementation brief and Council settings into clearer visual regions without changing the underlying request model or command behavior.
- Dynamic Workspace, AI Plan, Council Log and Command Result sections retain their existing behavior but now use the same responsive panel styling.
- The builder no longer has the old fixed 1100px maximum width and can use the available viewport.

## Preserved

- `@rendermode InteractiveServer` remains on Install, Chat, Minecraft Mod Builder, Test Lab, 1-Wire Security and the other maintained interactive pages.
- Existing async continuation policy, logging, component activity diagnostics, provider-qualified Council behavior, persistence and command boundaries are unchanged.
- No LocalGPT 1-Wire protocol version change was made because this release changes UI layout only.

## Version

- LocalGPT: `2.5.5`
- LocalGPTInstallerConsole: `2.5.5`
- LocalGPTWebviewWrapper: `2.5.5`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)

## Build boundary

Per the delivery constraint, this package was not restored, compiled, built, published or run with the .NET SDK. No GitHub or online repository access was used. Validation is source/static only and is recorded in `VALIDATION-v2.5.5-source.md`.
