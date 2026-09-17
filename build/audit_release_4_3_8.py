#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    raise SystemExit(f'LocalGPT 4.3.8 audit failed: {message}')


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
        require(read(relative), '<Version>4.3.8</Version>', relative)

    release_markers = {
        'RELEASE.md': '# LocalGPT 4.3.8',
        'VALIDATION.md': '# LocalGPT 4.3.8 source validation',
        'CHANGELOG-v4.3.8-ARCHITECTURE-COMPLIANCE-AND-ASCII-CONTROL.md': '# LocalGPT 4.3.8',
        'VALIDATION-v4.3.8-source.md': '# LocalGPT 4.3.8 source validation',
        'docs/index.md': '**Version 4.3.8**',
        'docs/docfx.json': '"localgptVersion": "4.3.8"',
        'docs/pdf/toc.yml': 'LocalGPT-4.3.8.pdf',
        'src/LocalGPT/Components/App.razor': 'localgpt-game-console.js?v=4.3.8',
    }
    for relative, marker in release_markers.items():
        require(read(relative), marker, relative)

    bounded = read('src/LocalGPT/Components/Shared/BoundedNumberEditor.razor')
    hardware = read('src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor')
    chat = read('src/LocalGPT/Components/Pages/Chat.razor')
    require(bounded, '@onchange="OnSliderChangedAsync"', 'bounded numeric slider')
    require(hardware, '@onchange="args => SetLoadOverride(route, args)"', 'Council hardware slider')
    require(chat, '@onchange="OnCouncilResourceLoadChanged"', 'Council load slider')

    layout = read('src/LocalGPT/Components/Layout/MainLayout.razor')
    drawer = read('src/LocalGPT/Components/Layout/Drawer.razor')
    require(layout, '<SafeErrorBoundary @key="NavigationManager.Uri"', 'maintained layout diagnostics boundary')
    require(layout, 'ToggledSidebar="@ToggledSidebar"', 'layout-owned drawer state')
    require(layout, 'Click="ToggleSidebar"', 'non-navigation drawer toggle')
    if '@key="new Uri(NavigationManager.Uri).AbsolutePath"' in layout:
        fail('the 4.3.7 route-key workaround must not replace the maintained diagnostics boundary')
    require(drawer, '[Parameter]\n    public bool ToggledSidebar { get; set; }', 'drawer parameter ownership')
    if '[SupplyParameterFromQuery(Name = NavigationUrlService.ToggleSidebarName)]\n    public bool ToggledSidebar' in drawer:
        fail('Drawer still owns a competing query-bound sidebar state')

    operator_partial = read('src/LocalGPT/Components/Pages/Chat.AsciiOperator.razor.cs')
    if 'ConfigureAwait(true)' in operator_partial:
        fail('new Chat Operator partial contains an unapproved renderer-affine ConfigureAwait(true)')
    require(operator_partial, 'ConsoleOperator.ResolveSavedConversation', 'service-owned saved-session resolution')

    operator_service = read('src/LocalGPT/Services/ConsoleOperatorService.cs')
    operator_interface = read('src/LocalGPT/Interfaces/IConsoleOperatorService.cs')
    for marker in (
        'ParseChatAction', 'ParseCouncilAction', 'ParseSessionAction', 'ParseGameAction',
        'ParseModeAction', 'ParseFullscreenAction', 'FormatSavedConversations',
        'ResolveSavedConversation', 'LocalConsoleOperatorApplicationAction',
    ):
        require(operator_service + operator_interface, marker, 'service-owned ASCII Operator behavior')

    game_console = read('src/LocalGPT/Components/Shared/ChatGameConsole.razor')
    for forbidden in ('private static bool IsHotSeatRequest', '.StartsWith("open "', "input.IndexOf(' ')", 'string.Join(Environment.NewLine, lines)'):
        if forbidden in game_console:
            fail(f'component-owned parsing/static helper returned: {forbidden}')
    for marker in (
        'DispatchApplicationOperatorActionAsync',
        'Operator.FormatSavedConversations(SavedConversations)',
        'AsciiText.IsCouncilRoleResponseRequest',
        'SubmitGameInteractionAsync',
        'CouncilRunId = CouncilRunId',
        '<button type="button" @onclick="FullscreenAsync">Fullscreen</button>',
    ):
        require(game_console, marker, 'ASCII game/operator contract')

    inbox = read('src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor')
    if 'private static bool IsAsciiHotSeatRequest' in inbox:
        fail('HumanCollaborationInbox reintroduced a static hot-seat classifier')
    require(inbox, 'IsAsciiGameRoleRequest', 'service-backed game-role filtering')
    require(inbox, 'AsciiText.IsCouncilRoleResponseRequest', 'service-backed Council role-response contract')
    require(inbox, 'asciiGameCouncilRunIds', 'active game/Council ownership filtering')

    live = read('src/LocalGPT/Services/CouncilLiveSessionService.cs')
    game_service = read('src/LocalGPT/Services/CouncilGameSessionService.cs')
    dx = read('src/LocalGPT/Services/CouncilGameDxAiFunctions.cs')
    for marker in (
        'activity.IsRunning = false;',
        'gameSessions.EndByCouncilRun(runId, "Council completed")',
        'gameSessions.EndByCouncilRun(runId, "Council cancellation")',
    ):
        require(live, marker, 'Council completion/cancellation cleanup')
    require(game_service, 'public int EndByCouncilRun(Guid councilRunId', 'Council-owned game cleanup')
    require(dx, 'CouncilRunId = ambientContext.Current.CouncilRunId', 'AI-started Council game ownership')

    js_path = ROOT / 'src/LocalGPT/wwwroot/js/localgpt-game-console.js'
    js = js_path.read_text(encoding='utf-8')
    require(js, 'resizeObserver', 'ASCII console resize handling')
    require(js, "target.matches('input, textarea, select')", 'ASCII shortcut input guard')
    normalized = js.replace('\r\n', '\n').replace('\r', '\n').encode('utf-8')
    digest = hashlib.sha256(normalized).hexdigest()
    manifest = read('build/javascript-diagnostics-files.sha256')
    require(manifest, f'{digest}  src/LocalGPT/wwwroot/js/localgpt-game-console.js', 'reviewed JavaScript diagnostics manifest')

    css = read('src/LocalGPT/Components/Pages/Chat.razor.css')
    require(css, 'overflow-y: auto !important;', '/chat vertical reachability')
    game_css = read('src/LocalGPT/Components/Shared/ChatGameConsole.razor.css')
    require(game_css, '.chat-game-console:fullscreen .ascii-operator-stage', 'Operator fullscreen')
    require(game_css, '.chat-game-console:fullscreen .ascii-conversation-stage', 'ASCII chat fullscreen')

    pages = ROOT / 'src/LocalGPT/Components/Pages'
    missing = []
    for page in pages.rglob('*.razor'):
        text = page.read_text(encoding='utf-8', errors='ignore')
        if '@page ' in text and page.name != 'Error.razor' and '@rendermode InteractiveServer' not in text:
            missing.append(str(page.relative_to(ROOT)))
    if missing:
        fail(f'InteractiveServer render mode missing from {missing[0]}')

    print('LocalGPT 4.3.8 audit passed: requested 4.3.7 behavior is retained behind the maintained diagnostics, DI/service, async, static, text-ownership, JavaScript-review and InteractiveServer contracts.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
