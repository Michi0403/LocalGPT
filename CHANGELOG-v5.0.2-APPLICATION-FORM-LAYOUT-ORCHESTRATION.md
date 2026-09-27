# LocalGPT 5.0.2

## Application-wide DevExpress layout envelope
- Wraps every route exposed by `NavMenu` in the shared `LocalGptFormLayoutSurface`, whose outer container is a DevExpress `DxFormLayout` with a full-width `DxFormLayoutItem` by default.
- Keeps each page's existing rows, columns, flexboxes, grids, specialized DevExpress controls, CSS and render-mode ownership inside that envelope instead of flattening or redesigning working layouts.
- Adds nested layout surfaces around the primary editors/workbenches for Chat Quick Council, Council Teams, Projects, Project Maintenance, Database, Remote Control, DX Functions, 1-Wire Security, Minecraft Mod Builder, Test Lab, Help, Model Council, and every Install workbench section.
- Supports an optional nested `DxStackLayout` orientation without removing the required outer FormLayout envelope.
- Keeps route envelopes, the Chat Quick Council/Voice surface, and the Layout Studio editor itself non-hideable so a saved layout cannot lock the operator out of navigation, microphone access, or layout recovery.

## Stable layout identity and persistence
- Adds `LocalGptUiLayoutSurfaceAttribute` declarations to routable pages and registered subeditors.
- Keys layout objects as `assembly | fully-qualified Razor component | section`, so refactors can target one mental/layout boundary instead of relying on DOM position or incidental CSS selectors.
- Persists operator overrides as JSON under the LocalGPT per-user data root at `ui-layouts/form-layouts.json`.
- Keeps source-declared layout defaults authoritative when no override exists, while persisted user overrides control visibility where permitted, order metadata, medium breakpoint span, optional stack orientation/length and an extra theme/layout CSS class.
- Adds `ILocalGptUiLayoutService` / `LocalGptUiLayoutService` and `api/ui-layouts` GET/PUT/DELETE endpoints for future layout orchestration and external tooling.

## Install Layout Studio
- Adds a dedicated `Layout Studio` section to `/install`.
- Lists all source-declared page and section surfaces even if a page has not been visited in the current process.
- Provides DevExpress editors for the persisted layout envelope and a 12-column optical preview inspired by PublisherStudio's visual editing approach.
- Supports save, reload and reset-to-source-default workflows without mixing layout preferences into business/provider configuration.

## Maintenance contract
- Adds `build/Assert-FormLayoutOwnership.ps1` and wires it into normal LocalGPT builds so every NavMenu route must retain both its stable layout declaration and the shared DevExpress FormLayout envelope.
- Preserves `@rendermode InteractiveServer` declarations and all existing feature behavior, including the Chat microphone/SpeechRecognition work, AI function-call presentation, MCP gateway, OneWire, Council, project, runtime and approval systems.

## Version
- Advances LocalGPT from 5.0.1 to 5.0.2.
