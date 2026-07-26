# LocalGPT.WireProtocolVersion

This project is the versioned, transport-neutral contract assembly for LocalGPT and organic plugin systems.

- The authoritative source lives in the LocalGPT repository.
- Consumer repositories may carry a synchronized source mirror so their solution builds offline and does not depend on a second checkout.
- The public DTOs and interfaces are bidirectional: “target system” means whichever peer receives the current envelope.
- TCP/UDP are the current adapters. `IOneWireTransportAdapter` keeps the contract reusable for later UART, SPI and MQTT adapters.
- Protocol changes require a version change and synchronized contract-source validation on every consumer.

Do not move application services, controllers, database entities or UI types into this assembly. It contains only stable wire DTOs, enums, interfaces and compatibility helpers.
