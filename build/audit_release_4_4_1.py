#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    raise SystemExit(f'LocalGPT 4.4.1 audit failed: {message}')


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        fail(f'missing required source file: {relative}')
    return path.read_text(encoding='utf-8', errors='strict')


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        fail(f'{label}: missing {marker!r}')


def main() -> int:
    for relative in (
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ):
        require(read(relative), '<Version>4.4.1</Version>', relative)

    release_markers = {
        'RELEASE.md': '# LocalGPT 4.4.1',
        'VALIDATION.md': '# LocalGPT 4.4.1 source validation',
        'CHANGELOG-v4.4.1-SIDEBAR-STATE-CONTRACT-REPAIR.md': '# LocalGPT 4.4.1',
        'VALIDATION-v4.4.1-source.md': '# LocalGPT 4.4.1 source validation',
        'docs/index.md': '**Version 4.4.1**',
        'docs/docfx.json': '"localgptVersion": "4.4.1"',
        'docs/pdf/toc.yml': 'LocalGPT-4.4.1.pdf',
        'src/LocalGPT/Components/App.razor': 'localgpt-game-console.js?v=4.4.1',
    }
    for relative, marker in release_markers.items():
        require(read(relative), marker, relative)

    layout = read('src/LocalGPT/Components/Layout/MainLayout.razor')
    drawer = read('src/LocalGPT/Components/Layout/Drawer.razor')
    index = read('src/LocalGPT/Components/Pages/Index.razor')
    workflow = read('build/Assert-WorkflowContracts.ps1')

    query_marker = '[SupplyParameterFromQuery(Name = NavigationUrlService.ToggleSidebarName)]'
    require(layout, query_marker, 'MainLayout maintained query contract')
    require(drawer, query_marker, 'Drawer maintained query contract')
    require(index, query_marker, 'Index maintained query contract')
    require(workflow, "'src/LocalGPT/Components/Layout/Drawer.razor',", 'maintained workflow guard')
    require(workflow, "'src/LocalGPT/Components/Layout/MainLayout.razor',", 'maintained workflow guard')
    require(workflow, "'src/LocalGPT/Components/Pages/Index.razor'", 'maintained workflow guard')
    require(workflow, "$content.IndexOf('Name = NavigationUrlService.ToggleSidebarName'", 'maintained sidebar query assertion')

    require(layout, '<SafeErrorBoundary @key="NavigationManager.Uri"', 'maintained operational diagnostics boundary')
    require(layout, 'OpenState="@SidebarOpen"', 'renderer-owned Drawer override')
    require(layout, 'SidebarOpen = !SidebarOpen;', 'live menu toggle')
    require(layout, 'if (SidebarStateLocation != NavigationManager.Uri)', 'URI-change query synchronization')
    require(layout, 'SidebarOpen = ToggledSidebar;', 'query-to-live synchronization')
    require(layout, 'IconCssClass="@(SidebarOpen ? "icon icon-close" : "icon icon-menu")"', 'header icon live state')
    require(drawer, 'public bool? OpenState { get; set; }', 'optional live Drawer override')
    require(drawer, 'drawerOpen = OpenState ?? ToggledSidebar;', 'query-backed Drawer fallback')

    if 'href="@NavigationUrls.GetUrl(new Uri(NavigationManager.Uri).LocalPath, !ToggledSidebar)"' in layout:
        fail('menu toggle must not navigate or mutate the current URI')

    # Preserve the next-layer Game-project work while fixing only the shell regression.
    require(read('src/LocalGPT/BusinessObjects/LocalGptProjectModels.cs'), 'LocalGptGameProjectProfile', '4.4.0 Game-project profile')
    require(read('src/LocalGPT/Services/GameProjectService.cs'), 'ProjectGameDefinition', 'Project -> built game definition boundary')
    require(read('src/LocalGPT/Components/Pages/Projects.razor'), 'Save Game Design', 'Game-project authoring workflow')

    print('LocalGPT 4.4.1 audit passed: sidebar live state is restored without URI mutation, the original maintained query contract is again honored, and 4.4.0 Game-project work remains present.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
