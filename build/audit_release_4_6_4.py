#!/usr/bin/env python3
from pathlib import Path
import hashlib, re, sys
ROOT = Path(__file__).resolve().parents[1]
VERSION = '4.6.4'

def read(rel):
    p = ROOT / rel
    if not p.is_file():
        raise SystemExit(f'LocalGPT {VERSION} audit failed: missing {rel}')
    return p.read_text(encoding='utf-8', errors='strict')

def req(rel, marker):
    if marker not in read(rel):
        raise SystemExit(f'LocalGPT {VERSION} audit failed: {rel} missing {marker!r}')

def forbid(rel, marker):
    if marker in read(rel):
        raise SystemExit(f'LocalGPT {VERSION} audit failed: {rel} contains forbidden {marker!r}')

def main():
    parts = [int(x) for x in VERSION.split('.')]
    if len(parts) != 3 or parts[1] >= 10 or parts[2] >= 10:
        raise SystemExit('version-slot policy failed')

    for rel in [
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj']:
        req(rel, f'<Version>{VERSION}</Version>')
    req('RELEASE.md', f'# LocalGPT {VERSION}')
    req('VALIDATION.md', f'# LocalGPT {VERSION} source validation')
    req('CHANGELOG-v4.6.4-XML-DOCUMENTATION-WARNING-CLEANUP.md', f'# LocalGPT {VERSION}')
    req('VALIDATION-v4.6.4-source.md', f'# LocalGPT {VERSION} source validation')
    req('src/LocalGPT/Components/App.razor', 'localgpt-game-console.js?v=4.6.4')
    req('src/LocalGPT/Components/App.razor', 'localgpt-chat-ui.js?v=4.6.4')
    req('src/LocalGPT/Services/InitialSetupAssistantService.cs', 'LocalGPT/4.6.4')
    req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/4.6.4')
    req('docs/index.md', '**Version 4.6.4**')
    req('docs/docfx.json', '"localgptVersion": "4.6.4"')


    # 4.6.4: reported XML documentation warnings stay fixed.
    sync = 'src/LocalGPT/Services/LearningProjectWorkspaceSyncService.cs'
    req(sync, '/// <param name="ProjectType">')
    req(sync, '/// <param name="Toolchains">')
    learning = 'src/LocalGPT/Services/LearningRoundService.cs'
    req(learning, '/// <param name="regexCuratorService">')
    regexdx = 'src/LocalGPT/Services/RegexDxAiFunctions.cs'
    req(regexdx, '/// <param name="regexCurator">')

    # 4.6.1 startup lifetime repair stays intact.
    reg = 'src/LocalGPT/Program.ServiceRegistration.cs'
    coll = 'src/LocalGPT/Services/HumanCollaborationService.cs'
    req(reg, 'builder.Services.AddSingleton<IHumanCollaborationService, HumanCollaborationService>();')
    req(reg, 'builder.Services.AddScoped<IKnowledgeFreshnessReviewService, KnowledgeFreshnessReviewService>();')
    req(coll, 'IServiceScopeFactory serviceScopeFactory')
    req(coll, 'using var freshnessScope = serviceScopeFactory.CreateScope();')
    forbid(coll, 'IKnowledgeFreshnessReviewService knowledgeFreshness,')

    # 4.6.2 integration capabilities remain wired.
    for rel, markers in {
        'src/LocalGPT/Services/ProjectIngestionService.cs': ['InspectAsync', 'PromoteAsync', 'ingestion-gate.json', 'RequiredIndependentApprovals', 'UserApproved'],
        'src/LocalGPT/Services/ProjectBlobReconstructionService.cs': ['SHA256.HashData', 'ManifestSha256', 'ValidateRelativePath', 'ChunkCount', 'CreateWorkspaceAsync'],
        'src/LocalGPT/Services/RegexCuratorService.cs': ['NeedsReview', 'ReviewAsync', 'SetUserApprovalAsync', 'ListApprovedPatternsAsync'],
        'src/LocalGPT/Services/ProjectEvidenceClassifierService.cs': ['*.csproj', 'pom.xml', 'build.gradle', 'package.json', 'Cargo.toml', 'go.mod', 'pyproject.toml', 'fabric.mod.json', 'neoforge.mods.toml', 'paper-plugin.yml'],
        'src/LocalGPT/Services/OneWireCouncilHostEnrollmentService.cs': ['GetTrustedPeersAsync', 'EnrollAsync', 'normal 1-Wire pairing/trust'],
        'src/LocalGPT/Services/ProjectAutomationDxAiFunctions.cs': ['onewire.pairing.ticket', 'onewire.trust.establish', 'onewire.council.host.enroll', 'project.ingestion.inspect', 'project.ingestion.promote', 'project.blob.start', 'localgpt.regex.curator.review', 'localgpt.ascii.actions.invoke'],
        'src/LocalGPT/Services/AsciiSemanticActionService.cs': ['ApplyControlAsync'],
    }.items():
        for marker in markers:
            req(rel, marker)

    # 4.6.3 /install repair: DevExpress controls must not carry Bootstrap native input classes.
    razor_files = list((ROOT / 'src/LocalGPT').rglob('*.razor'))
    native_input = re.compile(r'<Dx(?:TextBox|Memo|SpinEdit|ComboBox|TagBox|DateEdit|TimeEdit|CheckBox)\\b[^>]*?CssClass="[^"]*(?:form-control|form-check-input|form-select)[^"]*"', re.S)
    bad = []
    for path in razor_files:
        text = path.read_text(encoding='utf-8', errors='strict')
        if native_input.search(text):
            bad.append(path.relative_to(ROOT).as_posix())
    if bad:
        raise SystemExit(f'LocalGPT {VERSION} audit failed: Bootstrap native-input classes remain on DevExpress controls: {bad}')

    panel = 'src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor'
    for marker in [
        'InvariantDoubleEditor @bind-Value="device.DedicatedVramGiB"',
        'InvariantDoubleEditor @bind-Value="device.SystemMemoryGiB"',
        'ResolveAndInstallRecommendationAsync(item)',
        '@T("Resolve & install model")',
        'FindRecommendationChoice',
        'NormalizeHardwareCapacity',
        'initial-setup-installed-identity',
        'initial-setup-model-actions']:
        req(panel, marker)
    forbid(panel, '<DxSpinEdit @bind-Value="device.DedicatedVramGiB"')
    forbid(panel, '<DxSpinEdit @bind-Value="device.SystemMemoryGiB"')

    install = 'src/LocalGPT/Components/Pages/Install.razor'
    req(install, 'InvariantDoubleEditor @bind-Value="hardwareDraft.SystemMemoryGiB"')
    req(install, 'InvariantDoubleEditor @bind-Value="hardwareDraft.DedicatedVramGiB"')
    editor = 'src/LocalGPT/Components/Shared/InvariantDoubleEditor.razor'
    for marker in ['CultureInfo.InvariantCulture', 'CultureInfo.CurrentUICulture', 'Normalize(', 'Step', 'DecimalPlaces']:
        req(editor, marker)
    middleware = 'src/LocalGPT/Program.Middleware.cs'
    req(middleware, 'numericCulture.NumberFormat')
    req(middleware, 'CultureInfo.InvariantCulture.NumberFormat')
    req(middleware, 'CultureInfo.CurrentUICulture = uiCulture')

    css = 'src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor.css'
    for marker in ['initial-setup-model-actions', 'initial-setup-installed-row', 'grid-template-columns: auto auto minmax(0, 1fr)', 'initial-setup-installed-grid']:
        req(css, marker)
    req('src/LocalGPT/wwwroot/css/site.css', '.localgpt-check-row')
    req('src/LocalGPT/wwwroot/css/site.css', '.localgpt-editor')

    # InteractiveServer topology remains explicit.
    routed = []
    for p in (ROOT / 'src/LocalGPT/Components/Pages').glob('*.razor'):
        text = p.read_text(encoding='utf-8', errors='strict')
        if '@page ' in text and p.name != 'Error.razor':
            routed.append((p, text))
    missing = [p.relative_to(ROOT).as_posix() for p, text in routed if '@rendermode InteractiveServer' not in text]
    if missing:
        raise SystemExit(f'LocalGPT {VERSION} audit failed: routed InteractiveServer pages missing rendermode: {missing}')

    # Browser diagnostics manifest remains byte-consistent.
    checked = 0
    for line in read('build/javascript-diagnostics-files.sha256').splitlines():
        line = line.strip()
        if not line or line.startswith('#'):
            continue
        digest, rel = line.split(maxsplit=1)
        p = ROOT / rel
        if not p.is_file():
            raise SystemExit(f'LocalGPT {VERSION} audit failed: JS manifest missing {rel}')
        norm = p.read_text(encoding='utf-8').replace('\r\n', '\n').replace('\r', '\n')
        if hashlib.sha256(norm.encode()).hexdigest() != digest:
            raise SystemExit(f'LocalGPT {VERSION} audit failed: stale JS hash {rel}')
        checked += 1
    if checked < 20:
        raise SystemExit('unexpectedly small JS diagnostics manifest')

    print(f'LocalGPT {VERSION} release audit passed: reported XML documentation parameter warnings are repaired; /install, invariant numeric, CanIRun, prior feature wiring, lifetime repair and {checked} JS hashes remain consistent.')
    return 0

if __name__ == '__main__':
    sys.exit(main())
