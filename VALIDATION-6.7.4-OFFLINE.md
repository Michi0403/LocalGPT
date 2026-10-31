# LocalGPT 6.7.4 — Offline validation

## Basis

Full source rebuilt from `LocalGPT_6_7_3_source.zip`, retaining all original files.

## Changes

- `src/LocalGPT/Components/Pages/Chat.razor.cs`: safe ASCII mirror lifetime access, stop rescheduling after disposal; reduce UI snapshot frequency.
- `src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs`: debounce list refresh while running multi-model Councils.
- `build/Assert-CouncilXRoundWiring.ps1`: build-wired lifetime/cadence source assertions.
- `src/LocalGPT/LocalGPT.csproj`: version 6.7.4.

## Verified and not verified

- Verified by offline source inspection: all pre-existing files retained; no direct `DotNetStreamReference` construction in maintained application source; no browser JS files modified; seven existing platform RIDs retained; package CRC check.
- **Not verified**: compilation, PowerShell execution, WebSocket/circuit operation, exact identity of stream ID 8, resolution of the underlying ten-second stream-handshake timeout, or absence of all concurrency faults.

## Relevant log interpretation

The reported `CircuitHost.TryClaimPendingStream` exception is for **.NET-to-JS** streaming. The subsequent WebSocket closure and `Cannot send data if ... not ... Connected` messages are downstream. The `ObjectDisposedException` in the Chat ASCII worker is an independent post-disconnect lifecycle issue addressed here. Additional browser/CLR tracing is required to attribute the stream producer before claiming a full circuit fix.
