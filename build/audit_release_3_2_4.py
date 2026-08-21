#!/usr/bin/env python3
from pathlib import Path
import hashlib, json, sys
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
try:
    for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
        req(rel,'<Version>3.2.4</Version>','3.2.4 package version')
    req('global.json','"version": "10.0.400"','source SDK 10.0.400')
    req('src/LocalGPT/LocalGPT.csproj','<TargetFramework>net10.0</TargetFramework>','LocalGPT net10.0')
    req('src/LocalGPT/Components/App.razor','localgpt-chat-ui.js?v=3.2.4','3.2.4 browser cache key')
    req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/3.2.4','3.2.4 outbound user agent')

    if sha('src/LocalGPT/Components/Pages/Chat.razor')!='0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569': raise AssertionError('Chat.razor changed')
    checks.append('protected Chat.razor')
    if sha('src/LocalGPT/Components/Pages/Chat.razor.css')!='2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4': raise AssertionError('Chat.razor.css changed')
    checks.append('protected Chat.razor.css')

    editor='src/LocalGPT/Components/Shared/UserDxFunctionEditor.razor'
    forbid(editor,'.StartsWith(','component has no new direct StartsWith ownership violation')
    req(editor,'UserFunctions.IsGeneratedSourceKey(item.Key)','pipeline filtering is service-owned')
    req(editor,'UserFunctions.IsGeneratedSourceKey(item.PipelineKey)','source function classification is service-owned')
    req(editor,'UserFunctions.IsGeneratedSourceKey(pipeline.ConnectorKey)','connector classification is service-owned')
    req(editor,'UserFunctions.CreateGeneratedSourceKey(_edit.FunctionName)','generated source key creation is service-owned')
    svc='src/LocalGPT/Services/UserDxAiFunctionService.cs'
    req(svc,'public bool IsGeneratedSourceKey(string? key)','service source-key classifier')
    req(svc,'public string CreateGeneratedSourceKey(string functionName)','service generated-key creator')
    req('src/LocalGPT/Interfaces/IUserDxAiFunctionService.cs','bool IsGeneratedSourceKey(string? key);','service contract classifier')
    req('src/LocalGPT/Interfaces/IUserDxAiFunctionService.cs','string CreateGeneratedSourceKey(string functionName);','service contract key creator')

    sync='src/LocalGPT/Services/LearningProjectWorkspaceSyncService.cs'
    req(sync,'private IReadOnlyList<string> EnumerateRepositoryFiles(string root, string searchPattern = "*")','materialized repository enumeration')
    forbid(sync,'private IEnumerable<string> EnumerateRepositoryFiles(','old yield iterator removed')
    req(sync,'results.AddRange(files);','repository file materialization')
    req(sync,'logger.LogWarning(exception, "Skipping an inaccessible source repository directory','inaccessible directory diagnostic')

    catalog='src/LocalGPT/Components/Pages/DxFunctionCatalog.razor'
    req(catalog,'private string _userEditorInitialMode = "Pipeline";','missing Razor backing field restored')
    req(catalog,'New JSON / OData AI function','simple source action retained')
    req(catalog,'Advanced pipeline AI function','advanced pipeline action retained')
    req(catalog,'href="/council-teams#x-functions-automation"','X automation navigation retained')

    req('src/LocalGPT/Services/LearningRoundService.cs','projectWorkspaceSync.SynchronizeAsync(request.WorkspaceName','learning project sync retained')
    for n in ['LocalGptProjectVersion','LocalGptProjectRevision','ProjectStructureJson','ProjectWorkspaceRoot','LocalGptProjectTrackedFile','SourceSnapshotHash','stale.Status = "Superseded"','stale.Priority = "Historical"']:
        req(sync,n,'project source sync:'+n)
    req('src/LocalGPT/Services/LocalGptProjectService.cs','Never invent fallback versions or ask whether the project targets .NET 7/8','source-backed .NET requirement grounding retained')
    req('src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs','xRoundCause ?? string.Empty','nullable X-Round recovery repair retained')

    loc=sorted((ROOT/'src/LocalGPT/Localization').glob('*.json'))
    if len(loc)!=6: raise AssertionError(f'expected 6 localization catalogs, found {len(loc)}')
    sets=[set(json.loads(p.read_text(encoding='utf-8-sig'))) for p in loc]
    if any(x!=sets[0] for x in sets[1:]): raise AssertionError('localization key mismatch')
    if len(sets[0])!=1994: raise AssertionError(f'unexpected localization key count {len(sets[0])}')
    checks.append('six localization catalogs / 1994-key parity')

    req('RELEASE.md','# LocalGPT 3.2.4','release file')
    req('CHANGELOG-v3.2.4-BUILD-GUARD-OWNERSHIP-ITERATOR-EDITOR-FIX.md','No ownership baseline or exemption was added.','changelog ownership-policy statement')
    req('VALIDATION-v3.2.4-source.md','SOURCE-NOT-COMPILED','source-only validation boundary')
    print(f'LocalGPT 3.2.4 source release audit passed: {len(checks)} checks.')
except Exception as exc:
    print(f'LocalGPT 3.2.4 source release audit failed: {exc}',file=sys.stderr)
    raise SystemExit(1)
