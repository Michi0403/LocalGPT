# LocalGPT 5.9.6 — Third-party release notice synchronization

## Release-blocking regression

The `Build-Release.ps1` preflight invokes `build/Assert-ThirdPartyNotices.ps1`, which correctly requires the consolidated release notice to contain the complete, current, unmodified text of every required third-party notice.

In 5.7.8, `third_party/PDFsharp/SOURCE-NOTICE.md` was amended to describe `PdfDocumentOptions.cs` compressed-content/compact PDF behavior. The matching block in `THIRD-PARTY-NOTICES.txt` was not updated. This outdated paragraph persisted through 5.9.5, so release publishing correctly refused to proceed.

## Correction

- Replaced **only the outdated PDFsharp provenance paragraph** inside the consolidated `THIRD-PARTY-NOTICES.txt` with the exact current paragraph from `third_party/PDFsharp/SOURCE-NOTICE.md`.
- Retained the original upstream PDFsharp 6.2.4 MIT license, the full bundled source notice, both Open Iconic licenses and all previously published third-party sections.
- Preserved the strict `Assert-ThirdPartyNotices.ps1` fail-closed release validation and `Build-Release.ps1` entrypoint without modifications.
- Advanced the application version to **5.9.6**; ReleasePackaging and wire-protocol versions are unchanged.
- No modifications to vendored PDFsharp implementation or PublisherStudio.

## Validation and release impact

Static verification compared the entire text of all five required source notices to `THIRD-PARTY-NOTICES.txt` with the same BOM, CRLF and trailing-whitespace normalization as the PowerShell release guard, and confirmed all mandatory identity markers. The original 5.9.5 ZIP source entries were retained without modification except for `THIRD-PARTY-NOTICES.txt` and `src/LocalGPT/LocalGPT.csproj`.

**Not run here:** PowerShell, `Build-Release.ps1`, .NET compilation, NuGet package build, or a macOS publish. The Windows/macOS release execution remains the definitive test.

Because 5.9.6 changes the application version, its documentation PDF has a new version identity. The first 5.9.6 build may need to generate that PDF; later same-version builds should follow the existing reuse behavior.
