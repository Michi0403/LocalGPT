# LocalGPT v0.1.8 — remaining work

The following items are intentionally documented rather than represented as finished:

- Native Windows `.NET 10`, WinUI/WebView2, and DevExpress compilation/runtime validation must be performed on the user's development machine. The package contains source-contract and archive tests, not a replacement for that toolchain.
- Real device identity, certificate/key management, message signing, and payload encryption are not implemented. Current `Signature` and `EncryptedPayload` fields remain protocol extension points.
- UART, SPI, and MQTT are represented by transport-oriented protocol interfaces and architecture documentation; working adapters are not included in this release.
- A complete process or OS crash cannot resume the exact native inference call that was executing inside the terminated process. The Council checkpoint is retained and the interrupted call must be marked/retried.
- Some reflected public methods accept runtime-only types, streams, delegates, framework contexts, or values that cannot be meaningfully represented as JSON. They can remain discoverable for architecture knowledge, but need an explicit typed adapter before practical invocation.
- The user-controlled catalog does not attempt to infer semantic safety from method names. The receiving frontend policy, exact typed binding, and user confirmation/editor path are the authority. Future releases can add richer generated parameter forms and method-specific help without restricting the user's control.
- Cryptographic peer revocation persistence and certificate-backed mutual authentication remain future work. This version provides explicit two-frontend link approval, per-peer exposure, per-call receiving policy, and transport disconnection.
- Debug metadata reading supports bounded managed assembly/portable PDB inspection. Native PDB semantic/source reconstruction and arbitrary debugger execution are not included.
