# LocalGPT 7.1.2 — macOS native package acceptance

The 7.1.1 Apple Silicon installation failure occurred in `./preinstall` before application startup. Treat a successful source build as distinct from a successful package install.

## Early, cross-host source preflight

```powershell
pwsh ./build/Assert-PowerShellCompatibility.ps1
# Includes Assert-MacPackageScripts.ps1 (writer exercised with CRLF, negative fixtures)
```

Run the PowerShell guard on Windows PowerShell 5.1 as well; it must validate bytes and sources even if Unix `sh` is unavailable. On macOS/Linux it also validates the shell script syntax when `sh` is installed.

## macOS package validation

Build the release normally. The native packaging script verifies all of the following before it retains any `.pkg`:

1. `preinstall` and `postinstall` are present, executable in the staging tree, have an LF-only `#!/bin/sh` and pass `sh -n`.
2. The generated Distribution XML references `README.txt`, `LICENSE.txt`, and `background.png` and `productbuild` gets `--resources`.
3. The built `.pkg` expands with `pkgutil --expand-full`. Both lifecycle scripts and all three resources appear in the expanded package. The expanded scripts pass the same byte and syntax tests.
4. `installer -pkg ... -target / -showChoicesXML` can read the package.

Only after these checks should a user attempt a real test installation of `LocalGPT-7.1.2-osx-arm64-full.pkg` on a disposable/backup installation. Check `sudo tail -n 300 /var/log/install.log` for `PKInstallErrorDomain`, and confirm the installed app launches and exposes its local runtime endpoint.

## Not validated in this workspace

`pwsh`, Apple `pkgbuild`, Apple `productbuild`, `pkgutil`, the real Installer GUI and the .NET SDK are not available here; no claim is made that an actual 7.1.2 release install has already passed.
