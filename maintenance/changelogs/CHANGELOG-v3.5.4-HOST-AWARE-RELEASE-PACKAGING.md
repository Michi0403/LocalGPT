# LocalGPT 3.5.4 - host-aware release packaging

- Changed the default `Build-Release` runtime selection to follow the build host instead of forcing every maintained OS from every machine:
  - Windows builds the three Windows application/setup RIDs (`win-x64`, `win-x86`, `win-arm64`).
  - Linux builds the Linux application RIDs (`linux-x64`, `linux-arm64`) without a Windows setup console.
  - macOS builds the macOS application RIDs (`osx-x64`, `osx-arm64`) and performs the native DMG step when `hdiutil` is available.
- Added explicit `-Runtime all-rids` for deliberate cross-host publish attempts. Native installer/package steps remain host-bound even when .NET cross-publishing is requested.
- RPM and AppImage are now optional Linux-native finishing formats. Missing `rpmbuild`/`appimagetool` no longer fails a release; Docker/Podman is not required and is used only when `-UseContainerPackaging` is explicitly requested.
- Cross-architecture Linux releases still receive TAR.GZ and DEB output from the LocalGPT-owned .NET packaging helper; native RPM/AppImage finishing is limited to the current Linux host architecture.
- Windows-only LocalGPT release builds still create and cache `LocalGPT.ReleasePackaging` 1.0.1 for PublisherStudio, but no longer install the Unix packaging tool when it is not needed.
- Added UTF-8 console initialization for Windows PowerShell release/development entry points and code-page initialization in the `.cmd` launchers so UTF-8 `dotnet` output is not rendered as mojibake.
- Preserved the 3.5.3 TAR.GZ/DEB file-handle repair and all reviewed application/InteractiveServer runtime behavior.
