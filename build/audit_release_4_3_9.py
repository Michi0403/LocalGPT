#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    raise SystemExit(f'LocalGPT 4.3.9 audit failed: {message}')


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        fail(f'missing required source file: {relative}')
    return path.read_text(encoding='utf-8', errors='strict')


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        fail(f'{label}: missing {marker!r}')


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        fail(f'{label}: forbidden marker returned: {marker!r}')


def main() -> int:
    for relative in (
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ):
        require(read(relative), '<Version>4.3.9</Version>', relative)

    for relative, marker in {
        'RELEASE.md': '# LocalGPT 4.3.9',
        'VALIDATION.md': '# LocalGPT 4.3.9 source validation',
        'CHANGELOG-v4.3.9-GAME-PROJECT-BUILD-RUNTIME-BOUNDARY.md': '# LocalGPT 4.3.9',
        'VALIDATION-v4.3.9-source.md': '# LocalGPT 4.3.9 source validation',
        'docs/index.md': '**Version 4.3.9**',
        'docs/docfx.json': '"localgptVersion": "4.3.9"',
        'docs/pdf/toc.yml': 'LocalGPT-4.3.9.pdf',
        'src/LocalGPT/Components/App.razor': 'localgpt-game-console.js?v=4.3.9',
    }.items():
        require(read(relative), marker, relative)

    project_models = read('src/LocalGPT/BusinessObjects/GameProjectModels.cs')
    for marker in ('ProjectGameDefinition', 'CouncilGameRuntimeProfile', 'BuildProjectGameRequest', 'LaunchProjectGameRequest'):
        require(project_models, marker, 'game project business objects')

    game_projects = read('src/LocalGPT/Services/GameProjectService.cs')
    for marker in (
        'ArtifactKind = "GameBuild"',
        'Name = "Runtime Definition"',
        'DataType = "application/vnd.localgpt.game+json"',
        'architecture.SaveArtifactAsync',
        'GetBuildAsync',
        'LaunchBySelectorAsync',
    ):
        require(game_projects, marker, 'project-owned game build service')
    forbid(game_projects, ' static ', 'game project service application-static policy')

    runtime = read('src/LocalGPT/Services/CouncilGameSessionService.cs')
    runtime += read('src/LocalGPT/Services/CouncilGameSessionService.Rules.cs')
    runtime += read('src/LocalGPT/Services/CouncilGameSessionService.Rendering.cs')
    runtime += read('src/LocalGPT/Services/CouncilGameSessionService.Autoplay.cs')
    runtime += read('src/LocalGPT/Services/CouncilGameSessionService.Combat.cs')
    runtime += read('src/LocalGPT/Services/CouncilGameSessionService.Snapshots.cs')
    for marker in (
        'request.Definition',
        'CouncilGameRuntimeProfile.Corridor',
        'CouncilGameRuntimeProfile.Story',
        'ProjectId = session.ProjectId',
        'ProjectVersion = session.ProjectVersion',
        'RuntimeProfile = session.RuntimeProfile',
    ):
        require(runtime, marker, 'project definition/runtime boundary')
    if 'session.GameKey == "green-dragon"' in runtime or 'session.GameKey != "ascii-doom"' in runtime:
        fail('project runtime behavior still branches on historical game identity instead of runtime profile')

    project_page = read('src/LocalGPT/Components/Pages/Projects.razor')
    for marker in (
        '@rendermode InteractiveServer',
        '@inject IGameProjectService GameProjects',
        'Game project build',
        'BuildGameProjectAsync',
        ':game project &lt;project name or id&gt;',
    ):
        require(project_page, marker, 'Projects Game authoring surface')

    operator = read('src/LocalGPT/Services/ConsoleOperatorService.cs')
    console = read('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
    require(operator, 'LocalConsoleOperatorApplicationAction.GameStartProject', 'service-owned operator parsing')
    require(operator, 'Usage: :game project <project id or name>.', 'project game operator syntax')
    require(console, 'GameProjects.LaunchBySelectorAsync', 'ASCII project-game dispatch')
    forbid(console, '.StartsWith("project "', 'Razor project selector parsing')

    dx = read('src/LocalGPT/Services/GameProjectDxAiFunctions.cs')
    for marker in ('"project.game.get"', '"project.game.build"', '"project.game.start"', 'RequiresHumanConfirmation: true'):
        require(dx, marker, 'project-game DX functions')

    controller = read('src/LocalGPT/Controller/LocalGptProjectsController.cs')
    for marker in ('{projectId:guid}/game/build', '{projectId:guid}/game/start', 'IGameProjectService gameProjects'):
        require(controller, marker, 'project-game HTTP API')

    layout = read('src/LocalGPT/Components/Layout/MainLayout.razor')
    require(layout, '<SafeErrorBoundary @key="NavigationManager.Uri"', 'maintained operational diagnostics boundary')

    pages = ROOT / 'src/LocalGPT/Components/Pages'
    missing = []
    for page in pages.rglob('*.razor'):
        text = page.read_text(encoding='utf-8', errors='ignore')
        if '@page ' in text and page.name != 'Error.razor' and '@rendermode InteractiveServer' not in text:
            missing.append(str(page.relative_to(ROOT)))
    if missing:
        fail(f'InteractiveServer render mode missing from {missing[0]}')

    print('LocalGPT 4.3.9 audit passed: Game projects own authoring/build artifacts, GameDirector consumes compiled definitions below that boundary, and the 4.3.8 architecture/ASCII contracts remain intact.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
