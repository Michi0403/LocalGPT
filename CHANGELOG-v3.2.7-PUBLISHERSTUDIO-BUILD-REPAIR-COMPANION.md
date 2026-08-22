# LocalGPT 3.2.7 — PublisherStudio build-repair companion

## Release scope

- Carries forward the complete LocalGPT 3.2.6 Remote Control guided integration workbench without changing its connector behavior, persistence format, layout model or localization data.
- Advances the LocalGPT, InstallerConsole and WebView wrapper release metadata to **3.2.7** so this paired source handoff follows the requested always-increment release workflow.
- Updates the browser cache marker and LocalGPT outbound product User-Agent version to 3.2.7.
- No database/schema migration, provider contract change, render-mode change, package upgrade or new runtime dependency is introduced.

## Retained 3.2.6 behavior

- `/remote-control` remains full-width and aligned with the configuration/install workbench family.
- Allowed hosts and headers remain guided row-based editors instead of raw-array entry surfaces.
- Existing connector JSON persistence remains compatible through the maintained text/JSON service boundaries.
- DevExpress remains 25.2.9 and the existing deployment architecture remains unchanged.

## Validation status

This is a source-only companion version bump for an otherwise unchanged LocalGPT 3.2.6 feature set. No `dotnet` build/run was performed in the preparation environment. The 3.2.7 source release audit and applicable repository Python/static checks pass.
