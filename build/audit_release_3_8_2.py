#!/usr/bin/env python3
from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
errors=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file(): errors.append(f'missing file: {rel}'); return ''
    return p.read_text(encoding='utf-8-sig', errors='replace')
def req(rel,m):
    if m not in read(rel): errors.append(f'{rel} missing marker: {m}')
def forbid_all(pattern, message):
    rx=re.compile(pattern,re.I|re.M)
    for p in ROOT.rglob('*'):
        if p.suffix.lower() not in ('.ps1','.psm1') or not p.is_file(): continue
        rel=p.relative_to(ROOT).as_posix()
        if re.search(r'(^|/)(bin|obj|artifacts|packages|node_modules)(/|$)',rel): continue
        text=p.read_text(encoding='utf-8-sig',errors='replace')
        for m in rx.finditer(text):
            line=text.count('\n',0,m.start())+1
            errors.append(f'{rel}:{line} {message}')
version=(3, 8, 2)
if version[1]>9 or version[2]>9: errors.append('version violates one-digit minor/patch policy')
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'):
    req(rel,'<Version>3.8.2</Version>')
    try: ET.parse(ROOT/rel)
    except Exception as exc: errors.append(f'{rel} XML parse failed: {exc}')
try:
    meta=json.loads(read('docs/docfx.json')).get('build',{}).get('globalMetadata',{})
    if meta.get('localgptVersion')!='3.8.2': errors.append('docs/docfx.json localgptVersion != 3.8.2')
except Exception as exc: errors.append(f'docfx json parse failed: {exc}')
for rel,mark in (
    ('docs/index.md','**Version 3.8.2**'),('docs/pdf/toc.yml','LocalGPT-3.8.2.pdf'),
    ('RELEASE.md','# LocalGPT 3.8.2'),('CHANGELOG-v3.8.2-ARTIFACT-LOCAL-NOTARY-TRANSACTIONS.md','artifact-local notarization transactions'),
    ('VALIDATION-v3.8.2-source.md','# LocalGPT 3.8.2 source validation'),
    ('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.8.2'),
    ('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.8.2')): req(rel,mark)

docs=read('build/Build-Documentation.ps1')
for marker in (
    "$tail.IndexOf('%%EOF', [StringComparison]::Ordinal) -ge 0",
    'function Set-PortableProcessArguments','GetProperty(\'ArgumentList\')',
    'function Stop-PortableProcessTree',"GetMethod('Kill', [Type[]]@([bool]))",
    'Invoke-LocalGptChunkedBrowserPdf','Reusing durable documentation PDF chunk','html-browser-chunked'):
    if marker not in docs: errors.append(f'Build-Documentation missing: {marker}')
