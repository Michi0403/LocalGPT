# LocalGPT 4.0.4 — macOS PKG readability repair

LocalGPT 4.0.4 hardens the macOS installer package lane after the 4.0.2/4.0.3 update-lifecycle work. The lifecycle ownership rules and `server.json` rendezvous behavior remain unchanged.

The macOS package is now produced as a `productbuild` distribution package around a validated `pkgbuild` component package. The component payload is checked before wrapping, the final distribution package is expanded again with `pkgutil --expand-full`, signed packages are checked with `pkgutil --check-signature`, and macOS `installer -showChoicesXML` must be able to read the finished package before the artifact is retained. An unreadable package is deleted instead of being handed off.

DMG/TAR.GZ behavior, runtime identity stamps, stale-process ownership checks, alternate-host rendezvous preservation, Ollama update/download behavior, Council behavior and routed InteractiveServer ownership are preserved.
