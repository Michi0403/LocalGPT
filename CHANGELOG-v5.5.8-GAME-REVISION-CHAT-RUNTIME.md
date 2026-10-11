# LocalGPT 5.5.8 — approved Game Studio revisions playable in Chat

## Completed

1. **Actual Project business objects, not example JSON:** `IGameProjectService` enumerates the database-backed, approved Game Studio revisions. It reads the exact `ProjectStructureJson.scene` that `GameStudioService.CloseRevisionAsync` stored on user confirmation. Only nonempty, semantically curated scenes qualify; corrupt and unapproved revisions are excluded with diagnostics. The documentation example scene is never used as runtime input.
2. **Select historical revisions in the regular ASCII chat:** A `DxListBox` in `ChatGameConsole` shows game-project identity, revision name and a stable revision-ID prefix. Refresh and Play buttons use DevExpress, and selection does not edit or commit project files. The Play action launches the exact selected revision with the current conversation and Council run context, not the latest unrelated project build.
3. **Shared authoritative runtime:** `CouncilGameSessionService` now retains the authored scene, immutable project revision identity, and separate mutable `GameSceneClock`. `CouncilGameDirectorService` still approves legal actions. Approved movement/turn/use input advances the existing generic `GameSceneEngine` at its configured world time/ticks; frame text comes from the curated scene instead of a hardcoded corridor. One optional `GameSceneActor.PlayerControlled` flag enables actual collision-checked actor movement and a following camera; without a marked actor scenes remain explorable with free-camera controls. Curation rejects ambiguous multiple player actors. Legacy corridor/Green Dragon games are unchanged.
4. **ASCII and Pixel presentation:** The regular Chat console continues to render accessible, copyable ASCII and now uses the existing Game Studio pixel/scanline renderer for authored sprites, parallax, palettes and HUD in Pixel mode. The authored scene and clock cross Blazor/JavaScript only when the authoritative scene tick changes. No new demo game class, Godot SDK dependency or server-side C# presentation duplication was added.
5. **GameDirector prediction isolation:** Corridor/DOOM-specific creature and door predictions no longer appear in an authored Game Studio world. The same GameDirector approval still governs controls and errors.
6. **Godot source clarity:** `GameTemplates/GodotSceneRuntime.cs.txt` is deliberately an embedded Godot-C# *generation template*, not a LocalGPT source file. `GameGodotWorkspaceService` writes it into the exported/revisioned Godot worktree as `src/LocalGptSceneRuntime.cs`, and the generated `Main.tscn` refers to that compiled Godot script. In-process Chat scene execution is instead provided by the compiled `GameSceneEngine.cs`. Both use the project-owned scene model.

## Release maintenance and acceptance

- Advanced LocalGPT **5.5.7 → 5.5.8**, updated cache identifiers and retained source acceptance scripts; no version slot reaches 10.
- Preserved the current scoped Blazor/DevExpress layout, permission approval flows, service injection boundaries and existing build guards. Registered only the two new renderer-affine Chat callbacks in the existing async policy.
- Added the standalone source acceptance script `build/audit_game_revision_chat_5_5_8.py` covering persistence, exact revision launch, live scene runtime, Chat UI, Godot template generation and authored pixel path.
- Updated the maintained JavaScript diagnostic SHA-256 entry for the changed game console script.
- See `RELEASE-CHECKLIST-v5.5.8.md` for an entry for **every changed or added source file** and exact test/verification limits.

## Verification boundary

All recorded checks are **source-only**, including Node JavaScript syntax checking; the environment has no .NET or Godot compiler and no GitHub access was attempted. Windows Visual Studio/MSBuild, Godot export and actual interactive game-controller testing must be performed on the owner's PC before considering the release binary-verified. PublisherStudio was not modified.
