#!/usr/bin/env python3
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
fail=[]
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig')
def req(rel,*needles):
    s=text(rel)
    for n in needles:
        if n not in s: fail.append(f'{rel}: missing {n}')
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
    req(rel,'<Version>3.2.5</Version>')
req('src/LocalGPT/LocalGPT.csproj','<TargetFramework>net10.0</TargetFramework>')
req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.2.5')
req('src/LocalGPT/Services/Persistence/DatabaseInitializationService.RepositoryMaintenance.cs',
    'PrepareLocalGptReleaseHistory','ResolveLocalGptSourceVersion','SetInMemoryProjectCurrentMarkers',
    'SeedPublisherStudioProjectAsync','repository-refresh.localgpt','repository-refresh.publisherstudio',
    'https://github.com/Michi0403/LocalGPT','https://github.com/Michi0403/BlazorPublisher')
req('src/LocalGPT/Services/RemoteKnowledgeImportDxAiFunctions.cs','localgpt.repository.knowledge.refresh','SynchronizeRemoteRepositoryAsync')
req('src/LocalGPT/Services/LearningProjectWorkspaceSyncService.cs','CanonicalProjectName','PublisherStudio','BlazorPublisher','ChatUploadWorkspace')
req('src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs',
    'another identifiable repository maintains its own project tied to the chat workspace',
    'https://github.com/Michi0403/LocalGPT','https://github.com/Michi0403/BlazorPublisher')
req('src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs','!string.Equals(request.CouncilTeamKey, "learning-round", StringComparison.OrdinalIgnoreCase)')
req('src/LocalGPT/Components/Pages/DxFunctionCatalog.razor','@rendermode InteractiveServer','ConfigurationWorkbenchNav','_interactiveAttached','aria-busy')
req('src/LocalGPT/Components/Shared/UserDxFunctionEditor.razor','OnParametersSetAsync','_busy')
req('src/LocalGPT/Components/Pages/RemoteControl.razor','@rendermode InteractiveServer','ConfigurationWorkbenchNav','ConfigurationWorkbenchPanel','SectionKey="connectors"','SectionKey="pipelines"','SectionKey="history"','SectionKey="templates"')
req('src/LocalGPT/Components/Pages/RemoteControl.razor.cs','ActiveRemoteControlSection','RemoteControlSections','OnRemoteControlSectionChanged')
req('CHANGELOG-v3.2.5-REPOSITORY-MAINTENANCE-DX-WORKBENCH-GITHUB-REFRESH.md','LocalGPT 3.2.5')
req('VALIDATION-v3.2.5-source.md','source-only and not compiled')
if fail:
    print('LocalGPT 3.2.5 release audit failed:')
    print('\n'.join(' - '+x for x in fail)); sys.exit(1)
print('LocalGPT 3.2.5 release audit passed: repository maintenance, canonical refresh pipelines, Learning Round project ownership, DX attach-state/workbench behavior, Remote Control responsive workbench, .NET 10 and version alignment are present.')
