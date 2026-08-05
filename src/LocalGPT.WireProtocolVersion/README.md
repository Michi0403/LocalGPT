# LocalGPT.WireProtocolVersion

This project is the versioned, transport-neutral contract assembly for LocalGPT and organic plugin systems.

- The authoritative source lives in the LocalGPT repository.
- Consumer repositories reference the published DLL-backed NuGet package. They must not carry a second Git-revisioned source mirror of this contract project.
- The public DTOs and interfaces are bidirectional: “target system” means whichever peer receives the current envelope.
- TCP/UDP are the current adapters. `IOneWireTransportAdapter` keeps the contract reusable for later UART, SPI and MQTT adapters.
- Protocol changes require a version change and synchronized contract-source validation on every consumer.

Do not move application services, controllers, database entities or UI types into this assembly. It contains only stable wire DTOs, enums, interfaces and compatibility helpers.
