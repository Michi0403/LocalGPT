# LocalGPT 6.7.4 — Blazor stream/circuit stabilization (source candidate)

## Confirmed evidence

- In the browser on 2026-10-30 at 15:23:28 UTC, ASP.NET Core logged `CircuitHost.TryClaimPendingStream`: `The stream with ID 8 is not available. It may have timed out.` The browser then disconnected; pending JS-to-.NET result acknowledgements produced `Cannot send data if the connection is not in the 'Connected' State` errors.
- ASP.NET Core's `RemoteJSRuntime.TransmitStreamAsync` retains pending **.NET-to-JS** stream references for approximately 10 seconds. This is **not** the SignalR `ClientTimeoutInterval`; increasing its value to two minutes does not address the stream claim timeout.
- The supplied LocalGPT server log additionally recorded `ObjectDisposedException: The CancellationTokenSource has been disposed` in `Chat.StartAsciiConversationMirrorLoop()` following circuit teardown. That is a real component lifecycle bug but is downstream of the disconnection in the supplied trace.
- No direct `DotNetStreamReference` construction is present in the maintained LocalGPT application source. Therefore the exact initiating .NET-to-JS stream (framework/DevExpress/other dependency) cannot be attributed from the current logs. Neither an image upload nor the microphone JS-to-.NET stream is established as the cause.

## Targeted changes

- Prevent the ASCII transcript mirror from launching after Chat disposal and absorb the known `CancellationTokenSource` disposal race without creating an unsupervised worker. Preserve cancellation and existing backend Council ownership.
- Prevent the ASCII mirror's finally block from re-scheduling work after component disposal.
- Reduce the frequency of large DevExpress `SaveMessages()` snapshots while the ASCII terminal is open (700 ms -> 1500 ms). Keep the terminal mirroring behavior, but avoid saturating renderer throughput under multi-member Councils.
- Debounce non-essential Chat session-list renders (200 ms -> 750 ms) without changing the already two-second live session attachment cadence or the persisted transcript/history logic.
- Add source assertions to the **existing build-wired PowerShell Council guard**. No test runner, build guard, warning, or migration was disabled.
- Preserve the existing seven RID targets, Council runs, rejoin methods, OCR flow, role assignments, model settings, and all user data.

## Remaining diagnosis and acceptance

This is **not** a verified cure for the underlying stream-ID failure. The producer of stream 8 is still unidentified. Capture browser-console and matching `Microsoft.AspNetCore.Components.Server.Circuits` server debug/error logs for the failing interval; where possible test an identical run without the ASCII terminal and compare renderer and browser-main-thread stalls. Verify 17-model runs, screen/tab switching, attachment transmission, complete and failed Council rejoin, manual stop and UI disconnect.

Actual Windows Debug/Release .NET compilation, PowerShell guard execution, DevExpress 25.2 UI behavior, and browser/SignalR stream handshakes have **not** been performed in this artifact-building environment. Do not interpret ZIP integrity or offline pattern checks as those tests.
