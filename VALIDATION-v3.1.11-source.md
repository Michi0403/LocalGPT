# LocalGPT 3.1.11 source validation

## Validation boundary

This environment does not contain the LocalGPT .NET/DevExpress build toolchain. **No compile, publish, runtime, browser or database execution claim is made.** Validation below is source/static only.

## Focused 3.1.11 regression audit

`build/audit_council_formatting_learning_session_restore_3_1_11.py` passed 54 checks covering the new formatting boundary, topic-neutral persisted Learning workflow, session-configuration capture/rejoin restoration, version wiring, and protected Chat surfaces.

Protected files are byte-identical to 3.1.10:

- `Chat.razor`: `0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569`
- `Chat.razor.css`: `2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4`
- `wwwroot/js/localgpt-chat-ui.js`: `26a7609b73a450ae3643e922050bf8a821001be400b2ff93eb5ac07f3d1e817d`

Therefore this release does not alter the working DxAIChat composer geometry, Attach/Send/Stop markup, quick-selector row markup/CSS or the 3.1.10 attachment/draft browser bridge.

## Static audits executed successfully

- Chat quick-preset row regression audit: 34 checks.
- Application architecture audit: passed.
- Configurable Council behavior-policy audit: passed with current Council seed version 27.
- Provider-stream repetition policy: passed.
- Provider-qualified Council feature audit: 282 checks.
- Council X-Round/heartbeat source audit: passed.
- Async-continuation audit: 254 source files; 2,868 await tokens; 2,591 `ConfigureAwait(false)`; 64 renderer-affine `ConfigureAwait(true)`; 208 explicitly configured async disposals; 5 configured async streams.
- Service resilience audit: 2,105 service methods own try/catch plus diagnostics; 29 yield methods and 3 direct boot methods skipped by policy.
- XML documentation validation: 9,931 direct C# declarations across 632 files.
- Razor XML documentation audit: exited successfully.
- Python focused-audit syntax compilation: passed.
- `node --check` for the unchanged chat UI bridge: passed.

The complete command/output record is in `VALIDATION-v3.1.11-static-audit.log`.

## Persistence and compatibility

- EF migration aggregate digest is unchanged from 3.1.10: `68d59a54ded7a2dd9798d8e3c03d93a22dc65bc28cc1f3543ab5582bac784439`.
- `DatabaseMigrationCompatibilityService.cs` remains unchanged: `50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba`.
- No database migration is introduced. Council team seed evolution uses the existing persisted seed-version mechanism.
- BenchmarkEvidence schema remains 1.
- Existing .NET 10 / DevExpress 25.2 package lane is retained.
- LocalGPT wire protocol package remains `2.1.1` (`src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj`).

## 3.1.11 behavior covered by source validation

Council formatting now recognizes both LocalGPT assessment envelope names before Markdown processing, tolerates a model mixing the two recognized opening/closing names, removes a single wrapper escape after a JSON object/array when present, uses relaxed JSON escaping for human-readable Unicode, decodes HTML entities only inside controlled structured/code-display data, and HTML-encodes again at the final render boundary. Provider function calls/results use the same inert code surface.

Learning Round seed version 27 is topic-neutral and defines four persisted literal workflow steps: evidence inventory, study, verification and learning maintenance. The seed disables repetitive readiness introductions, treats user uploads and user-written content as first-class evidence, uses bounded read-only tools to settle resolvable ambiguity, keeps knowledge as the primary output and makes regex maintenance optional and evidence-tested. Existing user-modified Council-team rows continue through the repository's seed-preservation path.

Running Council state now captures team key, model-preset identity, hardware-performance-preset identity, critique rounds, memory and project-creation flags in addition to the existing models/routes and hardware/token settings. Live rejoin applies that snapshot on the Blazor renderer to the existing Chat Configuration state; the existing quick selectors derive from the same state. Applying a performance preset to a running Council updates the run-owned preset identity.

## Patch validation

The incremental 3.1.10 → 3.1.11 patch was generated with relative repository paths and `patch --dry-run -p1` completed successfully against a fresh copy of the 3.1.10 source tree.
