#!/usr/bin/env python3
"""Source guard for real shell execution and DevExpress ASCII popup lifecycle."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / 'src' / 'LocalGPT'
command = (src / 'Services' / 'ConsoleCommandService.cs').read_text(encoding='utf-8')
operator = (src / 'Services' / 'ConsoleOperatorService.cs').read_text(encoding='utf-8')
page = (src / 'Components' / 'Pages' / 'Chat.razor.cs').read_text(encoding='utf-8')
component = (src / 'Components' / 'Shared' / 'ChatGameConsole.razor').read_text(encoding='utf-8')
style = (src / 'wwwroot' / 'css' / 'site.css').read_text(encoding='utf-8')
script = (src / 'wwwroot' / 'js' / 'localgpt-game-console.js').read_text(encoding='utf-8')
checks = {
    'never schedule an unrepresentable timeout': '(long)timeoutSeconds * 1000 <= uint.MaxValue - 1L' in command,
    'retain bounded explicit cancellation': 'timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds))' in command,
    'retain manual job cancellation': 'CancellationTokenSource.CreateLinkedTokenSource(linkedCancellation.Token)' in command,
    'retain process stdout and stderr streaming': 'PumpOutputAsync(process.StandardOutput' in command and 'PumpOutputAsync(process.StandardError' in command,
    'retain manual shell confirmation': 'if (!request.UserConfirmed)' in operator,
    'retain raw human shell dispatch': 'CommandText = input' in operator,
    'retain session-local cd': 'return ChangeDirectory(request, input[3..].Trim())' in operator,
    'clear terminal through renderer-owned callback': 'private async Task CloseGameConsole()' in page and 'await OnGameConsoleVisibilityChangedAsync(false).ConfigureAwait(false);' in page,
    'do not launch background render from close': 'TaskRunner.Run(nameof(Chat), nameof(CloseGameConsole)' not in page,
    'terminal close exits fullscreen': '"localGptGameConsole.exitFullscreen"' in component,
    'fullscreen targets dialog-owning popup root': "element.closest('dxbl-popup-root')" in script,
    'fullscreen is the browser fullscreen API': "host.requestFullscreen({ navigationUI:'hide' })" in script,
    'sizing traverses primary form layout': '.chat-ascii-layout-surface dxbl-form-layout' in style,
    'sizing traverses game component form layout': '.chat-game-popup-body dxbl-form-layout' in style,
    'sizing traverses modal form wrappers': '.dxbl-modal-body > .razor-component-boundary dxbl-form-layout' in style,
    'fullscreen handles game stage': '.chat-game-console.localgpt-game-fullscreen .chat-game-stage' in style,
    'fullscreen handles operator stage': '.chat-game-console.localgpt-game-fullscreen .ascii-operator-stage' in style,
    'fullscreen handles conversation stage': '.chat-game-console.localgpt-game-fullscreen .ascii-conversation-stage' in style,
    'nonfullscreen popup height is fixed': 'dxbl-modal-dialog.localgpt-chat-dialog-surface .chat-game-stage' in style,
    'normal and fullscreen scope is local': 'dxbl-modal-dialog.localgpt-chat-dialog-surface' in style and 'dxbl-popup-root.localgpt-game-fullscreen-host:fullscreen' in style,
}
errors = [name for name, passed in checks.items() if not passed]
if errors:
    raise SystemExit('ASCII console regression audit FAILED:\n- ' + '\n- '.join(errors))
print(f'ASCII terminal shell/fullscreen regression audit passed: {len(checks)} checks.')
