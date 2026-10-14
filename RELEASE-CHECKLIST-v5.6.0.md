# LocalGPT 5.6.0 — per-file repair and learning checklist

Every changed source, test and release file has a separate entry. The retained Project Maintenance learning checklist and prior releases remain unchanged. No PublisherStudio source file was modified.

| Changed or added file | Individual maintenance check | Status |
|---|---|---|
| `src/LocalGPT/Services/ConsoleCommandService.cs` | Preserve actual redirected PowerShell CRLF stdout/stderr; render user-confirmed command input while protecting automated command privacy | Fixed; source contract added |
| `src/LocalGPT/Components/Shared/ChatGameConsole.razor` | Empty list must not inflate toolbar; retain `DxListBox`; stable enum shell selection; guide/fullscreen buttons retained | Fixed; DevExpress source audit passed |
| `src/LocalGPT/Components/Shared/ChatGameConsole.razor.css` | Compact collapsed Games selector and floating full-viewport HUD without offscreen gutters | Fixed; stylesheet reviewed |
| `src/LocalGPT/wwwroot/css/site.css` | Non-fullscreen ASCII popup must fill its nested FormLayout owners; fullscreen must not reserve an invisible header row; unrelated dialogs unaffected | Fixed; scoped style audited |
| `src/LocalGPT/BusinessObjects/InitialSetupAssistantModels.cs` | Maintain explicit, nullable attributed memory-plan fields on model recommendation | Fixed; service object ownership retained |
| `src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs` | Calculate per-model, per-reviewed-host dedicated/shared/RAM plans only when source model requirement is known | Fixed; no invented measurements |
| `src/LocalGPT/Services/InitialSetupAssistantService.cs` | Preserve memory-plan fields across multi-host, endpoint-attributed recommendation copies | Fixed; source contract added |
| `src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor` | Show per-model RAM, GPU VRAM and unified budget after the user-approved lookup and in DevExpress grid | Fixed; source contract added |
| `src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor.css` | Fit memory figures and caveat into the existing model cards without stretching columns | Fixed; style reviewed |
| `src/LocalGPT/LocalGPT.csproj` | Version rollover 5.5.9 → 5.6.0; never x.y.10 | Fixed |
| `src/LocalGPT/Components/App.razor` | Align JavaScript game, studio and chat UI cache versions | Fixed |
| `src/LocalGPT/Components/Shared/ChatMicrophone.razor` | Align imported microphone module cache version | Fixed |
| `build/audit_ascii_console_memory_5_6_0.py` | Protect CRLF, masked command boundaries, DevExpress controls, popup/fullscreen and model memory display | Added; source-only check |
| `build/audit_game_studio_5_5_5.py` | Maintain the previous Game Studio compiler and scene contracts | Version updated; passed |
| `build/audit_game_revision_chat_5_5_8.py` | Preserve approved revision playback, DevExpress list and authored runtime linkage | Version updated; passed |
| `build/audit_learning_checklist_5_4_9.py` | Preserve per-file learning checklist contract | Version updated; passed |
| `build/audit_data_management_5_5_1.py` | Preserve service-owned Data Management/Council contracts | Version updated; passed |
| `build/audit_data_management_workspace_5_5_3.py` | Preserve service-owned CRUD and editor navigation contracts | Version updated; passed |
| `build/audit_data_management_text_service_5_5_4.py` | Preserve text-service ownership/SQLite key handling | Version updated; passed |
| `CHANGELOG-v5.6.0-ASCII-OPERATOR-FULLSCREEN-MODEL-MEMORY.md` | Root causes, intended UX, safety properties, verification limitations | Added |
| `RELEASE-CHECKLIST-v5.6.0.md` | One checklist entry per changed file and reproducible acceptance instructions | Added |

## Operator and Install user acceptance on Windows

- [ ] Open `/chat` → ASCII console with zero approved Game Studio revisions. Header stays one compact control row; `Games (0)` can expand to show Refresh without an empty list.
- [ ] Create an approved revision, reopen `Games (N)` and select/launch it using the retained DevExpress `DxListBox`.
- [ ] Enter Operator mode, select each installed shell in the DevExpress ComboBox and issue `dir`/`Get-ChildItem` or `pwd`/`ls` as appropriate. The exact entered command and output appear in the same console, with job completion. Other providers' command arguments remain masked.
- [ ] In normal DxPopup, ASCII stage fills the dialog below the header without a large blank region; general editor popups still scroll.
- [ ] Enter true fullscreen, hide guide, verify stage fills viewport; guide/fullscreen/close remain available. Exit fullscreen and restore all controls.
- [ ] On `/Install`, detect/review dedicated VRAM and system memory, explicitly opt into CanIRun.ai and query models. Each scored model shows source-backed memory budget with dedicated VRAM, PC RAM and unified values or an explicit unknown; shared memory is not summed twice.
- [x] Source-only Game Studio, revisioned Chat, Data Management, DevExpress, architecture, cross-platform, async and service-resilience checks completed.
- [ ] Actual MSBuild/.NET 10 compile and interactive Windows test — unavailable in this environment; user verification required.
