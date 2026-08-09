# LocalGPT 2.5.3 — Configuration workbench and scroll repair

## User-facing fixes

- Fixed the Chat configuration workspace regression that could leave the modal body effectively unscrollable. The final CSS contract no longer forces the grid body to `height: 0`; the modal now uses one bounded vertical scroll owner with mouse, trackpad, keyboard, and touch scrolling.
- Added a sticky in-workspace section navigator for Provider, AI Council, Memory & projects, and Architecture settings so long configurations are reachable without hunting through the full form.
- Reworked `/install` into a PublisherStudio-inspired configuration workbench while preserving the existing provider, discovery, logging, network endpoint, TLS certificate, onboarding, toolchain, and localization options.
- Added a shared `WorkbenchHeader` component so configuration pages can reuse the same visual structure instead of growing more page-specific header markup.
- Added sticky section navigation and an explicit page scroll owner to `/install`; long provider/network/certificate pages remain responsive and keyboard/touch reachable.
- Reworked `/test-lab` into a responsive diagnostics workbench with section navigation, bounded result/source scrolling, and clearer full-width/dual-column behavior on desktop and narrow screens.
- Added synchronized English and German localization keys for the new configuration-workbench navigation and headings.

## Preserved behavior

- `@rendermode InteractiveServer` remains present on `/chat`, `/install`, and `/test-lab`; no working render-mode boundary was removed or converted.
- Existing logging, cancellation, notification, persistence, provider discovery, certificate, toolchain, Learn-Base, remote knowledge, artifact workspace, and diagnostic route logic is unchanged.
- Existing `ConfigureAwait(false)` use is preserved. Renderer-affine loading continuations already marked with `ConfigureAwait(true)` remain unchanged.
- No configuration field or action was intentionally removed from the three redesigned surfaces.

## Maintenance

- Advanced LocalGPT, LocalGPTInstallerConsole, and LocalGPTWebviewWrapper from 2.5.2 to 2.5.3. The version-number rollover rule remains satisfied; neither the minor nor patch slot reaches two digits.
- Repaired the ASCII-console static release gate so its version check accepts later valid semantic versions instead of being permanently pinned to the old 2.3.x line.

## Source validation

This delivery is source-only by request. No `dotnet build`, restore, publish, GitHub access, or online repository access was performed. Static repository audits and source-structure checks are documented in `VALIDATION-v2.5.3-source.md`.
