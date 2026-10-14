from pathlib import Path
import json, re

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
    require('<Version>3.3.5</Version>' in read(project), f'{project} is not 3.3.5')

require('js/localgpt-chat-ui.js?v=3.3.5' in read('src/LocalGPT/Components/App.razor'), 'browser cache marker is not 3.3.5')
require('LocalGPT/3.3.5' in read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs'), 'outbound LocalGPT product version is not 3.3.5')

release = read('Build-Release.ps1')
localdev = read('Build-LocalDevelopment.ps1')
docs_build = read('build/Build-Documentation.ps1')
node = read('build/NodeRuntime.Common.ps1')
preflight = read('build/Initialize-BuildPrerequisites.ps1')
app = read('src/LocalGPT/LocalGPT.csproj')

required_docs = [
    'docs/index.md', 'docs/docfx.json', 'docs/toc.yml', 'docs/pdf/toc.yml', 'docs/pdf-cover.html',
    'docs/architecture/system-overview.md', 'docs/architecture/council-runtime.md',
    'docs/architecture/frontend-and-themes.md', 'docs/architecture/ai-host.md',
    'docs/architecture/project-data.md', 'docs/architecture/onewire-security.md',
    'docs/engineering/build-validation.md', 'docs/reference/capability-map.md',
    'docs/templates/localgpt/public/main.css', 'docs/templates/localgpt/public/main.js',
    'docs/templates/localgpt/public/favicon.ico', 'docs/templates/localgpt/public/favicon.svg',
    'docs/templates/localgpt/public/logo.svg',
]
for rel in required_docs:
    require((root / rel).is_file(), f'missing required documentation source: {rel}')

conceptual = [p for p in (root/'docs').rglob('*.md') if 'api' not in p.parts]
require(len(conceptual) >= 26, f'expected restored conceptual documentation set, found {len(conceptual)} markdown files')

config = json.loads(read('docs/docfx.json'))
require(isinstance(config.get('metadata'), list), 'docfx metadata must be an array')
meta = config['metadata'][0]
require(meta.get('namespaceLayout') == 'nested', 'docfx namespaceLayout is not nested')
require(meta.get('memberLayout') == 'samePage', 'docfx memberLayout is not samePage')
require('modern' in config.get('build', {}).get('template', []), 'docfx modern template missing')

root_toc = read('docs/toc.yml')
pdf_toc = read('docs/pdf/toc.yml')
require(re.search(r'(?m)^\s*href:\s*guide/\s*$', root_toc), 'root toc missing guide include')
require(re.search(r'(?m)^\s*href:\s*api/\s*$', root_toc), 'root toc missing api include')
require(re.search(r'(?m)^\s*href:\s*\.\./guide/toc\.yml\s*$', pdf_toc), 'pdf toc missing guide toc')
require(re.search(r'(?m)^\s*href:\s*\.\./api/toc\.yml\s*$', pdf_toc), 'pdf toc missing api toc')
require('pdfFileName: LocalGPT-3.3.5.pdf' in pdf_toc, 'pdf toc versioned file name is not 3.3.5')

for content, label in [(release, 'release'), (localdev, 'development')]:
    require('build/Initialize-BuildPrerequisites.ps1' in content, f'{label} build does not run prerequisite bootstrap')
    require('CopyLocalLockFileAssemblies=true' in content, f'{label} documentation build lost assembly materialization')
    require('unresolvedAssemblyReferenceCount -ne 0' in content, f'{label} unresolved-reference guard missing')

require('requiredDocumentationSources' in preflight, 'documentation source preflight missing')
require('source archive is incomplete' in preflight, 'incomplete-source diagnostic missing')
require('Initialize-DevExpressLicense.ps1' in preflight, 'DevExpress license preflight missing')
require("Version '22.23.2'" in preflight, 'Node 22.23.2 bootstrap version missing')
require('Resolve-LocalGptNodeRuntime' in preflight and 'Resolve-LocalGptNodeRuntime' in docs_build, 'shared Node resolver wiring missing')

for token in ["'win'", "'darwin'", "'linux'", "'arm64'", "'x64'", 'SHASUMS256.txt', 'Get-FileHash', 'PLAYWRIGHT_NODEJS_PATH']:
    require(token in node, f'Node runtime helper missing {token}')

require('Resolve-LocalGptNuGetAssemblyReference' in docs_build, 'NuGet assembly-reference resolver missing')
require('$env:NUGET_PACKAGES' in docs_build, 'explicit NUGET_PACKAGES probe missing')
require("'.nuget/packages'" in docs_build, 'default per-user NuGet package cache probe missing')
require("net10\\.0" in docs_build, 'net10.0 NuGet assembly preference missing')
require('Repair-LocalGptDocfxAssemblyReferences' in docs_build, 'DocFX assembly repair missing')
require('Get-LocalGptUnresolvedAssemblyReferences' in docs_build, 'unresolved-reference scan missing')
require('PackageReference Include="System.Formats.Nrbf"' not in app, 'synthetic System.Formats.Nrbf app dependency was added')

readonly_pattern = re.compile(r'(?i)\$(?:IsWindows|IsLinux|IsMacOS|IsCoreCLR)\s*=')
for ps1 in root.rglob('*.ps1'):
    relative = ps1.relative_to(root).as_posix()
    if any(part in {'.git', 'artifacts', 'bin', 'obj', 'packages', 'node_modules'} for part in ps1.parts):
        continue
    content = ps1.read_text(encoding='utf-8-sig')
    require(not readonly_pattern.search(content), f'{relative} assigns to a protected PowerShell platform variable')

for version in re.findall(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', '\n'.join(read(p) for p in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
])):
    require(int(version[1]) <= 9 and int(version[2]) <= 9, f'invalid version convention: {version}')

require((root / 'CHANGELOG-v3.3.5-DOCUMENTATION-SOURCE-PACKAGE-REPAIR.md').is_file(), '3.3.5 changelog missing')
require((root / 'VALIDATION-v3.3.5-source.md').is_file(), '3.3.5 validation file missing')
require('# LocalGPT 3.3.5' in read('RELEASE.md'), 'RELEASE.md is not 3.3.5')
print('LocalGPT 3.3.5 static release audit passed.')
