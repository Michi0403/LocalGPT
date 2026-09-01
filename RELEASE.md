# LocalGPT 3.6.0

LocalGPT 3.6.0 is the rollover release after 3.5.9 and keeps the existing macOS full-release coordinator, Windows/Linux release lanes, launcher behavior, and optional WSL2 delegation intact. The patch is focused on documentation resilience, deterministic release-console output, and macOS package correctness.

Release documentation now has a durable validated payload cache under the existing external LocalGPT documentation-tool cache rather than under repository `bin`/`obj`. The cache key includes the version, compiled documentation assembly/XML, documentation sources, and documentation build/repair scripts. A successful DocFX HTML build is committed before the long PDF stage, so an interrupted or failed PDF render can reuse the complete API/HTML tree on the next run. A successfully validated PDF is cached alongside it. Normal release cleanup still removes every repository-local `bin`/`obj` directory. LocalGPT and PublisherStudio share a machine-level PDF-render lock so two large DocFX/Chromium jobs do not compete at the same time, and the default DocFX navigation timeout is 30 minutes instead of the macOS-specific five-minute limit that failed on slower systems.

PowerShell file-provider progress is suppressed in the documentation, release-cleanup, and native-packaging paths. Cleanup still happens, but it is line-oriented and deterministic rather than asynchronously overwriting DocFX/Spectre progress with impossible removed-file/byte totals.

macOS DMGs are now created and verified headlessly with `hdiutil`; release builds no longer mount the image or drive Finder through AppleEvents. The application bundle, Applications alias, and branded background asset are retained without the unreliable Finder layout step. PKGs now use an explicit root payload containing `/Applications/LocalGPT.app`, then validate the emitted payload with `pkgutil --payload-files` before accepting the package. LocalGPT.ReleasePackaging remains at 1.0.1 because its C# package-writer implementation did not change.

See `CHANGELOG-v3.6.0-MACOS-COORDINATOR-NATIVE-PACKAGING.md`, `VALIDATION-v3.6.0-source.md`, and `docs/engineering/release-and-docs.md`.
