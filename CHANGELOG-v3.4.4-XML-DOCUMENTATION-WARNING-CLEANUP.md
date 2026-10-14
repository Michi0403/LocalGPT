# LocalGPT 3.4.4 — XML Documentation Warning Cleanup

## Fixed

- Added the missing XML `<param>` documentation for the platform/runtime dependencies reported by the real 3.4.3 release build.
- Covers all 15 `CS1573` warnings shown for service constructors, including both Minecraft workspace platform parameters.
- Runtime code paths, dependency-injection signatures, release packaging, PowerShell 5.1 compatibility, and the 3.4.2 documentation/PDF payload split remain unchanged.

## Version

- Application, installer console, and webview wrapper: `3.4.4`.
- The LocalGPT wire-protocol package remains `2.1.1`; no protocol change was required.
- Minor and patch version slots remain single-digit.
