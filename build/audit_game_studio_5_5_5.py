#!/usr/bin/env python3
"""Retained source-only Game Studio acceptance contract; never invokes dotnet or Godot."""
from __future__ import annotations

import json
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
APP = ROOT / 'src' / 'LocalGPT'


def require(condition: bool, detail: str) -> None:
    if not condition:
        raise SystemExit(f"LocalGPT 5.6.6 Game Studio acceptance failed: {detail}")


def source(relative: str) -> str:
    path = APP / relative
    require(path.is_file(), f"missing maintained source file {relative}")
    return path.read_text(encoding='utf-8-sig')


require('<Version>5.6.6</Version>' in source('LocalGPT.csproj'), 'version is not 5.6.6')
require('EmbeddedResource Include="GameTemplates/GodotSceneRuntime.cs.txt"' in source('LocalGPT.csproj'), 'Godot C# template must be embedded')

models = source('BusinessObjects/GameStudioModels.cs')
for token in ('GameSceneDocument', 'FixedTicksPerSecond', 'WorldSecondsPerRealSecond', 'SecondsPerTurn',
              'ChunkSize', 'BlocksDecoration', 'GameSceneSprite', 'GameSceneAnimation', 'GameSceneHudElement',
              'GameScenePaletteKey', 'GameSceneTimedEvent', 'GameStudioDraft', 'ClosedRevisionId',
              'GameSceneProposal', 'GameSceneReview'):
    require(token in models, f'authoring model omitted {token}')

engine = source('Services/GameSceneEngine.cs')
for token in ('Curate(', 'Advance(', 'Preview(', 'FractionalRealSeconds', 'state.Tick++',
              'state.WorldSeconds = state.Tick * deltaReal * scene.WorldSecondsPerRealSecond',
              'blocked.Contains', 'SemanticOcclusion', 'PixelPalette', 'ScrollCellsPerWorldSecondX',
              'ScrollCellsPerWorldSecondY', 'hud.Format.Replace'):
    require(token in engine, f'generic simulation or independent quality checks missing {token}')

studio = source('Services/GameStudioService.cs')
for token in ('GameStudioWorkingFile', 'GameStudioRevisionFile', 'GameStudioDraft', 'ProposePatchAsync', 'GetProposalAsync',
              'SaveDraftAsync', 'SaveFileAsync', 'CloseRevisionAsync', 'SaveRevisionAsync',
              'UserConfirmed = true', 'RequireApproval(userConfirmed)', 'GetReviewAsync',
              'review.Approved', 'ClosedRevisionId is not null', 'Scene = proposal.Scene'):
    require(token in studio, f'edit/commit/curator boundary missing {token}')
require(studio.index('await architecture.SaveRevisionAsync') < studio.index('draft.ClosedRevisionId = revision.Id'),
        'draft must close only after revision persistence')
require("path.Split('/').Any(segment => segment.Length == 0 || segment.StartsWith('.')))" in studio,
        'Godot path validation must close Any and the enclosing if expression (CS1026 regression)')

godot = source('Services/GameGodotWorkspaceService.cs')
for token in ('GodotSceneRuntime.cs.txt', 'project.godot', 'GameStudio.csproj', 'scenes/Main.tscn',
              'src/LocalGptSceneRuntime.cs', 'scene.json', 'Hash(existing)', 'authorizedHash',
              'FileAttributes.ReparsePoint', 'Path.GetFullPath', 'ImportChangedAsync',
              'ExportAsync', 'SaveReceiptsAsync', 'RequireApproval(userConfirmed)'):
    require(token in godot, f'editable Godot workspace conflict/approval contract missing {token}')

template = source('GameTemplates/GodotSceneRuntime.cs.txt')
for token in ('public partial class LocalGptSceneRuntime : Node2D',
              'override void _Process(double delta)', 'override void _Draw()',
              '_accumulator', '_worldSeconds', 'spriteId', 'paletteTimeline',
              'scrollCellsPerWorldSecondX', 'scrollCellsPerWorldSecondY', 'animations', 'hud'):
    require(token in template, f'generic Godot runtime omitted {token}')

page = source('Components/Pages/GameStudio.razor')
require(page.splitlines()[0] == '@rendermode @(new InteractiveServerRenderMode(prerender: false))',
        'the InteractiveServer render-mode directive must be the first Game Studio directive')
codebehind = source('Components/Pages/GameStudio.razor.cs')
for token in ('@page "/game-studio"', '@rendermode @(new InteractiveServerRenderMode(prerender: false))',
              '@inject ILogger<GameStudio> Logger', '@inject INotificationService Notifier',
              '@inject IComponentActivityService ComponentActivity', '<DxFormLayout', '<DxTreeView',
              '<DxSplitter', '<DxSpinEdit', '<DxMemo', '<DxGrid', 'Close worktree and create revision',
              'Import Godot edits', 'Export to project root'):
    require(token in page, f'DevExpress interactive authoring omitted {token}')
for token in ('OnAfterRenderAsync', 'SaveSceneAsync', 'SaveFileAsync', 'SubmitPatchAsync', 'ReviewProposalAsync',
              'AcceptProposalAsync', 'CloseRevisionAsync', 'Engine.Advance', 'Engine.Preview'):
    require(token in codebehind, f'Game Studio UI missing functional method {token}')
