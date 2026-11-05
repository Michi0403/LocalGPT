# LocalGPT 6.7.3 — Council XML documentation compile repair

## Windows build failure

The 6.7.2 update inserted `GetRecentSummaries(int take = 50)` into `CouncilSpoolerService.cs` between the XML documentation for `GetSnapshot(Guid runId)` and its method declaration. The resulting documentation included an invalid `<param name="runId">` tag on `GetRecentSummaries` and two `<summary>` tags, producing compiler error `CS1572` when documentation compilation is enabled.

## Corrections

- Attach one appropriate `<summary>`, `<param name="take">`, and `<returns>` to `GetRecentSummaries`.
- Restore the `<summary>`, `<param name="runId">`, and `<returns>` immediately above `GetSnapshot`.
- Fix two additional misattached/duplicated property documentation blocks in `Chat.razor.cs` (Council session history/feedback options and ASCII action state). Existing UI behavior stays unchanged.
- Extend the existing build-wired `build/Assert-CouncilXRoundWiring.ps1` guard to validate adjacent XML documentation in both the spooler implementation and its interface, plus these Chat properties. No build policy or warning is disabled.
- Advance the version to 6.7.3. All previous Council session recovery, retained-run history, image/OCR actions, approval boundaries, EF migrations, Hyperlight providers, and platform targets remain unmodified.

## Limitations

Only source-level checks are possible in the current environment. Neither the .NET 10 SDK nor PowerShell is installed. A Windows build and live reconnect/image workflow remain required acceptance checks. The separate Visual Studio restore warning about `LocalGPTWebviewWrapper` is not diagnosed or resolved by this source repair.
