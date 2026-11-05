# LocalGPT 5.5.2 — Data Management InteractiveServer registry repair

## Confirmed Visual Studio failure

- The supplied 5.5.1 build passed localization (2,388 UI strings) and failed in `build/Assert-InteractiveServerRenderModes.ps1` because the new routed `Components/Pages/DataManagement.razor` was absent from its explicit approved render-mode registry.
- The page already had an InteractiveServer directive. The failure was caused by the missing registry entry, not by missing DI registrations, unavailable storage, changed paths, DevExpress replacement or the user's environment.

## Repair and intended render ownership

- Data Management now explicitly declares `@rendermode @(new InteractiveServerRenderMode(prerender: false))`, consistent with its service-heavy interactive data/editor behavior. No duplicate static-SSR component instance attempts to load the same service-owned data before the interactive circuit attaches.
- The repository's existing PowerShell render-mode guard now registers this exact page with this exact directive, checks the first nonempty source line, and verifies that it is one **explicitly reviewed non-prerendered routed page**. Every other routed page continues to require its original `@rendermode InteractiveServer` mode, and the static error fallback remains static. The guard still fails on unregistered routes or mode drift; there is no general skip or permission downgrade.
- Added `build/audit_interactive_server_registry_5_5_2.py`, a source-only parity test covering every routed page and the guard's exact reviewed exception. This catches the missing-entry defect before a Windows build.

## Version and retained functionality

- Repaired the shared DevExpress `InvariantDoubleEditor` numeric mask: a fixed-width integer mask could prevent entering a reviewed 24 GiB VRAM value in both the configured-host form and setup-guide hardware rows. The invariant-culture numeric mask now permits multi-digit integral capacities while retaining the specified decimal precision and the existing minimum/maximum values.
- Repaired CanIRun.ai result visibility in `/install`: successful opt-in lookup rows and their source-attributed model scores survive a switch away from and back to the Setup Guide tab for the lifetime of the Install route. The provider-model section now exposes a paged, searchable DevExpress grid of model/score/grade/status/host evidence, even before a local provider has been selected; existing resolve/install actions and recommendation cards remain available.
- Hardened CanIRun.ai score extraction for numeric and invariant-culture fractional score values; the compatibility response now prefers the most informative parsed/fallback scored record instead of allowing a scoreless fallback to mask a scored compatibility record. No scores are guessed or manufactured when the source omits them. Added `build/audit_install_hardware_scores_5_5_2.py` to guard these contracts.
- LocalGPT advances **5.5.1 → 5.5.2**; one-digit version slots are preserved. Cache-busting resource versions and retained release-specific audits match 5.5.2.
- Keeps all 5.5.1 DevExpress controls and retention counts, the new Data Management data-domain/service UI, relations/record editor, explicit model-health releases, one-run Council decision consumption, visible completed graphical lanes, six UI languages, the per-file learning checklist and the mandatory PDF merge pipeline.
- PublisherStudio was not changed. This release therefore contains only a new LocalGPT source ZIP.

## Verification limit

- Source-only route-registry, Install hardware/score, data-management, localization/checklist, DevExpress, async, service-resilience, architecture and PDF-merge contracts were checked without .NET or GitHub. Windows PowerShell, Razor compilation, live CanIRun.ai calls and an actual Visual Studio build must be exercised by the maintainer; no successful .NET build or live score lookup is asserted.
