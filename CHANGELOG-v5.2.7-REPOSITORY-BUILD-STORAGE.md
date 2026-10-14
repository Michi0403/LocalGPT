# LocalGPT 5.2.7 — repository-local release-build storage

## Fixed

- Full release builds now keep heavy mutable tool state with the repository by default instead of allowing macOS/Linux per-user caches and the operating-system temp directory to consume the system partition while the checkout itself lives on an external volume.
- `Build-Release.ps1` initializes `artifacts/.build-storage` before compile, restore, documentation, browser rendering, and native packaging work. The build redirects `DOTNET_CLI_HOME`, NuGet package/HTTP/plugin/scratch caches, npm cache, XDG cache, documentation caches, and `TMPDIR`/`TMP`/`TEMP` into that repository-owned root.
- Standalone documentation builds initialize the same storage policy before DocFX, browser-PDF, or temporary profile work.
- Documentation/Node runtime cache resolution now prefers the supplied repository fallback before `LocalApplicationData`; the user profile is only a last-resort standalone fallback.
- LocalGPT's shared protocol/release-packaging package copy is now kept under repository build storage rather than written to the host user's application-data directory.

## Maintenance protection

- `Assert-PowerShellCompatibility.ps1` now verifies the repository build-storage helper, all redirected cache/temp environment variables, release-entrypoint ordering, standalone documentation initialization, and fallback-before-user-cache policy.
- `build/audit_repository_build_storage.py` mirrors the contract in source-only environments where PowerShell is unavailable.
- `AGENTS.md` documents the repair pattern: new release tools that download, unpack, render, cache, or stage substantial data must use repository build storage or inherit its redirected environment.

## Operator override

`FUTURE2_BUILD_STORAGE_ROOT` may point the heavy build state at another build volume. `-DocumentationCacheRoot` remains a narrower override for documentation state.
