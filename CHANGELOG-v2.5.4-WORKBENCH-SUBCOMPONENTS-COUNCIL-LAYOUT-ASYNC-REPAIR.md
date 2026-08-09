# LocalGPT 2.5.4 — Workbench subcomponents, Council layout and async repair

## Scope

This release is source-only. It addresses the LocalGPT configuration/test/security UI regressions and the interactive-circuit stalls reported against 2.5.3. Existing provider, Council, persistence, logging, 1-Wire and deployment behavior is retained; the work is concentrated on how those capabilities are composed and initialized in the Blazor UI.

## Fixed

### Configuration workbenches are real selectable surfaces

- Added reusable `ConfigurationWorkbenchNav` and `ConfigurationWorkbenchPanel` components plus a shared `WorkbenchNavItem` model.
- `/install` now renders only the selected setup segment instead of using a left navigation that merely jumps to anchors on one very large page.
- `/test-lab` now renders one selected diagnostic tool surface at a time instead of presenting all diagnostics as one long page.
- `/onewire-security` now uses the same workbench structure for Identity, Protocol, Pairing and Trusted Peers. The very large protocol catalog is isolated from the security controls rather than dominating the entire page.
- Chat configuration now uses Provider, AI Council, Memory & projects and Architecture workbench sections.
- Existing deep links remain useful: known `/install#...` and `/test-lab#...` fragments select the corresponding workbench segment instead of pointing at content that is not currently rendered.

### Chat AI Council / Memory panel collision

- AI Council, Memory & project context and Architecture are now separate conditionally rendered workbench panels.
- Only the active configuration panel exists in the configuration viewport. Hidden Memory/Project and Architecture controls can therefore no longer paint over an expanded AI Council panel.
- The Chat configuration stage is the single vertical scroll owner and forces nested Council/Memory/Architecture surfaces back into normal document flow (`position: static`, bounded width, no inherited transforms/insets).
- Responsive behavior switches the left navigation into a horizontal section strip on narrow screens.

### Install provider visibility and multi-host setup

- Added a persistent `Configured AI hosts` catalog to `/install`.
- The catalog exposes primary and additional OpenAI-compatible / LM Studio endpoints, primary and additional Ollama bindings, OpenAI Cloud and Azure OpenAI configuration.
- Additional local OpenAI-compatible hosts and additional Ollama model/endpoint bindings remain editable and removable from the existing provider forms.
- The provider count is surfaced in the workbench navigation so a multi-host AI Council configuration is visible before opening the detailed provider form.

### Interactive startup / ConfigureAwait policy

- Chat no longer performs its database-backed defaults, memory, project, Council-team and model-preset loading inside the initial component lifecycle before the interactive shell can become usable. The shell renders first; initialization runs under the existing supervised task runner and only short renderer updates are dispatched back to Blazor.
- DXAiChat runtime activation is gated until the background Chat state initialization is complete, avoiding a race between DevExpress initialization and database-backed state loading.
- Install first-render discovery/onboarding/toolchain/Ollama/connectivity refresh now runs as supervised background work rather than holding the renderer lifecycle task.
- Test Lab renders immediately and preloads Council role choices through supervised background work.
- 1-Wire Security renders its shell first and loads security/capability state outside the renderer with an eight-second bounded initialization path.
- Chat auto-save persistence no longer executes the complete persistence sequence through `InvokeAsync`; only the DevExpress message snapshot is captured on the renderer, while SQLite/memory/project persistence continues off-dispatcher.
- Ordinary application/component awaits use `.ConfigureAwait(false)` by default. The async policy baseline now keeps `.ConfigureAwait(true)` only for the explicitly reviewed Chat renderer/JS helpers and lifecycle-affine browser operations.
- Removed a recursive failure path from the Install UI activity logger so a failed renderer refresh cannot recursively log through the same failed path.

### Render mode preservation

- `@rendermode InteractiveServer` remains on the maintained interactive pages, including Chat, Install, Test Lab and 1-Wire Security.
- The new shared workbench child components deliberately inherit the parent InteractiveServer circuit. They do not create a nested render-mode boundary around `RenderFragment` content.

## Version

- LocalGPT: `2.5.4`
- LocalGPTInstallerConsole: `2.5.4`
- LocalGPTWebviewWrapper: `2.5.4`

The repository's version-slot rule is respected; no minor or patch slot reaches two digits.

## Build note

Per the delivery constraint, this package was not restored, compiled, built, published or run with the .NET SDK. No GitHub or online repository access was used. Static repository audits and source/package checks are documented in `VALIDATION-v2.5.4-source.md`.
