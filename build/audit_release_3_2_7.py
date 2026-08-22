#!/usr/bin/env python3
"""Source-only release audit for LocalGPT 3.2.7 Remote Control workbench changes."""
from pathlib import Path
import json, re

ROOT=Path(__file__).resolve().parents[1]
APP=ROOT/'src/LocalGPT'
failures=[]; checks=[]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def req(text,needle,label):
    if needle not in text: failures.append(f'missing {label}: {needle}')
    else: checks.append(label)

for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
    req(read(rel),'<Version>3.2.7</Version>',f'3.2.7 version in {rel}')
major,minor,patch=map(int,'3.2.7'.split('.'))
if minor>=10 or patch>=10: failures.append('release version violates single-digit minor/patch policy')
else: checks.append('single-digit minor/patch release policy')
req(read('src/LocalGPT/LocalGPT.csproj'),'<DevExpressVersion>25.2.9</DevExpressVersion>','DevExpress 25.2.9 retention')

razor=read('src/LocalGPT/Components/Pages/RemoteControl.razor')
code=read('src/LocalGPT/Components/Pages/RemoteControl.razor.cs')
css=read('src/LocalGPT/Components/Pages/RemoteControl.razor.css')
req(razor,'@rendermode InteractiveServer','Remote Control InteractiveServer boundary')
for needle,label in [
    ('remote-control-workbench-layout','configuration workbench layout'),
    ('ConfigurationWorkbenchNav','configuration workbench navigation'),
    ('AddAllowedHost','guided allowed-host add command'),
    ('UseUrlHost','guided URL-host command'),
    ('RemoveAllowedHost','guided allowed-host remove command'),
    ('AddHeader','guided header add command'),
    ('RemoveHeader','guided header remove command'),
    ('AddAcceptJsonHeader','Accept JSON preset'),
    ('AddBearerHeader','Bearer token preset'),
    ('AddApiKeyHeader','API-key preset'),
]: req(razor+code,needle,label)
for raw in ['Allowed hosts JSON','Headers JSON templates']:
    if raw in razor: failures.append(f'raw JSON editor label still exposed: {raw}')
    else: checks.append(f'guided replacement for {raw}')
for needle,label in [
    ('LoadConnectorGuidedFields','guided-field deserialization'),
    ('ApplyConnectorGuidedFields','guided-field serialization'),
    ('JsonText','existing JSON text service ownership'),
]: req(code,needle,label)
for needle,label in [
    ('width: 100%;','full-width Remote Control page'),
    ('grid-template-columns: minmax(14rem, 18rem) minmax(0, 1fr);','desktop nav/editor width allocation'),
    ('@media (max-width: 980px)','responsive single-column breakpoint'),
    ('overflow-x: auto','mobile horizontal navigation'),
]: req(css,needle,label)
if re.search(r'max-width\s*:\s*1600px',css,re.I): failures.append('Remote Control still contains the old 1600px width ceiling')
else: checks.append('old 1600px Remote Control width ceiling removed')

# The shared MainLayout must not constrain routed page width.
layout=read('src/LocalGPT/Components/Layout/MainLayout.razor.css')
req(layout,'.page,','shared page width rule')
req(layout,'max-width: 100%;','shared main-window max-width contract')

locales=['en-US','de-DE','es-ES','fr-FR','ja-JP','uk-UA']
catalogs={c:json.loads((APP/f'Localization/{c}.json').read_text(encoding='utf-8-sig')) for c in locales}
sets=[set(catalogs[c]) for c in locales]
if not all(s==sets[0] for s in sets[1:]): failures.append('LocalGPT localization catalogs are not in exact key parity')
else: checks.append(f'six localization catalogs / {len(sets[0])}-key parity')
for key in ['RemoteControl.AllowedHosts','RemoteControl.Headers','RemoteControl.HeaderPresets','RemoteControl.UseUrlHost']:
    if key not in catalogs['en-US']: failures.append(f'missing Remote Control localization key {key}')
    else: checks.append(f'localized {key}')

if failures:
    print('LocalGPT 3.2.7 source release audit failed:')
    for failure in failures: print('  -',failure)
    raise SystemExit(1)
print(f'LocalGPT 3.2.7 source release audit passed: {len(checks)} checks.')
