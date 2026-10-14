# LocalGPT 5.5.5 — reviewed file checklist

Each changed or added source file is accounted for here. This release checklist supplements (and does not replace) the **Project Maintenance → Learning checklist per file**: after scanning the configured workspace, that existing UI creates tracked-file learning tasks. A file's presence/hash is not equivalent to completed semantic Council review.

| File | Review obligation | Source status |
|---|---|---|
| `src/LocalGPT/BusinessObjects/GameStudioModels.cs` | World, time, sprite, HUD, small patch, proposal and draft schemas | Added; source-audited |
| `src/LocalGPT/BusinessObjects/GameProjectModels.cs` | Optional compiled scene on a completed project build | Updated; source-audited |
| `src/LocalGPT/Services/GameSceneEngine.cs` | Fixed ticks, bounded catch-up, semantic curation and text preview | Added; source-audited |
| `src/LocalGPT/Interfaces/IGameStudioService.cs` | Explicit open/save/review/close state machine | Added; source-audited |
| `src/LocalGPT/Services/GameStudioService.cs` | Durable mutable artifacts, immutable revision file snapshots, human gate | Added; source-audited |
| `src/LocalGPT/Interfaces/IGameGodotWorkspaceService.cs` | Public approved Godot sync boundary | Added; source-audited |
| `src/LocalGPT/Services/GameGodotWorkspaceService.cs` | Generate/import/export, conflict hashes, path ownership | Added; source-audited |
| `src/LocalGPT/GameTemplates/GodotSceneRuntime.cs.txt` | Editable general C# runtime with per-row scroll/pixel sprites | Added; template inspection only |
| `src/LocalGPT/Services/GameStudioDxAiFunctions.cs` | Read, full/patch propose, exact proposal lookup, independent review; standard confirmation policy | Added; source-audited |
| `src/LocalGPT/Services/GameProjectService.cs` | Compile only a closed scene and bound artifact size | Updated; source-audited |
| `src/LocalGPT/Program.ServiceRegistration.cs` | Scoped service DI | Updated; source-audited |
| `src/LocalGPT/Components/Pages/GameStudio.razor` | Tree/split/form/grid/memo/approval UI | Added; Razor-audited |
| `src/LocalGPT/Components/Pages/GameStudio.razor.cs` | Handler error boundaries, AI review, source editor and preview | Added; Razor/async-audited |
| `src/LocalGPT/Components/Pages/GameStudio.razor.css` | Viewport-bounded panel and responsive preview | Added; source-reviewed |
| `src/LocalGPT/wwwroot/js/localgpt-game-studio.js` | JIT pixel projection of authored sprites and animation | Added; Node syntax-checked |
| `src/LocalGPT/Components/Layout/NavMenu.razor` | Discoverability in maintained navigation | Updated; source-audited |
| `src/LocalGPT/Components/App.razor` | Versioned pixel JS import and retained game/chat scripts | Updated; source-audited |
| `src/LocalGPT/Components/Shared/ChatMicrophone.razor` | 5.5.5 versioned import, unchanged microphone behavior | Version-only update |
| `src/LocalGPT/LocalGPT.csproj` | 5.5.5 version and embedded Godot template | Updated; source-audited |
| `build/Assert-InteractiveServerRenderModes.ps1` | Explicit new InteractiveServer page, existing guard intact | Updated; source-audited |
| `build/async-continuation-policy.json` | Exact GameStudio renderer-affine methods (no wildcard) | Updated; async audit passed |
| `build/audit_game_studio_5_5_5.py` | New source-only contract and authoring demo sanity checks | Added; to execute |
| `build/audit_data_management_5_5_1.py` | Retained data/Council guard and release number | Updated; source audit passed |
| `build/audit_data_management_workspace_5_5_3.py` | Retained editor/DevExpress/CRUD guard and version | Updated; source audit passed |
| `build/audit_data_management_text_service_5_5_4.py` | Retained text ownership and primary-key guard | Updated; source audit passed |
| `build/audit_learning_checklist_5_4_9.py` | Retained per-file learning guard and release number | Updated; source audit passed |
| `docs/examples/game-studio-parallax-scene.json` | Valid authored sample: sprites, collision masks, per-row parallax and palette | Added; JSON/geometry audit |
| `docs/reference/game-studio-revisioned-godot.md` | Exact editing, revision lifecycle and known limitations | Added; documentation review |
| `CHANGELOG-v5.5.5-GAME-STUDIO-WORLD-TIME-GODOT.md` | Release rationale, changes and explicit limitations | Added |
| `RELEASE-CHECKLIST-v5.5.5.md` | This complete per-file traceability list | Added |

## Release checks

- [x] Existing DevExpress form/split/tree/Grid controls preserved; new Game Studio editor uses the same architecture.
- [x] Existing one-use Council human approval gate kept; AI changes require explicit human direction.
- [x] Existing prompt/regex ownership and project change permissions unchanged.
- [x] Project artifacts update during an open draft; only explicit **Close** creates a Project revision.
- [x] Source-only async, resilience, Razor, transient state, DevExpress and retained previous-release contracts validated (see changelog).
- [ ] Visual Studio .NET compiler/build — **deliberately not run** in this environment.
- [ ] Godot C# editor import/play test — requires the target Godot/.NET installation.
- [ ] Full runtime interaction and device/performance smoke tests — to be completed in the owner's environment.
