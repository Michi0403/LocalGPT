#!/usr/bin/env python3
from pathlib import Path
import json
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

def text(rel):
    p = ROOT / rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig', errors='replace')

def req(rel, needle):
    if needle not in text(rel):
        errors.append(f'{rel} missing: {needle}')

def forbid(rel, needle):
    if needle in text(rel):
        errors.append(f'{rel} contains forbidden: {needle}')

version = (3, 6, 0)
if any(x > 9 for x in version[1:]):
    errors.append('version violates one-digit minor/patch policy')

for rel in (
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
):
    req(rel, '<Version>3.6.0</Version>')
    try:
        ET.parse(ROOT / rel)
    except Exception as exc:
        errors.append(f'{rel} XML parse failed: {exc}')

req('docs/docfx.json', '"localgptVersion": "3.6.0"')
req('docs/pdf/toc.yml', 'LocalGPT-3.6.0.pdf')
req('src/LocalGPT/Components/App.razor', 'localgpt-chat-ui.js?v=3.6.0')
req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.6.0')
req('RELEASE.md', 'CHANGELOG-v3.6.0-MACOS-COORDINATOR-NATIVE-PACKAGING.md')
req('RELEASE.md', 'VALIDATION-v3.6.0-source.md')
text('CHANGELOG-v3.6.0-MACOS-COORDINATOR-NATIVE-PACKAGING.md')
text('VALIDATION-v3.6.0-source.md')

# No forbidden two-digit patch rollover is allowed to become an active version.
for p in ROOT.rglob('*'):
    if not p.is_file() or any(part in {'.git', 'artifacts'} for part in p.parts):
        continue
    if p.name.startswith(('CHANGELOG-', 'VALIDATION-', 'audit_release_')):
        continue
    if p.suffix.lower() not in {'.ps1','.psm1','.cs','.csproj','.razor','.md','.json','.yml','.yaml','.txt','.cmd','.sh','.py','.html'}:
        continue
    try:
        data = p.read_text(encoding='utf-8-sig', errors='replace')
    except Exception:
        continue
    if '3.5.10' in data:
        errors.append(f'forbidden rollover version 3.5.10 found in {p.relative_to(ROOT)}')

release = text('Build-Release.ps1')
for marker in (
    '$ProgressPreference = "SilentlyContinue"',
    '$buildStateDirectories = @(',
    'Remove-Item -LiteralPath $buildStateDirectory.FullName -Recurse -Force -ErrorAction Stop',
    'Durable documentation caches outside bin/obj were preserved.',
):
    if marker not in release:
        errors.append(f'Build-Release.ps1 missing deterministic cleanup marker: {marker}')

doc = text('build/Build-Documentation.ps1')
for marker in (
    '$ProgressPreference = "SilentlyContinue"',
    'payload-cache/LocalGPT',
    'Get-LocalGptDocumentationCacheKey',
    'Save-LocalGptDocumentationHtmlCache',
    'Save-LocalGptDocumentationPdfCache',
    'Reused durable LocalGPT DocFX HTML cache',
    'Skipping DocFX tool restore because validated LocalGPT HTML was restored from the durable documentation cache.',
    '$pdfTimeoutMilliseconds = 1800000',
    '$configuredPdfTimeout -gt 0',
    'localgpt-publisherstudio-docfx-pdf.lock',
    'Enter-LocalGptSharedPdfLock',
    'cached-validated-pdf',
):
    if marker not in doc:
        errors.append(f'build/Build-Documentation.ps1 missing resilience marker: {marker}')
for forbidden in (
    '$pdfTimeoutMilliseconds = if ($isMacOsHost) { 300000 } else { 1800000 }',
    'elseif ($docfxBuildSucceeded) {\n        $warnings.Add("Complete PDF generation was explicitly disabled',
):
    if forbidden in doc:
        errors.append(f'build/Build-Documentation.ps1 retains broken marker: {forbidden}')
if doc.find('Save-LocalGptDocumentationHtmlCache') > doc.find('Enter-LocalGptSharedPdfLock'):
    errors.append('durable HTML cache is not committed before the long PDF-render lock/stage')

native = text('build/NativeReleasePackaging.ps1')
for marker in (
    "$ProgressPreference = 'SilentlyContinue'",
    'function New-Dmg',
    'hdiutil create -volname $volumeName -srcfolder $stage -ov -format UDZO',
    'hdiutil verify $Destination',
    "New-Item -ItemType SymbolicLink -Path (Join-Path $stage 'Applications') -Target '/Applications'",
    'function New-MacPkg',
    '--root $pkgRoot',
    "--install-location '/'",
    'pkgutil --payload-files',
    '$stagedInfoPlist = Join-Path $applicationsRoot "$appName/Contents/Info.plist"',
):
    if marker not in native:
        errors.append(f'build/NativeReleasePackaging.ps1 missing packaging marker: {marker}')
for forbidden in (
    'tell application "Finder"',
    'set background picture of theViewOptions',
    'hdiutil attach $rwDmg',
    'hdiutil convert $rwDmg',
    '--component $AppPath',
):
    if forbidden in native:
        errors.append(f'build/NativeReleasePackaging.ps1 retains unreliable packaging path: {forbidden}')

# Preserve critical existing render-mode boundaries and packaging helper version.
for rel in ('Components/Pages/Chat.razor','Components/Pages/Database.razor','Components/Pages/Help.razor','Components/Pages/ModelCouncil.razor'):
    req('src/LocalGPT/' + rel, '@rendermode InteractiveServer')
req('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj', '<Version>1.0.1</Version>')

try:
    json.loads(text('docs/docfx.json'))
except Exception as exc:
    errors.append(f'docs/docfx.json JSON parse failed: {exc}')

if errors:
    print('LocalGPT 3.6.0 static release audit FAILED:')
    for error in errors:
        print(' -', error)
    raise SystemExit(1)
print('LocalGPT 3.6.0 static release audit passed.')
