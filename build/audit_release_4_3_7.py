#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    raise SystemExit(f'LocalGPT 4.3.7 audit failed: {message}')


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        fail(f'{label}: missing {marker!r}')


def main() -> int:
    versions = {
        'src/LocalGPT/LocalGPT.csproj': '<Version>4.3.7</Version>',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj': '<Version>4.3.7</Version>',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj': '<Version>4.3.7</Version>',
    }
    for relative, marker in versions.items():
        require((ROOT / relative).read_text(encoding='utf-8'), marker, relative)

    release_markers = {
        'RELEASE.md': '# LocalGPT 4.3.7',
        'VALIDATION.md': '# LocalGPT 4.3.7 source validation',
        'CHANGELOG-v4.3.7-CHAT-ASCII-COUNCIL-INTERACTION-REPAIR.md': '# LocalGPT 4.3.7',
        'VALIDATION-v4.3.7-source.md': '# LocalGPT 4.3.7 source validation',
        'docs/index.md': '**Version 4.3.7**',
        'docs/docfx.json': '"localgptVersion": "4.3.7"',
        'docs/pdf/toc.yml': 'LocalGPT-4.3.7.pdf',
        'src/LocalGPT/Components/App.razor': 'localgpt-game-console.js?v=4.3.7',
    }
    for relative, marker in release_markers.items():
        require((ROOT / relative).read_text(encoding='utf-8'), marker, relative)

    bounded = (ROOT / 'src/LocalGPT/Components/Shared/BoundedNumberEditor.razor').read_text(encoding='utf-8')
    hardware = (ROOT / 'src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor').read_text(encoding='utf-8')
    chat = (ROOT / 'src/LocalGPT/Components/Pages/Chat.razor').read_text(encoding='utf-8')
    require(bounded, '@onchange="OnSliderChangedAsync"', 'bounded numeric slider')
    require(hardware, '@onchange="args => SetLoadOverride(route, args)"', 'Council hardware slider')
    require(chat, '@onchange="OnCouncilResourceLoadChanged"', 'Council load slider')

    layout = (ROOT / 'src/LocalGPT/Components/Layout/MainLayout.razor').read_text(encoding='utf-8')
    require(layout, '@key="new Uri(NavigationManager.Uri).AbsolutePath"', 'chat route preservation')
    if 'data-enhance-nav="false" href="@NavigationUrls.GetUrl(new Uri(NavigationManager.Uri).LocalPath, !ToggledSidebar)"' in layout:
        fail('same-route sidebar toggle still disables enhanced navigation')

    game_console = (ROOT / 'src/LocalGPT/Components/Shared/ChatGameConsole.razor').read_text(encoding='utf-8')
    for marker in (
        'SavedConversations="@SavedConversations"',
        ':chat <prompt>',
        ':council <prompt|status|stop|skip>',
        ':session <list|new|open selector>',
        ':game <corridor|dragon|status|end>',
        ':fullscreen [fit|width|native]',
        'HasGameInteractionRequest',
        'SubmitGameInteractionAsync',
        'CouncilRunId = CouncilRunId',
        '<button type="button" @onclick="FullscreenAsync">Fullscreen</button>',
    ):
        # The SavedConversations binding is in Chat.razor, all other markers are in the console.
        source = chat if marker == 'SavedConversations="@SavedConversations"' else game_console
        require(source, marker, 'ASCII game/operator contract')

    inbox = (ROOT / 'src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor').read_text(encoding='utf-8')
    require(inbox, '!IsAsciiHotSeatRequest(item)', 'hot-seat inbox filtering')
    require(inbox, 'Player 1 Hot Seat', 'hot-seat role filtering')
    require(inbox, 'Player 2 Hot Seat', 'hot-seat role filtering')

    live = (ROOT / 'src/LocalGPT/Services/CouncilLiveSessionService.cs').read_text(encoding='utf-8')
    game_service = (ROOT / 'src/LocalGPT/Services/CouncilGameSessionService.cs').read_text(encoding='utf-8')
    dx = (ROOT / 'src/LocalGPT/Services/CouncilGameDxAiFunctions.cs').read_text(encoding='utf-8')
    for marker in ('activity.IsRunning = false;', 'gameSessions.EndByCouncilRun(runId, "Council completed")', 'gameSessions.EndByCouncilRun(runId, "Council cancellation")'):
        require(live, marker, 'Council completion/cancellation cleanup')
    require(game_service, 'public int EndByCouncilRun(Guid councilRunId', 'Council-owned game cleanup')
    require(dx, 'CouncilRunId = ambientContext.Current.CouncilRunId', 'AI-started Council game ownership')

    js = (ROOT / 'src/LocalGPT/wwwroot/js/localgpt-game-console.js').read_text(encoding='utf-8')
    require(js, 'resizeObserver', 'ASCII console resize handling')
    require(js, "target.matches('input, textarea, select')", 'ASCII shortcut input guard')

    css = (ROOT / 'src/LocalGPT/Components/Pages/Chat.razor.css').read_text(encoding='utf-8')
    require(css, 'overflow-y: auto !important;', '/chat vertical reachability')
    game_css = (ROOT / 'src/LocalGPT/Components/Shared/ChatGameConsole.razor.css').read_text(encoding='utf-8')
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

    with zipfile.ZipFile(ROOT / '.github/pages/localgpt-kawaii-docs.zip') as archive:
        if archive.testzip() is not None:
            fail('tracked LocalGPT Pages snapshot is corrupt')
        if b'LocalGPT-4.3.7.pdf' not in archive.read('index.html'):
            fail('tracked LocalGPT Pages snapshot index was not synchronized to 4.3.7')

    print('LocalGPT 4.3.7 audit passed: slider commit behavior, chat reachability, ASCII gameplay/operator controls, Council lifecycle cleanup, render modes and release metadata are present.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
