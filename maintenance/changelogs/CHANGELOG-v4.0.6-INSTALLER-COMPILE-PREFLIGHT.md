# LocalGPT 4.0.6 — installer compile and release preflight repair

LocalGPT 4.0.6 is a focused installer/release reliability correction.

- Restored the missing `System` namespace import in `SetupFileLoggerProvider`, fixing the Windows setup compilation errors for `Exception`, `Func<,,>` and `IDisposable` observed after the 4.0.5 macOS/Linux release work had already completed.
- `Build-Release.ps1` now restores and compiles `LocalGPTInstallerConsole` before generating the multi-hour documentation PDF or entering native package signing/notarization. Installer-only compiler failures therefore fail early.
- Existing application runtime behavior, Council/provider behavior, `server.json` rendezvous semantics, Ollama setup/update behavior, macOS app ownership, distribution PKG readback, signing, notarization, and packaging contracts remain unchanged.
