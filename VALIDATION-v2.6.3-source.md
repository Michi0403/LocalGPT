# LocalGPT 2.6.3 source validation

This package was prepared without running `dotnet`, MSBuild, restore, build, publish, or GitHub access.

Static validation performed:

- Parsed both maintained localization JSON catalogs successfully.
- Raw case-insensitive duplicate-key scan: **0 collisions** in both maintained catalogs.
- Verified the obsolete `Text.Archive␠Preset` key is absent from both cultures.
- Verified `SourceText.ArchivePreset.SentenceCase` and `Phrase.Archive␠Preset` remain present in both cultures.
- Verified English/German key sets are identical.
- Verified `LocalGptLocalizationService.LoadCatalogFile` still performs defensive entry-by-entry `OrdinalIgnoreCase` normalization.
- Verified the localization integrity guard remains enabled from `Directory.Build.targets` and now rejects the obsolete Archive Preset key explicitly.
- Parsed project/MSBuild XML files successfully.
- Verified versions: LocalGPT / InstallerConsole / WebviewWrapper = `2.6.3`; Wire Protocol unchanged.
- No .NET build was executed; the user's Windows build remains authoritative.
