# LocalGPT 4.0.7

LocalGPT 4.0.7 completes the Windows setup compile repair from 4.0.6. The installer setup logger now explicitly imports `System.IO` as well as `System`, matching the installer project's no-implicit-usings compilation surface.

The early installer restore/build preflight introduced in 4.0.6 remains in place, and application runtime, Council/provider behavior, `server.json` rendezvous semantics, Ollama handling, macOS ownership/signing/notarization, and packaging behavior are otherwise unchanged.

See `CHANGELOG-v4.0.7-INSTALLER-COMPILE-SURFACE-REPAIR.md` and `VALIDATION-v4.0.7-source.md`.
