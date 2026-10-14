# LocalGPT 2.6.3 — Localization legacy-key cleanup

## Build guard repair

- Removed the obsolete `Text.Archive␠Preset` localization key from both maintained catalogs. The two visible source phrases remain covered by the existing `Phrase.Archive␠Preset` and `SourceText.ArchivePreset.SentenceCase` entries.
- This avoids the historic case-only `Text.Archive␠Preset` / `Text.Archive␠preset` collision entirely instead of relying on case-distinct source-text keys.
- `Assert-LocalizationIntegrity.ps1` remains enabled. It now explicitly rejects reintroduction of the obsolete Archive Preset `Text.*` key and requires the semantic source-text replacement.
- English and German remain key-aligned after the cleanup.

## Runtime loading

- Retains the 2.6.2 defensive `LoadCatalogFile` implementation that normalizes entries one-by-one into an `OrdinalIgnoreCase` dictionary and logs optional-catalog collisions instead of crashing the application shell.
- Source-controlled catalogs still fail the build on case-insensitive duplicate keys; runtime tolerance does not weaken source integrity.

## Versions

- LocalGPT: `2.6.3`
- LocalGPTInstallerConsole: `2.6.3`
- LocalGPTWebviewWrapper: `2.6.3`
- LocalGPT Wire Protocol: unchanged `2.1.1`
