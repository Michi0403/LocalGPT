# Windows + WSL Linux releases

LocalGPT can use an initialized WSL2 Linux distribution as a headless Linux release backend while Windows remains the release coordinator. This is optional: developers without WSL keep the existing Windows build behavior. Native Linux developers continue to run `pwsh ./Build-Release.ps1` directly.

## One-time WSL setup

Install and initialize Ubuntu (or another supported Ubuntu/Debian **WSL2** distribution) using Windows' normal WSL setup. Complete the distro's first launch so it has a Linux user, then from the LocalGPT repository run:

```powershell
.\Setup-WslLinuxBuild.ps1 -Provision
```

The provisioning helper first verifies that the selected distro is WSL2, then installs PowerShell, the .NET 10 SDK, Python 3, `rpmbuild`, and `appimagetool` prerequisites inside that WSL distro. A legacy WSL1 distro is rejected with the exact `wsl.exe --set-version <name> 2` conversion command instead of being half-provisioned. It does not install Docker or Podman. Normal release builds never provision Linux packages unless `-ProvisionWslBuildTools` is explicitly supplied.

Use `-Distribution Ubuntu` (or set `WSL_BUILD_DISTRO`) when multiple distros are installed. `-SkipAppImageTool` leaves AppImage as an optional missing format.

## DevExpress license

DevExpress treats the build key as build-machine material. The normal locations are `%APPDATA%\DevExpress\DevExpress_License.txt` on Windows and `$HOME/.config/DevExpress/DevExpress_License.txt` on Linux. The case-sensitive `DevExpress_LicensePath` and `DevExpress_License` variables are also supported.

During a Windows-coordinated WSL release, LocalGPT automatically bridges a valid Windows license folder/key into the WSL child process through WSL environment interop. The bridge uses the Windows-to-WSL direction and path translation for `DevExpress_LicensePath`, so the normal Windows license folder becomes a valid Linux path inside the child. The private key is not copied into the source tree or output bundle. If a standalone Linux-side copy is preferred, register it once with:

```powershell
.\Setup-WslLinuxBuild.ps1 -CopyWindowsDevExpressLicense
# or
.\Setup-WslLinuxBuild.ps1 -DevExpressLicenseFile C:\secure\DevExpress_License.txt
```

The helper writes the persistent copy to `$HOME/.config/DevExpress/DevExpress_License.txt` with user-only permissions.

## Normal release behavior

```powershell
.\Build-Release.ps1
```

On Windows, `-WslLinux Auto` is the default. The build probes an existing initialized distro. If its core tools and DevExpress license are ready, Windows builds the native Windows outputs and WSL builds Linux x64/ARM64 Full and Light payloads. The WSL source is mirrored into its Linux filesystem rather than compiled under `/mnt/c`; resulting packages are copied back and included in the same Windows release bundle. Because the parent already generated the documentation, the delegated child skips documentation-only Node.js provisioning.

If WSL is missing or not ready, host-aware `-Runtime all` continues with Windows only. Explicit Linux RIDs preserve the older Windows cross-publish fallback. Use `-WslLinux Require` when a missing/unready WSL backend must fail the release, or `-WslLinux Off` to disable WSL even when installed.

`-WslShutdown IfStarted` is the default: a distro that was already running is left running, while a distro started for the release is terminated afterward. `Always` and `Never` are available for explicit control. `-KeepWslBuildTree` retains the mirrored Linux working tree for diagnostics.

## Linux packages

For every delegated Linux RID and Full/Light mode, TAR.GZ and DEB are mandatory. RPM and AppImage are optional unless `-RequireOptionalNativePackages` is supplied. `rpmbuild` supports explicit Linux architecture targets. `appimagetool` is given `ARCH=x86_64` or `ARCH=aarch64`; under WSL the script enables extract-and-run mode so a FUSE mount is not required. `-UseContainerPackaging` remains an optional fallback only when Docker/Podman is already installed.

## Cross-host matrix

Windows can therefore own Windows + Linux release production when WSL is ready. `-Runtime all-rids` can additionally attempt portable macOS cross-publishes, but DMG creation, Apple signing, and notarization remain macOS-native finalization steps. A Mac is still the authoritative host for those Apple-native outputs.
