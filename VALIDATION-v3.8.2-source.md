# LocalGPT 3.8.2 source validation

Static/source validation for the artifact-local notarization repair. This environment cannot execute macOS signing/notarytool or the .NET release build.

Required invariants:
- no retry wrapper may invoke `notarytool submit`;
- pending state is written before submit;
- ambiguous submit resolves via history before any future upload;
- artifact state is SHA-256 bound;
- accepted/stapled artifacts remain reusable;
- PowerShell 5.1-compatible syntax is preserved.
