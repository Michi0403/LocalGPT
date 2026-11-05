# LocalGPT 6.6.0 — Detachable Council observers and explicit backend Stop

## Corrected lifecycle

- Council execution now starts **before** the first streamed browser update is yielded. A browser leaving after that first update can no longer leave a registered live run that has never started its backend work.
- Blazor/DevExpress streaming cancellation now **only detaches the current viewer**. It never calls `CouncilLiveSessions.Cancel` implicitly. The execution scope and token remain process-owned through `CouncilLiveSessionService`.
- The existing native DevExpress Stop button forwards a **real click** to the existing `[JSInvokable] StopActiveCouncilRunAsync`, which invokes the authoritative server-side `CouncilLiveSessions.Cancel(runId)`. Existing LocalGPT Stop buttons, game-console Stop and user-confirmed cancellation remain supported.
- On viewer cancellation, updates stop flowing to its private channel immediately; the private delivery channel is bounded to 512 updates, preventing indefinite buffering if a viewer stalls. The shared live-session transcript, progress, spooler checkpoints and active game continue updating as before. No change to Council/team/project/knowledge/regex mechanics.

## Platform and legacy behavior

- Shared `.NET 10` C# and browser JavaScript only. **No Windows, Linux, macOS paths, native integrations, installer scripts, database schemas, OneWire, or renderer implementations changed.** All seven existing runtime identifiers are preserved.
- The legacy Kernel Creature Tournament skeleton/ASCII renderer and new project-authored ASCII/ANSI/pixel renderers are unchanged.
- An application **process exit or crash** still ends active model calls; this is not a durable process-restart scheduler.

## Inspected diagnostic archive

- On Oct 28, 2026, a tournament record ran 10:08:47–11:17:39 UTC and completed, which demonstrates server-side continuation in that instance.
- The archive also records six paired DevExpress `DeviceInfo` null-reference failures in one browser circuit at 09:06:42 UTC and DevExpress browser keyboard navigation JavaScript errors. Those are separate, not proven to be caused by the Council adapter, and are **not claimed fixed** by this change.
- The archive contains sensitive application data; it is intentionally **not included** in this source release.

## Validation

- Source-level automated invariants, JavaScript parse check, C# project XML parsing, ZIP integrity and source-file preservation checks, without invoking GitHub or `dotnet`.
- Runtime behavior, .NET compilation and platform installers were not executed and cannot be certified from this environment.
