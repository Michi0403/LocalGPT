# LocalGPT 4.0.5 — durable logging recovery

LocalGPT 4.0.5 restores the application/setup logging contract while preserving the macOS PKG readability repair while preserving the working 4.0.4 installer, provider, Council, runtime-identity, and cross-platform behavior.

- The default application log resolves to `%LOCALAPPDATA%\LocalGPT\LocalGPT.log` on Windows and `~/Library/Application Support/LocalGPT/LocalGPT.log` on macOS rather than depending on the executable/current directory or a hidden `logs` subdirectory.
- Startup appends a bootstrap diagnostic directly to the durable log before the configured logger pipeline is required. Fatal startup/host exceptions are appended in full and mirrored to stderr.
- Setup writes a separate durable `%LOCALAPPDATA%\LocalGPT\LocalGPT.Setup.log` transcript (with the platform-equivalent LocalGPT user-data directory on non-Windows systems) and persists full exceptions.
- The macOS launcher keeps its separate launcher diagnostic log while routing the application file logger to the durable LocalGPT user-data root.
- 4.0.4 macOS PKG readability checks, runtime/source identity, `server.json` rendezvous behavior, Ollama update/install behavior, setup recovery, Council self-clean behavior, provider candidate reuse, and render ownership remain unchanged.
