# LocalGPT 3.6.0 — DocFX cache and headless macOS packaging repair

- Rolled 3.5.9 forward to 3.6.0 under the repository's one-digit minor/patch version policy.
- Fixed the corrupt asynchronous yellow cleanup display by suppressing PowerShell file-provider progress in release cleanup, documentation cleanup, and native packaging while keeping the actual cleanup work intact.
- Release cleanup still deletes every repository-local `src/**/bin` and `src/**/obj` directory and now emits one deterministic completion line instead of per-file provider progress.
- Added a durable documentation payload cache outside repository `bin`/`obj`, keyed by version, compiled documentation assembly/XML, documentation source inputs, and documentation build/repair scripts.
- Commits validated DocFX HTML/API output to that durable cache before PDF rendering, so a PDF timeout or interrupted build can resume from the completed HTML tree rather than rebuilding more than a thousand API pages.
- Caches a successfully validated PDF beside the HTML payload and reuses it when the cache key still matches.
- Restored a 30-minute default DocFX per-navigation timeout on macOS and other hosts; `DOCFX_PDF_TIMEOUT` remains an explicit operator override.
- Added a shared LocalGPT/PublisherStudio PDF-render lock so the two large DocFX/Chromium PDF jobs do not run concurrently on the same machine.
- Removed Finder AppleEvent DMG layout automation that could time out with macOS error `-1712`. DMGs are now created and verified headlessly with `hdiutil`, retaining the `.app`, Applications alias, and bundled background artwork asset.
- Replaced component-inferred PKG construction with an explicit `/Applications/LocalGPT.app` root payload and validates the finished package layout using `pkgutil --payload-files`; an invalid or uninspectable PKG is removed instead of being accepted as a release artifact.
- Preserved the existing macOS launcher fixes, application icon/signing behavior, cross-platform release coordinator, Linux packaging behavior, optional WSL2 path, and explicit InteractiveServer component boundaries.
- Kept LocalGPT.ReleasePackaging at 1.0.1 because its C# package-writer implementation did not change.
