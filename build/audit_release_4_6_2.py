#!/usr/bin/env python3
from pathlib import Path
import hashlib, sys
ROOT=Path(__file__).resolve().parents[1]
VERSION='4.6.2'

def read(rel):
    p=ROOT/rel
    if not p.is_file(): raise SystemExit(f'LocalGPT {VERSION} audit failed: missing {rel}')
    return p.read_text(encoding='utf-8',errors='strict')

def req(rel, marker):
    if marker not in read(rel): raise SystemExit(f'LocalGPT {VERSION} audit failed: {rel} missing {marker!r}')

def forbid(rel, marker):
    if marker in read(rel): raise SystemExit(f'LocalGPT {VERSION} audit failed: {rel} contains forbidden {marker!r}')

def main():
    parts=[int(x) for x in VERSION.split('.')]
    if len(parts)!=3 or parts[1]>=10 or parts[2]>=10: raise SystemExit('version-slot policy failed')
    for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
        req(rel,f'<Version>{VERSION}</Version>')
    req('RELEASE.md',f'# LocalGPT {VERSION}')
    req('VALIDATION.md',f'# LocalGPT {VERSION} source validation')
    req('CHANGELOG-v4.6.2-PROJECT-INGESTION-ONEWIRE-SEMANTIC-AUTOMATION.md',f'# LocalGPT {VERSION}')
    req('VALIDATION-v4.6.2-source.md',f'# LocalGPT {VERSION} source validation')
    for marker in ['localgpt-game-console.js?v=4.6.2','localgpt-chat-ui.js?v=4.6.2']:
        req('src/LocalGPT/Components/App.razor',marker)
    req('src/LocalGPT/Services/InitialSetupAssistantService.cs','LocalGPT/4.6.2')
    req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs','LocalGPT/4.6.2')
    req('docs/index.md','**Version 4.6.2**')
    req('docs/docfx.json','"localgptVersion": "4.6.2"')

    # 4.6.1 startup lifetime repair remains intact.
    reg='src/LocalGPT/Program.ServiceRegistration.cs'; coll='src/LocalGPT/Services/HumanCollaborationService.cs'
    req(reg,'builder.Services.AddSingleton<IHumanCollaborationService, HumanCollaborationService>();')
    req(reg,'builder.Services.AddScoped<IKnowledgeFreshnessReviewService, KnowledgeFreshnessReviewService>();')
    req(coll,'IServiceScopeFactory serviceScopeFactory')
    req(coll,'using var freshnessScope = serviceScopeFactory.CreateScope();')
    forbid(coll,'IKnowledgeFreshnessReviewService knowledgeFreshness,')

    # Quarantine-first upload and promotion.
    upload='src/LocalGPT/Services/ChatUploadWorkspaceService.cs'
    req(upload,'"original"')
    req(upload,'MaxSingleFileBytes')
    req(upload,'MaxTotalFileBytes')
    ingestion='src/LocalGPT/Services/ProjectIngestionService.cs'
    for marker in ['InspectAsync','PromoteAsync','ingestion-gate.json','InspectZip','RequiredIndependentApprovals','UserApproved']:
        req(ingestion,marker)
    # Blob reconstruction is hash/length/path verified and feeds upload quarantine.
    blob='src/LocalGPT/Services/ProjectBlobReconstructionService.cs'
    for marker in ['SHA256.HashData','ManifestSha256','ValidateRelativePath','ChunkCount','Length','CreateWorkspaceAsync']:
        req(blob,marker)
    # Curator review lifecycle.
    curator='src/LocalGPT/Services/RegexCuratorService.cs'
    for marker in ['NeedsReview','ReviewAsync','SetUserApprovalAsync','ListApprovedPatternsAsync','ReviewerIdentity']:
        req(curator,marker)
    req('src/LocalGPT/Services/RegexDxAiFunctions.cs','MarkSuggestedAsync')
    req('src/LocalGPT/Services/LearningRoundService.cs','MarkSuggestedAsync')
    # Generic classifier coverage.
    classifier='src/LocalGPT/Services/ProjectEvidenceClassifierService.cs'
    for marker in ['*.csproj','pom.xml','build.gradle','package.json','Cargo.toml','go.mod','pyproject.toml','fabric.mod.json','neoforge.mods.toml','paper-plugin.yml']:
        req(classifier,marker)
    # 1-Wire pairing/trust remains authoritative before host enrollment.
    hosts='src/LocalGPT/Services/OneWireCouncilHostEnrollmentService.cs'
    for marker in ['GetTrustedPeersAsync','EnrollAsync','normal 1-Wire pairing/trust','ValidUntilUtc']:
        req(hosts,marker)
    funcs='src/LocalGPT/Services/ProjectAutomationDxAiFunctions.cs'
    for marker in ['onewire.team.status','onewire.pairing.ticket','onewire.trust.establish','onewire.council.host.enroll','project.ingestion.inspect','project.ingestion.promote','project.blob.start','localgpt.regex.curator.review','localgpt.ascii.actions.list','localgpt.ascii.actions.invoke']:
        req(funcs,marker)
    # Shared semantic ASCII input, including pointer metadata in the UI/JS.
    req('src/LocalGPT/Services/AsciiSemanticActionService.cs','ApplyControlAsync')
    req('src/LocalGPT/Components/Shared/ChatGameConsole.razor','data-semantic-action')
    js='src/LocalGPT/wwwroot/js/localgpt-game-console.js'
    for marker in ['semanticAction','pointerdown','gamepad']:
        req(js,marker)

    # DI wiring for new services.
    for marker in [
        'AddSingleton<IRegexCuratorService, RegexCuratorService>()',
        'AddSingleton<IProjectEvidenceClassifierService, ProjectEvidenceClassifierService>()',
        'AddScoped<IProjectIngestionService, ProjectIngestionService>()',
        'AddScoped<IProjectBlobReconstructionService, ProjectBlobReconstructionService>()',
        'AddScoped<IOneWireCouncilHostEnrollmentService, OneWireCouncilHostEnrollmentService>()',
        'AddSingleton<IAsciiSemanticActionService, AsciiSemanticActionService>()']:
        req(reg,marker)

    # Browser diagnostics manifest byte consistency.
    checked=0
    for line in read('build/javascript-diagnostics-files.sha256').splitlines():
        line=line.strip()
        if not line or line.startswith('#'): continue
        digest, rel=line.split(maxsplit=1)
        p=ROOT/rel
        if not p.is_file(): raise SystemExit(f'LocalGPT {VERSION} audit failed: JS manifest missing {rel}')
        norm=p.read_text(encoding='utf-8').replace('\r\n','\n').replace('\r','\n')
        if hashlib.sha256(norm.encode()).hexdigest()!=digest: raise SystemExit(f'LocalGPT {VERSION} audit failed: stale JS hash {rel}')
        checked+=1
    if checked<20: raise SystemExit('unexpectedly small JS diagnostics manifest')
    print(f'LocalGPT {VERSION} release audit passed: ingestion, curator, generic classifier, trusted 1-Wire team, semantic ASCII, lifetime repair, version identity and {checked} JS hashes are consistent.')
    return 0
if __name__=='__main__': sys.exit(main())
