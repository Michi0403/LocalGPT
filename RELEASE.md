# LocalGPT 4.0.4

LocalGPT 4.0.4 fixes the macOS PKG handoff so a package that macOS Installer cannot read is no longer shipped. The PKG is built as a `productbuild` distribution package around the validated component payload and is re-opened by both `pkgutil` and `installer -showChoicesXML` before release handoff.

The 4.0.2/4.0.3 runtime identity, updater lifecycle, alternate-host `server.json` rendezvous handling, Ollama fast download/update path, console progress, setup recovery and render ownership remain intact.

See `CHANGELOG-v4.0.4-MACOS-PKG-READABILITY-REPAIR.md` and `VALIDATION-v4.0.4-source.md`.
