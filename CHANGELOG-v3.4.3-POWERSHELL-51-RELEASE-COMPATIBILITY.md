# LocalGPT 3.4.3 — PowerShell 5.1 Release Compatibility

## Fixed

- Replaced the PowerShell-7-only `String.Contains(value, StringComparison)` call in `Build-Release.ps1` with the Windows PowerShell 5.1-compatible `String.IndexOf(value, comparison) -ge 0` equivalent.
- Preserved the case-insensitive PDF-link detection behavior used when preparing runtime documentation.
- Kept the 3.4.2 Pages/PDF payload split unchanged.

## Version

- Application, installer console, and webview wrapper: `3.4.3`.
- The LocalGPT wire-protocol package remains `2.1.1`; no protocol change was required.
- Minor and patch version slots remain single-digit.
