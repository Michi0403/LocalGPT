# LocalGPT 6.7.3 — Offline verification

Baseline: `LocalGPT_6_7_2_source.zip` (complete-source archive). This release is additive: existing source files preserved, changed files limited to `src/LocalGPT/Services/Council/CouncilSpoolerService.cs`, `src/LocalGPT/Components/Pages/Chat.razor.cs`, `src/LocalGPT/LocalGPT.csproj`, and `build/Assert-CouncilXRoundWiring.ps1`, plus new documentation.

- `CS1572` diagnosis: source 6.7.2 attaches the `runId` XML tag to `GetRecentSummaries(int take)` and leaves `GetSnapshot(Guid runId)` without adjacent documentation.
- Corrected the XML documentation order. Checked that `GetRecentSummaries` has only `take` and `GetSnapshot` has only `runId`, each with exactly one summary and returns tag in the implementation and interface.
- Corrected two unrelated duplicate/misattached XML summary blocks introduced by the preceding Chat UI changes.
- The mandatory existing PowerShell guard now checks these source contracts; the corresponding offline regex checks reject the previous source and accept the fixed source.
- All existing JavaScript files and hashes, supported runtime identifiers, EF migration assets, and Council behavioral code were preserved.
- ZIP archive CRC and baseline-preservation comparison performed after packaging.

**Not performed:** `dotnet build`, Windows PowerShell/PowerShell Core policy execution, browser/DevExpress runtime acceptance, Council reconnect/OCR integration, database migration against a user's database, or virtualization guest execution. These cannot be claimed as passing without a supported test host.

`LocalGPTWebviewWrapper` NuGet project loading is a separate Visual Studio restore warning; no evidence in this report shows it is fixed.
