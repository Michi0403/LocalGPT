# LocalGPT 3.7.1 source validation

Static validation covers version consistency, self-contained Full-only Unix/macOS packaging, SHA-bound persisted Apple submission resume state, standards-correct notarization resume by polling `notarytool info`, same-version release reuse, configurable documentation/release roots, browser-PDF timeout/fallback behavior, Ghostscript compression with a 256 MiB default ceiling (including cached PDFs), restoration of the compressed PDF into the embedded `wwwroot/help-docs` runtime payload, Homebrew RPM provisioning, cleanup of generated build state, architecture/signing markers, the guarded Windows-only shortcut COM path, and the 15 reviewed InteractiveServer boundaries.

The documentation release path now requires the physical current-version PDF in runtime help, keeps local handbook links intact, records `pdfAvailable=true`/`runtimePdfPublished=true`, and rejects a handbook whose physical size disagrees with the generated status metadata or exceeds the configured maximum.

Runtime .NET builds, PowerShell execution on macOS, Ghostscript/Homebrew execution, native package production, Developer ID signing, and Apple notarization are intentionally not claimed in this source-only environment.
