#!/usr/bin/env python3
from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors=[]

def read(rel):
    p=ROOT/rel
    if not p.is_file():
        errors.append(f'missing file: {rel}')
        return ''
    return p.read_text(encoding='utf-8-sig', errors='replace')

def require(rel, marker):
    if marker not in read(rel): errors.append(f'{rel} missing marker: {marker}')

def forbid(rel, marker):
    if marker in read(rel): errors.append(f'{rel} contains forbidden marker: {marker}')

version=(3,7,8)
if version[1] > 9 or version[2] > 9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    require(rel,'<Version>3.7.8</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('localgptVersion') != '3.7.8': errors.append('docs/docfx.json localgptVersion != 3.7.8')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.7.8**'),('docs/pdf/toc.yml','LocalGPT-3.7.8.pdf'),
    ('RELEASE.md','# LocalGPT 3.7.8'),('CHANGELOG-v3.7.8-RELEASE-ORCHESTRATION-RESILIENCE.md','progress checkpoint'),
    ('VALIDATION-v3.7.8-source.md','# LocalGPT 3.7.8 source validation'),
    ('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.7.8'),
    ('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.7.8')):
    require(rel,mark)

native=read('build/NativeReleasePackaging.ps1')
for marker in (
    'function Test-MacNotaryCredentialRecoveryRequired',
    'function Wait-MacNotaryCredentialRecovery',
    'function Invoke-MacNotaryToolWithCredentialRecovery',
    'Read-Host "Unlock/approve the macOS Keychain prompt',
    "@('submit',$ArtifactPath)",
    'Save-MacNotaryState -ArtifactPath $ArtifactPath -SubmissionId $submissionId',
    'Apple upload completed; submission $submissionId was persisted immediately',
    'progress checkpoint only',
    'Continuing to wait; no upload or build work will be repeated',
    'Resuming Apple notarization submission',
    'Signed, notarized, stapled, and validated macOS',
):
    if marker not in native: errors.append(f'NativeReleasePackaging missing: {marker}')
for forbidden in ('notarytool submit $ArtifactPath @notaryArgs --wait --timeout','submit --wait --timeout'):
    if forbidden in native: errors.append(f'NativeReleasePackaging still contains single-process wait regression: {forbidden}')

trust=read('build/Initialize-MacReleaseTrust.ps1')
for marker in (
    "'future2-notary'", 'Test-MacNotaryCredentialRecoveryRequired', 'Test-MacNotaryTransientServiceRecoveryRequired', 'PSNativeCommandUseErrorActionPreference', 'Wait-MacNotaryCredentialRecovery',
    'Read-Host "Unlock/approve the macOS Keychain prompt', 'while ($true)',
    'Apple notarization credentials are ready through keychain profile',
):
    if marker not in trust: errors.append(f'Initialize-MacReleaseTrust missing: {marker}')

build=read('Build-Release.ps1')
for marker in ("build/Initialize-MacReleaseTrust.ps1", "-ProductName 'LocalGPT'", '-SelectedRuntimes @($Rid)', '-AllowUnsignedMacPackages:$AllowUnsignedMacPackages'):
    if marker not in build: errors.append(f'Build-Release missing: {marker}')

docs=read('build/Build-Documentation.ps1')
for marker in (
    'Invoke-LocalGptChunkedBrowserPdf', '[Parameter(Mandatory)][string]$ChunkCacheRoot',
    "Join-Path $documentationCacheEntryRoot 'browser-pdf-chunks'", 'Reusing durable documentation PDF chunk',
    "$tail.Contains('%%EOF', [StringComparison]::Ordinal)", 'Completed documentation PDF chunk', 'Durable chunks were retained for the next attempt',
    "$mergeArguments.Add('pdf-merge')", 'html-browser-chunked', 'cached-validated-pdf',
    'Find-LocalGptQpdf', 'Find-LocalGptGhostscript',
):
    if marker not in docs: errors.append(f'Build-Documentation missing: {marker}')

require('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj','<Version>1.0.2</Version>')
require('src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj','<PackageReference Include="PDFsharp" Version="6.2.4" />')
for marker in ('case "pdf-merge"','case "pdf-optimize"','PdfReader.Open','--optimize-images'):
    if marker not in read('src/LocalGPT.ReleasePackaging/Program.cs'): errors.append(f'packaging helper missing: {marker}')

pages_root=ROOT/'src/LocalGPT/Components/Pages'
for p in pages_root.rglob('*.razor'):
    text=p.read_text(encoding='utf-8-sig',errors='replace')
    if '@page' not in text: continue
    rel=p.relative_to(ROOT).as_posix()
    if p.name == 'Error.razor': continue
    if '@rendermode InteractiveServer' not in text: errors.append(f'routed page lost InteractiveServer: {rel}')

for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts: errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')

if errors:
    print('LocalGPT 3.7.8 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('LocalGPT 3.7.8 source audit passed.')
