from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
failures = []

def read(rel):
    return (root / rel).read_text(encoding='utf-8')

def require(condition, message):
    if not condition:
        failures.append(message)

for project in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
]:
    require('<Version>3.3.8</Version>' in read(project), f'{project} is not 3.3.8')

doc = read('build/Build-Documentation.ps1')
compat = read('build/Assert-PowerShellCompatibility.ps1')
require('${restoreExitCode}: $DependencyProjectPath' in doc, 'DocFX restore error interpolation is not delimited')
require('$restoreExitCode: $DependencyProjectPath' not in doc, 'invalid DocFX restore interpolation remains')
require('System.Management.Automation.Language.Parser]::ParseInput' in compat, 'PowerShell parser preflight is missing')
require('has a PowerShell parser error' in compat, 'PowerShell parser diagnostics are missing')
require((root / 'docs/DocfxDependencies.csproj').is_file(), 'DocFX dependency project is missing')
probe = read('docs/DocfxDependencies.csproj')
require('System.Formats.Nrbf' in probe and 'Version="10.0.11"' in probe and 'PrivateAssets="all"' in probe, 'DocFX NRBF probe is not pinned correctly')
require('System.Formats.Nrbf' not in read('src/LocalGPT/LocalGPT.csproj'), 'LocalGPT runtime project gained direct NRBF dependency')

require('[Complete API reference](../api/index.md)' in read('docs/reference/index.md'), 'API conceptual link does not target authored Markdown source')
require('../api/index.html' not in read('docs/reference/index.md'), 'stale generated API HTML link remains')
require('LocalGPT-*.pdf' in read('docs/docfx.json'), 'DocFX PDF validation stub is not configured as a resource')
require('[System.Collections.Generic.List[string]]::new()' in doc, 'DocFX live output capture list is missing')
require('ForEach-Object {' in doc and 'Write-Host "[DocFX] $displayLine"' in doc, 'DocFX output is not streamed live')
require('@("pdf", $configPath, "--logLevel", "verbose")' in doc, 'DocFX PDF command is not verbose')
require('can take several minutes for $pdfSourcePageCount pages' in doc, 'long PDF progress notice is missing')
require('js/localgpt-chat-ui.js?v=3.3.8' in read('src/LocalGPT/Components/App.razor'), 'browser cache marker is not 3.3.8')
require('LocalGPT/3.3.8' in read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs'), 'outbound product marker is not 3.3.8')
require('pdfFileName: LocalGPT-3.3.8.pdf' in read('docs/pdf/toc.yml'), 'PDF marker is not 3.3.8')
require('"localgptVersion": "3.3.8"' in read('docs/docfx.json'), 'DocFX marker is not 3.3.8')
require((root / 'CHANGELOG-v3.3.8-DOCFX-LINK-PROGRESS-REPAIR.md').is_file(), '3.3.8 changelog missing')
require((root / 'VALIDATION-v3.3.8-source.md').is_file(), '3.3.8 validation missing')
require('# LocalGPT 3.3.8' in read('RELEASE.md'), 'RELEASE.md is not 3.3.8')

# Static colon scan: accept only PowerShell scope/provider prefixes.
allowed = {'env','global','script','local','private','using','variable','function','alias'}
pat = re.compile(r'\$([A-Za-z_][A-Za-z0-9_]*):')
for ext in ('*.ps1','*.psm1'):
    for path in root.rglob(ext):
        rel = path.relative_to(root).as_posix()
        if any(part in {'.git','.vs','artifacts','bin','obj','packages','node_modules'} for part in path.parts):
            continue
        for line_no, line in enumerate(path.read_text(encoding='utf-8').splitlines(), 1):
            for match in pat.finditer(line):
                if match.group(1).lower() not in allowed:
                    failures.append(f'{rel}:{line_no} contains suspicious PowerShell variable-colon token ${match.group(1)}:')

if failures:
    raise SystemExit('LocalGPT 3.3.8 static release audit failed:\n - ' + '\n - '.join(failures))
print('LocalGPT 3.3.8 static release audit passed.')
