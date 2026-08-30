# LocalGPT 3.5.4

LocalGPT 3.5.4 is the **host-aware release packaging** maintenance release.

The supplied Windows 3.5.3 release log reaches successful Windows x64/x86/ARM64 application/setup publishing and successful Linux TAR.GZ/DEB generation before failing only at the mandatory RPM step. The default release path no longer asks a Windows machine to complete Linux/macOS native packaging.

`Build-Release` now treats `-Runtime all` as all maintained runtimes for the current host OS. `-Runtime all-rids` is retained for an explicit cross-host publish attempt. Linux RPM/AppImage finishing is optional and native-tool driven; Docker/Podman is opt-in rather than a release prerequisite.

Windows-only LocalGPT builds still create and populate the shared `LocalGPT.ReleasePackaging` 1.0.1 NuGet cache for PublisherStudio, without installing the Unix packaging tool when it is not used. Windows command/PowerShell entry points also initialize UTF-8 console handling for `dotnet` output.

See `CHANGELOG-v3.5.4-HOST-AWARE-RELEASE-PACKAGING.md` and `VALIDATION-v3.5.4-source.md`.
