#!/usr/bin/env python3
from pathlib import Path
import json, re, subprocess, sys, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
errors=[]

def text(rel):
    p=ROOT/rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig',errors='replace')

def req(rel,needle):
    if needle not in text(rel): errors.append(f'{rel} missing: {needle}')

def forbid(rel,needle):
    if needle in text(rel): errors.append(f'{rel} contains forbidden: {needle}')

version=(3,6,5)
if any(x>9 for x in version[1:]): errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.6.5</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    data=json.loads(text('docs/docfx.json'))
    if data.get('build',{}).get('globalMetadata',{}).get('localgptVersion')!='3.6.5':
        errors.append('docs/docfx.json localgptVersion is not 3.6.5')
except Exception as exc: errors.append(f'docs/docfx.json JSON parse failed: {exc}')
req('docs/pdf/toc.yml','LocalGPT-3.6.5.pdf')
req('docs/index.md','**Version 3.6.5**')
req('RELEASE.md','# LocalGPT 3.6.5')
req('CHANGELOG-v3.6.5-MACOS-ARCHITECTURE-FUTURE2-LICENSING.md','Version advanced from 3.6.4 to 3.6.5.')
req('VALIDATION-v3.6.5-source.md','# LocalGPT 3.6.5 source validation')

native=text('build/NativeReleasePackaging.ps1')
for marker in (
    'sysctl -n hw.optional.arm64',
    'sysctl -n sysctl.proc_translated',
    'LOCALGPT_NATIVE_REEXEC',
    'exec /usr/bin/arch -arm64 /bin/sh "$0" "$@"',
    'Runtime architecture check: hardware=$hardware process=$process_arch translated=$translated',
    'native-architecture-manifest.txt',
    'Exact offending file(s):',
    'Remove-NonTargetMacRuntimeAssets $app $Rid',
    'Assert-MacBundleArchitecture $app $Rid',
    '<key>LSArchitecturePriority</key>',
    '<key>LSRequiresNativeExecution</key><true/>',
    '"$BIN" --port 0',
    'probe="${candidate%/}/health"',
):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing: {marker}')
for bad in (
    'machine=$(/usr/bin/uname -m',
    'This installation contains an Apple-Silicon-only LocalGPT runtime on an Intel Mac',
    'This installation contains an Intel-only LocalGPT runtime on Apple Silicon',
    'sysctl -in sysctl.proc_translated',
):
    if bad in native: errors.append(f'build/NativeReleasePackaging.ps1 retains bad marker: {bad}')

for rel,cls in (
    ('src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs','DatabaseInitializationHostedService'),
    ('src/LocalGPT/Services/Council/RuntimeCapabilityDirectoryService.cs','RuntimeCapabilityDirectoryHostedService'),
    ('src/LocalGPT/Services/DxAiFunctionCatalogService.cs','DxAiFunctionCatalogHostedService'),
):
    value=text(rel)
    if not re.search(rf'class\s+{re.escape(cls)}[\s\S]*?\)\s*:\s*BackgroundService',value):
        errors.append(f'{rel}: {cls} is not a BackgroundService')
    if 'await Task.Delay(1, stoppingToken).ConfigureAwait(false);' not in value:
        errors.append(f'{rel}: missing policy-compliant startup handoff')

async_audit=subprocess.run([sys.executable,str(ROOT/'build/audit_async_continuations.py'),'--source-root',str(ROOT/'src/LocalGPT')],cwd=ROOT,text=True,capture_output=True)
if async_audit.returncode!=0:
    errors.append('async audit failed:\n'+async_audit.stdout+async_audit.stderr)

req('README.md','### Future2 mission')
req('README.md','centralized corporate or government service')
req('README.md','independently operated AI centers')
forbid('README.md','welcome side effect rather than the product pitch')
req('LICENSE.MD','This notice does not add a new DevExpress requirement beyond those terms.')
forbid('LICENSE.MD','To use the DevExpress-based version of LocalGPT, you need your own valid DevExpress license.')
req('THIRD-PARTY-NOTICES.md','Current .NET package restore uses NuGet.org')
forbid('THIRD-PARTY-NOTICES.md','package source may be required')

# Preserve maintained InteractiveServer map count from 3.6.4 (15 occurrences).
count=0
for p in (ROOT/'src/LocalGPT').rglob('*.razor'):
    count += p.read_text(encoding='utf-8-sig',errors='replace').count('@rendermode InteractiveServer')
if count != 15: errors.append(f'InteractiveServer occurrence count changed: expected 15, found {count}')

if errors:
    print('LocalGPT 3.6.5 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('LocalGPT 3.6.5 source audit passed.')
