# LocalGPT 2.0.0 remaining and externally verifiable work

## Requires maintainer compilation/runtime evidence

- Run full Debug and Release solution builds with the licensed DevExpress feed and the exact .NET 10 SDK.
- Exercise database migration/seed upgrade against copies of historic user databases and verify every user-edited row remains intact.
- Run LocalGPT repeatedly through desktop wrapper, installer, browser refresh, process restart and port-reuse scenarios.
- Pair with PublisherStudio 2.0.0 and test every confirmation/input path in both directions.

## Deliberately not represented as complete

- Real protocol signing, encryption, key rotation, peer revocation and authenticated discovery.
- UART, SPI and MQTT transport adapters. The shared interfaces and DTO fields are prepared, but adapters are not implemented.
- Resuming an individual in-flight model inference after a complete process/OS crash. Saved run checkpoints and transcripts remain rejoinable; the interrupted model call is marked honestly.
- Native PDB debugger semantics, symbol-server acquisition or source reconstruction. Current inspection is bounded metadata/document/checksum awareness and never executes an uploaded binary.
- Automatic adapters for public methods whose parameters require runtime-only objects that cannot be represented safely as JSON. Such methods remain discoverable until a typed adapter is supplied.
- Chunked transfer and durable storage for multi-gigabyte media payloads across 1-Wire. Current organic media exchange uses bounded references/results rather than copying arbitrary binaries into one envelope.
- Complete migration of all historic UI literals/themes/accessibility behavior.

## Maintainer debug sequence

1. Restore and build `LocalGPTWebviewWrapper.sln`.
2. Confirm `LocalGPT.WireProtocolVersion.2.0.0.nupkg` is emitted under `artifacts/release/protocol`.
3. Start LocalGPT twice in sequence and verify the runtime endpoint file is owned/cleaned by the correct process.
4. Open `/dx-functions`, synchronize the runtime catalog and choose which methods are exposed to AI Chat and PublisherStudio.
5. Open `/council-teams`, verify the readiness/introduction step and edit a copy.
6. Run `localgpt.time_state.now` from Chat/Council and verify three bounded log/run rows.
7. Pair PublisherStudio and approve the link in the global Human Collaboration Inbox.
8. Trigger a PublisherStudio Story Editor request and a screenshot request; verify receiving-frontend and browser confirmations occur before execution.
