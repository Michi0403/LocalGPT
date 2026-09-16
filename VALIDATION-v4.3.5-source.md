# LocalGPT 4.3.5 source validation

## Validation boundary

This validation was performed against the complete LocalGPT 4.3.5 source tree without running `dotnet`. The current environment was used only for source/static checks and archive verification. GitHub and the GitHub API were not accessed.

The user's real 4.3.4 build output reported two concrete maintenance failures: a Razor `RZ9986` mixed-content `CssClass` expression and two new component-owned `Split`/`string.Join` operations rejected by LocalGPT's text-service ownership guard. 4.3.5 removes both source patterns. A compiler-clean result is not claimed because no .NET compiler was run here.

## Passed checks

- `build/audit_application_architecture.py --root . --product localgpt --mode all`
- `build/audit_cross_platform_boundaries.py`
- `build/audit_async_continuations.py --source-root src/LocalGPT`
- `build/audit_service_resilience.py --root . --product localgpt`
- `build/audit_powershell_variable_interpolation.py`
- `build/audit_chat_ascii_console.py --root .`
- JavaScript syntax: `node --check docs/templates/localgpt/public/main.js`
- JavaScript syntax: `node --check src/LocalGPT/wwwroot/js/localgpt-game-console.js`
- JavaScript syntax: `node --check src/LocalGPT/wwwroot/js/localgpt-chat-ui.js`
- Python emulation of `Assert-TextServiceOwnership.ps1` against `build/text-service-ownership-baseline.json`: no new component/controller direct string/regex operations.
- `build/audit_release_4_3_5.py`
- XML parsing for maintained project/target files and JSON parsing for DocFX configuration through the release audit.
- Authored documentation theme bytes match both in-app help-doc copies and the tracked Pages archive through the release audit.
- InteractiveServer render-mode ownership remains on the established owner-component set; the Error page remains static.

## Release-specific source assertions

The 4.3.5 release audit verifies the one-digit version contract, required documentation PDF setting, the 4.3.4 canonical-chat/provider/gameplay repairs, the direct Contextual ASCII fun control, the non-mixed Razor `CssClass` binding, service-owned hot-seat/scenario text normalization, bounded Ollama presentation batching, the artifact-free documentation sky, disabled backdrop filtering/pseudo-nebula layer, guaranteed longer-travel satellites, Mermaid attached-node rendering, documentation asset parity and tracked Pages archive integrity.

## Not claimed

No .NET compiler/build, runtime browser, installer, publish, PDF render, signing, notarization, or platform-package execution is claimed by this source-only validation.
