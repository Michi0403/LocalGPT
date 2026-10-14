# LocalGPT 3.5.5 - macOS native bundle permission repair

- Preserves the 3.5.4 host-aware release matrix: normal macOS `Build-Release` runs select only `osx-x64` and `osx-arm64`; RPM and AppImage remain Linux-only and cannot fail a normal macOS release.
- Repairs executable file modes inside generated macOS `.app`/DMG payloads. The generated `Contents/MacOS/LocalGPT` launcher, the published LocalGPT apphost, and `install-dependencies.sh` are now explicitly marked executable before TAR.GZ/DMG materialization.
- Writes `Info.plist` with the shared UTF-8-no-BOM helper rather than host-dependent `Set-Content` encoding behavior.
- Hardens the same shared native packaging script for Linux by explicitly preserving executable staging for the apphost and AppImage `AppRun`. Missing optional RPM/AppImage tooling still produces warnings rather than release failure.
- Docker/Podman remains opt-in only. No container runtime is required for ordinary Windows, macOS, TAR.GZ, or DEB release work.
- No LocalGPT application behavior, persisted configuration, InteractiveServer boundary, or LocalGPT.ReleasePackaging 1.0.1 archive-writer behavior changed.
