# LocalGPT 4.3.4 source validation

## Validation boundary

This validation was performed against the complete LocalGPT 4.3.4 source tree without running `dotnet`. The environment was used only for source/static checks and archive verification. GitHub and the GitHub API were not accessed.

## Passed checks

- `build/audit_application_architecture.py --root . --product localgpt --mode all`
- `build/audit_cross_platform_boundaries.py`
- `build/audit_async_continuations.py --source-root src/LocalGPT`
- `build/audit_service_resilience.py --root . --product localgpt`
- `build/audit_powershell_variable_interpolation.py`
- `build/audit_chat_ascii_console.py --root .`
- `node --check docs/templates/localgpt/public/main.js`
- `node --check src/LocalGPT/wwwroot/js/localgpt-game-console.js`
- `node --check src/LocalGPT/wwwroot/js/localgpt-chat-ui.js`
- `build/audit_release_4_3_4.py`
- XML parsing for maintained project/target files and JSON parsing for the DocFX configuration through the release audit.
- Authored documentation theme bytes match both in-app help-doc copies and the tracked Pages archive through the release audit.
- InteractiveServer render-mode ownership remains on the established owner-component set; the Error page remains static.

## Release-specific source assertions

The 4.3.4 audit verifies the canonical chat transcript, direct Contextual ASCII fun action, edited-primary Ollama precedence, distinct hot-seat runtime roles with fallback, Reactive ASCII Gameplay quick action, map/scenario replay controls, conversation-bound game lifecycle, stable game viewport contract, dynamic-sky handoff, guaranteed satellites, attached-node Mermaid rendering, bounded Mermaid retries, required documentation PDF setting, and one-digit version rollover rules.

The historical `audit_ascii_experience_4_2_5.py` script was not used as a 4.3.4 release gate because it asserts exact implementation strings from the older 4.2.5 ownership/guidance shape that have since been superseded. The maintained 4.3.4 release audit and current architecture/ASCII-console contracts cover the active implementation instead.

## Not claimed

No compiler, runtime-browser, installer, publish, PDF-render, signing, notarization, or platform-package execution is claimed by this source-only validation.
