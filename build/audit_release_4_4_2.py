from pathlib import Path
import sys
ROOT = Path(__file__).resolve().parents[1]
errors=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file():
        errors.append(f'missing: {rel}')
        return ''
    return p.read_text(encoding='utf-8')
def require(text, marker, purpose):
    if marker not in text:
        errors.append(f'{purpose}: missing {marker!r}')
layout=read('src/LocalGPT/Components/Layout/MainLayout.razor')
drawer=read('src/LocalGPT/Components/Layout/Drawer.razor')
workflow=read('build/Assert-WorkflowContracts.ps1')
ops=read('build/Assert-OperationalDiagnostics.ps1')
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
    require(read(rel), '<Version>4.4.2</Version>', f'{rel} version')
require(layout, '[SupplyParameterFromQuery(Name = NavigationUrlService.ToggleSidebarName)]', 'MainLayout query contract')
require(drawer, '[SupplyParameterFromQuery(Name = NavigationUrlService.ToggleSidebarName)]', 'Drawer query contract')
require(layout, 'href="@NavigationUrls.GetUrl(new Uri(NavigationManager.Uri).LocalPath, !ToggledSidebar)"', 'main menu navigation toggle')
require(layout, 'href="@NavigationUrls.GetUrl(new Uri(NavigationManager.Uri).LocalPath, !ToggledSidebar)"', 'drawer-header navigation toggle')
require(drawer, 'drawerOpen = ToggledSidebar;', 'Drawer query-to-open state')
require(layout, '<SafeErrorBoundary @key="NavigationManager.Uri"', 'maintained layout error boundary')
require(workflow, "'src/LocalGPT/Components/Layout/Drawer.razor',", 'workflow guard Drawer contract')
require(workflow, "'src/LocalGPT/Components/Layout/MainLayout.razor',", 'workflow guard MainLayout contract')
require(ops, '<SafeErrorBoundary\\s+@key="NavigationManager\\.Uri"', 'operational diagnostics guard')
for forbidden in ['private bool SidebarOpen', 'OpenState="@SidebarOpen"', 'Click="ToggleSidebar"']:
    if forbidden in layout:
        errors.append(f'MainLayout still contains 4.4.1 shadow-state workaround: {forbidden}')
if 'public bool? OpenState' in drawer or 'OpenState ?? ToggledSidebar' in drawer:
    errors.append('Drawer still contains 4.4.1 OpenState workaround')
# Preserve Game Project work markers
require(read('src/LocalGPT/BusinessObjects/GameProjectModels.cs'), 'ProjectGameDefinition', 'Game Project build artifact')
require(read('src/LocalGPT/Services/GameProjectService.cs'), 'SaveProfileAsync', 'Game Project authoring service')
require(read('src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs'), 'LocalGptGameProjectProfile', 'Game Project persistence')
if errors:
    print('LocalGPT 4.4.2 release audit failed:')
    for e in errors: print(' -', e)
    sys.exit(1)
print('LocalGPT 4.4.2 release audit passed: working menu contract restored and Game Project architecture preserved.')
