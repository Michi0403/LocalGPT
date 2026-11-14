# LocalGPT 7.1.2 — macOS PKG lifecycle and Installer resources

## Evidence / failure

The macOS Installer log for the 7.1.1 Apple Silicon package reported `PKInstallErrorDomain Code=112` while executing `./preinstall`. The prior `build/NativeReleasePackaging.ps1` was checked in with CRLF line endings; its generated `#!/bin/sh` here-strings were written byte-for-byte by `Write-Utf8NoBom`, preserving CRLF in executable macOS package scripts. This yields an invalid shebang interpreter (`/bin/sh\r`) on Unix. The log does not expose the script's actual stderr, so this is a source-confirmed defect consistent with, but not independently proven as, the only cause of the reported failure.

The same log noted missing Installer background, readme, and license resources. The sources already contain a PNG, README and license, but the prior distribution did not package or reference them.

## Corrections

- Native packaging now loads separately maintained, LF-only shell templates from `build/macpkg/`. Template validation rejects BOM, CR, NUL, malformed main shebang, or a second fragment shebang.
- The repository's native-release UTF-8 writer now normalizes Windows CRLF to Unix LF, without writing a UTF-8 BOM. The actual generated `preinstall` and `postinstall` are checked for LF/shebang, shell syntax (`sh -n`), and executable mode before `pkgbuild` runs.
- `productbuild` receives real resources: `README.txt` from `README.md`, `LICENSE.txt` from `LICENSE.MD`, and `background.png` from the existing `build/assets/LocalGPT-dmg-background.png`. The Distribution XML now references each resource.
- After building, `pkgutil --expand-full` checks the **actual output package** for both lifecycle scripts and all three referenced resources. Missing or invalid content aborts packaging instead of claiming success.
- New `build/Assert-MacPackageScripts.ps1` is called by the existing `build/Assert-PowerShellCompatibility.ps1`, so its fixture checks execute on Windows, macOS and Linux during normal source/release preflight. Negative fixtures cover CRLF, BOM and missing shebang.
- Application version: **7.1.2**. Setup project version: **5.0.3** (installer packaging change).

No modifications to application startup, TLS/MCP/1-Wire, database, installer `Program.cs`, Windows installation layout, Linux runtime packaging, or user-data paths.

## Evidence boundaries

- Local static tests: shell templates pass `/bin/sh -n`; LF/shebang and source-preservation checks pass; ZIP CRC verified.
- Not run here: `pwsh` compatibility guards, .NET compilation, `pkgbuild`, `productbuild`, or a live macOS installation. The package must be rebuilt and installed on macOS to confirm the failure is resolved.
