# LocalGPT 3.4.7 source validation

- Patched the exact 3.4.6 source ZIP supplied/tested in this conversation.
- Fixed only the invalid PowerShell interpolation in `build/Build-Documentation.ps1`: `${name}:` now delimits the variable before the colon.
- Searched PowerShell sources for the same unbraced variable-before-colon pattern; the repaired DocFX line is no longer present.
- Current LocalGPT identity was advanced from 3.4.6 to 3.4.7 while preserving the single-digit semantic minor/patch policy.
- Full documentation/PDF generation and all cross-platform build guards remain enabled.
- No .NET build and no GitHub access were used.
