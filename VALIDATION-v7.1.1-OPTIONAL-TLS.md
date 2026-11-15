# LocalGPT 7.1.1 — macOS/Linux/Windows optional TLS verification

## Evidence

The user's November 12, 2026 macOS ARM64 launcher log shows LocalGPT 7.1.0 entering `Program.ResolveRemoteWebEndpoint`, then exiting with `PlatformNotSupportedException` because the X509CertificateLoader was given `EphemeralKeySet`. The launcher, architecture selection, and app executable were already functioning.

## Source changes

- Remote PFX preflight and MCP PFX preflight use `X509KeyStorageFlags.DefaultKeySet`. No `EphemeralKeySet` is used by the runtime sources.
- Invalid/missing **configured** HTTPS certificate: optional remote web listener is disabled (not converted to HTTP); local loopback remains configured.
- Invalid/missing **configured** dedicated MCP HTTPS certificate: MCP gateway disabled for this run. No TLS-to-HTTP downgrade.
- Invalid remote IP/port options are handled as optional configuration errors rather than thrown through primary host creation.
- No installer/bootstrap, OneWire/MFA protocol, PublisherStudio coupling, runtime packaging, cross-platform launcher, or data-path modifications.

## Real host acceptance matrix (not executed here)

1. **macOS arm64:** Use the 7.1.1 macOS package on MacBook Neo with the same configured PFX as 7.1.0. Launch from Finder. Confirm the app reports a runtime URL and loads `/install`, and the valid HTTPS endpoint answers on its selected port.
2. **macOS with invalid/missing PFX:** Confirm the main localhost URL boots while the configured HTTPS listener does not listen, and there is no plaintext fallback on that remote port. Check that logger reports optional endpoint disabled.
3. **Linux arm64/x64:** Repeat valid and invalid PFX checks. Validate 1-Wire connectivity with an authorized peer.
4. **Windows x64:** Repeat valid and invalid PFX checks, including updating the existing canonical installation. Confirm no impact on current installer or application version path.
5. **MCP:** Test valid HTTPS and invalid/missing PFX, plus a loopback-only MCP HTTP configuration with *no* certificate configured. Explicitly verify authentication and host-origin checks with PublisherStudio.
6. **MFA/pairing:** Test authorized pairing, rejected token, expired token and reconnect on each platform. These mechanisms were not changed, and no end-to-end success is claimed.

## Known limitation

The optional remote-web endpoint still shares the primary Kestrel host. This patch isolates **configuration and certificate validation** failures, but it does not guarantee the primary app survives a socket bind conflict that occurs later during Kestrel StartAsync. Dedicated MCP is already hosted separately. Avoid claiming complete listener fault isolation until the remote endpoint is isolated and tested as well.

## Verification performed here

- Compared every entry of the original 7.1.0 ZIP with the new ZIP; all unchanged file bytes are retained.
- Verified ZIP CRCs, version metadata, source contract markers, no native `EphemeralKeySet` in the LocalGPT runtime sources, and no accidental installer or platform launcher edits.
- `pwsh` and `dotnet` were unavailable here; no .NET compilation, native runtime test, or PowerShell execution.
