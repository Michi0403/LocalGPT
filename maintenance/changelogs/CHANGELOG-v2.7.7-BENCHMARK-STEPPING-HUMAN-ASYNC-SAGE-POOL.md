# LocalGPT 2.7.7 changelog

## Provider-qualified Benchmark Council

- Added `localgpt.models.benchmark.provider`, an approval-gated DXFunction backed by the same transport-neutral `IProviderModelBenchmarkService` used by the Chat configuration UI. Council workflows can now benchmark exact provider/endpoint/model identities across configured local and LAN AI hosts instead of falling back to the loopback-only adaptive Ollama path.
- The provider benchmark no longer truncates the selected target set at 24 models. Every selected provider-qualified benchmark target is retained, and `allDiscoveredModels=true` explicitly benchmarks all currently installed benchmark-capable provider candidates.
- Model-preset normalization no longer truncates provider model names/routes at 24, so a large benchmark selection survives preset persistence instead of silently losing later hosts/models.
- Added two profile-generation modes. Existing adaptive named profiles remain available; the new **Evenly spaced** mode interprets the configured profile count as the requested number of measurement points between the maintained minimum and the user-selected maximum context/output values. A five-step run therefore includes the lower endpoint, three evenly distributed intermediate points, and the configured maximum endpoint.
- Added configurable minimum context/output values, adaptive CPU-safe-control inclusion, and optional improvement-based early stopping. Early stopping is disabled by default in the Benchmark Council UI so an explicit stepping plan is actually measured unless the user opts into adaptive shortening.
- Added a provider-qualified reviewer pool picker and reviewer-count control. When no explicit reviewer pool is supplied, the maintained ranking prefers capable general/code reviewers such as exact `gpt-oss:20b` before tiny control models such as `deepscaler:1.5b`.
- The seeded Adaptive Ollama Benchmark Council now delegates its actual measurement phase to `localgpt.models.benchmark.provider` exactly once and tells preflight/task-curator roles to reuse authoritative discovery, hardware-road, attachment, and prior-human evidence instead of restarting the model/hardware questionnaire.
- Council team seed version is **19**. Existing user-modified team definitions remain authoritative under the normal seed-update policy.

## Council role provider pools and sage-style selection

- Added `AssignedModelsRandomRange` to Council role AI selection. The role owns an exact provider-qualified model pool, then chooses a deterministic-random invocation count from the configured minimum/maximum range for each Council run.
- Council Teams exposes an **Exact role invocations** shortcut. Setting it to `N` fixes both role minimum and maximum to `N`; leaving it unset retains the fully configurable random range.
- The selected invocation count may deliberately exceed the number of distinct models in the exact pool. LocalGPT consumes every member at most once in a shuffled cycle before starting another deterministic shuffled cycle, so a small sage pool can provide repeated role turns without silently substituting another provider or model.
- Repeated provider-bound turns switch only the affected workflow phase from a parallel all-member mode to sequential execution. This preserves explicit repeated invocations while keeping live-stream, heartbeat, activity and result identities unambiguous.
- Distinct paired-role contracts remain strict: a configuration that requests more repeated participants than the distinct pairing can satisfy is rejected instead of silently violating the pairing rule.
- Removed the former 100-participant normalization ceiling from generic random-range Council role counts. Participant count is now controlled by the saved role configuration and normal runtime/resource policy rather than a product correctness cap.

## Human Collaboration InteractiveServer responsiveness

- Human Collaboration decisions no longer await an approved deferred DXFunction on the Blazor renderer operation. The decision is persisted first, the editor is released immediately, and approved deferred work is supervised outside the renderer with a completion callback when the circuit is still alive.
- This prevents a long benchmark/autotune approval from holding the panel-wide busy state for minutes or hours. Reopening the panel should no longer be required merely to regain decision controls while a deferred operation continues in the server spooler.
- Human-collaboration request fingerprints no longer vary because of presentation-only target-member, round, or phase metadata. The same substantive question within a Council run can therefore reuse its maintained decision rather than being re-created by another member in another round.
- The human-collaboration DXFunction guidance now explicitly instructs Council members to consume existing discovery, hardware roads, attachments and prior human guidance, consolidate genuinely missing facts, and avoid repeated hardware/model questions.

## Viewport-safe bounded number editors

- `BoundedNumberEditor` popovers now anchor to the right edge of their editor, cap their width to the browser viewport, and use a tiny shared JavaScript clamp to shift an open popover back inside the visible viewport when layout/zoom/container geometry would otherwise overflow.
- The repair is implemented in the shared bounded-number component and therefore applies to Council answer/context sliders, benchmark token controls and every other current/future consumer rather than patching one popup locally.
- Narrow/mobile behavior retains the existing fixed inset layout.

## XML documentation tooling integrity

- Corrected the shared C# XML-documentation scanner so multiline auto-property/object/collection initializers are consumed through their real member terminator. Named constructor arguments inside those initializers can no longer be mistaken for fields/properties and receive generated XML blocks.
- Removed the false generator blocks exposed by the repaired parser and re-ran the sophisticated documentation enhancer. The final second pass reports **0 missing blocks and 0 enrichments**.
- XML documentation coverage/quality passes for **7,443 direct maintained C# declarations across 408 source files**, including private/internal implementation members, properties, fields/events, parameters, return values and value documentation where applicable.
- Authored rich documentation remains authoritative; the generator fills/enriches missing weak documentation but does not intentionally downgrade detailed source comments.

## Source integrity and version

- Existing X-Rounds, foreground-only heartbeat restart, provider-qualified live lanes/results, attachment restoration, text-service ownership, code-generation workspace, PowerShell output, 1-Wire capability synchronization and controller catalog fixes are preserved.
- LocalGPT application, WebView wrapper and installer version: **2.7.7**.
- `LocalGPT.WireProtocolVersion` remains **2.1.1** because no 1-Wire message shape changed.
- Generated DocFX `wwwroot/help-docs` output was not regenerated in this source-only environment.
