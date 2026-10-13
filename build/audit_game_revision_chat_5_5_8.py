#!/usr/bin/env python3
"""Source-only contract for approved Game Studio revisions playing through Chat's shared GameDirector."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
APP = ROOT / 'src' / 'LocalGPT'


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        raise SystemExit(f'Game revision Chat integration: required source missing: {path}')
    return target.read_text(encoding='utf-8-sig')


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f'Game revision Chat integration: {message}')


version = read('src/LocalGPT/LocalGPT.csproj')
require('<Version>5.6.3</Version>' in version, 'release version not advanced')
require('EmbeddedResource Include="GameTemplates/GodotSceneRuntime.cs.txt"' in version,
        'Godot C# source template is not embedded for generation into Godot projects')

workspace = read('src/LocalGPT/Services/GameGodotWorkspaceService.cs')
for token in ('ReadGodotTemplate()', 'GetManifestResourceStream',
              '"src/LocalGptSceneRuntime.cs"', '"scenes/Main.tscn"', '"scene.json"'):
    require(token in workspace, f'Godot project generation is missing {token}')

studio = read('src/LocalGPT/Services/GameStudioService.cs')
for token in ('CloseRevisionAsync', 'RequireApproval(userConfirmed)',
              'ProjectStructureJson = JsonSerializer.Serialize', 'Scene = draft.Scene',
              'await architecture.SaveRevisionAsync', 'UserConfirmed = true'):
    require(token in studio, f'user-approved scene revision persistence missing {token}')

service = read('src/LocalGPT/Services/GameProjectService.cs')
for token in ('ListPlayableRevisionsAsync', 'LaunchRevisionAsync',
              'ReadPlayableRevisionScene', 'IsUserApproved', 'TryGetProperty("scene"',
              'engine.Curate(scene)', 'SourceRevisionId = revision.Id',
              'GameKey = profile?.GameKey', 'games.StartAsync'):
    require(token in service, f'project business-object revision launch missing {token}')
require('game-studio-parallax-scene.json' not in service,
        'demo examples cannot be game runtime data sources')

console = read('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
for token in ('<DxListBox Data="@playableRevisions"', 'TextFieldName="@nameof(ProjectGameRevisionChoice.DisplayName)"',
              'Click="StartSelectedRevisionAsync"', 'Click="RefreshPlayableRevisionsAsync"',
              'GameProjects.ListPlayableRevisionsAsync()', 'GameProjects.LaunchRevisionAsync(',
              'ConversationId = ConversationId', 'CouncilRunId = CouncilRunId',
              'snapshot.Scene, snapshot.SceneClock', 'localGptGameConsole.setAuthoredScene'):
    require(token in console, f'Chat DevExpress revision player missing {token}')

director = read('src/LocalGPT/Services/CouncilGameDirectorService.cs')
require('if (context.Session.Scene is null)' in director,
        'legacy Doom-only actor predictions must not contaminate user-authored worlds')

actor_models = read('src/LocalGPT/BusinessObjects/GameStudioModels.cs')
engine = read('src/LocalGPT/Services/GameSceneEngine.cs')
require('public bool PlayerControlled { get; set; }' in actor_models,
        'data-authored actor control flag is missing')
require('scene.Actors.Count(actor => actor.PlayerControlled) > 1' in engine,
        'curation must reject ambiguous multi-actor direct control')

game = read('src/LocalGPT/Services/CouncilGameSessionService.cs')
snapshot = read('src/LocalGPT/Services/CouncilGameSessionService.Snapshots.cs')
renderer = read('src/LocalGPT/Services/CouncilGameSessionService.Rendering.cs')
runtime = read('src/LocalGPT/Services/CouncilGameSessionService.SceneRuntime.cs')
for token in ('gameSceneEngine.Curate(authoredScene)', 'ApplySceneControl(session, decision.NormalizedAction)',
              'Scene = authoredScene', 'SceneClock = authoredScene is null', 'SourceRevisionId = definition?.SourceRevisionId'):
    require(token in game, f'live runtime scene ownership missing {token}')
for token in ('SceneClock = session.SceneClock', 'SourceRevisionId = session.SourceRevisionId'):
    require(token in snapshot, f'scene clock/revision snapshot missing {token}')
require('gameSceneEngine.Preview(session.Scene, session.SceneClock)' in renderer,
        'Chat ASCII renderer does not project the curated scene')
require('gameSceneEngine.Advance(scene, clock, 0, takeTurn: true)' in runtime,
        'approved actions are not advancing authored world-time ticks')
require('scene.Actors.FirstOrDefault(actor => actor.PlayerControlled)' in runtime,
        'authoritative controls must move the authored player actor when configured')

javascript = read('src/LocalGPT/wwwroot/js/localgpt-game-console.js')
for token in ('setAuthoredScene(id, scene, clock)', 'localgptGameStudio?.draw',
              'state.authoredScene', 'state.authoredClock'):
    require(token in javascript, f'Chat pixel renderer does not use authored scene data: {token}')

registration = read('src/LocalGPT/Program.ServiceRegistration.cs')
require('AddSingleton<GameSceneEngine>()' in registration,
        'stateless scene engine must be compatible with singleton GameDirector session ownership')
print('LocalGPT 5.6.3 revisioned Chat play audit passed: approved project data, exact revision identity, DevExpress list, shared controls, curated scene ticks, ASCII/pixel projection and embedded Godot template.')
