# LocalGPT 4.0.3 — release parser and runtime rendezvous repair

## Why this release exists

The first macOS release attempt from the 4.0.2 source never reached publishing. `pwsh Build-Release.ps1` stopped during parsing because a double-quoted error message contained `$mode:`. In PowerShell, a colon immediately following an un-delimited variable name is parsed as part of a scoped-variable reference; `${mode}:` is required when the colon is literal text.

The preceding 4.0.2 macOS lifecycle repair also made `server.json` endpoint acceptance too strict for LocalGPT's established hosting model. `server.json` is intentionally a runtime rendezvous contract used by packaged launchers, debug hosts, WebView/WinUI-style hosts and other local hosting arrangements. A different executable path is therefore not automatically stale or unsafe.

## Changes

- **PowerShell release parser repair**
  - The published-assembly identity error now uses `${mode}:` instead of `$mode:`.
  - The release source audit scans `Build-Release.ps1` for unscoped `$name:` interpolation so this parser failure is caught before another handoff.

- **Preserved `server.json` rendezvous behavior**
  - The macOS launcher still rejects and replaces an old packaged `/Applications/LocalGPT.app` runtime when the endpoint belongs to that packaged executable but its version/identity is stale.
  - Endpoints owned by a different executable are treated as legitimate alternate hosts and remain eligible for the historical rendezvous/reuse path when their loopback endpoint responds.
  - Legacy/alternate endpoints are no longer terminated merely because they do not carry the packaged app's version or executable identity.
  - Dead-PID endpoint files are still removed as stale.

- **Installer ownership boundary**
  - PKG `preinstall` and `postinstall` remove `server.json` only when the endpoint belongs to the installed `/Applications/LocalGPT.app` process (or when its recorded owner PID is dead).
  - An active debug/server/alternate-host endpoint is preserved during packaged application replacement.
  - Process termination remains restricted to the installed application binary path.

## Preserved behavior

- The 4.0.2 package/runtime version and source-fingerprint checks remain intact.
- The macOS updater still stops an old installed LocalGPT process before replacing the bundle and relaunches the new app when appropriate.
- Durable per-user working-directory/log-path handling and fail-soft hardware probing remain intact.
- Ollama update/install, fast Windows Ollama download, compatible-model discovery/scoring, provider-catalog resolution, progress display, Council recovery and existing InteractiveServer ownership are unchanged.

## Validation boundary

This release is source/static validated only. No `dotnet build`, `dotnet publish`, native PKG execution, signing/notarization or GitHub access was performed here. The supplied Mac `pwsh Build-Release.ps1` run is the authoritative parser/build confirmation.
