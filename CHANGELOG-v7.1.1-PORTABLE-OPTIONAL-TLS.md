# LocalGPT 7.1.1 — portable optional TLS startup

Base: LocalGPT 7.1.0 (complete user-supplied build source).

- macOS launcher `PlatformNotSupportedException` at `Program.Hosting.cs` 313: remove unsupported `EphemeralKeySet` for optional remote PFX and MCP PFX preflight. Use `.NET 10` `DefaultKeySet` (platform-default temporary-key behavior); no persistent-key or exportable-key flags introduced.
- Invalid/missing remote TLS certificate disables the optional remote listener **without downgrading to HTTP**; the main localhost app remains the authoritative endpoint.
- Invalid/missing dedicated MCP TLS certificate disables the optional MCP gateway for that run, without downgrading configured TLS to HTTP. Main UI and 1-Wire/MFA services remain independently configured.
- Invalid optional remote bind configuration does not throw into primary Kestrel startup. Existing unencrypted HTTP-only explicitly configured endpoints retain their behavior.
- Tightened the existing mandatory `Assert-NetworkCertificateContract.ps1` to detect these portable/fail-closed requirements; no new installer guards, packaging changes or boot scripts.

## Scope and limitations

This change fixes the **logged cross-platform PFX preflight exception** and known unsafe fallback paths; it does **not** prove end-to-end MFA or 1-Wire interoperability and does not guarantee resilience to a later Kestrel socket bind race or conflict when a valid optional remote endpoint starts on the main host. Real Windows/macOS/Linux startup and LAN TLS tests are required.

Installer, Linux/macOS boot scripts, application data path, binary packaging, setup and PublisherStudio are unchanged.
