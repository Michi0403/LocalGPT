# LocalGPT 3.9.3 — Setup, CanIRun, and release-identity repair

## Fixed

- Uses the canonical `https://www.canirun.ai/api/recommend` endpoint. The Windows runtime log showed the previous non-`www` endpoint returning a redirect and LocalGPT aborting the optional recommendation request before any model list could be built.
- Keeps the HTTP handler redirect-disabled, but adds bounded manual redirect handling: at most two redirects, HTTPS only, and only the maintained CanIRun.ai hosts accepted by the existing URI validator. The approved hardware POST payload is preserved across that validated redirect.
- Extracts provider-specific Ollama model metadata from tolerated CanIRun JSON fields when it is present, while retaining the CanIRun model page as the attributed source for each recommendation.
- Keeps every parsed recommendation visible even if one provider mapping cannot be established safely. One bad mapping no longer removes the whole recommendation/model-choice list.
- Adds conservative known Ollama family/size inference for stable CanIRun identifiers and expands fresh-install Knowledge aliases for current common models. Unknown sized recommendation slugs are not passed blindly to `ollama pull`; they remain visible with the manual provider-model-ID fallback.
- Makes installed-model discovery best-effort while building recommendation choices, so recommendations can still be offered for installation even when the provider model inventory is temporarily unavailable.
- Matches installed provider models by their complete provider token (with only `:latest` treated as optional) instead of family name alone, so an installed `qwen3:4b` can no longer incorrectly suppress the install button for a recommended `qwen3:32b`.
- Makes multi-hardware CanIRun lookup best-effort per reviewed hardware row; one failed row no longer discards successful recommendations from another row.
- Isolates provider-profile and first-run-status failures inside the Setup snapshot in addition to the existing hardware and installed-model isolation.
- Adds a provider-first recovery snapshot in the Razor component so a remaining snapshot failure cannot leave the Setup guide permanently stuck at `Loading local setup state…`.
- Prevents recommendation installation when LocalGPT does not have a safe provider install mapping, while preserving the explicit manual model-ID installation path.
- Removes installed-package errors caused by requiring `src/LocalGPT/LocalGPT.csproj` at runtime. Source launches still read the project version; installed packages fall back to normalized assembly/informational version metadata.
- Adds a safe current-directory resolver for remaining runtime source/repository discovery helpers so replacing an installed application directory cannot reintroduce `GetCwd()` failures outside the already-repaired Setup child-process paths.
- Routes the remaining LocalPathExplorer and project-maintenance process fallbacks through that safe resolver, and makes optional file-logger construction fail closed to `NullLogger` with a single console warning rather than propagating a logging-initialization exception into Blazor/DevExpress rendering.
- Adds source identity to release artifact reuse. `Build-Release.ps1` computes a deterministic source SHA-256, clears stale same-version generated artifacts when that identity changes or is missing, records `SOURCE-SHA256.txt` in the final bundle, and requires the bundle fingerprint to match before treating a same-version release as complete. This prevents an older notarized `.app`/DMG/PKG/TAR from silently representing newer same-version source.

## Preserved

- One unified Setup guide remains in `/install`: hardware → optional CanIRun.ai recommendations → provider/Ollama lifecycle → model installation → benchmark team.
- Ollama Start/Stop/Restart/Refresh and endpoint registration remain explicit user actions; LocalGPT does not silently start Ollama.
- Manual model-ID installation remains available without CanIRun.ai.
- CanIRun.ai remains explicit opt-in only and receives only the reviewed hardware facts described in the UI.
- Provider-profile v2 parsing and v1 fallback remain intact for upgraded databases.
- The maintained 512-object CanIRun JSON traversal safety bound remains intact; the removed 24/32/96 display/service truncations do not return.
- Fifteen `@rendermode InteractiveServer` boundaries, eight hosted services, and the executable Council SQL seed remain unchanged.

## Version

- LocalGPT: `3.9.3`
- LocalGPTInstallerConsole: `3.9.3`
- LocalGPTWebviewWrapper: `3.9.3`
- LocalGPT.WireProtocolVersion: unchanged (`2.1.1`)
- LocalGPT.ReleasePackaging: unchanged (`1.0.2`)

## Validation boundary

The supplied macOS and Windows logs/screenshots are treated as authoritative runtime evidence. This environment does not run the .NET/PowerShell release pipeline. 3.9.3 is therefore validated here through source/static regression checks; the next Windows/macOS package run remains the authoritative runtime verification.
