# LocalGPT 6.8.7 — Council request and rejoin regression repair

Base: LocalGPT 6.8.6. PublisherStudio unchanged.

## Verified failure paths

- `CouncilChatClient.GetStreamingResponseAsync` called `ArgumentNullException.ThrowIfNull(request)` after `CreateRequest` swallowed a disposed-component `OperationCanceledException` and returned null. A failed browser-scoped request factory cannot legitimately launch a Council.
- Selecting a Council run merely updated the dropdown without restoring its transcript. The explicit Rejoin button refused completed runs. Opening a live run also incorrectly required an already-written spool snapshot, preventing early attachment.

## Changes

- `CouncilChatClient.cs`: stream and nonstream entry points detect failed request creation before dereferencing the request. They produce a bounded reconnect/rejoin message instead of throwing `ArgumentNullException` and do not start a new backend run. Factory cancellation is logged at Debug instead of being misclassified as provider failure.
- `Chat.LiveCouncil.razor.cs`: selecting a run immediately loads exact run evidence and attaches its chat when a retained live snapshot exists; completed-run Rejoin opens durable evidence. Live attachment does not require a prior spool checkpoint. Run IDs are always used to select a specific session.
- `Assert-CouncilXRoundWiring.ps1`: explicit preflight regressions for both request guards, selecting a run, and the live-before-spool attachment path.
- `LocalGPT.csproj`: version 6.8.6 to 6.8.7.

## Deliberately not changed

- No change to Council execution, watchdog, deterministic game engine or game rules.
- No expansion of component-scoped CancellationTokenSources or modifications to timeout settings.

## Validation boundary

Static source/negative-control checks and ZIP CRC passed. **No PowerShell execution, .NET compilation, Windows runtime check, or end-to-end Blazor rejoin was possible in this environment.** The separate Blazor `TryClaimPendingStream` timeout must still be investigated with a runtime trace; this package does not claim to fix that unrelated transport exception.
