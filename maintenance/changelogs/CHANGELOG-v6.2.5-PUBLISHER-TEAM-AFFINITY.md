# LocalGPT 6.2.5 — PublisherStudio organic team model affinity

Baseline: LocalGPT 6.2.4. Source-only release.

- Adds PublisherStudio organic capability affinity controls to the existing **Council Teams** visual editor, sharing the *persisted team PreferredCapabilities* that normal Council execution already uses. Includes spreadsheet, OpenSCAD, panel modules, browser-view and media capabilities.
- Adds an explicit shortcut to bind **one provider-qualified model currently reported installed and configured** to all non-human-only roles, while assigning it to steps that already use AssignedModelSingle. The change remains unsaved until the user reviews and confirms Save, preserving the existing team policy and permissions.
- Adds a separate explicit action to remove unavailable saved role model bindings. Never silently alters an existing team or switches it to another host.
- Existing team editing for multiple model pools and exact role assignments remains available.

## Limits

- Model inventory flags indicate discovery/configuration, not proof of connectivity at the instant a future run starts. Execution preflight remains authoritative; no unsafe offline-machine fallback is added.
- This is a team-local capability preference, not a new global per-1-Wire-function override database. Existing clients continue to select a team key on submission.
- .NET build, live two-host failover, and Windows PowerShell architecture tests have not run here.
