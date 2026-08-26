# LocalGPT 3.3.9

LocalGPT 3.3.9 fixes the documentation accessibility failure that was only discovered after the 46-minute DocFX PDF render on macOS.

The maintained DocFX theme post-processing pass now guarantees a non-empty `lang="en"` attribute on generated HTML pages, including API pages where the modern template can omit it. The production GitHub Pages HTML parser is also available in an HTML-only mode and is executed before PDF generation, so missing language metadata, viewport/title/landmark issues, missing image alt text, or broken local links fail before the expensive PDF phase begins.

The pre-PDF validator receives the temporary versioned PDF link stub only while checking links; the placeholder is removed before the early HTML publication pass and before the real PDF render. `documentation-status.json` records `htmlPreflightValidated`, and both Release and LocalDevelopment guards require it. Python 3 is checked in the shared prerequisite phase because the same validator is required later for the GitHub Pages snapshot.

The working DocFX-only `System.Formats.Nrbf` dependency probe, cross-platform Node.js provisioning, DevExpress license preflight, PowerShell parser guard, live PDF progress, and installer/platform work remain intact.

See `CHANGELOG-v3.3.9-DOCUMENTATION-ACCESSIBILITY-FAILFAST.md` and `VALIDATION-v3.3.9-source.md`.
