# Theme Fusion runtime architecture

## Goal

LocalGPT exposes Theme Fusion through two explicit, persistent selections:

1. **Base Theme** — page background, navigation, native LocalGPT surfaces, Bootstrap metadata and Highlight.js.
2. **Style Layer** — blends with, overrides or extends the Base Theme across DevExpress editors, grids, chat controls, buttons and other DevExpress components.

The two selections may be identical or different, allowing combinations that behave as blends, overrides or extensions. They must survive routed navigation, circuit recreation, new tabs and application restarts without racing back to Office White.

## Source of truth

`ThemeService` is scoped and resolved through dependency injection. `MenuIsland` is the single interactive render-mode owner for the switcher; `ThemeSwitcher`, `ThemeSwitcherContainer`, and `ThemeSwitcherItem` inherit that circuit and must not create nested interactive roots.

`ThemeService.ActiveShellTheme` and `ThemeService.ActiveComponentTheme` are independent. The former `ActiveTheme` property remains a compatibility alias for the component theme only.

## Startup and persistence

The server-rendered shell reads:

- `ActiveShellTheme`;
- `ActiveComponentTheme`;
- legacy `ActiveTheme` as a migration fallback.

The browser stores both names in cookies and local storage. The interactive dispatcher reads the live browser state after it attaches, which avoids reusing the stale initial HTTP cookie snapshot during later routed component recreation.

## DevExpress ownership

Each LocalGPT `Theme` contains the actual DevExpress `ITheme` object.

- `Components/App.razor` registers `ActiveComponentTheme.DevExpressTheme` for startup.
- `ThemeJsChangeDispatcher` awaits `IThemeChangeService.SetTheme(...)` only when the Style Layer changes.
- A Base Theme-only change does not load a competing DevExpress stylesheet.

Fluent component themes do not apply themselves to page elements. LocalGPT shell palettes are expressed through higher-specificity Bootstrap variables selected by `data-localgpt-shell-theme`.

## JavaScript boundary

`wwwroot/switcher-resources/theme-controller.js` may:

- read and persist the two validated theme names;
- set `data-bs-theme`, `data-localgpt-shell-theme`, and `data-localgpt-component-theme`;
- update the optional Highlight.js stylesheet;
- notify .NET after a requested layer finishes applying.

It does not add or remove DevExpress component-theme stylesheets.

## Failure behavior

A failed Style Layer switch restores the prior DevExpress `ITheme`. A failed Base Theme switch restores the prior shell state. Technical exceptions are logged, bounded component-activity records are written, and the user receives a sanitized notification.
