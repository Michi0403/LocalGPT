#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    raise SystemExit(f'LocalGPT 4.4.0 audit failed: {message}')


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
        require(read(relative), '<Version>4.4.0</Version>', relative)

    for relative, marker in {
        'RELEASE.md': '# LocalGPT 4.4.0',
        'VALIDATION.md': '# LocalGPT 4.4.0 source validation',
        'CHANGELOG-v4.4.0-GAME-PROJECT-AUTHORING-REQUIREMENTS-TEAMS.md': '# LocalGPT 4.4.0',
        'VALIDATION-v4.4.0-source.md': '# LocalGPT 4.4.0 source validation',
        'docs/index.md': '**Version 4.4.0**',
        'docs/docfx.json': '"localgptVersion": "4.4.0"',
        'docs/pdf/toc.yml': 'LocalGPT-4.4.0.pdf',
        'src/LocalGPT/Components/App.razor': 'localgpt-game-console.js?v=4.4.0',
    }.items():
        require(read(relative), marker, relative)

    guidance = read('AGENTS.md')
    require(guidance, '### Game-project layering', 'repository architecture guidance')
    require(guidance, 'Saving that state and compiling it are separate operations.', 'repository architecture guidance')

    project_models = read('src/LocalGPT/BusinessObjects/GameProjectModels.cs')
    for marker in (
        'class LocalGptGameProjectProfile',
        'class SaveGameProjectProfileRequest',
        'class BuildProjectGameRequest',
        'AuthoringProfileId',
        'SourceRevisionId',
        'SourceRequirementIds',
        'ProjectGameDefinition',
    ):
        require(project_models, marker, 'game-project structures')
    forbid(
        project_models[project_models.index('public sealed class BuildProjectGameRequest'):project_models.index('public sealed class ProjectGameBuildResult')],
        'GameKey',
        'approval-only build request',
    )

    project_domain = read('src/LocalGPT/BusinessObjects/LocalGptProjectModels.cs')
    require(project_domain, 'LocalGptGameProjectProfile? GameProfile { get; set; }', 'Project owns game authoring profile')
    require(project_domain, 'LocalGptGameProjectProfile? GameProfile { get; init; }', 'Project details expose game authoring profile')

    context = read('src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs')
    for marker in (
        'DbSet<LocalGptGameProjectProfile> LocalGptGameProjectProfiles',
        'modelBuilder.Entity<LocalGptGameProjectProfile>(entity =>',
        '.WithOne(project => project.GameProfile)',
        '.HasForeignKey<LocalGptGameProjectProfile>(item => item.ProjectId)',
        'entity.HasIndex(item => item.ProjectId).IsUnique();',
    ):
        require(context, marker, 'EF Game-project profile mapping')

    migration = read('src/LocalGPT/Migrations/20260916204500_AddGameProjectAuthoringProfiles.cs')
    for marker in (
        '[Migration("20260916204500_AddGameProjectAuthoringProfiles")]',
        'name: "LocalGptGameProjectProfiles"',
        'FK_LocalGptGameProjectProfiles_LocalGptProjects_ProjectId',
        'IX_LocalGptGameProjectProfiles_ProjectId',
        'unique: true',
        'IX_LocalGptGameProjectProfiles_UpdatedAtUtc',
        'migrationBuilder.DropTable(name: "LocalGptGameProjectProfiles")',
    ):
        require(migration, marker, 'Game-project EF migration')

    snapshot = read('src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs')
    for marker in (
        'modelBuilder.Entity("LocalGPT.BusinessObjects.LocalGptGameProjectProfile", b =>',
        'b.ToTable("LocalGptGameProjectProfiles", (string)null)',
        '.WithOne("GameProfile")',
        '.HasForeignKey("LocalGPT.BusinessObjects.LocalGptGameProjectProfile", "ProjectId")',
        'b.Navigation("GameProfile");',
    ):
        require(snapshot, marker, 'EF Game-project snapshot')

    project_service = read('src/LocalGPT/Services/LocalGptProjectService.cs')
    require(project_service, 'var gameProfile = await db.LocalGptGameProjectProfiles', 'project details persistence ownership')
    require(project_service, 'GameProfile = gameProfile', 'project details profile surface')

    game_projects = read('src/LocalGPT/Services/GameProjectService.cs')
    for marker in (
        'SaveGameProjectProfileRequest request',
        'details.GameProfile',
        'SourceRequirementIds = requirementIds',
        '.Where(item => item.IsUserApproved)',
        'ArtifactKind = "GameBuild"',
        'Name = "Runtime Definition"',
        'DataType = "application/vnd.localgpt.game+json"',
        'architecture.SaveArtifactAsync',
        'GetBuildAsync',
        'LaunchBySelectorAsync',
    ):
        require(game_projects, marker, 'project-owned game profile/build service')
    build_method = game_projects[game_projects.index('public async Task<ProjectGameBuildResult> BuildAsync'):game_projects.index('public async Task<ProjectGameBuildResult?> GetBuildAsync')]
    forbid(build_method, 'SaveProfileAsync(', 'build must not mutate Game-project authoring state')
    forbid(game_projects, ' static ', 'game project service application-static policy')

    request_factory = read('src/LocalGPT/Interfaces/ILocalGptRequestFactoryService.cs') + read('src/LocalGPT/Services/LocalGptRequestFactoryService.cs')
    require(request_factory, 'CreateGameProjectProfileRequest', 'separate Game-project authoring request factory')
    require(request_factory, 'CreateGameBuildRequest', 'separate Game-project build request factory')
    require(request_factory, 'DefaultTeamKey = "game-project-playtest"', 'maintained Game-project playtest default')

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
        'Save Game Design',
        'Build Game Definition',
        'CreateGameProjectProfileRequest',
        'CreateGameBuildRequest',
        'CurrentGameProfile is not null',
        'SaveGameProjectProfileAsync',
        'BuildGameProjectAsync',
        ':game project &lt;project name or id&gt;',
    ):
        require(project_page, marker, 'Projects Game authoring/build surface')

    operator = read('src/LocalGPT/Services/ConsoleOperatorService.cs')
    console = read('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
    require(operator, 'LocalConsoleOperatorApplicationAction.GameStartProject', 'service-owned operator parsing')
    require(operator, 'Usage: :game project <project id or name>.', 'project game operator syntax')
    require(console, 'GameProjects.LaunchBySelectorAsync', 'ASCII project-game dispatch')
    forbid(console, '.StartsWith("project "', 'Razor project selector parsing')

    dx = read('src/LocalGPT/Services/GameProjectDxAiFunctions.cs')
    for marker in (
        '"project.game.profile.get"',
        '"project.game.profile.save"',
        '"project.game.get"',
        '"project.game.build"',
        '"project.game.start"',
        'Editable game settings must already be persisted through project.game.profile.save.',
    ):
        require(dx, marker, 'project-game DX functions')

    controller = read('src/LocalGPT/Controller/LocalGptProjectsController.cs')
    for marker in (
        '{projectId:guid}/game/profile',
        'SaveGameProjectProfileRequest request',
        '{projectId:guid}/game/build',
        '{projectId:guid}/game/start',
        'IGameProjectService gameProjects',
    ):
        require(controller, marker, 'project-game HTTP API')

    teams = read('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.GameProjectTemplates.cs')
    for marker in (
        'Key = "game-project-discovery"',
        'DisplayName = "Game Project Discovery & Requirements"',
        'human.collaboration.request',
        'project.requirement.save',
        'Key = "game-project-development"',
        'Key = "game-engine-extension-development"',
        'Schema changes require an explicit EF migration plus matching model snapshot',
        'Key = "game-project-playtest"',
        'Passed, Gap, Regression or Not Tested',
    ):
        require(teams, marker, 'Game-project Council presets')
    seed = read('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs')
    for marker in (
        'CreateGameProjectDiscoveryTeam()',
        'CreateGameProjectDevelopmentTeam()',
        'CreateGameEngineExtensionDevelopmentTeam()',
        'CreateGameProjectPlaytestTeam()',
    ):
        require(seed, marker, 'Game-project team seed registration')

    ef_guard = read('build/Assert-EfSnapshotArchitecture.ps1')
    for marker in (
        "LocalGptProject navigation 'GameProfile' must occur exactly once",
        '.WithOne(project => project.GameProfile)',
        'DbSet<LocalGptGameProjectProfile> LocalGptGameProjectProfiles',
    ):
        require(ef_guard, marker, 'strengthened EF architecture guard')

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

    async_operator = read('src/LocalGPT/Components/Pages/Chat.AsciiOperator.razor.cs')
    forbid(async_operator, 'ConfigureAwait(true)', 'ASCII Operator async-continuation policy')

    print('LocalGPT 4.4.0 audit passed: Game-project authoring is persisted and migration-backed inside the Project system, builds compile saved design plus approved requirements without mutating it, maintained discovery/development/engine/playtest teams are seeded, and the 4.3.8/4.3.9 runtime and architecture contracts remain intact.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
