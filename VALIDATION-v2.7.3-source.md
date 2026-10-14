# LocalGPT 2.7.3 source validation

This package was edited and inspected directly from the supplied LocalGPT 2.7.2 source ZIP. No GitHub/network repository access was used. No `dotnet`, MSBuild, restore, build, test, publish or DocFX command was invoked; the user's Windows/.NET build remains authoritative.

## Completed source/static validation

- Provider-qualified Council audit: **210 checks** pass.
- Architecture policy: passes application static/diagnostic/C# structure boundaries.
- Async continuation audit: passes for **154 source files**, **2,267 await tokens**, **2,061 ConfigureAwait(false)**, **30 reviewed ConfigureAwait(true)**, **2 preconfigured awaitables**, **171 reviewed await-using disposals**, and **3 configured async streams**.
- Chat ASCII-console audit: **17 checks** pass.
- Service resilience: **1,806 service methods** own try/catch + diagnostics; 30 iterator/yield and 3 direct Program/Startup methods are intentionally excluded.
- Code-generation/DXFunction audit: passes DI discovery, five review functions, eight output kinds including PowerShell, approval-gated plain workspace writes, CodeDOM fallback, policy-backed project/import/upload paths and removal of former arbitrary repository/code-generation ceilings.
- Documentation/1-Wire contract audit passes; Kawaii documentation-layout audit passes.
- Council X-Round/heartbeat audit passes the five X functions, workflow policy persistence, immutable revisions, reconsider/reexecute separation, human/transition/depth gates, no-forward-jump guard, single-consumer live-message restart, shared later heartbeat context, remote live-lane pre-registration, per-host concurrency and live model-timeout wiring.
- XML documentation coverage passes for **7,225 maintained C# type/method/public API declarations**.
- LocalGPT application, wrapper and installer projects are versioned **2.7.3**; the shared wire protocol remains **2.1.1**.

## Targeted source checks

- Council Teams owns X-Round configuration per workflow step and exposes Gatekeeper, Reactive revisit, Derived single-model and Derived-Council convenience presets without hiding the individual permissions.
- X revisit targets are restricted to the current or an earlier configured workflow step so an X action cannot skip forward across a human/workflow gate.
- Reconsider revisions suppress the target step's DX/organic function policy; reexecute revisions deliberately retain that policy and its ordinary approvals.
- A direct live user message has a single immediate restart claimant while remaining queued for future Council heartbeat context.
- Every phase participant is registered in the live Council activity board before provider execution so remote/local members are represented equivalently even while ordered transcript presentation waits for an earlier member.
- Chat exposes host-balanced versus hardware-road parallel scheduling, hardware load, per-host concurrency, model timeout and the existing per-model road editor.
- The active-run snapshot carries per-host concurrency and model timeout so UI edits apply to provider work that has not started yet.
- Saved model presets preserve requested parallelism instead of applying the former fixed maximum of eight.
- README contains both explicit GitHub Pages URLs.
