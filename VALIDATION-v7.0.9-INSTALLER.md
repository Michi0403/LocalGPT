# LocalGPT 7.0.9 installer validation

**Source base:** user-supplied latest `LocalGPT-main (5).zip` (the file with the manual installer restoration).

**Source comparison:** The base `src/LocalGPTInstallerConsole/Program.cs` is byte-identical to the supplied 6.3.4 working reference before the changes in 7.0.9. Two other historical LocalGPT ZIPs and PublisherStudio 4.7.6 were compared at the installer and release-archive boundaries. PublisherStudio's stable archive roots informed the 7.0.9 packaging correction. PublisherStudio itself was not changed.

Run the full repository build on Windows or macOS as appropriate:

```powershell
pwsh ./Build-Release.ps1
```

On Windows, locate the produced `winx64.zip` and `setupwinx64.zip` under the release artifacts, then run the **optional** test against the *actual produced files*:

```powershell
pwsh ./tests/Test-InstallerReleaseArchives.ps1 -AppArchive 'PATH/TO/winx64.zip' -SetupArchive 'PATH/TO/setupwinx64.zip' -ExpectedVersion 7.0.9 -ExpectedSetupVersion 5.0.1
```

Expected archive entries: `winx64/LocalGPT.exe`, `winx64/LocalGPT.dll`, `winx64/RELEASE-VERSION.txt`, `setupwinx64/LocalGPTInstallerConsole.exe`, `setupwinx64/Start.cmd`, and `setupwinx64/RELEASE-VERSION.txt`. No `LocalGPT-7.0.9-win-x64/` wrapper.

Then run the **freshly published installer** (not an older Setup 4.9.6 binary). Verify `%LOCALAPPDATA%\LocalGPT\winx64\LocalGPT.exe` has version 7.0.9, setup shortcuts resolve to `%LOCALAPPDATA%\LocalGPT\setupwinx64\Start.cmd`, and persistent data remains intact. The new installer should exit with an error rather than reporting success if verification fails. The installer does **not** automatically remove pre-existing versioned leftovers from earlier broken releases; any such cleanup is a separate, explicit user operation.

**Not performed here:** PowerShell execution, .NET build, Windows installation, installer/process-lock behavior, archive-native publish or actual GitHub release-asset replacement. Report a successful build/installation only after those tests pass.


## PublisherStudio-aligned reconstruction

The setup version is **5.0.1**, independent of the LocalGPT application release **7.0.9**.
For macOS/Linux use the same optional archive smoke test with `-RuntimeFolder linuxx64`,
`linuxarm64`, `macosx64`, or `macosarm64`, and matching archives. This tests ZIP overlay only;
actual native DMG/PKG/TAR.GZ boot tests are separate.

`--localgpt-zip` and `--localgpt-setup-zip` permit an offline, paired-asset test
without accessing GitHub. Setup self-detaches on Windows when launched from its installed
path, following the PublisherStudio installer. Its update stops only the LocalGPT
process in the canonical installation.

The rejected installer-layout build guard is not present or reintroduced.

### Freshly built setup executable check

Confirm the standalone setup executable reports setup version **5.0.1**, not an older 4.9.6 or 5.0.0 build. `Build-Release.ps1` checks its managed assembly version before releasing the archives and marks setup ZIPs with `SETUP-VERSION.txt`. On Windows, the installer additionally checks the installed executable's FileVersion against that marker.

**Seven portable runtime ZIP pairs:** Windows winx64/winx86/winarm64, Linux linuxx64/linuxarm64, macOS macosx64/macosarm64. macOS and Linux native packaging is retained; portable ZIPs are additional artifacts. For WSL Linux builds, the WSL bridge forwards the new Linux ZIPs as well.

Normal `Install.cmd`, `Update.cmd`, and `LocalGPT-Install-Start.cmd` do not set `--force-delete`. To delete any installation, the user must explicitly choose a force-delete command or flag.
