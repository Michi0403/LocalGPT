# LocalGPT 5.5.7 — per-file release checklist

Each touched file has its own review record. These release entries complement the UI-visible Project Maintenance learning checklist; no change is marked as semantically learned without source evidence.

| Changed or added file | Required review | Result |
|---|---|---|
| `src/LocalGPT/Services/GameStudioService.cs` | Repair CS1026 while preserving Godot path traversal rules | Closing parenthesis restored; source contract checked |
| `src/LocalGPT/Components/Pages/GameStudio.razor` | Put scoped, non-prerendered InteractiveServer directive first | Directive moved; source contract checked |
| `build/Assert-InteractiveServerRenderModes.ps1` | Register Game Studio in both protected render contracts | Explicit routed-page list corrected; no bypass |
| `src/LocalGPT/wwwroot/js/localgpt-game-studio.js` | Add owned diagnostics marker without changing the renderer | Marker added; Node syntax and guard checked |
| `build/javascript-diagnostics-files.sha256` | Record reviewed normalized hash for Game Studio JS | 26-file full inventory verified |
| `build/audit_game_studio_5_5_5.py` | Retain AI/Godot/DevExpress feature contract; add checks for all three defects | Strengthened source-only contract |
| `src/LocalGPT/LocalGPT.csproj` | Advance source release version to 5.5.7 | Version checked |
| `src/LocalGPT/Components/App.razor` | Advance existing three script cache identifiers | Version-only edit |
| `src/LocalGPT/Components/Shared/ChatMicrophone.razor` | Keep microphone JS import cache aligned with release | Version-only edit |
| `build/audit_data_management_5_5_1.py` | Maintain retained Data Management/Council contract for 5.5.7 | Version contract updated |
| `build/audit_data_management_workspace_5_5_3.py` | Retain compact service-owned CRUD/editor audit | Version contract updated |
| `build/audit_data_management_text_service_5_5_4.py` | Retain text ownership and protected SQLite editor checks | Diagnostic version updated |
| `build/audit_learning_checklist_5_4_9.py` | Retain UI-visible file learning progress contract | Version contract updated |
| `CHANGELOG-v5.5.7-GAME-STUDIO-BUILD-REPAIR.md` | Record exact failures, corrections and limits | Added |
| `RELEASE-CHECKLIST-v5.5.7.md` | Ensure one tracked entry per changed file | Added |

## Acceptance

- [x] Game Studio first-directive failure corrected.
- [x] Render guard's independent non-prerendered route registry corrected.
- [x] Game Studio JS SHA-256 entry and diagnostics marker restored.
- [x] `GameStudioService.cs` condition properly closed without relaxing path validation.
- [x] Cross-platform build gate and the previous release's platform service corrections preserved.
- [x] Current version 5.5.7, compatible with one-digit version slot rule.
- [x] All 22 protected InteractiveServer components have correct first directives and routed-page modes.
- [x] All 26 maintained browser JavaScript files have verified diagnostics markers, exception handling and exact normalized hashes.
- [x] Source-only Game Studio, Data Management, learning checklist, DevExpress, Razor, transient UI, async, service resilience, runtime text and general architecture audits passed.
- [ ] Full .NET compiler build on Windows — outside this environment.
- [ ] Full cross-RID release/publish and Godot runtime interaction — outside this environment.
