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

version=(3,6,7)
if any(x>9 for x in version[1:]): errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.6.7</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    data=json.loads(text('docs/docfx.json'))
    if data.get('build',{}).get('globalMetadata',{}).get('localgptVersion')!='3.6.7':
        errors.append('docs/docfx.json localgptVersion is not 3.6.7')
except Exception as exc: errors.append(f'docs/docfx.json JSON parse failed: {exc}')
req('docs/pdf/toc.yml','LocalGPT-3.6.7.pdf')
req('docs/index.md','**Version 3.6.7**')
req('RELEASE.md','# LocalGPT 3.6.7')
req('CHANGELOG-v3.6.7-MACOS-GATEKEEPER-NOTARIZATION.md','Version advanced from 3.6.6 to 3.6.7.')
req('VALIDATION-v3.6.7-source.md','# LocalGPT 3.6.7 source validation')
req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.6.7')
req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.6.7')

doc=text('build/Build-Documentation.ps1')
for marker in (
    '$maximumBrowserPrintSourcePages = 1000',
    'function Ensure-LocalGptDocfxToolForPdfFallback',
    'manifest-pdf-fallback',
    'isolated-tool-path-pdf-fallback',
    'if (-not (Ensure-LocalGptDocfxToolForPdfFallback))',
    'DocFX PDF fallback is unavailable because no runnable DocFX command could be resolved.',
    '$pdfResult = Invoke-LocalGptDocfx -Arguments @("pdf", $configPath, "--logLevel", "verbose")',
    'localgpt-publisherstudio-docfx-pdf.lock',
    '$pdfTimeoutMilliseconds = 1800000',
    'Save-LocalGptDocumentationPdfCache',
):
    if marker not in doc: errors.append(f'build/Build-Documentation.ps1 missing: {marker}')
ensure_pos=doc.find('if (-not (Ensure-LocalGptDocfxToolForPdfFallback))')
invoke_pos=doc.find('$pdfResult = Invoke-LocalGptDocfx -Arguments @("pdf", $configPath, "--logLevel", "verbose")')
if ensure_pos < 0 or invoke_pos < 0 or ensure_pos > invoke_pos:
    errors.append('DocFX PDF invocation is not guarded by the lazy tool resolver')

# Preserve macOS architecture hardening and require Gatekeeper distribution hooks.
native=text('build/NativeReleasePackaging.ps1')
for marker in (
    'sysctl -n hw.optional.arm64',
    'sysctl -n sysctl.proc_translated',
    'LOCALGPT_NATIVE_REEXEC',
    'native-architecture-manifest.txt',
    'Exact offending file(s):',
    'Assert-MacBundleArchitecture $app $Rid',
    '<key>LSArchitecturePriority</key>',
    '"$BIN" --port 0',
):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing retained marker: {marker}')


for marker in (
    'MACOS_DEVELOPER_ID_APPLICATION', 'MACOS_DEVELOPER_ID_INSTALLER',
    'MACOS_NOTARY_KEYCHAIN_PROFILE', 'MACOS_REQUIRE_NOTARIZATION',
    'Developer ID Application:', 'Developer ID Installer:',
    'notarytool submit', 'stapler staple', 'stapler validate',
    'com.apple.security.cs.allow-jit', '--options runtime', '--check-signature', 'Complete-MacDistributionArtifact'
):
    if marker not in native: errors.append(f'build/NativeReleasePackaging.ps1 missing Gatekeeper marker: {marker}')
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
req('LICENSE.MD','This notice does not add a new DevExpress requirement beyond those terms.')
req('THIRD-PARTY-NOTICES.md','Current .NET package restore uses NuGet.org')

count=0
for p in (ROOT/'src/LocalGPT').rglob('*.razor'):
    count += p.read_text(encoding='utf-8-sig',errors='replace').count('@rendermode InteractiveServer')
if count != 15: errors.append(f'InteractiveServer occurrence count changed: expected 15, found {count}')

for bad in ('/bin/','/obj/'):
    pass

if errors:
    print('LocalGPT 3.6.7 static release audit FAILED:')
    for e in errors: print(' -',e)
    raise SystemExit(1)
print('LocalGPT 3.6.7 source audit passed.')
