# LocalGPT 3.1.10 source validation

Source-only validation record. No `dotnet`, MSBuild, NuGet restore, DevExpress compile, build, publish or EF migration command was available/executed while preparing this archive.

The reported 3.1.9 published behavior is consistent with a pre-provider native attachment failure: the selected file is accepted by the paperclip UI, but native send reports a missing MIME type and no provider-response diagnostics follow for that attempted prompt. The repair therefore stays at the browser upload metadata boundary and adds a backend fallback without replacing the native DXAiChat delivery path.

Validated statically:

- LocalGPT, LocalGPTInstallerConsole and LocalGPTWebviewWrapper versions are 3.1.10;
- 1-Wire protocol remains 2.1.1;
- .NET SDK policy remains 10.0.400 / `net10.0`; the existing DevExpress 25.2 package lane remains unchanged;
- `Chat.razor` SHA-256 is identical to the 3.1.9 baseline;
- `Chat.razor.css` SHA-256 is identical to the 3.1.9 baseline;
- the detailed Chat Configuration markup remains unchanged;
- the complete `DxAIChat` subtree remains unchanged;
- the 3.1.9 Team / Models / Performance quick row remains unchanged;
- no CSS is introduced for attachment recovery;
- native browser file-selection events are normalized in capture phase before the existing DevExpress target/bubble handling;
- only files whose browser MIME value is blank are cloned, with name/bytes/last-modified preserved and `application/octet-stream` supplied;
- successful native DXAiChat delivery is still automatic; no `MessageSent` override/manual response pipeline is introduced;
- a short-lived native-send draft snapshot is restored only when the specific missing-MIME pre-send validation failure newly appears;
- draft recovery preserves typed composer text and cached browser `File` objects without inventing a sent user message;
- `CouncilTextService.ExtractUploadFiles(...)` supplies `application/octet-stream` if `DataContent.MediaType` is empty;
- existing live-Council upload behavior and its existing content-type fallback remain unchanged;
- `localgpt-chat-ui.js` passes Node syntax validation and its maintained JavaScript diagnostics manifest hash is refreshed;
- provider-stream repetition watchdog and Council recovery/failover remain present;
- BenchmarkEvidence JSON schema remains version 1;
- EF migration source digest remains `27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f`;
- `DatabaseMigrationCompatibilityService.cs` digest remains `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`;
- XML documentation completeness remains enforced for maintained C# and Razor source.

The packaged ZIP is extracted into a fresh directory and the 3.1.10 attachment audit, 3.1.9 quick-row isolation audit, release audit, XML documentation audit, async continuation audit, service resilience audit, application architecture audit, configurable behavior audit and provider-stream repetition policy audit are rerun from the packaged source before handoff where those audits are executable in this environment.
