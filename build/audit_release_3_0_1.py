#!/usr/bin/env python3
"""Source-only release gate for LocalGPT 3.0.1 namespace, wiring, structure, text ownership, and live rejoin repair."""
from __future__ import annotations
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'src' / 'LocalGPT'
sys.path.insert(0, str(ROOT / 'build'))
from audit_application_architecture import parse_types  # type: ignore

failures: list[str] = []
checks = 0

def require(condition: bool, label: str) -> None:
    global checks
    checks += 1
    if not condition:
        failures.append(label)

def read(rel: str) -> str:
    p = ROOT / rel
    return p.read_text(encoding='utf-8-sig', errors='replace') if p.is_file() else ''

def csharp_parts(rel_without_ext: str) -> str:
    p = ROOT / rel_without_ext
    return '\n'.join(x.read_text(encoding='utf-8-sig', errors='replace') for x in sorted(p.parent.glob(p.name + '*.cs')))

def component_parts(rel_without_ext: str) -> str:
    p = ROOT / rel_without_ext
    razor = p.with_suffix('.razor')
    parts = ([razor] if razor.is_file() else []) + sorted(p.parent.glob(p.name + '*.razor.cs'))
    return '\n'.join(x.read_text(encoding='utf-8-sig', errors='replace') for x in parts)

# Version-slot policy.
for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
    value = read(rel)
    require('<Version>3.0.4</Version>' in value, f'{rel}: version is not 3.0.1')
    m = re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', value)
    require(bool(m) and int(m.group(2)) <= 9 and int(m.group(3)) <= 9, f'{rel}: version-slot policy violated')
require('<Version>2.1.1</Version>' in read('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'), 'wire protocol changed from 2.1.1')

# Namespace repairs.
require('namespace LocalGPT.Controller' in read('src/LocalGPT/Controller/MinecraftDiagnosticController.cs'), 'MinecraftDiagnosticController namespace is not LocalGPT.Controller')
require('namespace LocalGPT.Services' in read('src/LocalGPT/Services/NotificationService.cs'), 'NotificationService namespace is not LocalGPT.Services')
require('namespace LocalGPT.Hubs' in read('src/LocalGPT/Hubs/ChatHub.cs'), 'ChatHub namespace/folder is not LocalGPT.Hubs')
productive = '\n'.join(p.read_text(encoding='utf-8-sig', errors='replace') for p in SRC.rglob('*.cs') if not any(x in p.parts for x in ('bin','obj')))
require('namespace TacosPortal' not in productive and 'using TacosPortal' not in productive, 'TacosPortal namespace/import remains in productive C#')
require('namespace LocalGPT.Endpoints' not in productive and 'using LocalGPT.Endpoints' not in productive, 'LocalGPT.Endpoints namespace/import remains in productive C#')
require('TacosPortal.Services' not in read('build/Build-Documentation.ps1'), 'documentation builder still rewrites TacosPortal.Services')

# Text/regex ownership: baseline must be empty and UI/API cannot bypass injected services.
try:
    baseline = json.loads(read('build/text-service-ownership-baseline.json'))
except Exception:
    baseline = None
require(baseline == [], 'text-service ownership baseline is not empty')
pattern = re.compile(r'(?m)^(?P<line>.*(?:\bRegex\s*\.|\bnew\s+Regex\s*\(|\.Replace\s*\(|\.Split\s*\(|\bstring\.Join\s*\(|\bWebUtility\.HtmlDecode\s*\(|\.StartsWith\s*\(|\.EndsWith\s*\(|\.IndexOf\s*\(|\.Substring\s*\(|\.Contains\s*\([^\r\n;]*StringComparison\.).*)$')
ownership_failures=[]
for folder in ('Components','Controllers','Controller'):
    base=SRC/folder
    if not base.exists(): continue
    for p in base.rglob('*'):
        if p.suffix not in {'.cs','.razor'}: continue
        text=p.read_text(encoding='utf-8-sig', errors='replace')
        require('using LocalGPT.Extensions' not in text, f'{p.relative_to(ROOT)}: UI/API directly imports LocalGPT.Extensions')
        for m in pattern.finditer(text):
            line=' '.join(m.group('line').strip().split())
            if re.search(r'(?:CouncilText|PanelText|TextService|RegexService|StringService|ReviewerPolicy)\.', line):
                continue
            ownership_failures.append(f'{p.relative_to(ROOT)}|{line}')
require(not ownership_failures, 'direct UI/API text or regex manipulation remains: ' + '; '.join(ownership_failures[:5]))

