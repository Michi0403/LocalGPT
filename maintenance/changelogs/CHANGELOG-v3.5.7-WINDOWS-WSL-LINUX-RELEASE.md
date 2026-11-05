# LocalGPT 3.5.7 — Windows/WSL Linux release

- Added optional `WslLinux=Auto|Off|Require` release coordination on Windows. Ready WSL distros add Linux x64/ARM64 artifacts; missing WSL does not break the normal Windows release.
- Added `Setup-WslLinuxBuild.ps1/.cmd`, reusable WSL discovery/license helpers, explicit Ubuntu/Debian provisioning, and a headless Linux release child.
- The child mirrors source into the WSL Linux filesystem, reuses the Windows parent's documentation, imports Linux artifacts, and can terminate WSL after use.
- DevExpress build licensing can be bridged from the normal Windows license location/environment without committing or publishing the private key.
- Linux Full/Light TAR.GZ and DEB remain mandatory. RPM/AppImage are optional by default; provisioned WSL can create both without Docker.
- `appimagetool` now receives the target `ARCH` so one Linux/WSL host can finish x64 and ARM64 AppImages from the corresponding cross-published AppDirs. WSL extract-and-run mode avoids a FUSE requirement.
- Native Linux and macOS release paths remain available. Apple-native DMG/signing/notarization is still a macOS responsibility.
- WSL readiness now explicitly requires WSL2, and the DevExpress environment bridge uses the correct Windows-to-WSL direction with path translation.
- Delegated LocalGPT children reuse parent documentation and skip documentation-only Node.js provisioning.
