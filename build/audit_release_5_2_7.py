from __future__ import annotations
from pathlib import Path
import re, subprocess, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
failures=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file(): failures.append(f'missing {rel}'); return ''
    return p.read_text(encoding='utf-8-sig')
def require(rel,token,label):
    if token not in read(rel): failures.append(f'{label} missing from {rel}: {token}')
for script in ('build/audit_repository_build_storage.py','build/audit_powershell_portable_paths.py'):
    r=subprocess.run([sys.executable,str(ROOT/script)],text=True,capture_output=True)
    if r.returncode: failures.append(f'{script} failed: '+(r.stderr or r.stdout).strip())
r=subprocess.run([sys.executable,str(ROOT/'build/audit_transient_ui_state_ownership.py'),'--root',str(ROOT),'--product','localgpt'],text=True,capture_output=True)
if r.returncode: failures.append('transient UI-state ownership audit failed: '+(r.stderr or r.stdout).strip())
for rel,token,label in (
 ('build/RepositoryBuildStorage.Common.ps1',"Join-Path $repository 'artifacts/.build-storage'",'repository-local default storage'),
 ('Build-Release.ps1','Initialize-Future2RepositoryBuildStorage','release storage initialization'),
 ('build/Build-Documentation.ps1','Initialize-Future2RepositoryBuildStorage','documentation storage initialization'),
 ('build/Assert-PowerShellCompatibility.ps1','repository build-storage contract','maintenance guard'),
 ('AGENTS.md','## Repository-local release-build storage','architecture documentation'),
 ('CHANGELOG-v5.2.7-REPOSITORY-BUILD-STORAGE.md','# LocalGPT 5.2.7','changelog'),
 ('RELEASE.md','# LocalGPT 5.2.7','release notes'),
 ('VALIDATION-v5.2.7-source.md','# LocalGPT 5.2.7 source validation','validation'),
): require(rel,token,label)
version='5.2.7'
for rel in ('src/LocalGPT/LocalGPT.csproj','src/LocalGPT/Components/App.razor','src/LocalGPT/Components/Shared/ChatMicrophone.razor'):
    if version not in read(rel): failures.append(f'{version} active identity missing from {rel}')
m=re.fullmatch(r'(\d+)\.(\d+)\.(\d+)',version)
if not m or int(m.group(2))>9 or int(m.group(3))>9: failures.append('version violates single-digit minor/patch policy')
try:
    project=ET.fromstring((ROOT/'src/LocalGPT/LocalGPT.csproj').read_text(encoding='utf-8-sig'))
    if project.findtext('.//Version')!=version: failures.append('LocalGPT.csproj version mismatch')
    ET.parse(ROOT/'Directory.Build.targets')
except Exception as exc: failures.append(f'XML parse failure: {exc}')
if failures:
    for f in failures: print('FAIL: '+f,file=sys.stderr)
    raise SystemExit(1)
print('LocalGPT 5.2.7 repository-build-storage audit passed: heavy release caches/temp follow the repository, maintenance guards protect the contract, transient UI ownership remains intact, and active identity is aligned.')
