# LocalGPT v0.1.5 — Remaining Work

These are the deliberately unclaimed items after the runtime-completion pass.

## Target-machine verification

- Run the native Visual Studio/.NET 10 build, WinUI/WebView2 launch, installer/update/uninstall and an upgrade against a backed-up real user database.
- Exercise repeated launch/close/relaunch and an intentional port collision on Windows to confirm the wrapper's verified reconnect/error paths.

## Provider and very-large-media realities

- Application-level limits are maximized, but a selected Ollama/LM Studio model can still impose its own context, image, audio, video or tokenizer limits.
- Multi-gigabyte payloads currently rely on local file/controller exchange or a capable organic plugin. A chunked, resumable, low-memory one-wire media stream is still preferable to one enormous in-memory envelope.

## Distributed runtime recovery

- Joining active Council runs works inside the current LocalGPT process. Exact token/event replay after a process crash or machine restart is not complete.
- Multi-host Council workers, remote hardware-lane leases and distributed restart recovery remain future work.

## Protocol production security and transports

- Signature/encryption fields exist, but authenticated discovery, key enrollment/rotation and a complete encrypted trust lifecycle remain open.
- UART, SPI and MQTT transport adapters remain extension points; TCP/UDP is the implemented prototype transport.
- Explicit routing UI is still required when multiple compatible PublisherStudio peers are connected simultaneously.

## Continued architecture cleanup

- The supplied legacy static behavior has been preserved and webbed through database/services/controllers/DX functions. Further compiler-guided splitting of the largest legacy services into smaller domain/application/infrastructure namespaces can continue without changing behavior.
- Empirical automatic ranking of every model's best DX/controller/organic skills on every CPU/GPU lane requires real benchmark runs and persisted evidence.
- A complete accessibility/theme audit of every older DevExpress/internal control remains broader than this repair.
