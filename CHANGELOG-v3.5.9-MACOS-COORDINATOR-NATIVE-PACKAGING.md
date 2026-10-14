# LocalGPT 3.5.9 — macOS coordinator and native packaging repair

- Fixed two additional launcher defects found during final validation: endpoint discovery no longer runs inside a pipeline subshell (which discarded successful returns on macOS `/bin/sh`), and the launcher no longer passes the unsupported bare `--no-browser` argument into ASP.NET configuration.
- Fixed the packaged macOS launcher to read `BaseUrl` from LocalGPT runtime `server.json`; legacy `Url` is accepted for compatibility.
- Added stale-process endpoint cleanup, persistent launcher logging, native startup failure reporting, and correct application working-directory startup.
- Added native `.icns` generation from LocalGPT artwork, `Info.plist` icon/application metadata, ad-hoc signing, and Windows executable/installer icons.
- Reworked DMG finishing through a writable image with a branded drag-to-Applications Finder layout.
- Added native macOS PKG output through built-in `pkgbuild` when available.
- macOS `-Runtime all` now coordinates macOS x64/ARM64, Linux x64/ARM64, and Windows x64/x86/ARM64 application/setup builds. The optional WinUI/WebView wrapper remains Windows-host-native.
- Kept Linux TAR.GZ/DEB managed packaging, Homebrew-aware RPM support, Linux/WSL AppImage finishing, optional WSL2 delegation on Windows, and native Linux compatibility.
- Kept LocalGPT.ReleasePackaging at 1.0.1 because its C# package-writer implementation did not change in this patch.
