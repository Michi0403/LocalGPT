# LocalGPT 6.9.0 — Offline Validation

Source baseline: `LocalGPT-6.8.9-Source-and-Review.zip`.

- 17 targeted source assertions and negative controls passed before documentation and ZIP verification.
- Negative control: original 6.8.9 matches **two** static declarations in `Chat.LiveCouncil.razor.cs`; corrected source matches zero.
- Minimal functional delta: both `static` modifiers removed, exact method bodies preserved; no Council restore caller changed.
- `CouncilText` remains injected on `Chat.razor`, and `HasCouncilRunMarker` still uses the approved text-service method with exact `StringComparison.Ordinal` matching.
- `DxAiChat.LoadMessages` and the spool-recovery fallback remain in place.
- `Assert-ApplicationStaticPolicy.ps1` and its `Directory.Build.targets` preflight wiring retained.
- Version `6.9.0` in the main web project; installer and publisher versions untouched.
- Complete release ZIP and external minimal unified diff provided separately.

### Not established

- No Windows PowerShell 5.1/pwsh invocation was possible here.
- No .NET 10 compile/Debug/Release build was run.
- No live Blazor WebSocket reconnect, Council rejoin, or installer/database functional test was run.
- Later build gates or unrelated compiler diagnostics may still reveal additional errors.