# Shared DI service boundaries.
program = csharp_parts('src/LocalGPT/Program')
for marker in [
    'AddSingleton<IJsonTextService, JsonTextService>()',
    'AddSingleton<IRegexCompilationService, RegexCompilationService>()',
    'AddSingleton<IProviderModelReviewerPolicyService, ProviderModelReviewerPolicyService>()',
    'AddSingleton<MinecraftDatapackService>()',
    'AddSingleton<MinecraftProjectService>()',
    'AddSingleton<CouncilKnowledgeContentService>()',
]:
    require(marker in program, f'Program DI registration missing: {marker}')
project_maintenance = csharp_parts('src/LocalGPT/Services/ProjectMaintenanceService')
require('new Regex(' not in project_maintenance, 'ProjectMaintenanceService still compiles Regex directly')
require('regexCompilation.Compile(' in project_maintenance, 'ProjectMaintenanceService does not route regex compilation through DI service')
require('new Regex(' in csharp_parts('src/LocalGPT/Services/RegexCompilationService'), 'RegexCompilationService no longer owns framework regex compilation')
reviewer = read('src/LocalGPT/Services/ProviderModelReviewerPolicyService.cs')
require('gpt-oss:20b' in reviewer and 'qwen' in reviewer.lower(), 'benchmark reviewer policy was not centralized')

# Minecraft/Datapack and Knowledge domain extraction.
require((SRC/'Services/MinecraftProjectService.cs').is_file(), 'MinecraftProjectService missing')
require((SRC/'Services/MinecraftDatapackService.cs').is_file(), 'MinecraftDatapackService missing')
require((SRC/'Services/CouncilKnowledgeContentService.cs').is_file(), 'CouncilKnowledgeContentService missing')
old_domains = csharp_parts('src/LocalGPT/Services/CouncilTextService') + '\n' + csharp_parts('src/LocalGPT/Services/CouncilRuntimeService')
for marker in ['CreateDatapackAdminBookFunction', 'CreateDatapackBuildScript', 'CreateWorkspaceReadme', 'ResolveFabricDependencyVersions']:
    require(marker not in old_domains, f'Minecraft/Datapack responsibility still remains in Council text/runtime: {marker}')
require('CouncilKnowledgeContentService' not in csharp_parts('src/LocalGPT/Services/SqliteUtilityService'), 'SQLite utility still owns Council knowledge content service behavior')

# Live Council rejoin stays lightweight while running and durable after completion.
chat = component_parts('src/LocalGPT/Components/Pages/Chat')
attach_signature='private async Task<bool> AttachToLiveCouncilSessionAsync(Guid runId, bool reloadChatControl = false)'
require(attach_signature in chat, 'live Council attach no longer returns success/failure')
attach_start = chat.find(attach_signature)
attach_end = chat.find('[JSInvokable]', attach_start)
attach = chat[attach_start:attach_end if attach_end > attach_start else len(chat)]
for marker in ['CouncilLiveSessions.GetAttachmentSnapshot(runId)', 'snapshot.IsRunning ? string.Empty : CouncilLiveSessions.GetTranscript(runId)', 'LiveCouncilMessageMarkerPrefix', 'JSDisconnectedException', 'InvokeAsync(async () =>']:
    require(marker in attach, f'live Council lightweight/retryable attach missing: {marker}')
require('CouncilLiveSessions.Get(runId)' not in attach, 'live Council rejoin materializes the full snapshot')
require(attach.count('DxAiChat.LoadMessages(') == 1, 'live Council attach must bind DevExpress messages exactly once')
require('PersistCurrentConversationAsync(force: true, showToast: false)' in attach, 'completed Council transcript is not persisted after lightweight live attach')
require('CouncilLiveSessionAttachmentSnapshot? attachedLiveCouncilSnapshot;' in chat, 'Chat still stores a full live Council snapshot')

# Maintainability: no individual maintained C# declaration spans >=1000 source lines.
large=[]
for p in SRC.rglob('*.cs'):
    if any(x in p.parts for x in ('bin','obj','Migrations')) or p.name.endswith('.Designer.cs'):
        continue
    text=p.read_text(encoding='utf-8-sig', errors='replace')
    for typ in parse_types(text):
        lines=text[typ.start:typ.close + 1].count('\n')+1
        if lines >= 1000:
            large.append(f'{p.relative_to(ROOT)}:{typ.name}:{lines}')
require(not large, 'maintained C# declaration >=1000 lines: ' + '; '.join(large[:10]))
for p in SRC.rglob('*.razor.cs'):
    lines=sum(1 for _ in p.open(encoding='utf-8-sig', errors='replace'))
    require(lines < 1000, f'{p.relative_to(ROOT)}: Razor code-behind partial has {lines} lines')

if failures:
    print('LocalGPT 3.0.1 source audit failed:')
    for f in failures:
        print('  -', f)
    raise SystemExit(1)
print(f'LocalGPT 3.0.1 namespace/wiring/structure/rejoin source audit passed: {checks} checks.')
