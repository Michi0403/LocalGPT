# LocalGPT 2.4.5 test candidate — network endpoint and multi-host AI providers

Additive test changes only.

- Keeps the historical `http://127.0.0.1:<port>` Kestrel listener unchanged for desktop/installer compatibility.
- Adds an optional second Kestrel listener under `LocalGPT:RemoteEndpoint` for LAN/VPN browser access.
- The secondary listener supports a concrete bind IP, `0.0.0.0`, or `::`; a PFX path switches it to HTTPS.
- Supports CLI/env overrides: `--network-enabled`, `--network-address`, `--network-port`, `--network-certificate`, `--network-certificate-password` and `LOCALGPT_NETWORK_*`.
- Adds `AICore:ChatGPTLocalCores` so multiple LM Studio/vLLM/OpenAI-compatible endpoints can coexist with multiple existing `OllamaCores`.
- Provider-qualified runtime now accepts configured remote OpenAI-compatible hosts instead of rejecting every non-loopback host.
- Credentials remain endpoint-owned and are never borrowed across hosts.
- Existing primary provider fields remain valid for backwards compatibility.

The optional network listener does not add authentication. Expose it only behind intended firewall/VPN policy; use HTTPS when browser traffic crosses an untrusted network.
