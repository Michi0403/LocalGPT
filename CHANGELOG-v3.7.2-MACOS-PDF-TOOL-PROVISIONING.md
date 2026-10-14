# LocalGPT 3.7.2 — macOS PDF tool provisioning

Version advanced from 3.7.1 to 3.7.2 because release/documentation scripts changed.

## Fix

- The documentation build now proactively verifies Ghostscript before a required macOS PDF build.
- Homebrew is discovered from the current PATH and from `/opt/homebrew/bin/brew` and `/usr/local/bin/brew`, covering VS Code/non-login shells that do not inherit the normal Homebrew PATH.
- When Ghostscript is missing and Homebrew is available, the build runs `brew install ghostscript` automatically before the expensive PDF phase completes.
- Homebrew/Ghostscript provisioning failures produce actionable diagnostics and oversized/DocFX-fallback PDFs remain fail-closed rather than being shipped uncompressed.
- `Build-Release.ps1` exposes `-DisableDocumentationToolProvisioning` for maintainers who require a fully pre-provisioned/offline build machine. Default behavior remains automatic provisioning.
- Existing PDF compression, embedded offline handbook, Full/self-contained packaging, release resume/notarization reuse, RPM provisioning, and cache/output-root controls remain intact.
