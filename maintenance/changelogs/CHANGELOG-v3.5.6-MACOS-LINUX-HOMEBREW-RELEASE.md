# LocalGPT 3.5.6 - macOS Linux cross-release and Homebrew RPM support

- Expands the normal macOS `Build-Release.ps1` lane to publish `osx-x64`, `osx-arm64`, `linux-x64`, and `linux-arm64` in one run. Windows remains Windows-only by default, and Linux remains a supported Linux release host.
- Keeps Linux TAR.GZ and DEB materialization cross-host through the LocalGPT-owned managed `LocalGPT.ReleasePackaging` helper, so those Linux packages can be completed on macOS without Docker.
- Adds native macOS RPM finishing through `rpmbuild`. The script detects a normal `rpmbuild` on `PATH` and also resolves Homebrew's `rpm` formula. `brew install rpm` is the supported prerequisite on macOS.
- Adds `-ProvisionNativePackagingTools` as an explicit opt-in. When used on macOS with Homebrew already installed, the build may invoke `brew install rpm`; ordinary builds never install Homebrew or silently modify the machine.
- Uses an explicit Linux RPM target (`x86_64-unknown-linux` or `aarch64-unknown-linux`) so a pre-published Linux payload can be wrapped as an architecture-correct RPM from macOS or Linux without compiling inside RPM.
- Keeps AppImage as a Linux-native format. A macOS build skips it cleanly by default; `-UseContainerPackaging` can opt into an already-installed Docker/Podman fallback. No container runtime is required for Windows, macOS, Linux TAR.GZ, DEB, or Homebrew RPM output.
- Makes RPM/AppImage true optional finishers. Missing tools or finisher failures warn and continue by default; `-RequireOptionalNativePackages` restores strict failure behavior when a release operator explicitly needs those formats to be mandatory.
- Preserves the 3.5.5 macOS executable-bit/UTF-8 bundle repairs and the LocalGPT.ReleasePackaging 1.0.1 TAR/DEB file-handle repair. No application behavior, persisted policy, service architecture, or InteractiveServer boundary changed.
