# LocalGPT 7.0.9 installer rebuild based on PublisherStudio 4.7.6

The LocalGPT-specific Ollama/model/learning-base code was retained. The deployment workflow follows PublisherStudio: exact platform release assets, explicit local ZIP overrides, validation before extraction, canonical AppData destination, Windows-only owned-runtime shutdown, installed setup self-detach on Windows, normal overlay update preserving user data, and installed version checks.

## Runtime archive contract

| RID | App ZIP | Root folder | Setup ZIP | Root folder |
|---|---|---|---|---|
| win-x64 | winx64.zip | winx64 | setupwinx64.zip | setupwinx64 |
| win-x86 | winx86.zip | winx86 | setupwinx86.zip | setupwinx86 |
| win-arm64 | winarm64.zip | winarm64 | setupwinarm64.zip | setupwinarm64 |
| linux-x64 | linuxx64.zip | linuxx64 | setuplinuxx64.zip | setuplinuxx64 |
| linux-arm64 | linuxarm64.zip | linuxarm64 | setuplinuxarm64.zip | setuplinuxarm64 |
| osx-x64 | macosx64.zip | macosx64 | setupmacosx64.zip | setupmacosx64 |
| osx-arm64 | macosarm64.zip | macosarm64 | setupmacosarm64.zip | setupmacosarm64 |

All seven RIDs use one canonical `%LOCALAPPDATA%/LocalGPT` or equivalent per-user LocalApplicationData root. The packaging keeps LocalGPT's existing Linux names (`linuxx64`, `linuxarm64`), not PublisherStudio's (`linx64`, `linarm64`), to avoid changing LocalGPT's non-Windows distribution contracts. Portable macOS/Linux app/setup ZIP pairs are emitted alongside the existing native DMG/PKG/TAR.GZ/DEB/RPM packages; the native packaging implementation and launchers are unchanged. WSL Linux artifact transport now includes the portable ZIP pairs.

## Versioning

Application remains **7.0.9** as requested; the independently versioned setup project increases from **5.0.0** to **5.0.1**. `RELEASE-VERSION.txt` identifies the LocalGPT release in both ZIPs and `SETUP-VERSION.txt` identifies the actual setup build. The release script inspects the setup managed assembly identity before stamping that marker.

## Explicit limitations

This uses PublisherStudio-style overlay extraction and does not add transactional rollback. A power loss or locked file during extraction can leave a mixed installation; errors are reported. Existing version-named directories are not automatically deleted. The Windows application shutdown is limited to the exact executable in the current installation. No repository upload or GitHub change was performed.

To build from this revised 7.0.9 source after building an earlier 7.0.9, use `pwsh ./Build-Release.ps1 -ForceRebuildArtifacts` to avoid reusing a cached same-version release bundle. Read the script's parameter list before running further options.

## Startup launchers

The PublisherStudio `Install.cmd`, `Update.cmd`, and `Start.cmd` workflow has been adapted to LocalGPT's `LocalGPTInstallerConsole.exe` and port 5000. Normal `Install.cmd` / `Update.cmd` do **not** include `--force-delete`. The previous `LocalGPT-Install-Start.cmd` was corrected to remove its unexpected `--force-delete` flag; the explicitly destructive `Install-Force-Delete-Start.cmd` remains a separate opt-in command.
