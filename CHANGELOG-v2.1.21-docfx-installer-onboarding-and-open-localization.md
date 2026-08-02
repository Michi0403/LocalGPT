# LocalGPT 2.1.21

## Corrected build blockers

- Added the missing `LocalGPT.BusinessObjects` imports required by `DocumentationUpdatedAttribute` in the system-variable interface, navigation service and system-variable definition service.
- The DocFX build now stages the complete output-directory assembly set instead of only `LocalGPT.dll`, allowing metadata extraction to resolve DevExpress and other referenced runtime types.
- A failed DocFX metadata extraction no longer aborts a Debug application build. LocalGPT generates a bounded Markdown API reference directly from the compiler XML file and then continues the normal DocFX HTML/PDF pipeline.
- A failed HTML build remains fatal only when documentation/PDF output was explicitly required. Existing published help files are preserved otherwise.
- Temporary documentation assemblies are removed after the documentation stage so source trees are not polluted with binaries.

## Persistent installer onboarding

- `/install` permanently exposes the setup guide after the user marks it reviewed.
- The seeded adaptive benchmark Council, GameDirector preset and C#/PowerShell/Java/Minecraft development teams are available as direct Chat quick starts.
- Installer profiles list their reviewable command, recommended models and knowledge repositories.
- Documentation and Council-team configuration have direct buttons from the installer.

## Open localization catalogs

- English and German remain shipped fallback catalogs.
- LocalGPT accepts every culture known to the installed .NET runtime instead of hard-coding two supported cultures.
- Users can import a flat string-to-string JSON catalog from `/install` for cultures such as `fr-FR`, `es-ES` or `ja-JP`.
- Catalogs are validated for size, entry count, culture validity, empty keys and case-insensitive duplicate keys.
- Imported catalogs are written atomically to the current user's LocalGPT application-data directory and survive rebuilds.
- User catalogs can augment shipped catalogs; missing keys fall back to English.
- The global language selector now lists all installed LocalGPT catalogs dynamically.
- Localization catalog inspection and import are also available through `/api/localization/catalogs` and `/api/localization/catalogs/import`.

## Version

- Application, project seed and organic-wire advertisement updated to `2.1.21`.
