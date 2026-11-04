# LocalGPT 6.9.1 — Chat/Council start: renderer-dispatcher offload

## Evidence (6.9.0 Debug run, 2026-11-04)

Server log, in this order, for circuit `J79pttXy…`:

1. `ChatClientFactory: Finished extracting upload files.` — the send path has entered `CompositeChatClient.AddBootstrapContextAsync`.
2. `Blazor circuit … connection went down.` / `… closed.` — WebSocket lifetime 72.7 s.
3. `BuildDebugInventoryService: Finished GetSearchRoots` / `Finished enumerating build debug files.` — the bootstrap briefing was still running **after** the circuit had closed.
4. `Chat.CreateCouncilRequest()` → `OperationCanceledException` — the Council request factory was reached only after the circuit was gone, so no Council run could start.

Browser console from the same failure class: `CircuitHost.TryClaimPendingStream: The stream with ID 7 is not available. It may have timed out.`, then `Cannot send data if the connection is not in the 'Connected' State`.

## Root cause

`TryClaimPendingStream` executes on the circuit's renderer dispatcher, while pending .NET-to-JS stream references expire after ~10 s on a thread-pool timer. A dispatcher blocked for longer than that makes the claim fail and terminates the circuit.

The chat send path awaited `AddBootstrapContextAsync` directly from the DevExpress chat control, i.e. on the renderer dispatcher. That method reads SQLite (prompts, memory, knowledge, application logs), may create an upload workspace (archive extraction and SHA-256 hashing), and builds the bootstrap briefing including a recursive `bin`/`obj` debug-symbol scan. Microsoft.Data.Sqlite and the synchronous directory enumeration complete their "async" calls synchronously, so `ConfigureAwait(false)` never left the dispatcher for the database part. The UI therefore froze (spinner, no Council introduction) until the circuit was torn down.

The code path itself is the same as in 6.4.1. What grows between versions is the data it reads (application-log table, memory, knowledge, build output under `src/LocalGPT/bin|obj`). That explains a regression without a matching code change; it is an inference from the evidence above, not a measured timing.

## Fix

- `CompositeChatClient`: both call sites (`GetResponseAsync`, streaming path) now use `AddBootstrapContextOffDispatcherAsync`, which snapshots the message list on the caller thread and runs the unchanged enrichment via `Task.Run`. The work stays awaited and cancellation-aware. In a source checkout, the code after the enrichment already continued on the thread pool once the DevExpress inventory file reads completed; in an installed release without a repository root it previously stayed on the dispatcher. The Council request factory that runs afterwards only reads/writes Chat fields (its rendering already goes through `TaskRunner` + `InvokeAsync`) and calls `Notifier` only on error paths; the 6.9.0 source already documents that it may run outside the renderer.
- `BuildDebugInventoryService`: the recursive debug-symbol scan runs on the thread pool and honors the request's cancellation token (per root, per file and during enumeration). A canceled chat request no longer keeps scanning build output for tens of seconds.
- `CouncilChatClient` is unchanged. It is invoked after the enrichment, which now completes on the thread pool, so the Council run's synchronous prefix no longer starts on the dispatcher either. (Wrapping the launch in `Task.Run` was tried and rejected: `Assert-CouncilXRoundWiring` requires the literal launch-before-first-yield pattern, and the guard was not weakened.)
- `Chat.CreateCouncilRequest`: an `OperationCanceledException` caused by the ended circuit is logged at Debug level and rethrown (no misleading Error entry, no toast on a dead circuit). `CouncilChatClient` already handles this as "no run started".
- Version `6.9.0` → `6.9.1`.

Not changed: Rejoin/hydration logic, reconnect JavaScript, SignalR/circuit options, installer, SQLite recovery, DevExpress components, build guards.

## Verification limits

- All 36 build-wired PowerShell guards from `Directory.Build.targets` were executed with PowerShell 7.5.4 on Linux against this tree (see `VALIDATION-6.9.1-OFFLINE.md`).
- No .NET SDK was available, so no C# compilation, no Windows build, no runtime test. A successful Council start and Rejoin is **not** claimed.
