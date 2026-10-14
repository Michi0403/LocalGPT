#!/usr/bin/env python3
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
FAIL=[]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def req(ok,msg):
    if not ok: FAIL.append(msg)
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
    req('<Version>3.4.5</Version>' in read(rel), f'{rel} is not 3.4.5')
for rel in ['src/LocalGPT/Components/App.razor','docs/docfx.json','docs/pdf-cover.html','docs/pdf/toc.yml','docs/index.md','RELEASE.md']:
    req('3.4.5' in read(rel), f'{rel} current identity is not 3.4.5')
req('LocalGPT/3.4.5' in read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs'), 'outbound product marker is not 3.4.5')
req((ROOT/'CHANGELOG-v3.4.5-DOCFX-CONSOLE-PROGRESS-REPAIR.md').is_file(), '3.4.5 changelog missing')
req((ROOT/'VALIDATION-v3.4.5-source.md').is_file(), '3.4.5 validation missing')
doc=read('build/Build-Documentation.ps1')
req('$capturedOutput = [System.Collections.Generic.List[string]]::new()' in doc, 'DocFX diagnostic capture missing')
req(doc.count("$rawLine -split \"`r\"") == 2, 'both DocFX invocation paths must normalize carriage returns')
req(doc.count("(?:Removed|Copied)\\s+\\d+\\s+of\\s+\\d+\\s+files") == 2, 'both DocFX invocation paths must filter redirected transfer counters')
req('Output = @($capturedOutput.ToArray())' in doc, 'raw DocFX diagnostics are not preserved')
release=read('Build-Release.ps1')
req('.IndexOf($pdfName, [StringComparison]::OrdinalIgnoreCase) -ge 0' in release, 'PowerShell 5.1-safe PDF name check missing')
req('.Contains($pdfName, [StringComparison]::OrdinalIgnoreCase)' not in release, 'PowerShell 7-only Contains overload returned')
req('Prepare-LocalGptDocumentation' in release and 'Prepare-LocalGptDocumentation\n' in release, 'documentation build was removed from release path')
for v in re.findall(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', '\n'.join(read(x) for x in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'])):
    req(len(v[1])==1 and len(v[2])==1, f'two-digit minor/patch slot: {v}')
if FAIL: raise SystemExit('LocalGPT 3.4.5 audit failed:\n - '+'\n - '.join(FAIL))
print('LocalGPT 3.4.5 static release audit passed.')
