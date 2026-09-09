# LocalGPT 4.0.3 source validation

## Validation scope

LocalGPT 4.0.3 is source/static validated only. No `dotnet build`, `dotnet test`, `dotnet publish`, native macOS packaging, signing/notarization, PKG execution or GitHub access was used in this environment.

## Build failure addressed

The supplied macOS release invocation failed before any release work ran because `Build-Release.ps1` contained a double-quoted message with `$mode:`. PowerShell interprets the colon as part of a scoped-variable reference unless the variable is explicitly delimited. The source now uses `${mode}:`.

## 4.0.3 checks

- All maintained LocalGPT release version surfaces are `4.0.3` and preserve the one-digit minor/patch policy.
- `Build-Release.ps1` contains `${mode}:` in the published-assembly identity diagnostic and contains no unscoped `$name:` interpolation token.
- The 4.0.2 assembly/version/source-fingerprint validation remains present.
- Runtime `server.json` still publishes version and executable identity as useful metadata rather than turning those fields into a global ownership requirement.
- The macOS launcher distinguishes the installed packaged app from alternate/debug/server hosts:
  - packaged-app endpoints with stale/missing packaged identity are rejected;
  - alternate executable endpoints remain eligible for loopback rendezvous when responsive;
  - alternate owners are never terminated merely for a version/path mismatch.
- PKG lifecycle endpoint deletion is ownership-aware and preserves active alternate-host `server.json` records.
- PKG process termination remains limited to `/Applications/LocalGPT.app` ownership.
- Dead endpoint owners are still cleaned up.
- Existing durable macOS working directory, per-user log path, startup runtime identity logging and fail-soft optional NVIDIA behavior remain intact.
- Existing Ollama update/install, fast Windows HTTP updater, model discovery/scoring/install, console progress and provider resolution remain present.
- Localization catalogs retain exact key parity.
- Render-mode ownership remains unchanged: intended routed pages retain `InteractiveServer`, Error remains static, and the setup child owns no nested render mode.
- Source-package hygiene rejects `bin`, `obj`, `__pycache__`, Python bytecode, traversal paths and symlinks.

## Static validation performed

The final source tree passed the maintained source-only checks available without invoking the .NET toolchain:

- release audit: 15 routed pages, 14 routed `InteractiveServer` boundaries, 6 localization catalogs and 6 provider profiles;
- application architecture policy: passed;
- cross-platform boundaries: 22 checks passed;
- async continuation policy: 259 source files, 3108 await tokens, 2751 `ConfigureAwait(false)`, 135 renderer-affine `ConfigureAwait(true)`, 217 configured async disposals and 5 configured async streams;
- provider-qualified Council audit: 282 checks passed;
- service resilience: 2252 service methods passed, with 29 yield methods and 3 direct Program/Startup methods intentionally skipped by that audit;
- XML documentation: 10420 C# declarations across 656 maintained source files plus 802 direct Razor `@code` members across 45 component types;
- generated macOS launcher, PKG `preinstall`, and PKG `postinstall` templates pass POSIX `/bin/sh -n` syntax validation after placeholder substitution;
- focused shell ownership tests confirm that a live alternate/debug endpoint is preserved, a stale packaged-app endpoint is terminated/removed, an alternate endpoint survives PKG cleanup, and a dead owner is cleaned up.

PowerShell itself is unavailable in this environment, so the actual PowerShell parser execution remains to be confirmed by the next Mac run. The source audit explicitly guards the exact interpolation form that caused the reported parser error.
