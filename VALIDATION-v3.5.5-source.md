# LocalGPT 3.5.5 source validation

Static validation for this source handoff covers the host-aware release selection, macOS executable-mode preparation, UTF-8 `Info.plist`, optional Linux RPM/AppImage behavior, the prior 1.0.1 TAR/DEB handle-lifetime repair, architecture/async/service-policy audits, XML documentation, structured-file parsing, archive safety, and exact extracted-ZIP equality.

This environment does not contain the .NET SDK or PowerShell, so it does not claim a local `dotnet build`, PowerShell execution, DMG creation, or installer execution. The supplied macOS transcript is runtime evidence: the older 3.5.3 invocation fails at RPM, while the 3.5.4 invocation identifies `macOS` and selects only `osx-x64, osx-arm64` before continuing through the real .NET/documentation build.
