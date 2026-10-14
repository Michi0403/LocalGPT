from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]

def read(rel):
    return (root / rel).read_text(encoding='utf-8-sig')

def require(condition, message):
    if not condition:
        raise SystemExit('FAIL: ' + message)

for project in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
]:
    require('<Version>3.3.4</Version>' in read(project), f'{project} is not 3.3.4')

require('js/localgpt-chat-ui.js?v=3.3.4' in read('src/LocalGPT/Components/App.razor'), 'browser cache marker is not 3.3.4')
require('LocalGPT/3.3.4' in read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs'), 'outbound LocalGPT product version is not 3.3.4')

release = read('Build-Release.ps1')
localdev = read('Build-LocalDevelopment.ps1')
docs = read('build/Build-Documentation.ps1')
node = read('build/NodeRuntime.Common.ps1')
preflight = read('build/Initialize-BuildPrerequisites.ps1')
app = read('src/LocalGPT/LocalGPT.csproj')

for content, label in [(release, 'release'), (localdev, 'development')]:
    require('build/Initialize-BuildPrerequisites.ps1' in content, f'{label} build does not run prerequisite bootstrap')
    require('CopyLocalLockFileAssemblies=true' in content, f'{label} documentation build lost assembly materialization')
    require('unresolvedAssemblyReferenceCount -ne 0' in content, f'{label} unresolved-reference guard missing')

require('Initialize-DevExpressLicense.ps1' in preflight, 'DevExpress license preflight missing from shared build prerequisites')
require("Version '22.23.2'" in preflight, 'Node 22.23.2 bootstrap version missing')
require('Resolve-LocalGptNodeRuntime' in preflight, 'shared Node resolver not used by build prerequisites')
require('Resolve-LocalGptNodeRuntime' in docs, 'documentation pipeline does not use shared Node resolver')
require('NodeRuntime.Common.ps1' in docs, 'documentation pipeline does not load shared Node helper')
require('nodePlatform = $nodePlatformUsed' in docs, 'documentation status Node platform missing')
require('nodeArchitecture = $nodeArchitectureUsed' in docs, 'documentation status Node architecture missing')
require("{ 1500 } else { 1000 }" in docs, 'host-specific browser print threshold missing')
require('using the DocFX PDF plug-in directly' in docs, 'large Unix documentation direct plugin routing missing')

for token in [
    "'win'",
    "'darwin'",
    "'linux'",
    "'arm64'",
    "'x64'",
    "'x86'",
    'SHASUMS256.txt',
    'Get-FileHash',
    'Expand-Archive',
    "Get-Command tar",
    'PLAYWRIGHT_NODEJS_PATH',
    '[EnvironmentVariableTarget]::Process',
]:
    require(token in node, f'Node runtime helper missing {token}')

require('node-v$Version-$($hostInfo.Platform)-$($hostInfo.Architecture)' in node, 'Node distribution naming is not platform/architecture aware')
require('https://nodejs.org/download/release/v$Version' in node, 'official Node release origin missing')
require('Node.js archive checksum mismatch' in node, 'archive checksum rejection missing')
require('Automatic Node.js provisioning is currently Windows-only' not in docs, 'Windows-only Node provisioning error remains')
require('PackageReference Include="System.Formats.Nrbf"' not in app, 'synthetic System.Formats.Nrbf app dependency was added')
require('Repair-LocalGptDocfxAssemblyReferences' in docs, '3.3.3 DocFX assembly repair was lost')
require('Get-LocalGptUnresolvedAssemblyReferences' in docs, '3.3.3 unresolved-reference scan was lost')

# Keep PowerShell 7 automatic variables protected and Join-Path path literals portable.
readonly_pattern = re.compile(r'(?i)\$(?:IsWindows|IsLinux|IsMacOS|IsCoreCLR)\s*=')
for ps1 in root.rglob('*.ps1'):
    relative = ps1.relative_to(root).as_posix()
    if any(part in {'.git', 'artifacts', 'bin', 'obj', 'packages', 'node_modules'} for part in ps1.parts):
        continue
    content = ps1.read_text(encoding='utf-8-sig')
    require(not readonly_pattern.search(content), f'{relative} assigns to a protected PowerShell platform variable')
    for line_number, line in enumerate(content.splitlines(), 1):
        if 'join-path' not in line.lower():
            continue
        for quoted in re.finditer(r'(["\'])(?P<value>[^"\']*\\[^"\']*)\1', line):
            value = quoted.group('value')
            if value.startswith('[') or '(?:' in value:
                continue
            raise SystemExit(f'FAIL: {relative}:{line_number} contains a backslash path literal in Join-Path: {value}')

# Version convention: minor and patch are always single-digit slots.
for version in re.findall(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', '\n'.join(read(p) for p in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
])):
    require(int(version[1]) <= 9 and int(version[2]) <= 9, f'invalid version convention: {version}')

require((root / 'CHANGELOG-v3.3.4-CROSS-PLATFORM-DOCUMENTATION-RUNTIME.md').is_file(), '3.3.4 changelog missing')
require((root / 'VALIDATION-v3.3.4-source.md').is_file(), '3.3.4 validation file missing')
require('# LocalGPT 3.3.4' in read('RELEASE.md'), 'RELEASE.md is not 3.3.4')
print('LocalGPT 3.3.4 static release audit passed.')
