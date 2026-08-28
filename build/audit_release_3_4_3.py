#!/usr/bin/env python3
"""Static source-release audit for LocalGPT 3.4.3. Does not invoke dotnet or PowerShell."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
failures=[]

def read(rel): return (root/rel).read_text(encoding='utf-8')
def require(cond,msg):
    if not cond: failures.append(msg)

for project in [
    'src/LocalGPT/LocalGPT.csproj',
    'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
]:
    require('<Version>3.4.3</Version>' in read(project), f'{project} is not 3.4.3')

app=read('src/LocalGPT/LocalGPT.csproj')
for package in ['System.Drawing.Common','System.Data.OleDb','System.Diagnostics.PerformanceCounter','Microsoft.Windows.AI.MachineLearning','System.Security.Cryptography.ProtectedData']:
    require(package not in app, f'forbidden/unused platform package remains: {package}')

require('LocalGPT/3.4.3' in read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs'), 'outbound product marker is not 3.4.3')
require('js/localgpt-chat-ui.js?v=3.4.3' in read('src/LocalGPT/Components/App.razor'), 'browser cache marker is not 3.4.3')
require('pdfFileName: LocalGPT-3.4.3.pdf' in read('docs/pdf/toc.yml'), 'PDF marker is not 3.4.3')
require('"localgptVersion": "3.4.3"' in read('docs/docfx.json'), 'DocFX marker is not 3.4.3')
require((root/'CHANGELOG-v3.4.3-POWERSHELL-51-RELEASE-COMPATIBILITY.md').is_file(), '3.4.3 changelog missing')
require((root/'VALIDATION-v3.4.3-source.md').is_file(), '3.4.3 validation missing')
ollama = read('src/LocalGPT/Services/OllamaPlatformServices.cs')
require('protected virtual StringComparer ExecutablePathComparer => StringComparer.Ordinal;' in ollama, 'Ollama base executable-path comparer is missing')
require('protected override StringComparer ExecutablePathComparer => StringComparer.OrdinalIgnoreCase;' in ollama, 'Windows Ollama executable-path comparer override is missing')
require('# LocalGPT 3.4.3' in read('RELEASE.md'), 'RELEASE.md is not 3.4.3')
require((root/'build/audit_cross_platform_boundaries.py').is_file(), 'cross-platform audit missing')
require("Assert-CrossPlatformBoundaries.ps1" in read('Build-Release.ps1'), 'release build does not call cross-platform guard')
require("Assert-CrossPlatformBoundaries.ps1" in read('Build-LocalDevelopment.ps1'), 'development build does not call cross-platform guard')
require('"--export-tagged-pdf"' in read('build/Build-Documentation.ps1'), 'browser PDF path does not request tagged output')
require('pdfAccessibilityMode = if ($pdfMode -eq "docfx-pdf-plugin")' in read('build/Build-Documentation.ps1'), 'documentation status does not declare PDF accessibility policy')
pages_validator = read('.github/scripts/prepare-pages-artifact.py')
require('html-accessibility-fallback' in pages_validator and 'pdfAccessibilityMode' in pages_validator, 'Pages validator does not implement explicit DocFX PDF accessibility fallback')


# 3.4.3 Pages/runtime payload split: validate the full release PDF, but do not duplicate it into
# the tracked Pages ZIP or every runtime archive.
pages = read('.github/scripts/prepare-pages-artifact.py')
require('copy_pages_tree' in pages, 'Pages validator does not create a dedicated HTML-only snapshot')
require('pagesPdfPublished' in pages and 'releasePdfFileName' in pages and 'releasePdfBytes' in pages, 'Pages snapshot does not preserve release-PDF metadata')
require('https://github.com/Michi0403/LocalGPT/releases/latest' in pages, 'Pages PDF link is not redirected to the release channel')
doc_build = read('build/Build-Documentation.ps1')
require('Remove-Item -LiteralPath $docfxPdf.FullName' in doc_build, 'nested DocFX PDF candidate is not removed after canonicalization')
release_build = read('Build-Release.ps1')
require('Copy-LocalGptRuntimeDocumentation' in release_build, 'runtime documentation is not split from the standalone release PDF')
require('runtimePdfPublished' in release_build and 'releasePdfFileName' in release_build, 'runtime documentation does not preserve release-PDF metadata')


# 3.4.3 PowerShell 5.1 repair: the compatibility guard must not reject Build-Release itself.
release_ps = read('Build-Release.ps1')
require('.IndexOf($pdfName, [StringComparison]::OrdinalIgnoreCase) -ge 0' in release_ps, 'PowerShell 5.1-compatible PDF-name test is missing')
require('.Contains($pdfName, [StringComparison]::OrdinalIgnoreCase)' not in release_ps, 'PowerShell 7-only Contains overload remains in Build-Release.ps1')
for script in root.rglob('*.ps1'):
    rel = script.relative_to(root).as_posix()
    if any(part in {'.git','.vs','artifacts','bin','obj','packages','node_modules'} for part in script.relative_to(root).parts):
        continue
    require(not re.search(r'\.Contains\([^\r\n]*,\s*\[(?:System\.)?StringComparison\]::', script.read_text(encoding='utf-8-sig')), f'Windows PowerShell 5.1-incompatible Contains overload remains in {rel}')

# Version slots may not contain two digits.
for version in re.findall(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', '\n'.join(read(p) for p in [
    'src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'])):
    require(all(len(slot)==1 for slot in version[1:]), f'invalid two-digit minor/patch slot: {version}')

if failures:
    raise SystemExit('LocalGPT 3.4.3 static release audit failed:\n - ' + '\n - '.join(failures))
print('LocalGPT 3.4.3 static release audit passed.')
