# LocalGPT 5.7.1 — PowerShell-first build, Debug, documentation and release validation

**Authoritative input:** the user's repaired `LocalGPT.zip` (the contained project still identified itself as 5.6.9). No earlier AI archive was overlaid on it. The application Python.NET, media, plugins and Python job orchestration were not modified.

## Restored and repaired

- Restored the native `Assert-RuntimePluginSourceCallbacks.ps1` gate and the requested `async (Guid value) => await TaskHandler(...).ConfigureAwait(true)` DevExpress `ValueChanged` pattern for the project, revision and reviewed-file selectors. The reviewed-file handler now returns `Task`; render-affine updates use `InvokeAsync`, with existing ownership, selection-revision and error handling retained. No old version-number comparisons.
- Recreated standalone **PowerShell** architecture checks for async continuation boundaries, async-only UI/controller operations, service-method diagnostics, persisted runtime text/regex ownership, Razor maintenance, DevExpress component retention and protected repository entrypoints. Reconnected the production-relevant native checks to `Directory.Build.targets` without Python as a prerequisite.
- Added `Assert-RepositoryPowerShellBoundary.ps1` to fail the ordinary build on Python-based architecture/script invocations, missing MSBuild-referenced PowerShell files and Python-based GitHub Pages workflow commands.
- Removed Python 3 detection and the orphaned `audit_council_sql_seed.py` invocation from `Initialize-BuildPrerequisites.ps1`. Its mandatory SQL seed-shape/destructive-statement PowerShell checks remain intact.
- Replaced Python-dependent Council and DX code-generation source wrapper scripts with native PowerShell source checks, bound to the actual partial-class files in this source tree.
- Replaced the `prepare-pages-artifact.py` release publication dependency with `Assert-PagesArtifact.ps1`: HTML accessibility and local links, complete theme/status contracts, bounded/safe archive processing, release-PDF metadata, HTML-only Pages staging, version checks, and deployment metadata. Updated `Build-Documentation.ps1`, `Update-GitHubPagesSnapshot.ps1` and GitHub Actions to call the same PowerShell validator.
- Repaired a subtle invocation regression: calling a `.ps1` validator must not inspect a potentially stale `$LASTEXITCODE` left by an earlier native process. Native PowerShell exceptions are propagated instead.
- Removed Python from mandatory WSL release readiness; Python remains an optional LocalGPT runtime capability.
- Eliminated the retired `.github/scripts/prepare-pages-artifact.py` validator. The unrelated optional `.github/scripts/extract-shipped-docs.py` utility remains but is never called by Build/Debug/Release/Pages.
- Retained DevExpress version and all application Python media/plugin/runtime code as uploaded.

## Security, scope and fidelity

These PowerShell checks are **not represented as a byte-for-byte port of every removed Python AST/token parser**. The async-only gate enforces sync-over-async exclusions for controllers and components, while the dedicated continuation guard checks blocking/async-void patterns. Full compiler/Razor semantics remain the responsibility of the user's real .NET build. The DevExpress retention audit protects key business-facing editors and is complemented by the existing native all-component DevExpress gate; the user's deleted 58-entry retention snapshot was **not silently reinstated**. The PDF verifier checks header, size, metadata and tagged-document token evidence; compressed-only PDF tagged structure may require follow-up on Windows depending on the renderer. No failing check was turned into an unconditional pass.

The source archive deliberately excludes machine-specific `.vs`, `bin`, `obj` and already generated `wwwroot/help-docs` output from the uploaded development ZIP. The tracked GitHub Pages source archive and maintained documentation sources remain included.
