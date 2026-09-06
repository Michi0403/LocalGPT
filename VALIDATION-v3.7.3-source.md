# LocalGPT 3.7.3 source validation

Static validation covers version consistency, the Apple-notarization interpolation repair, repository PowerShell parser-preflight presence, a lexical guard against future unbraced variable-plus-colon interpolation, the embedded size-controlled documentation PDF, Full/self-contained Unix/macOS packaging, resumable notarization/reuse, Homebrew Ghostscript/RPM support, architecture/cross-platform boundaries, async/service resilience, and the 15 reviewed InteractiveServer boundaries.

This environment does not contain PowerShell or .NET, so the macOS `pwsh Build-Release.ps1` execution itself is not claimed here. The supplied source was checked statically and should be re-run on the Mac, where the existing parser preflight will be authoritative.
