# LocalGPT 3.6.9 — resumable releases and documentation size control

## Fixed

- Unix/macOS release packaging now emits only the self-contained `Full` lane. The framework-dependent `Light` duplication is removed from the release coordinator.
- Existing macOS DMG/PKG artifacts are validated before regeneration. A valid stapled Developer ID artifact is reused, and `-ForceRebuildArtifacts` explicitly opts out of reuse.
- Apple notarization is crash-resumable: immediately after upload, the artifact SHA-256 and submission ID are persisted beside the artifact in `.notary-state.json`. A later run resumes that exact Apple submission instead of uploading the same bytes again. `MACOS_NOTARY_WAIT_TIMEOUT` controls the local wait window (default `2h`); an Apple-side delay can therefore stop the local run without losing the submission.
- When the same-version TAR.GZ, DMG, and PKG already pass resume validation, the release coordinator skips the entire macOS RID publish/sign/package lane. A fully assembled same-version release bundle is SHA-256 verified and becomes a no-op on rerun unless `-ForceRebuildArtifacts` is specified.
- Final release-bundle assembly moves large generated artifacts directly into the version directory and reuses byte-identical files from an interrupted assembly instead of making another full-size staging copy.
- Documentation cache location is configurable with `-DocumentationCacheRoot` or `FUTURE2_DOCUMENTATION_CACHE_ROOT`; release outputs can use `-ReleaseOutputRoot` or `FUTURE2_RELEASE_OUTPUT_ROOT`. The shared documentation fan-out cache no longer defaults inside `artifacts/release`.
- Large PDF generation now tries the Chromium/Edge print-book path up to a configurable 1400 source pages with an 8-minute hard browser timeout, then falls back to DocFX.
- Oversized PDFs are screen-optimized with Ghostscript. On macOS the build discovers Homebrew Ghostscript and provisions `ghostscript` when needed. Cached PDFs are passed through the current compression/size policy too, so an old multi-gigabyte cached handbook cannot bypass the fix. The default release ceiling is 256 MiB and can be changed with `FUTURE2_DOCUMENTATION_PDF_MAX_BYTES`.
- The source/runtime help tree remains HTML-only and no longer duplicates the standalone release PDF into every self-contained app package; the standalone PDF is emitted once in the final release bundle.
- Failed documentation/release runs clean generated DocFX `_site`, repository `bin`/`obj`, RID staging, and transient app workspaces so large failed intermediates do not accumulate indefinitely.
- macOS Linux packaging provisions Homebrew `rpm` automatically when `rpmbuild` is missing, so RPM output is not silently skipped merely because the build host is macOS.
- The Windows shortcut COM resolver now returns before Windows-only COM APIs are touched on non-Windows hosts, matching the cross-platform analyzer contract used by the installer project.

## Preserved

- Self-contained application publishing, Developer ID signing/notarization/stapling, exact architecture checks, Windows packaging, DEB packaging, WSL/container fallbacks, 15 reviewed `InteractiveServer` directives, and `LocalGPT.ReleasePackaging` 1.0.1 remain intact.

## Version

- Version advanced from 3.6.8 to 3.6.9 because release/documentation PowerShell behavior changed.
