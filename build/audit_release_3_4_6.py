#!/usr/bin/env python3
from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
FAIL=[]
def require(cond,msg):
    if not cond: FAIL.append(msg)
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig', errors='replace')

# Current identity only.
for rel in [
    'src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','docs/docfx.json','docs/index.md',
    'docs/pdf/toc.yml','docs/pdf-cover.html','src/LocalGPT/Components/App.razor',
    'src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','RELEASE.md']:
    require('3.4.6' in text(rel), f'{rel}: current 3.4.6 identity missing')

# Build guards are cross-platform; OS only selects the PowerShell host.
targets=text('Directory.Build.targets')
try: ET.parse(ROOT/'Directory.Build.targets')
except Exception as e: FAIL.append(f'Directory.Build.targets is not valid XML: {e}')
for line in targets.splitlines():
    if 'Windows_NT' in line and 'RepositoryPowerShell' not in line:
        FAIL.append('Directory.Build.targets still contains a Windows-only active build condition: '+line.strip())
require('<RepositoryPowerShell Condition="\'$(RepositoryPowerShell)\' == \'\' and \'$(OS)\' != \'Windows_NT\'">pwsh</RepositoryPowerShell>' in targets,
        'non-Windows pwsh host selection missing')
for rel in ['build/Invoke-ArchitectureAudit.ps1','build/Assert-AsyncContinuationPolicy.ps1','build/Assert-MethodDiagnostics.ps1']:
    require('Get-Command python3' in text(rel), f'{rel}: python3 fallback missing for macOS/Linux guard execution')
require(re.search(r'<RequireLocalGptDocumentationPdf[^>]*Configuration[^>]*Release[^>]*>true</RequireLocalGptDocumentationPdf>', targets) is not None,
        'Release PDF default missing')
require("<RequireLocalGptDocumentationPdf Condition=\"'$(RequireLocalGptDocumentationPdf)' == ''\">false</RequireLocalGptDocumentationPdf>" in targets,
        'Debug/non-Release PDF default is not false')

# Fast browser PDF path and compact progress.
doc=text('build/Build-Documentation.ps1')
require('$localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)' in doc,
        'LocalApplicationData initialization missing')
require('$maximumBrowserPrintSourcePages = 1500' in doc, 'fast browser PDF ceiling is not 1500 pages')
for token in ['Google Chrome.app/Contents/MacOS/Google Chrome','Microsoft Edge.app/Contents/MacOS/Microsoft Edge','chromium-browser']:
    require(token in doc, f'browser probe missing: {token}')
for token in ["$displayLine -match '^\\s*(?:Removed|Copied)", "$progressState.ContainsKey($key)", "(?<name>[^\\r\\n]*?\\.pdf):\\s*(?<percent>"]:
    require(token in doc, f'compact DocFX progress logic missing token: {token}')

# Existing Node >= minimum wins before any provisioning.
node=text('build/NodeRuntime.Common.ps1')
pos_existing=node.find('if ($null -ne $nodeInfo)')
pos_provision=node.find('if ($AllowProvisioning)', pos_existing+1)
require(pos_existing >= 0 and pos_provision > pos_existing, 'existing Node reuse must precede provisioning')
require('no additional Node.js runtime will be provisioned' in node, 'newer existing Node reuse diagnostic missing')

# Release pipeline still owns one explicit complete PDF build.
release=text('Build-Release.ps1')
require('-RequirePdf' in release, 'Build-Release.ps1 no longer requires the complete PDF')
require('-p:BuildLocalGptDocumentation=false' in release, 'release assembly build no longer suppresses duplicate documentation generation')

# The new Ollama platform implementation is no longer an unreviewed iterator.
ollama=text('src/LocalGPT/Services/OllamaPlatformServices.cs')
require(not re.search(r'\byield\s+(?:return|break)\b', ollama), 'Ollama platform service still contains yield')

# Wire protocol project identity is deliberately not changed by this patch.

if FAIL:
    print('LocalGPT 3.4.6 static release audit failed:')
    for f in FAIL: print('  -',f)
    sys.exit(1)
print('LocalGPT 3.4.6 static release audit passed.')
