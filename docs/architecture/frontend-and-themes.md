# Frontend, DevExpress, and themes

## UI ownership

Bootstrap v5 owns responsive layout, spacing, breakpoints, and the application shell. DevExpress components provide behavior-rich grids, editors, dialogs, charts, tabs, menus, and chat surfaces where their features are actually needed.

Razor components present state and call services. They do not own provider protocols, command execution, database initialization, or file policy.

## Component contract

Maintained components use explicit injected dependencies for logging, notification, and bounded activity reporting. Long operations support cancellation and show progress. Dialogs have one internal scroll owner and remain usable at ordinary browser zoom.

A failure boundary prevents one component exception from taking down the full circuit when a recoverable user-facing result is possible.

## Design patterns

- persistent navigation shell;
- responsive card/grid layouts;
- collapsible advanced configuration;
- reusable provider-model panel;
- status chips with text, not color alone;
- confirmation dialogs that name the exact target;
- bounded tables with search/filter;
- empty states that explain the next action;
- no fake controls or decorative buttons without behavior.

## DevExpress assets

DevExpress packages and runtime assets remain governed by their own license. Generated license material, proprietary distribution assets, or credentials must not be committed accidentally. Build and publish scripts use the configured package source and fail clearly when licensed dependencies are unavailable.

## Theme Fusion in the application

The application theme system keeps two selections:

1. **Base Theme** — shell/background, Bootstrap metadata, syntax highlighting, and native LocalGPT surfaces.
2. **Style Layer** — optional blend/override for DevExpress and other component surfaces.

Selections are persistent and applied through a service/JavaScript boundary. A theme failure falls back to a readable base rather than leaving the UI half-styled.

## Runtime theme ownership

`ThemeService` is scoped and keeps independent Base Theme and Style Layer selections. `MenuIsland` owns the interactive render mode; nested switcher components inherit that circuit. Startup registers the active component theme through `DxResourceManager.RegisterTheme`, while runtime component-theme changes await `IThemeChangeService.SetTheme(...)`. Base-only changes do not inject a competing DevExpress stylesheet.

The browser boundary persists validated names, sets `data-bs-theme` and LocalGPT theme metadata, and optionally updates syntax highlighting. A failed change restores the prior readable state. The maintained catalog still supports classic themes such as **Blazing Berry** alongside newer palettes.

## Documentation theme

The DocFX site uses a separate Kawaii shell around generated content. It supports light, dark, and auto modes through the standard DocFX selector, with LocalGPT persistence layered on top.

The theme script:

- applies preference before paint;
- stores it in local storage and a first-party cookie;
- observes DocFX changes without replacing its dropdown;
- keeps the dropdown above decorative layers;
- respects reduced-motion and coarse-pointer preferences;
- decorates the brand with a real paw/cat SVG and favicon.

## JavaScript boundary

JavaScript is used for browser-only behavior: theme metadata, focus/layout helpers, fullscreen, file download, documentation decoration, and DevExpress interop. Each entry point is registered and diagnostically traceable. Missing functions fail with a useful message rather than a silent `undefined` call.

## Testing

Frontend validation combines static checks, component tests where available, browser automation for important routes, and manual visual review at responsive sizes. A screenshot alone is not a behavioral test, but it is useful evidence for layout regressions.
