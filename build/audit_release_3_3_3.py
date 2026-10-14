from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]

def read(rel):
    return (root / rel).read_text(encoding='utf-8')

def require(cond, msg):
    if not cond:
        raise SystemExit('FAIL: ' + msg)

for project in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
]:
    require('<Version>3.3.3</Version>' in read(project), f'{project} is not 3.3.3')

release = read('Build-Release.ps1')
localdev = read('Build-LocalDevelopment.ps1')
docs = read('build/Build-Documentation.ps1')
app = read('src/LocalGPT/LocalGPT.csproj')

require('CopyLocalLockFileAssemblies=true' in release, 'release documentation build does not materialize package assemblies')
require('CopyLocalLockFileAssemblies=true' in localdev, 'development documentation source build does not materialize package assemblies')
require('Get-LocalGptUnresolvedAssemblyReferences' in docs, 'DocFX unresolved-reference scanner missing')
require('Repair-LocalGptDocfxAssemblyReferences' in docs, 'DocFX dependency repair missing')
require('dotnet --list-runtimes' in docs, 'installed shared-runtime probe discovery missing')
require('unresolvedAssemblyReferenceCount' in docs, 'documentation status unresolved-reference count missing')
require('docfxDependencyRepairCount' in docs, 'documentation status dependency-repair count missing')
require('unresolvedAssemblyReferenceCount -ne 0' in release, 'release unresolved-reference guard missing')
require('unresolvedAssemblyReferenceCount -ne 0' in localdev, 'development unresolved-reference guard missing')
require('PackageReference Include="System.Formats.Nrbf"' not in app, 'synthetic System.Formats.Nrbf app dependency was added')

# Version convention: every visible semantic version keeps minor and patch to one digit.
for version in re.findall(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', '\n'.join(read(p) for p in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
])):
    require(int(version[1]) <= 9 and int(version[2]) <= 9, f'invalid version convention: {version}')

require((root / 'CHANGELOG-v3.3.3-DOCFX-ASSEMBLY-REFERENCE-CLOSURE.md').is_file(), '3.3.3 changelog missing')
require((root / 'VALIDATION-v3.3.3-source.md').is_file(), '3.3.3 validation file missing')
print('LocalGPT 3.3.3 static release audit passed.')
