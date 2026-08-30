# LocalGPT 3.5.3 - Windows native packaging file-handle repair

- Fixed the LocalGPT-owned `LocalGPT.ReleasePackaging` TAR.GZ writer so its `TarWriter`, `GZipStream`, and `FileStream` are disposed before the temporary archive is moved to its final path. On Windows the previous `using var` lifetime kept the source file open with `FileShare.None`, causing the deterministic `System.IO.IOException` seen immediately after a successful Linux publish.
- Fixed the same file-lifetime defect in DEB materialization before it could become the next failure after TAR.GZ creation.
- Added a bounded commit retry for brief destination-file sharing by antivirus/indexing software after all LocalGPT-owned handles are closed.
- Bumped the shared `LocalGPT.ReleasePackaging` tool package from 1.0.0 to 1.0.1 so PublisherStudio cannot silently reuse the known-broken package from its cache.
- Preserved the existing release matrix: Windows x64/x86/ARM64 setup console + portable ZIPs, Linux x64/ARM64 Full/Light payloads with TAR.GZ/DEB/RPM/AppImage packaging, and macOS x64/ARM64 Full/Light application bundles with TAR.GZ and native DMG completion on macOS.
- No application runtime behavior or reviewed InteractiveServer boundary was changed by this packaging repair.
