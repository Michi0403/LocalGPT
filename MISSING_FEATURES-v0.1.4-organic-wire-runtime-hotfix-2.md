# Missing features after LocalGPT v0.1.4 organic-wire runtime hotfix 2

These items are deliberately left open. They are not required for the repaired source package to preserve the existing bootstrap and current TCP/UDP organic-plugin prototype.

## Security and transports

- Actual message signing, peer-key management and encrypted payload implementation. The protocol fields, integrity hash and error check exist, but no false claim of authenticated encryption is made.
- UART, SPI and MQTT transport adapters. `IOneWireTransportAdapter` is the stable boundary; this package implements the current TCP/UDP path only.
- Cryptographically authenticated LAN discovery/pairing and certificate rotation.
- Chunked/blob transport for screenshots or media larger than the bounded inline message allowance.

## Council intelligence and scheduling

- Automatic empirical benchmarking that continuously adjusts a model’s self-reported DX-function/controller/organic-skill proficiency from accepted Council outcomes. Current proficiency and evidence are maintainable, but automatic calibration remains bounded future work.
- Advanced graphical Council workflow designer. The current database/UI supports editable roles, phases and prompt/workflow JSON without removing built-in knowledge; drag/drop UML authoring is not included.
- Cross-process distributed scheduling and recovery after machine restart for work that is actively executing on several hosts. Current roads are process-local and persist configuration, approvals and relevant work state.

## UI and accessibility

- Complete audit of every legacy page-specific hard-coded color. The shared theme contract and reported controls are repaired, but a full visual pass through all historical pages/themes still needs the owner’s browser/DevExpress environment.
- Optional per-column database-grid visibility/layout profiles. Long content now wraps; a full selectable column-profile editor is not included.
- Full WCAG keyboard/screen-reader validation across every legacy control and DevExpress theme.

## Architecture maintenance

- Further bounded subnamespacing/splitting of very large legacy services such as the Council runtime/catalog services. This should be done domain by domain with compiler tests; a risky all-at-once rewrite was intentionally avoided.
- Automated cross-repository publishing of the canonical WireProtocolVersion NuGet/DLL. The source mirror is synchronized in the delivered ZIPs, but package-feed release automation remains open.

## Owner-environment validation

- Native Debug/Release compilation with .NET 10, licensed DevExpress packages, Windows App SDK/WinUI wrapper and installer.
- Browser permission acceptance tests for recurring screen capture, input execution and real multi-GPU model processes.
