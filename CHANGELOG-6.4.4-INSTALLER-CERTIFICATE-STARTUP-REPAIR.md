# LocalGPT 6.4.4 — installer, TLS certificate and startup recovery

## Confirmed defects
- Release assets use version-bearing ZIP root folders. Earlier installers extracted them directly under LOCALAPPDATA/LocalGPT and then used recursive shortcut discovery. A successful download of version 6.4.1 therefore did not prove the installed application version was current.
- The certificate generator contained the wrong Server Authentication EKU OID (`1.3.6.2.5.5.7.3.1`). Kestrel rejects it (correct OID `1.3.6.1.5.5.7.3.1`).
- Deleting or moving the configured optional network PFX caused a FileNotFoundException that aborted the primary loopback application at startup.

## Changes
- Includes the 6.4.3 source-level canonical Windows deployment repair (ZIP wrapper validation, stage-and-unpack into stable `winx64` and `setupwinx64` directories, canonical shortcut targets, no recursive false success). Source script/test guards included.
- Generates a real TLS Server Authentication EKU. Verifies private key and EKU before replacing existing PFX. Existing RSA and hash algorithm choices remain.
- Preflights configured optional web and MCP certificates for existence, loadability, private key, validity window and Server Authentication EKU.
- When optional network certificate is missing or unusable, explicitly configured optional listeners can continue over HTTP with a warning instead of preventing primary LocalGPT from starting; an existing certificate is never auto-deleted.
- Does not modify the authoritative loopback listener, trust stores, database, MFA secrets, local Council config or network-enabled flag.
- Adds `build/Assert-NetworkCertificateContract.ps1` and mandatory build/release invocation. Invalid OID is a hard maintenance failure; no pre-existing guard weakened.
- Version: LocalGPT application 6.4.4, setup console 4.9.8.

## Limitations
No Windows/.NET build or running Kestrel test was possible. Source checks cannot guarantee every configuration on every installed machine. With an HTTP fallback, remote traffic is *not encrypted*; keep the optional network listener disabled or firewall/VPN-isolated until a valid HTTPS certificate is configured.

This change does **not** represent full resolution of the separate AI Council, Keyaccount, popup poll, live transcript or 1-Wire reciprocal pairing feature requests.
