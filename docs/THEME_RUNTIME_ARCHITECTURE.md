# DevExpress and Bootstrap theme runtime architecture

## Goal

LocalGPT supports DevExpress Classic themes, DevExpress Fluent light/dark themes, and local external Bootstrap themes without competing stylesheet managers. A theme change must preserve component behavior, Bootstrap layout variables, custom LocalGPT surfaces, user notification, and circuit safety.

## Source of truth

`ThemeService` is scoped and must be resolved through dependency injection. Components and services must never instantiate it with `new ThemeService(...)` or maintain an independent active-theme field.

Each LocalGPT `Theme` contains the actual DevExpress `ITheme` object. The application uses the same object in both supported phases:

1. `Components/App.razor` calls `DxResourceManager.RegisterTheme(Themes.ActiveTheme.DevExpressTheme)` for startup resources.
2. `ThemeJsChangeDispatcher` injects `IThemeChangeService` and **awaits** `SetTheme(theme.DevExpressTheme)` for runtime changes.

## Theme families

- **Classic:** clone `Themes.BlazingBerry`, `Themes.BlazingDark`, `Themes.Purple`, or `Themes.OfficeWhite`, then add the LocalGPT theme contract stylesheet.
- **Fluent:** clone `Themes.Fluent`, set `ThemeMode.Light` or `ThemeMode.Dark`, enable `UseBootstrapStyles` and `ApplyToPageElements`, then add the LocalGPT theme contract stylesheet.
- **External Bootstrap:** clone `Themes.BootstrapExternal`, add the selected local Bootstrap stylesheet with `AddFilePaths`, then add the LocalGPT theme contract stylesheet.

External Bootstrap stylesheets are local source assets. Model output, imported documents, repository text, database values, and remote URLs cannot become a theme stylesheet path.

## JavaScript boundary

`wwwroot/switcher-resources/theme-controller.js` does not add or remove DevExpress or Bootstrap theme links. DevExpress owns those resources. JavaScript may only:

- set `data-bs-theme` and `data-localgpt-theme` on `<html>`;
- persist the validated theme name in the `ActiveTheme` cookie;
- switch the optional Highlight.js stylesheet with a short timeout;
- call back into `ThemeLoadedAsync` after client metadata is applied.

## CSS contract

DevExpress internal selectors remain under DevExpress theme control. LocalGPT custom CSS uses variables from `css/localgpt-theme-contract.css`:

- `--localgpt-body-bg` and `--localgpt-body-color`;
- `--localgpt-surface-bg` and `--localgpt-surface-raised-bg`;
- `--localgpt-border-color`;
- status and RGB variables based on Bootstrap tokens;
- fallback shadow, radius, and focus-ring values.

Every variable resolves first to the active Bootstrap/DevExpress-provided token and then to an explicit safe fallback. Do not globally restyle `.dxbl-*` internals. Apply `CssClass` to the DevExpress component and style only that semantic application class when an override is necessary.

Standalone DevExtreme JavaScript widgets are not currently part of the maintained UI surface. If one is introduced later, its official theme stylesheet and runtime lifecycle must be registered explicitly; the Blazor theme dispatcher must not guess or synthesize DevExtreme asset names.

## State flow

The server-rendered shell validates the `ActiveTheme` cookie before registering startup resources. `ThemeSwitcher` transfers the effective theme name through `PersistentComponentState` into the interactive circuit. Unknown or blank names fall back to Office White. Runtime changes update the scoped service, DevExpress theme service, client metadata, persistent component state, and cookie.

## Failure behavior

A runtime failure performs one awaited restoration of the prior `ITheme`, records a technical log and bounded component-activity entry, and shows a sanitized notification. Rollback failure is logged separately and never hidden. A remote Highlight.js failure is optional and time-bounded; it cannot leave DevExpress controls half-switched.

## Preservation audit

The older component archive was compared against the maintained tree. Its theme resources, JavaScript helpers, startup marker, routed pages, and DevExpress control families remain. Blazing Berry was the only selectable theme omitted by the rewire and has been restored. The old `MainLayout` error fragment was replaced by the stronger shared `SafeErrorBoundary`; the old drawer disposal methods had no live resources to preserve.
