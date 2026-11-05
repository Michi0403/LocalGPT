# LocalGPT 3.4.7 — PowerShell DocFX Progress Parser Repair

## Fixed

- Fixed the DocFX progress formatter in `build/Build-Documentation.ps1` from `"[DocFX] $name: $percent%"` to `"[DocFX] ${name}: $percent%"`.
- This removes the Windows PowerShell parser error `InvalidVariableReferenceWithDrive` caused by the colon immediately following `$name`.

## Preserved

- Full release documentation and PDF generation remain enabled.
- Debug-build documentation behavior from 3.4.6 remains unchanged.
- Cross-platform build guards remain enabled for Windows, macOS, and Linux.
- Existing Node.js/browser reuse and the fast browser PDF path remain unchanged.
- Application/runtime/UI/InteractiveServer/persistence behavior and wire protocol 2.1.1 are unchanged.

Version: **3.4.7**.
