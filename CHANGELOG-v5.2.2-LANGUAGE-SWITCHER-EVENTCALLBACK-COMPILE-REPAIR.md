# LocalGPT 5.2.2 — language switcher EventCallback compile repair

## Fixed

- Repaired the 5.2.1 `LanguageSwitcher` DevExpress `DxComboBox` callback binding that failed Razor compilation with CS1503/CS1662.
- `ValueChanged` now owns an explicit typed asynchronous lambda (`string value`) and awaits the component handler with `ConfigureAwait(false)` instead of passing the handler as an incompatible method group.
- The culture-change handler now returns `Task`, validates the incoming callback value as a string, preserves the existing culture equality guard, activity diagnostics, canonical culture URL generation, forced document reload, notification boundary, and exception logging.

## Preserved

- The dedicated `LanguageSwitcher` remains an `InteractiveServerRenderMode(prerender: false)` island.
- The 5.2.1 function-result continuation, approval, Test Lab, documentation-popup, theme, drawer, DXFunction lifetime, and isolated MCP changes are otherwise unchanged.
- PublisherStudio was not modified.
- No `dotnet`, MSBuild, NuGet restore/publish, GitHub access, or online repository access was used for this source handoff.
