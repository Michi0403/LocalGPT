# LocalGPT 5.0.3 — Chat, layout and interaction repair

## Fixed

- Reworked browser microphone capture so capture objects stay inside the ES module and .NET carries only an opaque capture id. `start`, `stop`, `cancel`, and `dispose` are module functions, removing the failing JS-object-reference boundary that produced `The value 'start' is not a function`.
- Removed the teardown-only JavaScript module disposal call from `ThemeJsChangeDispatcher`, avoiding the known InteractiveServer `JSDisconnectedException` race while the circuit is already being disposed.
- Sanitizes raw JSON control characters inside quoted provider/tool strings before `JsonDocument.Parse`, preventing newline/control-byte payloads from throwing the reported formatter exception before the inert-code fallback can run.
- Restored a reliable drawer-content scrollbar for Chat and ordinary routes. Chat no longer hard-clips a streamed conversation behind a 100dvh/overflow-hidden chain.
- Replaced the shell language DxComboBox with the already styled native language selector so culture choices are not dependent on a body-level DevExpress popup portal.
- Workbench navigation explicitly uses text-mode DevExpress buttons and LocalGPT foreground tokens; selected/highlighted sections no longer turn their label/description text invisible under mixed shell/component themes.

## Layout Studio

- Layout surfaces now persist an outer `LayoutType` and orientation while retaining the 5.0.2 `InnerLayout` values for backward compatibility.
- The shared layout surface can render DevExpress `DxFormLayout`, `DxGridLayout`, `DxStackLayout`, `DxTabs`/`DxTabPage`, `DxSplitter`, and `DxCarousel`, or a neutral content envelope.
- Layout Studio exposes the full layout-family selector plus vertical/horizontal orientation.
- Chat now registers explicit layout surfaces for `FunctionCallInfoContentTemplate`, `MessageContentTemplate`, and the ASCII popup `BodyContentTemplate`, so previously missed template regions participate in Layout Studio.

## Coding workflow

- Council instructions now treat a concrete source-code request as sufficient scope for isolated reviewed source generation; optional web search cannot block code that can be drafted from established language knowledge.
- Python requests are directed through `codegen.review.create` with a real `.py` file so the existing database-backed greenfield project/revision binding and generated workspace path are used.
- `HumanApprovalPending` is explicitly treated as a pending side action: models must not re-request the same approval, wander into model benchmarking, or stop independent coding work.
- After approved generation, the model is instructed to report both the returned direct artifact `DownloadUrl` and local `WorkspacePath`.

## Versioning

- Advances LocalGPT from 5.0.2 to 5.0.3.
- Version components remain single-digit, matching the repository handoff policy.
