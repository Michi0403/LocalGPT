# LocalGPT 3.5.1 — Compiler Follow-up and Shared Release-Packaging

## Compiler maintenance

- Added the required `using LocalGPT.Interfaces;` import to `LocalGptLocalizationService.cs`, matching the user-confirmed Windows compiler fix for `ILocalGptRuntimePolicyDataService`.
- Added the required `LocalGPT.BusinessObjects` import to `ChatContentRenderer.cs` so the database-backed `LocalGptRuntimeValue.StructuredTextMaximumInputCharacters` policy resolves at compile time.
- Exposed `ThemeService.MaxFusionRouteSteps` as the instance policy value consumed by `ThemeJsChangeDispatcher`, and changed the dispatcher to use its injected `Themes` instance instead of attempting static access.
- Restored one shared `MaxTextLength` policy property on the partial `HumanCollaborationService`, so contribution/evaluation partials and the primary service use the same database-backed `HumanCollaborationMaximumTextLength` value.
- Preserved the 3.5.0 telemetry/OCR/operator-policy repairs and the existing `InteractiveServer` render-mode boundaries.

## Shared release-packaging package

- LocalGPT remains the authoritative source owner for `LocalGPT.ReleasePackaging`.
- `Build-Release.ps1` now places `LocalGPT.ReleasePackaging.1.0.0.nupkg` in the upload-ready LocalGPT release bundle and in the same `%LOCALAPPDATA%/LocalGPT/NuGet` shared cache used for the authoritative 1-Wire package.
- Release-packaging tool installation uses an isolated NuGet configuration rather than `--add-source`, avoiding the NuGet package-source-mapping conflict reported from the PublisherStudio release build.
- SHA-256 manifest writing no longer uses the PowerShell 7-only `utf8NoBOM` encoding token; it writes BOM-free UTF-8 through .NET so the Windows PowerShell 5.1 release launcher remains valid.
- The package still drives the native Unix packaging lane for TAR.GZ/DEB/checksum generation while macOS DMG and Linux RPM/AppImage finishing remain host/tool dependent as designed.

## Versioning

- Application, setup/wrapper, documentation, and frontend cache-busting identities move from 3.5.0 to 3.5.1.
- The one-digit minor/patch slot policy remains enforced.
