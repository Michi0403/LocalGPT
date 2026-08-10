# LocalGPT 2.6.2 — Localization duplicate-key integrity repair

## Build/runtime failure repair

- Repaired every case-insensitive duplicate key introduced by the 2.6.1 localization coverage expansion, not only the first key reported by the build guard.
- Ten English/German collision pairs were normalized to distinct semantic source-text keys while preserving both source phrases and both translations. Examples include `Archive Preset` / `Archive preset`, `Reachable` / `reachable`, and `Save Preset` / `Save preset`.
- English and German remain aligned at 1,800 maintained entries.
- `Assert-LocalizationIntegrity.ps1` remains enabled and unchanged. The source-controlled catalogs must still fail the build when a case-insensitive duplicate is introduced.

## Defensive localization loading

- `LocalGptLocalizationService.LoadCatalogFile` no longer constructs an `OrdinalIgnoreCase` dictionary directly from a case-sensitive deserialized dictionary. That constructor was the runtime source of `ArgumentException` when a manually edited/legacy optional catalog contained case-only duplicate keys.
- Optional runtime catalogs are now normalized entry-by-entry into the case-insensitive dictionary. If a duplicate reaches runtime outside the source guard, LocalGPT logs a warning and deterministically uses the later value instead of taking down the application shell.
- This defensive behavior does not weaken the build safeguard: maintained source catalogs are still rejected by `Assert-LocalizationIntegrity.ps1`.

## Versions

- LocalGPT: `2.6.2`
- LocalGPTInstallerConsole: `2.6.2`
- LocalGPTWebviewWrapper: `2.6.2`
- LocalGPT Wire Protocol: unchanged `2.1.1`
