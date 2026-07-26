# LocalGPT v0.1.4 — Missing Features after Organic Learning Hotfix 3

These items are intentionally recorded rather than represented as completed.

## Build validation still required on the target machine

- Native `dotnet build` / Visual Studio compilation was not available in the packaging environment. The source-contract tests pass, but Michael's Windows/DevExpress build remains the compiler authority.
- WinUI/WebView2 startup, installer/update/uninstall and DevExpress runtime assets must be exercised on the installed application.

## Council persistence and live joining

- The new Chat selector joins Council runs active in the current LocalGPT process. Full restart recovery and replay of a partially streamed Council heartbeat are not implemented yet.
- Persisting every streamed token/event for exact reconnect replay remains future work; completed conversations still use the existing chat-memory persistence.
- Multi-host distributed Council scheduling and remote worker recovery are not implemented. Current hardware-road scheduling is local-machine orchestration.

## Models and media

- LocalGPT now accepts arbitrary local attachments, but decoding an unknown file or media format still depends on the selected open-source model/provider or an advertised organic plugin capability.
- Provider-native maximum context/output values remain model-specific. LocalGPT avoids low application limits but cannot make a model accept more tokens than its runtime supports.
- Very large files are not chunk-streamed over one-wire yet; one-wire message size remains intentionally bounded and media should use file/controller exchange capabilities.

## One-wire production hardening

- Cryptographic signatures, encrypted payload key management and authenticated discovery are extension fields/contracts, not a finished production trust system.
- UART, SPI and MQTT transport adapters remain unimplemented; TCP/UDP is the working prototype transport.
- Direct text proposals auto-select only when exactly one compatible PublisherStudio peer is connected. A multi-peer picker/routing policy is still needed.

## Learning and legacy migration

- Seeded legacy regexes and the new database-backed functions are wired. A full compiler-guided conversion of every legacy static helper into smaller domain/application/infrastructure services is not complete.
- Model-suggested facts remain untrusted by design. Automatic promotion to verified knowledge is not planned without user/source verification.
- Automatic empirical ranking of every model's DX/controller/organic strengths across all hardware roads requires real benchmark runs and remains open.

## UI and accessibility

- The new Chat controls use Bootstrap/theme variables, but an exhaustive audit of every older native button, label, DevExpress internal element and accessibility state across every installed theme remains open.
- The approval work bar exists; richer filtering/grouping and a generalized visual organic-skill binding editor remain future work.
