# LocalGPT 2.0.1 — Organic 1-Wire 2.1 compiler hotfix

This source package continues the Organic 1-Wire 2.1, Council, runtime security, MFA, OCR, HTTP/JSON and responsive UI work and fixes the compiler failures reported on 2026-07-27.

## Fixed

- Added project-wide aliases for the 1-Wire runtime security contracts, including `OneWireRuntimeSecurityStatus`, `OneWireSecurityDescriptor`, `OneWirePairingTicket`, `OneWireTrustEstablishmentRequest`, and `OneWireTrustedPeerDescriptor`.
- Disambiguated the application `LocalGPT.BusinessObjects.ConfigurationRoot` in `LocalVisionOcrService` from `Microsoft.Extensions.Configuration.ConfigurationRoot`.
- Kept the deterministic protocol → LocalGPT → installer → WinUI-wrapper build order.
- `Build-Release.cmd` now sets the repository working directory, preserves the exit code, and pauses on failure instead of closing immediately.
- Updated the stale protocol 2.0 source test to the authoritative 2.1 contract and added regression checks for the exact compiler failures.

## First build

1. Close Visual Studio.
2. Replace the old source directory cleanly; do not overlay it.
3. Delete `.vs`, `bin`, and `obj` from the checkout if they remain outside the replaced directory.
4. Run `Build-LocalDevelopment.cmd`.
5. After a successful development build, run `Build-Release.cmd`.

No runtime `onewire-secret.json`, database, build output, protocol package, or private key is included. Runtime identity remains user-generated and resettable from the frontend.
