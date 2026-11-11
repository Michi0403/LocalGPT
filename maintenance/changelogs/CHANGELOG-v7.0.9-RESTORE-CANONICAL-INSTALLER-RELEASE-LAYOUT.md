# LocalGPT 7.0.9 — restore canonical installer/release agreement

- Compared two pre-regression LocalGPT installer histories with PublisherStudio 4.7.6 and the current user-repaired LocalGPT `Program.cs`. Retained the user-repaired, original direct-extraction behavior; no staged or versioned side-by-side installation scheme was reintroduced.
- `Build-Release.ps1`: application and setup ZIP entries now begin with the canonical, stable platform folders (`winx64/` and `setupwinx64/` for Windows x64) rather than `LocalGPT-<version>-win-x64/` and `LocalGPT-Setup-<version>-win-x64/`. Corresponding win-x86 and win-arm64 roots are stable as well. Separate Unix native packaging remains unchanged.
- Release version verification now reads each ZIP's `RELEASE-VERSION.txt` rather than deriving version identity from a folder name. The authoritative compiled app assembly version check remains intact.
- Installer obtains and validates **both** archives before changing the installed files. It requires exact current-platform asset filenames instead of selecting an arbitrary fallback ZIP; validates archive entry paths and duplicates; and checks application/setup version agreement.
- A successful extraction is followed by checks that the canonical installed executable, assembly and setup launcher exist, and that the installed managed assembly and both version markers match. Failed installs stop before provisioning shortcuts or launching an old executable.
- The optional `tests/Test-InstallerReleaseArchives.ps1` smoke test overlays real release archives onto a disposable old-version fixture, checks canonical directories and persistent data, and rejects versioned leftover wrappers. It is **not a build guard** and is not invoked automatically.
- Version advanced from `7.0.8` to `7.0.9`. No unrelated app architecture, Council functionality, data model, third-party components or PublisherStudio source was modified.

## Verification not performed in this environment

PowerShell, the .NET SDK, Windows installer runtime and a real `Build-Release.ps1` publish are unavailable here. This source revision is a **candidate** until the actual release ZIPs are built, the optional smoke test passes and the published installer is run on Windows.
