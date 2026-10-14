# LocalGPT 5.5.6 — per-file release checklist

This is the release change inventory. The app's existing **Project Maintenance → file learning checklist** is the separate persistent, UI-visible source-review workflow: scanning creates a task per tracked file, but does not falsely mark it learned without evidence.

| Changed/added file | Source-review task | Result |
|---|---|---|
| `src/LocalGPT/Services/LearningRoundService.Checklist.cs` | Remove unauthorized host-OS branch without weakening path containment, link safety or checklist persistence | Updated; cross-platform audit passed |
| `src/LocalGPT/Services/LearningRoundService.cs` | Inject the existing platform runtime service and document its dependency | Updated; DI source reviewed |
| `src/LocalGPT/Services/GameGodotWorkspaceService.cs` | Preserve descendant-only Godot path and reparse-point checks through the platform service | Updated; cross-platform audit passed |
| `Directory.Build.targets` | Fail normal Build and Publish on the same cross-platform audit, with no policy bypass | Updated; parsed and asserted by audit |
| `build/audit_cross_platform_boundaries.py` | Detect architectural leaks and assert its own enforced MSBuild target | Strengthened; audit passed |
| `src/LocalGPT/LocalGPT.csproj` | Increment version to 5.5.6 | Updated; source checked |
| `src/LocalGPT/Components/App.razor` | Keep three frontend scripts versioned with the release | Cache-only version update |
| `src/LocalGPT/Components/Shared/ChatMicrophone.razor` | Keep dynamic microphone import versioned with the release | Cache-only version update |
| `build/audit_game_studio_5_5_5.py` | Retain complete Game Studio acceptance contract with 5.5.6 identity | Version expectations updated; passed |
| `build/audit_data_management_5_5_1.py` | Retain Data Management, Council and locale contract | Version expectations updated; passed |
| `build/audit_data_management_workspace_5_5_3.py` | Retain CRUD/editor/workspace regression contract | Version expectations updated; passed |
| `build/audit_data_management_text_service_5_5_4.py` | Retain safe SQL editor/text-ownership contract | Messages updated; passed |
| `build/audit_learning_checklist_5_4_9.py` | Retain file learning, schema, Council and UI regression contract | Version expectations updated; passed |
| `CHANGELOG-v5.5.6-CROSS-PLATFORM-BUILD-GATE.md` | Record the exact failure, architectural correction and validation limitation | Added |
| `RELEASE-CHECKLIST-v5.5.6.md` | Record one review entry per changed file, including the release artifacts | Added |

## Acceptance

- [x] Original reported `LearningRoundService.Checklist.cs` platform violation removed.
- [x] Newer Godot workspace source audited and its separate platform violation removed.
- [x] No platform-audit allowlist expansions or guard disables.
- [x] Build and Publish execute the same mandatory platform policy.
- [x] Version advanced to 5.5.6; single-digit minor/patch rule satisfied.
- [x] Source-only existing Game Studio, per-file checklist and Data Management contracts pass.
- [x] Source-only architecture, Razor maintenance, DevExpress, UI ownership, async boundaries, service resilience and runtime text ownership audits pass.
- [ ] Windows/Unix `.NET` build — not available in this environment; **must run on owner host**.
- [ ] Publish across the desired RIDs — not available in this environment; **must run on owner host**.
- [ ] End-device runtime/interactive verification — **must run on owner hardware**.
