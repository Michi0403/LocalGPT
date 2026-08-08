# LocalGPT 2.3.10 — build-guard and path-formatting repair

This source revision is deliberately narrow and keeps all LocalGPT 2.3.9 service-resilience, 2.3.8 dynamic code-generation/function-recovery/path-explorer, Council, project, knowledge, deployment, and Kawaii documentation work intact.

## Build guard repair

- `HumanCollaborationInbox.razor` keeps its explicit `InteractiveServerRenderMode(prerender: false)` directive as the first non-empty directive, matching the maintained render-mode contract.
- The existing `@using System.Text.Json` remains directly below it; no Human Collaboration behavior was removed.

## Text-service ownership repair

- Local path warning composition moved from the Razor component into `ILocalPathExplorerService` / `LocalPathExplorerService`.
- `LocalPathExplorer.razor` now renders the already-owned service result instead of invoking `string.Join` directly.
- The new service method follows the 2.3.9 resilience policy with its own `try/catch`, `ILogger` diagnostic, and rethrow.

## Versioning

The application, WebView wrapper, and installer source versions are 2.3.10. The checked-in generated Kawaii DocFX tree/PDF is intentionally not relabeled; an owner-side .NET/DocFX build generates documentation for the real source version.
