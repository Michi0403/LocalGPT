#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.8.5 multilingual catalog integration."""
from pathlib import Path
import json, sys

root=Path(__file__).resolve().parents[1]

def read(rel):
    p=root/rel
    if not p.is_file(): raise AssertionError(f'missing {rel}')
    return p.read_text(encoding='utf-8')

def require(rel,*needles):
    text=read(rel)
    missing=[n for n in needles if n not in text]
    if missing: raise AssertionError(f'{rel} missing {missing}')

try:
    require('src/LocalGPT/LocalGPT.csproj','<Version>2.8.5</Version>')
    require('src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','<Version>2.8.5</Version>')
    require('src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','<Version>2.8.5</Version>')
    require('src/LocalGPT/Services/Localization/LocalGptLocalizationService.cs',
            'AddCatalogCultures(BuiltInLocalizationPath, cultures);',
            'return cultures.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();')

    loc=root/'src/LocalGPT/Localization'
    cultures=['de-DE','en-US','es-ES','fr-FR','ja-JP','uk-UA']
    catalogs={c:json.loads((loc/f'{c}.json').read_text(encoding='utf-8-sig')) for c in cultures}
    en=catalogs['en-US']
    for c,data in catalogs.items():
        if set(data)!=set(en): raise AssertionError(f'{c} localization key parity failed')
    for c in ['es-ES','fr-FR','ja-JP','uk-UA']:
        changed=sum(1 for k,v in en.items() if catalogs[c][k] != v)
        if changed < 0.70*len(en): raise AssertionError(f'{c} translation coverage too low: {changed}/{len(en)}')

    modes=[]
    for path in (root/'src/LocalGPT').rglob('*.razor'):
        for line in path.read_text(encoding='utf-8').splitlines():
            if '@rendermode' in line: modes.append((str(path.relative_to(root)),line.strip()))
    if len(modes)!=19: raise AssertionError(f'expected 19 LocalGPT rendermode directives, found {len(modes)}')

    print('LocalGPT 2.8.5 multilingual source regression audit passed.')
except (AssertionError, OSError, json.JSONDecodeError) as exc:
    print(f'LocalGPT 2.8.5 source regression audit failed: {exc}',file=sys.stderr)
    sys.exit(1)
