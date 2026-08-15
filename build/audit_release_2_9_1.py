#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.1 live Council transcript status."""
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
        require(rel,'<Version>2.9.1</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj','<Version>2.1.1</Version>')
    require('src/LocalGPT/Components/Pages/Chat.razor','class="localgpt-live-transcript-status" aria-live="off"')
    require('src/LocalGPT/Components/Pages/Chat.razor.css','.localgpt-live-transcript-status {')
    require('src/LocalGPT/Components/Pages/Chat.razor.css','margin: 1rem 0;')
    require('src/LocalGPT/Components/Pages/Chat.razor.css','background: transparent;')
    forbid('src/LocalGPT/Components/Pages/Chat.razor','localgpt-rejoined-live-spinner')
    forbid('src/LocalGPT/Components/Pages/Chat.razor.css','localgpt-rejoined-live-spinner')
    forbid('src/LocalGPT/Components/Pages/Chat.razor.css','localgpt-rejoined-live-status')
    forbid('src/LocalGPT/Components/Pages/Chat.razor.css','@keyframes localgpt-live-spin')
    print('LocalGPT 2.9.1 live Council transcript-status source audit passed.')
except Exception as exc:
    print(f'LocalGPT 2.9.1 source audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