require('using Microsoft.JSInterop;' in codebehind,
        'CS1061 regression: IJSRuntime.InvokeVoidAsync extension requires Microsoft.JSInterop in code-behind')
require('ValueChanged="@((Guid value) => SelectProjectAsync(value))"' in page,
        'CS1503/CS1662 regression: DevExpress project ComboBox needs a typed EventCallback value adapter')
require('ValueChanged="@SelectProjectAsync"' not in page,
        'untyped method-group ValueChanged must not be reintroduced for the Game Studio project picker')

require('NavigateUrl="/game-studio"' in source('Components/Layout/NavMenu.razor'), 'Game Studio menu is missing')
require("'Components/Pages/GameStudio.razor' = '@rendermode @(new InteractiveServerRenderMode(prerender: false))'"
        in (ROOT / 'build/Assert-InteractiveServerRenderModes.ps1').read_text(), 'the protected InteractiveServer registry was not extended')
render_mode_guard = (ROOT / 'build/Assert-InteractiveServerRenderModes.ps1').read_text(encoding='utf-8')
require("$reviewedNonPrerenderedPages = @('Components/Pages/DataManagement.razor', 'Components/Pages/GameStudio.razor')"
        in render_mode_guard, 'non-prerendered Game Studio route must also appear in routed-page validation')

js = source('wwwroot/js/localgpt-game-studio.js')
require('javascript-diagnostics: guarded' in js and 'try {' in js and 'catch (error) {' in js,
        'Game Studio JavaScript must own guarded diagnostics')
manifest = (ROOT / 'build/javascript-diagnostics-files.sha256').read_text(encoding='utf-8')
js_line = next((line for line in manifest.splitlines()
                if line.endswith('  src/LocalGPT/wwwroot/js/localgpt-game-studio.js')), '')
js_hash = hashlib.sha256(js.replace('\r\n', '\n').replace('\r', '\n').encode('utf-8')).hexdigest()
require(js_line == f'{js_hash}  src/LocalGPT/wwwroot/js/localgpt-game-studio.js',
        'reviewed Game Studio JS must be included with its correct normalized SHA-256 in the build manifest')
policy = json.loads((ROOT / 'build/async-continuation-policy.json').read_text())
require('Components/Pages/GameStudio.razor.cs' in policy['rendererAffineHelperMethods'], 'reviewed renderer-affine helper methods are not enumerated')

functions = source('Services/GameStudioDxAiFunctions.cs')
for name in ('project.game.scene.get', 'project.game.scene.propose', 'project.game.scene.patch.propose',
             'project.game.scene.proposal.get', 'project.game.scene.review'):
    require(name in functions, f'missing Council model function {name}')
require(functions.count('RequiresHumanConfirmation: true') == 3, 'Council write functions must retain explicit human approval')

require('Scene = committedScene' in source('Services/GameProjectService.cs'), 'game build must capture only the closed revision scene')
require('GameSceneDocument? Scene' in source('BusinessObjects/GameProjectModels.cs'), 'compiled project definition must transport an optional scene')
require('AddScoped<IGameStudioService, GameStudioService>()' in source('Program.ServiceRegistration.cs'), 'Game Studio DI registration absent')
require('AddScoped<IGameGodotWorkspaceService, GameGodotWorkspaceService>()' in source('Program.ServiceRegistration.cs'), 'Godot workspace DI registration absent')

sample = json.loads((ROOT / 'docs/examples/game-studio-parallax-scene.json').read_text())
require(sample['fixedTicksPerSecond'] == 60, 'demonstration must use fixed-step simulation')
require(sample['worldSecondsPerRealSecond'] > 0 and sample['secondsPerTurn'] > 0, 'world clock mapping invalid')
require(len(set((layer['scrollCellsPerWorldSecondX'] for layer in sample['layers']))) > 1, 'demo must showcase distinct parallax velocities')
require(len(sample['paletteTimeline']) > 1, 'demo must include palette changes on the world clock')
sprites = {sprite['id']: sprite for sprite in sample['sprites']}
for sprite in sprites.values():
    for row in sprite['pixelRows']:
        for character in row:
            require(character == ' ' or character in sprite['palette'], 'demo pixel art has an undefined palette token')
for actor in sample['actors']:
    require(actor['spriteId'] in sprites, 'demo actor references a missing sprite')
    require(not any(tile['solid'] and (int(actor['x']), int(actor['y'])) == (tile['x'], tile['y'])
                    for tile in sample['tiles']), 'demo art occupies a prohibited solid tile')

require('localgpt-game-studio.js?v=5.6.6' in source('Components/App.razor'), 'pixel-preview script not connected to App')
require((APP / 'wwwroot/js/localgpt-game-studio.js').is_file(), 'pixel-art browser projection is missing')
require((ROOT / 'docs/reference/game-studio-revisioned-godot.md').is_file(), 'Godot workflow documentation is missing')

print('LocalGPT 5.6.6 Game Studio acceptance passed: corrected JS interop namespace and typed DevExpress EventCallback, plus retained editable Godot worktree/explicit commits, unique Council art proposals, independent review, fixed world ticks, sprites, collision masks, parallax, palettes, DevExpress/InteractiveServer, version and demo.')
