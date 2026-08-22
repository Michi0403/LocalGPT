# LocalGPT 3.2.6 — Remote Control guided integration workbench

## Changed

- Reworked `/remote-control` into the same full-width configuration-workbench family used by LocalGPT's other configuration surfaces instead of a narrow centered page.
- Removed the former 1600px-style page-width ceiling from the Remote Control page and let the workbench consume the full routed content width.
- Added a 14–18rem desktop workbench navigation rail with the editor consuming the remaining width.
- Made the workbench navigation sticky on desktop and horizontal/scrollable below the responsive breakpoint.
- Made connector, pipeline, history and template-language surfaces responsive with min-width-safe form grids and mobile one-column fallbacks.
- Preserved `@rendermode InteractiveServer` on `/remote-control` and the existing shared MainLayout width contract.

## Guided connector editing

- Replaced the **Allowed hosts JSON** editing experience with Add host, Remove and Use URL host controls.
- Replaced **Headers JSON templates** with individual header name/value rows.
- Added guided presets for `Accept: application/json`, bearer authorization using `{{var:API_TOKEN}}`, and API-key headers using `{{var:API_KEY}}`.
- Kept persistence backward-compatible: the guided editor deserializes and serializes the existing connector JSON fields through the existing JSON text service rather than inventing a second persisted format.

## Localization and release wiring

- Added the Remote Control guided-editor strings to all six maintained catalogs; all six contain 2,012 keys in exact parity.
- Updated LocalGPT, InstallerConsole and WebView wrapper to **3.2.6**.
- Preserved DevExpress **25.2.9** and the existing deployment/runtime architecture.

## Validation status

Closed for the requested source changes. Source-only repository audits pass for architecture, async continuation policy, service resilience, code-generation/DXFunction wiring, provider-qualified Council coverage, XML documentation, and the 33-check 3.2.6 release audit. Two older optional audits reference documentation paths that are not present in the provided source ZIP (`docs/architecture/project-data.md` and `docs/guide/chat-and-council.md`); no files were fabricated to satisfy those unrelated historical checks. No `dotnet` build/run was performed because this environment does not provide the .NET toolchain, per the requested workflow.