native=read('build/NativeReleasePackaging.ps1')
for marker in ('function Get-RelativePathPortable',"GetMethod('GetRelativePath'",'function Invoke-MacNotaryToolWithCredentialRecovery','function Save-MacNotarySubmittedState'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing: {marker}')
compat=read('build/Assert-PowerShellCompatibility.ps1')
for marker in ('$unsupportedContainsPattern','$unsupportedPathRelativePattern','$unsupportedArgumentListPattern','$unsupportedKillTreePattern','Windows PowerShell 5.1 and modern pwsh'):
    if marker not in compat: errors.append(f'compatibility guard missing: {marker}')
# Direct runtime APIs that are absent from Windows PowerShell 5.1/.NET Framework are forbidden.
forbid_all(r'\.Contains\([^\r\n]*,\s*\[(?:System\.)?StringComparison\]::','uses incompatible String.Contains comparison overload')
forbid_all(r'\[(?:System\.)?IO\.Path\]::GetRelativePath\s*\(','uses direct Path.GetRelativePath')
forbid_all(r'\.ArgumentList(?:\.|\s*=)','uses direct ProcessStartInfo.ArgumentList')
forbid_all(r'\.Kill\(\s*\$true\s*\)','uses direct Process.Kill(true)')
# Common PowerShell 7-only language/cmdlet surfaces must not enter maintained scripts.
forbid_all(r'ForEach-Object\s+-Parallel\b','uses PowerShell 7-only ForEach-Object -Parallel')
forbid_all(r'ConvertFrom-Json\s+[^\r\n]*-AsHashtable\b','uses PowerShell 6+ ConvertFrom-Json -AsHashtable')
forbid_all(r'\bJoin-String\b','uses PowerShell 6+ Join-String')
forbid_all(r'\bTest-Json\b','uses PowerShell 6+ Test-Json')
forbid_all(r'\$PSStyle\b','uses PowerShell 7.2+ PSStyle')


# Notarization is a per-artifact transaction. `submit` is non-idempotent and must never be
# run through a retry loop. Pending state is written before upload and ambiguous local results
# are reconciled through idempotent Apple history queries.
build_release=read('Build-Release.ps1')
if build_release.count("build/Initialize-MacReleaseTrust.ps1") != 1: errors.append('Build-Release must invoke Initialize-MacReleaseTrust exactly once')
trust=read('build/Initialize-MacReleaseTrust.ps1')
if 'Read-Host' in trust: errors.append('Initialize-MacReleaseTrust must not block on Read-Host')
native=read('build/NativeReleasePackaging.ps1')
for marker in (
    'function Invoke-MacNotaryToolOnce',
    'function Invoke-MacNotaryToolWithCredentialRecovery',
    'function New-MacNotaryPendingState',
    'function Save-MacNotarySubmittedState',
    'function Get-MacNotaryHistoryEntries',
    'function Resolve-MacNotaryPendingSubmission',
    'baselineSubmissionIds',
    "phase = 'submit-pending'",
    'Submitting $artifactName to Apple notary service exactly once for this transaction',
    'no duplicate upload was issued',
    "Get-MacNotaryObjectPropertyText -InputObject $info -PropertyName 'status'",
    'Set-MacNotaryStatePhase -ArtifactPath $ArtifactPath -State $state -Phase complete'):
    if marker not in native: errors.append(f'NativeReleasePackaging missing artifact-local notary marker: {marker}')
if 'Read-Host' in native: errors.append('NativeReleasePackaging notarization must not block on Read-Host')
if 'Assert-MacNotaryCredentialsUsable' in native: errors.append('redundant pre-submit notary assertion must stay removed')
# Exactly one executable submit argument construction exists in the native packager.
submit_lines=[line for line in native.splitlines() if "@('submit',$ArtifactPath)" in line]
if len(submit_lines)!=1: errors.append(f'expected exactly one notary submit construction, found {len(submit_lines)}')
# The submit call itself must use Invoke-MacNotaryToolOnce, while recovery wrapper call sites are
# limited to history/info/log (idempotent operations).
if 'Invoke-MacNotaryToolOnce -Operation "single upload of $artifactName" -Arguments $submitArguments' not in native:
    errors.append('notary submit is not using the one-shot invocation path')
for m in re.finditer(r'Invoke-MacNotaryToolWithCredentialRecovery[^\n]*', native):
    line=m.group(0)
    if line.startswith('Invoke-MacNotaryToolWithCredentialRecovery('):
        continue
    if not any(token in line for token in ('submission-history query','status query for submission','failure-log download')):
        errors.append(f'retry wrapper used outside idempotent notary query: {line.strip()}')
for forbidden in ('$submit.status','$info.status','$state.submissionId','$state.artifactSha256'):
    if forbidden in native: errors.append(f'NativeReleasePackaging reintroduced StrictMode-unsafe optional property access: {forbidden}')
# Completed state is retained as a hash-bound audit/resume record; it must not be deleted after staple.
if re.search(r'(?m)^\s*Remove-MacNotaryState\s+\$ArtifactPath\s*$', native):
    errors.append('completed artifact state is still deleted instead of retained')
pages=ROOT/'src/LocalGPT/Components/Pages'
for p in pages.rglob('*.razor'):
    t=p.read_text(encoding='utf-8-sig',errors='replace')
    if '@page' in t and p.name!='Error.razor' and '@rendermode InteractiveServer' not in t:
        errors.append(f'routed page lost InteractiveServer: {p.relative_to(ROOT).as_posix()}')
for p in ROOT.rglob('*'):
    if p.is_dir() and p.name in ('bin','obj') and 'src' in p.parts: errors.append(f'repository-local build state present: {p.relative_to(ROOT)}')
if errors:
    print('LocalGPT 3.8.2 static release audit FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('LocalGPT 3.8.2 source audit passed.')
