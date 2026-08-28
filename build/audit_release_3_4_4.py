#!/usr/bin/env python3
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[1]
FAIL=[]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def req(ok,msg):
    if not ok: FAIL.append(msg)
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
    req('<Version>3.4.4</Version>' in read(rel), f'{rel} is not 3.4.4')
for rel in ['src/LocalGPT/Components/App.razor','docs/docfx.json','docs/pdf-cover.html','docs/pdf/toc.yml','docs/index.md','RELEASE.md']:
    req('3.4.4' in read(rel), f'{rel} current identity is not 3.4.4')
req('LocalGPT/3.4.4' in read('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs'), 'outbound product marker is not 3.4.4')
req((ROOT/'CHANGELOG-v3.4.4-XML-DOCUMENTATION-WARNING-CLEANUP.md').is_file(), '3.4.4 changelog missing')
req((ROOT/'VALIDATION-v3.4.4-source.md').is_file(), '3.4.4 validation missing')
checks={
'src/LocalGPT/Services/AiProviderBootstrapService.cs':['platformRuntime'],
'src/LocalGPT/Services/ArtifactBuildExecutor.cs':['platform'],
'src/LocalGPT/Services/CodeGenerationWorkflowService.cs':['platform'],
'src/LocalGPT/Services/ConsoleCommandService.cs':['platform'],
'src/LocalGPT/Services/DocumentationCatalogService.cs':['platform'],
'src/LocalGPT/Services/EmbeddedHardwareCatalogService.cs':['platform'],
'src/LocalGPT/Services/HardwareInventoryService.cs':['platformProbe'],
'src/LocalGPT/Services/LearningProjectWorkspaceSyncService.cs':['platform'],
'src/LocalGPT/Services/MinecraftModWorkspaceService.cs':['platform','consolePlatform'],
'src/LocalGPT/Services/OneWire/OneWireRuntimeSecurityService.cs':['secretFileProtection'],
'src/LocalGPT/Services/OrganicAddonManifestService.cs':['platform'],
'src/LocalGPT/Services/Persistence/InitialDataCatalog.cs':['platform'],
'src/LocalGPT/Services/ProjectMaintenanceService.cs':['platform'],
'src/LocalGPT/Services/ToolchainDiscoveryService.cs':['platform'],
}
for rel,names in checks.items():
    text=read(rel)
    for name in names:
        req(f'<param name="{name}">' in text, f'{rel} missing XML param {name}')
release=read('Build-Release.ps1')
req('.IndexOf($pdfName, [StringComparison]::OrdinalIgnoreCase) -ge 0' in release, 'PowerShell 5.1-safe PDF name check missing')
req('.Contains($pdfName, [StringComparison]::OrdinalIgnoreCase)' not in release, 'PowerShell 7-only Contains overload returned')
for v in re.findall(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', '\n'.join(read(x) for x in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj'])):
    req(len(v[1])==1 and len(v[2])==1, f'two-digit minor/patch slot: {v}')
if FAIL: raise SystemExit('LocalGPT 3.4.4 audit failed:\n - '+'\n - '.join(FAIL))
print('LocalGPT 3.4.4 static release audit passed.')
