# LocalGPT 4.4.7

LocalGPT 4.4.7 is a narrow source/build repair on top of 4.4.6.

The 4.4.6 ASCII integration work changed `src/LocalGPT/wwwroot/js/localgpt-game-console.js`, but its reviewed normalized SHA-256 entry in `build/javascript-diagnostics-files.sha256` was not refreshed. The supplied Windows build therefore correctly stopped in `Assert-JavaScriptDiagnostics.ps1` before compilation. 4.4.7 updates that diagnostics inventory entry without changing the 4.4.6 JavaScript behavior.

The Council-run game correlation/prebootstrap repair, promoted game functions, DevExpress terminal selector routing, runtime control modes, modal sizing, configurable Doom campaign, explicit async continuation policy, required ASCII-surface handling, documentation effects, and restored 4.4.2 shell remain preserved.

No .NET build, restore, publish, NuGet operation, GitHub access, GitHub API call, or other online repository access was performed in this environment. See `CHANGELOG-v4.4.7-JAVASCRIPT-DIAGNOSTICS-MANIFEST-REPAIR.md` and `VALIDATION-v4.4.7-source.md`.
