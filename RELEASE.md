# LocalGPT 3.5.3

LocalGPT 3.5.3 is the **Windows-hosted native packaging file-handle repair** maintenance release.

The supplied Windows release log proves the LocalGPT application, documentation, all three Windows application/setup RIDs, and the linux-x64 Full application payload build successfully. The failure occurs only when the LocalGPT-owned release-packaging helper tries to move its completed temporary TAR.GZ while its own `FileStream`/compression writer chain is still open.

This release closes the TAR.GZ writer chain before the final move and applies the same correction to DEB creation. The shared helper package is versioned as `LocalGPT.ReleasePackaging` 1.0.1 so PublisherStudio cannot silently reuse the broken 1.0.0 package.

The intended package matrix remains unchanged: Windows uses the one-click setup console and portable ZIPs; Linux uses Full/Light application payloads with TAR.GZ, DEB, RPM, and AppImage outputs; macOS uses Full/Light `.app`/TAR.GZ outputs and DMG completion on a macOS host.

See `CHANGELOG-v3.5.3-WINDOWS-NATIVE-PACKAGING-FILE-HANDLE-REPAIR.md` and `VALIDATION-v3.5.3-source.md`.
