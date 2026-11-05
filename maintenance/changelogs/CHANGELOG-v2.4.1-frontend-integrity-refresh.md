# LocalGPT 2.4.1 — frontend integrity refresh

- Adds a PowerShell-generated SHA-256 inventory for maintained first-party browser JavaScript.
- Ordered development and release build scripts refresh then validate the inventory before restore/build.
- Direct Visual Studio/MSBuild builds remain validation-only so an unreviewed JavaScript edit is not silently blessed.
- Adds `Update-FrontendIntegrity.cmd` for explicit manual refresh + validation after frontend work.
- Excludes `.example.js` placeholders from the maintained runtime inventory.
