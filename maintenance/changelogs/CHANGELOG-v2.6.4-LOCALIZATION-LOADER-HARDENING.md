# LocalGPT 2.6.4 — Localization loader hardening

## Runtime duplicate handling

- Replaced the intermediate `JsonSerializer.Deserialize<Dictionary<string, string>>` catalog load with `JsonDocument.Parse` + direct property enumeration.
- Duplicate JSON properties are now observed in source order and normalized directly into an `OrdinalIgnoreCase` dictionary. Case-only duplicates in optional/user catalogs therefore cannot throw during dictionary construction; the later value wins deterministically and a warning is logged.
- Non-object optional localization JSON is ignored with a warning instead of taking down the application shell.
- The source-controlled EN/DE catalogs remain strict: case-insensitive duplicate keys still fail `Assert-LocalizationIntegrity.ps1`. Runtime tolerance does not weaken the build safeguard.

## Guard coverage

- `Assert-LocalizationIntegrity.ps1` now also verifies the duplicate-safe loader contract (`JsonDocument.Parse`, `EnumerateObject`, `OrdinalIgnoreCase`) and rejects regression to direct dictionary deserialization.
- The obsolete Archive Preset `Text.*` key remains absent; the semantic `SourceText.ArchivePreset.SentenceCase` / `Phrase.Archive␠Preset` entries remain authoritative.

## Versions

- LocalGPT: `2.6.4`
- LocalGPTInstallerConsole: `2.6.4`
- LocalGPTWebviewWrapper: `2.6.4`
- LocalGPT Wire Protocol: unchanged `2.1.1`
