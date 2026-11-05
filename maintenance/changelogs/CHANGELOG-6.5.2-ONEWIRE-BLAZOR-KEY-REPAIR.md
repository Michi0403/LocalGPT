# LocalGPT 6.5.2 — 1-Wire Blazor circuit repair

- Corrected the trusted-peers view: connected peer rows and encrypted-trust rows are separate render sibling groups. Both groups can legitimately contain the same `PeerId` and must have **role-qualified Razor keys**. Grouping within one list is insufficient. No peer is removed from either logical view.
- Extended existing **build-blocking** `Assert-LocalAiDownloadAndPeerUI.ps1` to reject shared sibling keys. Existing guards, trust validation, DI registrations and runtime policy are unchanged.
- Regression contract tests assert both distinct key namespaces. Updated active project version to **6.5.2** (single-digit minor/patch fields).

## Limits
Full Windows Razor/.NET build, PowerShell execution and physical 1-Wire end-to-end session are not possible in the packaging environment; they have **not** been claimed as passing. This is a narrowly scoped correction to the crash in `/onewire-security`, not a claim that trust has already been re-established on the user's machine.
