from __future__ import annotations
from pathlib import Path
import subprocess, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
fail=[]
for script in ('build/audit_repository_build_storage.py','build/audit_powershell_portable_paths.py'):
    r=subprocess.run([sys.executable,str(ROOT/script)],cwd=ROOT)
    if r.returncode: fail.append(f'{script} failed')
if (ET.parse(ROOT/'src/LocalGPT/LocalGPT.csproj').findtext('.//Version') or '').strip()!='5.2.8': fail.append('LocalGPT.csproj version mismatch')
for rel in ('CHANGELOG-v5.2.8-ZERO-CONFIG-BUILD-STORAGE-PREFLIGHT.md','VALIDATION-v5.2.8-source.md','RELEASE.md'):
    if not (ROOT/rel).is_file(): fail.append(f'missing {rel}')
if fail:
    print('\n'.join('FAIL: '+x for x in fail),file=sys.stderr); raise SystemExit(1)
print('LocalGPT 5.2.8 zero-configuration build-storage preflight audit passed.')
