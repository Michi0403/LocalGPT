# LocalGPT 4.4.7 — JavaScript diagnostics manifest build repair

LocalGPT 4.4.7 is a narrow build-repair release on top of 4.4.6.

## Fixed

- Refreshed `build/javascript-diagnostics-files.sha256` for the reviewed 4.4.6 change to `src/LocalGPT/wwwroot/js/localgpt-game-console.js`.
- The JavaScript itself is unchanged from 4.4.6; this release only brings the maintained diagnostics inventory back in sync so direct IDE/MSBuild builds no longer fail `Assert-JavaScriptDiagnostics.ps1` before compilation.
- Advanced LocalGPT release metadata and browser cache keys to 4.4.7.

## Preserved

All 4.4.6 ASCII integration repairs remain intact: Council-run game correlation and prebootstrap, promoted game-function recovery, DevExpress terminal interaction routing, runtime Human / Human + AI / AI control selection, modal sizing, configurable ten-level Doom campaign data, explicit `ConfigureAwait(true/false)` continuation ownership, required ASCII-surface handling, and the restored 4.4.2 navigation shell.

No `dotnet` command or GitHub/online repository access was used while preparing this source repair.
