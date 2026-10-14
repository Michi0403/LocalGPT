#!/usr/bin/env python3
from pathlib import Path
import hashlib, json, re, sys
ROOT=Path(__file__).resolve().parents[1]
checks=[]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig',errors='replace')
def req(rel,needle,label=None):
    if needle not in read(rel): raise AssertionError(f'{rel}: missing {label or needle!r}')
    checks.append(label or needle)
def forbid(rel,needle,label=None):
    if needle in read(rel): raise AssertionError(f'{rel}: forbidden {label or needle!r}')
    checks.append(label or f'forbid:{needle}')
def sha(rel): return hashlib.sha256((ROOT/rel).read_bytes()).hexdigest()
def tree_digest(path):
    h=hashlib.sha256()
    if not path.exists(): return 'missing'
    for p in sorted(x for x in path.rglob('*') if x.is_file()):
        h.update(p.relative_to(path).as_posix().encode());h.update(b'\0');h.update(p.read_bytes());h.update(b'\0')
    return h.hexdigest()
try:
    for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
        req(rel,'<Version>3.2.3</Version>','3.2.3 package version')
    req('global.json','"version": "10.0.400"','source SDK 10.0.400')
    req('src/LocalGPT/LocalGPT.csproj','<TargetFramework>net10.0</TargetFramework>','LocalGPT net10.0')
    req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.2.3','3.2.3 browser cache key')
    req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.2.3','3.2.3 outbound user agent')

    # Protected chat UI remains byte-identical to the 3.2.2 baseline.
    if sha('src/LocalGPT/Components/Pages/Chat.razor')!='0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569': raise AssertionError('Chat.razor changed')
    checks.append('protected Chat.razor')
    if sha('src/LocalGPT/Components/Pages/Chat.razor.css')!='2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4': raise AssertionError('Chat.razor.css changed')
    checks.append('protected Chat.razor.css')

    # Simple JSON/OData AI-function UI and existing advanced mode.
    e='src/LocalGPT/Components/Shared/UserDxFunctionEditor.razor'
    for n in ['InitialMode','JSON','OData','user-source.','RemoteControlConnectorDefinition','RemoteControlPipelineDefinition','AllowInsecureHttp','AllowedHostsJson','StepsJson = "[]"']:
        req(e,n,'user source editor:'+n)
    d='src/LocalGPT/Components/Pages/DxFunctionCatalog.razor'
    req(d,'New JSON / OData AI function','simple source action')
    req(d,'Advanced pipeline AI function','advanced pipeline action retained')
    req(d,'href="/council-teams#x-functions-automation"','X automation navigation')
    u='src/LocalGPT/Services/UserDxAiFunctionService.cs'
    req(u,'pipeline.ConnectorKey.StartsWith("user-source."','source adapter invocation')
    req(u,'pipelines.ParseSteps(pipeline.StepsJson).Count == 0','zero-step source adapter')
    req(u,'connectors.PullAsync(pipeline.ConnectorKey, runPipelines: false','source connector pull')

    # Existing X-Round engine exposed rather than duplicated.
    c='src/LocalGPT/Components/Pages/CouncilTeams.razor'
    req(c,'id="x-functions-automation"','stable X automation section')
    req(c,'Rounds, workflow &amp; automated X Functions','X automation heading')
    req(c,'XFunctionsEnabled','existing X function policy retained')

    # Learning Round project persistence from chat upload workspace.
    req('src/LocalGPT/Program.ServiceRegistration.cs','AddScoped<ILearningProjectWorkspaceSyncService, LearningProjectWorkspaceSyncService>()','learning sync DI')
    req('src/LocalGPT/BusinessObjects/LearningRoundModels.cs','public bool SynchronizeProjectStructure { get; set; } = true;','learning sync defaults on')
    req('src/LocalGPT/Services/LearningRoundService.cs','projectWorkspaceSync.SynchronizeAsync(request.WorkspaceName','learning maintenance invokes source sync')
    s='src/LocalGPT/Services/LearningProjectWorkspaceSyncService.cs'
    for n in ['ChatUploadWorkspaceSummary','Path.Combine(workspace.RootPath, "extracted")','LocalGptProjectVersion','LocalGptProjectRevision','ProjectStructureJson','ProjectWorkspaceRoot','LocalGptProjectTrackedFile','SourceSnapshotHash','dotnet-sdk:','target-framework:','stale.Status = "Superseded"','stale.Priority = "Historical"','stale.IsUserApproved = false']:
        req(s,n,'project source sync:'+n)
    req('src/LocalGPT/Services/LocalGptProjectService.cs','Never invent fallback versions or ask whether the project targets .NET 7/8','source-backed project requirement grounding')
    req('src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs','xRoundCause ?? string.Empty','nullable X-Round recovery warning repair')

    # Localization catalogs must remain key-identical.
    loc=sorted((ROOT/'src/LocalGPT/Localization').glob('*.json'))
    if len(loc)!=6: raise AssertionError(f'expected 6 localization catalogs, found {len(loc)}')
    sets=[]
    for p in loc: sets.append(set(json.loads(p.read_text(encoding='utf-8-sig'))))
    if any(x!=sets[0] for x in sets[1:]): raise AssertionError('localization key mismatch')
    if len(sets[0])!=1994: raise AssertionError(f'unexpected localization key count {len(sets[0])}')
    checks.append('six localization catalogs / 1994-key parity')

    # No new migration source is part of this release; digest is recorded for package verification.
    req('RELEASE.md','# LocalGPT 3.2.3','release file')
    req('CHANGELOG-v3.2.3-AI-FUNCTIONS-X-AUTOMATION-LEARNING-PROJECT-SYNC.md','Every maintained repository file','changelog persistence statement')
    req('VALIDATION-v3.2.3-source.md','SOURCE-NOT-COMPILED','source-only validation boundary')
    print(f'LocalGPT 3.2.3 source release audit passed: {len(checks)} checks; migrations digest {tree_digest(ROOT/"src/LocalGPT/Migrations")}.')
except Exception as exc:
    print(f'LocalGPT 3.2.3 source release audit failed: {exc}',file=sys.stderr)
    raise SystemExit(1)
