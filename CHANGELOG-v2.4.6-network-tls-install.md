# LocalGPT 2.4.6 test candidate — install-page network/TLS configuration

- Preserves the authoritative loopback listener.
- Keeps the optional second Kestrel listener introduced in 2.4.5.
- Persists `LocalGPT:RemoteEndpoint` to `%LOCALAPPDATA%\LocalGPT\appsettings.user.json` from `/install`.
- Adds an explicit self-signed PFX certificate creation panel using .NET `CertificateRequest`.
- Exposes RSA key size, SHA hash, validity, SANs, PFX output/password, `StoreLocation`, and `StoreName`.
- Can wire the generated PFX into the optional HTTPS listener.
- Listener changes require restart because Kestrel endpoint bindings are established during host startup.
