# LocalGPT 2.8.5 changelog

## Localization

- Added Spanish (`es-ES`).
- Added French (`fr-FR`).
- Added Japanese (`ja-JP`).
- Added Ukrainian (`uk-UA`).
- Kept German (`de-DE`) and English (`en-US`) as existing built-in cultures.
- All six built-in catalogs use the complete current 1,862-key set. Ordinary UI/domain strings are localized; product names, model names, APIs, file formats, command literals, paths, units, and other technical identifiers remain canonical where translating them would be incorrect.
- No hard-coded culture allow-list was added: LocalGPT continues to discover built-in/user catalogs through `LocalGptLocalizationService`.

## Release policy

- LocalGPT: 2.8.5.
- LocalGPTWebviewWrapper: 2.8.5.
- LocalGPTInstallerConsole: 2.8.5.
- 1-Wire protocol: 2.1.1 (unchanged).
- Existing InteractiveServer boundaries are unchanged.
