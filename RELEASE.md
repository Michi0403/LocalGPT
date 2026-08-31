# LocalGPT 3.5.6

LocalGPT 3.5.6 is the **macOS Linux cross-release and Homebrew RPM** maintenance release.

The ordinary release matrix now intentionally uses two primary workstation lanes: Windows builds the Windows x64/x86/ARM64 application/setup outputs, while macOS builds both macOS x64/ARM64 and Linux x64/ARM64 application packages. Linux remains fully supported for developers and Linux-native release work.

On macOS, Linux TAR.GZ and DEB outputs use the managed LocalGPT.ReleasePackaging helper. RPM finishing can use Homebrew's `rpm`/`rpmbuild` (`brew install rpm`) and targets the Linux architecture explicitly. `-ProvisionNativePackagingTools` may install the Homebrew `rpm` formula only when the operator opts in and Homebrew already exists. AppImage remains Linux-native; it is skipped on macOS unless the operator explicitly enables the optional container fallback.

RPM/AppImage are optional finishers by default and cannot destroy an otherwise valid release just because a tool is missing or a native finisher fails. Use `-RequireOptionalNativePackages` when those optional formats must be strict.

See `CHANGELOG-v3.5.6-MACOS-LINUX-HOMEBREW-RELEASE.md` and `VALIDATION-v3.5.6-source.md`.
