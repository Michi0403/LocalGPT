from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
fail=[]
def need(path,text,label=None):
    s=(root/path).read_text(encoding='utf-8')
    if text not in s: fail.append(label or f"{path}: missing {text!r}")
    return s
need(Path('src/LocalGPT/LocalGPT.csproj'), '<Version>4.0.6</Version>')
need(Path('src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj'), '<Version>4.0.6</Version>')
need(Path('CHANGELOG-v4.0.6-INSTALLER-COMPILE-PREFLIGHT.md'), '4.0.6')
need(Path('VALIDATION-v4.0.6-source.md'), '4.0.6')
provider=need(Path('src/LocalGPTInstallerConsole/Helper/SetupFileLoggerProvider.cs'), 'using System;')
for token in ('Exception?', 'Func<TState, Exception?, string>', 'IDisposable?'):
    if token not in provider: fail.append(f'SetupFileLoggerProvider: expected compile-sensitive token missing: {token}')
build=need(Path('Build-Release.ps1'), 'Preflighting the LocalGPT installer compile before expensive documentation and native packaging...')
pre=build.find('Preflighting the LocalGPT installer compile before expensive documentation and native packaging...')
docs=build.find('Prepare-LocalGptDocumentation', pre)
if pre < 0 or docs < 0 or pre > docs: fail.append('Build-Release.ps1: setup compile preflight must occur before documentation generation')
if fail:
    print('LocalGPT 4.0.6 release audit FAILED:'); print('\n'.join(' - '+x for x in fail)); sys.exit(1)
print('LocalGPT 4.0.6 release audit passed: installer namespace compile repair, versioning, and early setup compile preflight are present.')
