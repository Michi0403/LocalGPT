#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.2 Council UI and true-bottom autoscroll repair."""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def require(rel, needle):
    if needle not in text(rel): raise AssertionError(f"{rel} missing: {needle}")
def forbid(rel, needle):
    if needle in text(rel): raise AssertionError(f"{rel} unexpectedly contains: {needle}")
try:
    for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj'):
        require(rel,'<Version>2.9.8</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj','<Version>2.1.1</Version>')
    require('src/LocalGPT/Components/Pages/Chat.razor','localgpt-message-utility-row localgpt-live-update-footer')
    require('src/LocalGPT/Components/Pages/Chat.razor','localgpt-rejoined-live-spinner')
    require('src/LocalGPT/Components/Pages/Chat.razor','class="chat-live-session-inline-info" aria-live="off"')
    forbid('src/LocalGPT/Components/Pages/Chat.razor','class="chat-live-session-banner"')
    require('src/LocalGPT/Components/Pages/Chat.razor.css','.localgpt-rejoined-live-status {')
    require('src/LocalGPT/Components/Pages/Chat.razor.css','.chat-live-session-inline-info {')
    require('src/LocalGPT/wwwroot/js/localgpt-chat-ui.js','const liveTargetTop = Math.max(0, state.region.scrollHeight - state.region.clientHeight);')
    require('src/LocalGPT/wwwroot/js/localgpt-chat-ui.js','localgpt-chat-ui.scroll.settleAtBottom')
    forbid('src/LocalGPT/wwwroot/js/localgpt-chat-ui.js','setRegionScrollTop(state, startTop + ((targetTop - startTop) * eased));')
    require('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=2.9.2')
    print('LocalGPT 2.9.2 Council UI/autoscroll source audit passed.')
except Exception as exc:
    print(f'LocalGPT 2.9.2 source audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
