from pathlib import Path
import re
import unicodedata
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]

def read(rel):
    return (root / rel).read_text(encoding='utf-8')

def require(cond, msg):
    if not cond:
        raise AssertionError(msg)

projects = [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
]
for project in projects:
    text = read(project)
    require('<Version>3.3.6</Version>' in text, f'{project} is not 3.3.6')
    ET.fromstring(text)

app = read('src/LocalGPT/LocalGPT.csproj')
require('PackageReference Include="System.Formats.Nrbf"' not in app, 'application runtime graph gained a direct System.Formats.Nrbf reference')

dep_rel = 'docs/DocfxDependencies.csproj'
dep = read(dep_rel)
ET.fromstring(dep)
require('PackageReference Include="System.Formats.Nrbf" Version="10.0.11" PrivateAssets="all"' in dep,
        'DocFX dependency project does not pin System.Formats.Nrbf 10.0.11 as private tooling-only dependency')

prereq = read('build/Initialize-BuildPrerequisites.ps1')
require("'docs/DocfxDependencies.csproj'" in prereq, 'source preflight does not require DocfxDependencies.csproj')
require('Resolve-LocalGptNodeRuntime' in prereq and 'Initialize-DevExpressLicense.ps1' in prereq,
        'cross-platform Node/DevExpress prerequisite wiring regressed')

doc = read('build/Build-Documentation.ps1')
for marker in [
    '$docfxDependencyProjectPath = Join-Path $docsRoot "DocfxDependencies.csproj"',
    'function Initialize-LocalGptDocfxPinnedDependencies',
    "Resolve-LocalGptNuGetAssemblyReference -ReferenceName 'System.Formats.Nrbf'",
    '& dotnet restore $DependencyProjectPath --disable-parallel --force-evaluate',
    "-ReferenceNames @('System.Formats.Nrbf')",
    'DocFX metadata extraction failed before PDF generation because unresolved assembly references remain',
    'unresolvedAssemblyReferenceCount',
]:
    require(marker in doc, f'documentation pipeline missing marker: {marker}')

# The pinned probe must be staged before the first metadata call.
pinned_pos = doc.find('Initialize-LocalGptDocfxPinnedDependencies -DependencyProjectPath $docfxDependencyProjectPath')
metadata_pos = doc.find('$metadataResult = Invoke-LocalGptDocfxWithRetry -Arguments @("metadata", $configPath)', pinned_pos)
require(pinned_pos >= 0 and metadata_pos > pinned_pos, 'pinned DocFX probe is not initialized before metadata extraction')

release = read('Build-Release.ps1')
localdev = read('Build-LocalDevelopment.ps1')
for label, content in [('release', release), ('local development', localdev)]:
    require('Initialize-BuildPrerequisites.ps1' in content, f'{label} build lost shared prerequisite initialization')
    require('CopyLocalLockFileAssemblies=true' in content, f'{label} documentation build lost package assembly materialization')

require('js/localgpt-chat-ui.js?v=3.3.6' in read('src/LocalGPT/Components/App.razor'), 'browser cache marker is not 3.3.6')
require('LocalGPT/3.3.6' in read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs'), 'outbound product version is not 3.3.6')
require('pdfFileName: LocalGPT-3.3.6.pdf' in read('docs/pdf/toc.yml'), 'PDF TOC version marker is not 3.3.6')
require('"localgptVersion": "3.3.6"' in read('docs/docfx.json'), 'DocFX version marker is not 3.3.6')
require((root / 'CHANGELOG-v3.3.6-DOCFX-PINNED-DEPENDENCY-PROBE.md').is_file(), '3.3.6 changelog missing')
require((root / 'VALIDATION-v3.3.6-source.md').is_file(), '3.3.6 validation missing')
require('# LocalGPT 3.3.6' in read('RELEASE.md'), 'RELEASE.md is not 3.3.6')

# Source docs required by the clean archive path must all exist.
for rel in [
    'docs/index.md','docs/docfx.json','docs/DocfxDependencies.csproj','docs/toc.yml','docs/pdf/toc.yml',
    'docs/architecture/system-overview.md','docs/architecture/council-runtime.md','docs/architecture/frontend-and-themes.md',
    'docs/architecture/ai-host.md','docs/architecture/project-data.md','docs/architecture/onewire-security.md',
    'docs/engineering/build-validation.md','docs/reference/capability-map.md',
]:
    require((root / rel).is_file(), f'missing required documentation source: {rel}')

# No source-tree case/Unicode normalized collisions (important for Finder/APFS extraction).
seen = {}
for p in root.rglob('*'):
    if not p.is_file():
        continue
    rel = p.relative_to(root).as_posix()
    key = unicodedata.normalize('NFC', rel).casefold()
    if key in seen and seen[key] != rel:
        raise AssertionError(f'case/unicode source collision: {seen[key]} vs {rel}')
    seen[key] = rel

print('LocalGPT 3.3.6 static release audit passed.')
