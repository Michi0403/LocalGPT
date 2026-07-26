# LocalGPT 2.0.1 remaining runtime verification

## Next maintainer checks

1. Rebuild `LocalGPTWebviewWrapper.sln` and confirm the first LocalGPT project error, if any, rather than downstream missing-DLL messages.
2. Confirm the protocol package builds without the former readme warning and is emitted under `artifacts/release/protocol`.
3. Open Chat and Model Council and verify the per-model hardware-road editor renders without `RZ10012`.
4. Invoke `localgpt.time_state.now`; verify physical hardware inventory, three recent logs, three recent Council runs and linked peers are returned.
5. Start twice, refresh/close the browser, and rejoin the running Council from the spooler panel.
6. Pair with PublisherStudio 2.0.1 and test Story Editor proposals plus screenshot confirmation on both frontends and in the browser.

## Deliberately still open

- Native build/runtime acceptance across the licensed DevExpress and supported OS matrix.
- Real signing, encryption, authenticated discovery, peer revocation and key lifecycle.
- UART, SPI and MQTT transport adapters.
- Process/OS-crash continuation inside an already-running model inference call.
- Native PDB debugger/symbol-server behavior beyond bounded portable-PDB metadata inspection.
- Chunked multi-gigabyte media transfer over 1-Wire.
- Repository-wide migration of every historic literal/theme/accessibility issue.
