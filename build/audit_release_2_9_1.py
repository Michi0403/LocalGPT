#!/usr/bin/env python3
"""Source-only regression audit for the superseded 2.9.1 Council status change on current LocalGPT source."""
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
        require(rel,'<Version>3.0.0</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj','<Version>2.1.1</Version>')
    # 2.9.2 intentionally restores the in-message live status and demotes only the external banner.
    require('src/LocalGPT/Components/Pages/Chat.razor','localgpt-message-utility-row localgpt-live-update-footer')
    require('src/LocalGPT/Components/Pages/Chat.razor','localgpt-rejoined-live-spinner')
    require('src/LocalGPT/Components/Pages/Chat.razor','class="chat-live-session-inline-info" aria-live="off"')
    forbid('src/LocalGPT/Components/Pages/Chat.razor','class="chat-live-session-banner"')
    require('src/LocalGPT/Components/Pages/Chat.razor.css','.chat-live-session-inline-info {')
    require('src/LocalGPT/Components/Pages/Chat.razor.css','background: transparent;')
    print('LocalGPT superseded 2.9.1 Council-status regression audit passed on current source.')
except Exception as exc:
    print(f'LocalGPT superseded 2.9.1 audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
