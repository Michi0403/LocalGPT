# LocalGPT 5.5.8 — per-file review and learning checklist

The checklist records each changed file independently. These evidence entries complement the interactive Project Maintenance learning checklist and **do not** mark files semantically learned without real source/run evidence.

| Changed or added file | Maintenance task | Source review |
|---|---|---|
| `src/LocalGPT/BusinessObjects/GameProjectModels.cs` | Exact approved project/revision selection business object | Reviewed; historical revision-ID label |
| `src/LocalGPT/BusinessObjects/GameStudioModels.cs` | Optional persisted per-scene controlled-actor flag | Reviewed; JSON-only domain property, no migration |
| `src/LocalGPT/BusinessObjects/CouncilGameModels.cs` | Authoritative scene/clock/revision identity on state and snapshot | Reviewed; no database schema change |
| `src/LocalGPT/Interfaces/IGameProjectService.cs` | Enumerate and launch approved revisions through domain service | Reviewed; async service contract |
| `src/LocalGPT/Services/GameProjectService.cs` | Use real database project revision scene, curate, launch with Council context | Reviewed; approval and error logging retained |
| `src/LocalGPT/Services/GameSceneEngine.cs` | Reject ambiguous multiple controlled actors in curator | Reviewed; existing world engine retained |
| `src/LocalGPT/Services/CouncilGameDirectorService.cs` | Keep legacy Doom-only prediction out of authored scene worlds | Reviewed; approval gate preserved |
| `src/LocalGPT/Services/CouncilGameSessionService.cs` | Start and advance generic scene sessions alongside legacy runtimes | Reviewed; scene clock and legacy paths distinct |
| `src/LocalGPT/Services/CouncilGameSessionService.Rendering.cs` | Render project scenes using engine ASCII preview | Reviewed; preexisting renderers retained |
| `src/LocalGPT/Services/CouncilGameSessionService.SceneRuntime.cs` | Generic player-actor/free-camera movement, collision and fixed-turn clock execution | Added; no game-specific class |
| `src/LocalGPT/Services/CouncilGameSessionService.Snapshots.cs` | Copy mutable clock and carry revision identity into UI | Reviewed; style runs isolated |
| `src/LocalGPT/Program.ServiceRegistration.cs` | Singleton-safe DI lifetime for stateless generic scene engine | Reviewed; no new globals |
| `src/LocalGPT/Components/Shared/ChatGameConsole.razor` | DevExpress revision list and shared game launch; scene render interop | Reviewed; component safety directives retained |
| `src/LocalGPT/Components/Shared/ChatGameConsole.razor.css` | Bound the revision selector in existing console header | Reviewed; normal layout retained |
| `src/LocalGPT/wwwroot/js/localgpt-game-console.js` | Use Game Studio scene/pixel renderer when revision data exists | Node syntax passed; legacy ASCII fallback retained |
| `build/javascript-diagnostics-files.sha256` | Update maintained normalized game-console JS SHA-256 | Recomputed; no exemption |
| `build/async-continuation-policy.json` | Register only two exact renderer-affine Chat helpers | Async continuation audit passed |
| `src/LocalGPT/LocalGPT.csproj` | Update release version and retain embedded Godot C# template | Version and resource verified |
| `src/LocalGPT/Components/App.razor` | Advance JavaScript cache IDs | Reviewed; loading order preserved |
| `src/LocalGPT/Components/Shared/ChatMicrophone.razor` | Advance retained module cache ID | Reviewed; no microphone behavior edit |
| `build/audit_game_revision_chat_5_5_8.py` | Lock down the business-object → GameDirector → Chat/Pixel wiring | New audit passed |
| `build/audit_game_studio_5_5_5.py` | Keep the prior Godot, syntax and Game Studio regression checks | Passed; version refreshed |
| `build/audit_data_management_5_5_1.py` | Retain full Data Management/Council contract | Passed; version refreshed |
| `build/audit_data_management_workspace_5_5_3.py` | Retain service-owned editor/CRUD contract | Passed; version refreshed |
| `build/audit_data_management_text_service_5_5_4.py` | Retain text-service and SQLite editing policy | Passed; diagnostic version refreshed |
| `build/audit_learning_checklist_5_4_9.py` | Keep UI-visible per-file learning checklist audit current | Passed; version refreshed |
| `docs/reference/ascii-game-authoring.md` | Explain exact project revision source and embedded-vs-compiled Godot distinction | Reviewed; no example injection |
| `CHANGELOG-v5.5.8-GAME-REVISION-CHAT-RUNTIME.md` | Record corrections, intended behavior and build limitations | Added |
| `RELEASE-CHECKLIST-v5.5.8.md` | One independent review entry per changed file | Added |

## Acceptance evidence

- [x] The checked Project business-object scene is the sole authored input; documentation sample JSON is never loaded for gameplay.
- [x] All finalized, approved historical revisions are selectable by exact GUID pair; unapproved, empty or invalid scenes do not appear.
- [x] DevExpress list and buttons retain the existing shared Chat layout, control gate and service ownership.
- [x] In-process game runtime uses existing generic `GameSceneEngine`, not a hardcoded sample game or uncompiled Godot template.
- [x] Pixel mode delegates authored art to the maintained Game Studio pixel projection.
- [x] Legacy DOOM-specific prediction isolated from generic project scenes.
- [x] All prior source-only Game Studio, Data Management, learning checklist, cross-platform, DevExpress, Razor maintenance, transient UI-state, async continuation/architecture, service resilience, runtime-text and general architecture validations passed.
- [x] JavaScript `node --check` passed and the changed diagnostic hash was recomputed.
- [x] Release number uses the required `5.5.8` one-digit minor and patch slots.
- [ ] Windows PowerShell 5.1 maintenance target suite, MSBuild/.NET compiler build and publish — **not available** in this environment.
- [ ] Actual Blazor circuit, GameDirector control playtest and Godot C# export/runtime — **must be verified on user machine**.
