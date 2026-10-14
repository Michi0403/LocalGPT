#!/usr/bin/env python3
from pathlib import Path
import hashlib, re, sys

ROOT = Path(__file__).resolve().parents[1]
VERSION = '4.6.8'


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
    req('CHANGELOG-v4.6.8-TOURNAMENT-PIXEL-REJOIN-RESILIENCE.md', f'# LocalGPT {VERSION}')
    req('VALIDATION-v4.6.8-source.md', f'# LocalGPT {VERSION} source validation')
    req('src/LocalGPT/Components/App.razor', 'localgpt-game-console.js?v=4.6.8')
    req('src/LocalGPT/Components/App.razor', 'localgpt-chat-ui.js?v=4.6.8')
    req('src/LocalGPT/Services/InitialSetupAssistantService.cs', 'LocalGPT/4.6.8')
    req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/4.6.8')
    req('docs/index.md', '**Version 4.6.8**')
    req('docs/docfx.json', '"localgptVersion": "4.6.8"')
    req('docs/pdf-cover.html', 'LocalGPT 4.6.8 Documentation')
    req('docs/pdf/toc.yml', 'LocalGPT-4.6.8.pdf')

    # 4.6.4: reported XML documentation warning repairs remain present.
    sync = 'src/LocalGPT/Services/LearningProjectWorkspaceSyncService.cs'
    req(sync, '/// <param name="ProjectType">')
    req(sync, '/// <param name="Toolchains">')
    req('src/LocalGPT/Services/LearningRoundService.cs', '/// <param name="regexCuratorService">')
    req('src/LocalGPT/Services/RegexDxAiFunctions.cs', '/// <param name="regexCurator">')

    # 4.6.1 startup lifetime repair remains intact.
    reg = 'src/LocalGPT/Program.ServiceRegistration.cs'
    coll = 'src/LocalGPT/Services/HumanCollaborationService.cs'
    req(reg, 'builder.Services.AddSingleton<IHumanCollaborationService, HumanCollaborationService>();')
    req(reg, 'builder.Services.AddScoped<IKnowledgeFreshnessReviewService, KnowledgeFreshnessReviewService>();')
    req(coll, 'IServiceScopeFactory serviceScopeFactory')
    req(coll, 'using var freshnessScope = serviceScopeFactory.CreateScope();')
    forbid(coll, 'IKnowledgeFreshnessReviewService knowledgeFreshness,')

    # 4.6.2 trust/quarantine/review/semantic action foundations remain wired.
    for rel, markers in {
        'src/LocalGPT/Services/ProjectIngestionService.cs': ['InspectAsync', 'PromoteAsync', 'ingestion-gate.json', 'RequiredIndependentApprovals', 'UserApproved'],
        'src/LocalGPT/Services/ProjectBlobReconstructionService.cs': ['SHA256.HashData', 'ManifestSha256', 'ValidateRelativePath', 'ChunkCount', 'CreateWorkspaceAsync'],
        'src/LocalGPT/Services/RegexCuratorService.cs': ['NeedsReview', 'ReviewAsync', 'SetUserApprovalAsync', 'ListApprovedPatternsAsync'],
        'src/LocalGPT/Services/OneWireCouncilHostEnrollmentService.cs': ['GetTrustedPeersAsync', 'EnrollAsync', 'normal 1-Wire pairing/trust'],
        'src/LocalGPT/Services/AsciiSemanticActionService.cs': ['ApplyControlAsync'],
    }.items():
        for marker in markers:
            req(rel, marker)
    for marker in ['onewire.pairing.ticket', 'onewire.trust.establish', 'onewire.council.host.enroll', 'project.ingestion.inspect', 'project.ingestion.promote', 'project.blob.start', 'localgpt.regex.curator.review', 'localgpt.ascii.actions.invoke']:
        req('src/LocalGPT/Services/ProjectAutomationDxAiFunctions.cs', marker)

    # 4.6.3 UI contract remains DevExpress-first and Bootstrap native-input classes stay off DevExpress editors.
    razor_files = list((ROOT / 'src/LocalGPT').rglob('*.razor'))
    native_input = re.compile(r'<Dx(?:TextBox|Memo|SpinEdit|ComboBox|TagBox|DateEdit|TimeEdit|CheckBox|FileInput)\\b[^>]*?(?:CssClass|class)="[^"]*(?:form-control|form-check-input|form-select|form-range)[^"]*"', re.S)
    bad = []
    native_file = []
    for path in razor_files:
        text = path.read_text(encoding='utf-8', errors='strict')
        if native_input.search(text):
            bad.append(path.relative_to(ROOT).as_posix())
        if '<InputFile' in text or re.search(r'<input\\b[^>]*type=["\']file["\']', text, re.I):
            native_file.append(path.relative_to(ROOT).as_posix())
    if bad:
        raise SystemExit(f'LocalGPT {VERSION} audit failed: Bootstrap native-input classes remain on DevExpress controls: {bad}')
    if native_file:
        raise SystemExit(f'LocalGPT {VERSION} audit failed: native file inputs remain in Razor: {native_file}')

    # Invariant numeric editor is a real DevExpress SpinEdit, never a slider/text fallback.
    editor = 'src/LocalGPT/Components/Shared/InvariantDoubleEditor.razor'
    for marker in ['<DxSpinEdit', '<DxNumericMaskProperties', 'CultureInfo.InvariantCulture', 'Mask="@NumericMask"', 'Increment="@Step"']:
        req(editor, marker)
    forbid(editor, '<DxTextBox')
    forbid(editor, 'DxRangeSelector')
    forbid(editor, 'type="range"')

    panel = 'src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor'
    for marker in [
        'InvariantDoubleEditor @bind-Value="device.DedicatedVramGiB"',
        'InvariantDoubleEditor @bind-Value="device.SystemMemoryGiB"',
        'ResolveAndInstallRecommendationAsync(item)',
        '@T("Resolve & install model")',
        '<DxFileInput',
        'BusyFor("hardware")', 'BusyFor("canirun")', 'BusyFor("provider")', 'BusyFor("models")', 'BusyFor("benchmark")',
        '<OperationalActivityFeed LocalText="@statusText"']:
        req(panel, marker)

    install = 'src/LocalGPT/Components/Pages/Install.razor'
    for marker in [
        'InvariantDoubleEditor @bind-Value="hardwareDraft.SystemMemoryGiB"',
        'InvariantDoubleEditor @bind-Value="hardwareDraft.DedicatedVramGiB"',
        '<OperationStatusStrip Busy="@HasActiveInstallOperation"',
        '<DxFileInput',
        '<OperationalActivityFeed LocalText="@_log"']:
        req(install, marker)

    # Operation feedback must use DevExpress wait indication and the shared real activity sources.
    status = 'src/LocalGPT/Components/Shared/OperationStatusStrip.razor'
    req(status, '<DxWaitIndicator')
    req(status, 'WaitIndicatorAnimationType.Spin')
    feed = 'src/LocalGPT/Components/Shared/OperationalActivityFeed.razor'
    for marker in ['<DxMemo', 'Console.Changed += OnSourceChanged', 'Activity.Changed += OnSourceChanged', 'IOperationalActivityFeedService']:
        req(feed, marker)
    feed_service = 'src/LocalGPT/Services/OperationalActivityFeedService.cs'
    req(feed_service, 'IConsoleCommandService console')
    req(feed_service, 'IComponentActivityService activity')
    req(reg, 'builder.Services.AddSingleton<IOperationalActivityFeedService, OperationalActivityFeedService>();')

    # Streaming upload advisor and quarantine boundaries.
    advisor = 'src/LocalGPT/Components/Shared/UploadProcessingAdvisor.razor'
    for marker in ['<DxFileInput', '<DxPopup', '<DxGridLayout', '<DxFormLayout', '<DxAccordion', '<OperationStatusStrip',
                   'Use recommendation in Chat', 'Ask suggested Council team', 'Request independent Council review', 'Approve promotion',
                   'UploadMode.OnButtonClick', 'CreateWorkspaceFromStreamsAsync']:
        req(advisor, marker)
    req('src/LocalGPT/Components/Pages/Chat.razor', '<UploadProcessingAdvisor')
    req('src/LocalGPT/Components/Pages/Chat.UploadAdvisor.razor.cs', 'SubmitUploadRecommendationToChatAsync')
    req('src/LocalGPT/Components/Pages/Chat.UploadAdvisor.razor.cs', 'SubmitUploadRecommendationToCouncilAsync')

    # 4.6.7: compile and architecture repair for the 4.6.5 upload-advisor pass.
    req(advisor, '<BodyContentTemplate Context="reviewPopupContext">')
    req(advisor, '@inject CouncilTextService CouncilText')
    req(advisor, 'CouncilText.FormatDistinctJoinedListOrFallback(values, " · ", fallback)')
    forbid(advisor, 'string.Join(')
    req('src/LocalGPT/Components/Shared/InvariantDoubleEditor.razor', 'ValueChanged="@((double? value) => OnValueChangedAsync(value))"')
    req('src/LocalGPT/Components/Pages/Chat.UploadAdvisor.razor.cs', 'using Microsoft.JSInterop;')
    req('src/LocalGPT/Services/CouncilTextService.cs', 'public string FormatDistinctJoinedListOrFallback(IEnumerable<string>? values, string separator, string fallback)')
    req('src/LocalGPT/Services/CouncilTextService.cs', 'serviceLogger.LogError(exception, "Formatting a distinct display list with fallback failed.");')

    # 4.6.7: the upload advisor must use the DevExpress GridLayout Template contract.
    advisor_text = read(advisor)
    grid_items = re.findall(r'<DxGridLayoutItem\b[^>]*>(.*?)</DxGridLayoutItem\s*>', advisor_text, re.S | re.I)
    if len(grid_items) != 4:
        raise SystemExit(f'LocalGPT {VERSION} audit failed: expected 4 upload-advisor grid items, found {len(grid_items)}')
    bad_grid_items = [body for body in grid_items if not re.match(r'\s*<Template(?:\s|>)', body, re.I)]
    if bad_grid_items:
        raise SystemExit(f'LocalGPT {VERSION} audit failed: upload-advisor DxGridLayoutItem content must use explicit Template children')
    guard = 'build/Assert-DevExpressBlazorControls.ps1'
    req(guard, 'error LGDX0002: DxGridLayoutItem content must be wrapped in its DevExpress <Template> child')
    req(guard, r"[regex]::Matches($content, '(?is)<DxGridLayoutItem\b[^>]*>(.*?)</DxGridLayoutItem\s*>')")

    upload_service = 'src/LocalGPT/Services/ChatUploadWorkspaceService.cs'
    for marker in ['CreateWorkspaceFromStreamsAsync', 'catalog.MaxSingleFileBytes', 'catalog.MaxTotalFileBytes',
                   'archive extraction is deferred until approved promotion', 'CopyBoundedStreamAsync']:
        req(upload_service, marker)
    req('src/LocalGPT/Interfaces/IChatUploadWorkspaceService.cs', 'CreateWorkspaceFromStreamsAsync')

    # Data-domain identity comes from approved curator regexes in the data catalog, not a compiled extension decision tree.
    classifier = 'src/LocalGPT/Services/ProjectEvidenceClassifierService.cs'
    req(classifier, 'private const string EvidenceRulePrefix = "project.evidence::";')
    req(classifier, 'ListApprovedPatternsAsync')
    seeds = 'src/LocalGPT/Services/Persistence/InitialDataCatalog.cs'
    for marker in [
        'project.evidence::SourceCode::DotNetProject::DotNet-MSBuild',
        'project.evidence::SourceCode::JavaMavenProject::Java-Maven',
        'project.evidence::SourceCode::JavaGradleProject::Java-Gradle',
        'project.evidence::SourceCode::NodeProject::Node-PackageManager',
        'project.evidence::SourceCode::RustProject::Rust-Cargo',
        'project.evidence::SourceCode::GoProject::Go',
        'project.evidence::SourceCode::PythonProject::Python',
        'project.evidence::GameProject::MinecraftProject::Minecraft',
        'project.evidence::Documentation::DocumentSet::Knowledge',
        'project.evidence::StructuredData::Dataset::DataAnalysis',
        'project.evidence::ImageMedia::ImageSet::Vision',
        'project.evidence::AudioVideoMedia::MediaSet::MediaAnalysis',
        'project.evidence::Archive::ArchiveSet::ArchiveReview',
        'project.evidence::EngineeringDesign::CadProject::DesignEngineering',
        'project.evidence::EmbeddedSource::EmbeddedProject::EmbeddedToolchain']:
        req(seeds, marker)

    # Recommendation is evidence/knowledge/team aware, AI-assisted only, and team identity is constrained to real persisted teams.
    recommend = 'src/LocalGPT/Services/UploadProcessingRecommendationService.cs'
    for marker in ['ICouncilKnowledgeService knowledge', 'ICouncilTeamConfigurationService teams', 'IChatClientFactory chatClientFactory',
                   'entry.IsUserApproved && !entry.IsArchived', 'teams.GetTeamsAsync', 'ValidateSuggestedTeam',
                   'processing-recommendation.json', 'chatClientFactory.Build()']:
        req(recommend, marker)
    req(reg, 'builder.Services.AddScoped<IUploadProcessingRecommendationService, UploadProcessingRecommendationService>();')
    req('src/LocalGPT/Services/ProjectAutomationDxAiFunctions.cs', 'project.ingestion.recommend')
    req('src/LocalGPT/Controller/ProjectIngestionController.cs', '[HttpPost("recommend")]')

    # 4.6.8: tournament/session correlation, pixel presentation and bounded rejoin.
    game_interface = 'src/LocalGPT/Interfaces/ICouncilGameSessionService.cs'
    game_service = 'src/LocalGPT/Services/CouncilGameSessionService.cs'
    game_bootstrap = 'src/LocalGPT/Services/CouncilGameWorkflowBootstrapService.cs'
    game_bridge = 'src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs'
    game_renderer = 'src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs'
    game_console = 'src/LocalGPT/Components/Shared/ChatGameConsole.razor'
    game_console_css = 'src/LocalGPT/Components/Shared/ChatGameConsole.razor.css'
    chat_css = 'src/LocalGPT/Components/Pages/Chat.razor.css'
    live_council = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'
    game_js = 'src/LocalGPT/wwwroot/js/localgpt-game-console.js'
    req(game_interface, 'string gameKey')
    req(game_service, 'NormalizeGameKey(gameKey)')
    req(game_service, 'string.Equals(item.GameKey, normalizedGameKey, StringComparison.OrdinalIgnoreCase)')
    req(game_bootstrap, 'GetActiveForCouncilRunAsync(request.RunId, gameKey')
    req(game_bootstrap, 'FrameWidth = highResolutionTournament ? 144 : 80')
    req(game_bootstrap, 'FrameHeight = highResolutionTournament ? 40 : 25')
    req(game_bridge, 'GetActiveForCouncilRunAsync(result.RunId, "kernel-creature-tournament"')
    req(game_renderer, 'Math.Min(160, Math.Max(96, session.FrameWidth))')
    req(game_renderer, 'Math.Min(48, Math.Max(32, session.FrameHeight))')
    req(game_console, 'new("Pixel", "Pixel screen")')
    req(game_console, 'data-game-pixel-screen')
    req(game_console, 'operatorReturnsToGame')
    req(game_console_css, '.chat-game-pixel-screen')
    req(game_js, 'function renderPixelFrame')
    req(game_js, 'setDisplayMode(id, mode)')
    req(game_js, 'if (entry.enabled) scrollToTail(region)')
    req(live_council, 'WaitAsync(TimeSpan.FromSeconds(3), componentLifetimeCts.Token)')
    req(live_council, 'Rejoining the run already projected by this circuit must not force DxAIChat to rebind')
    req(chat_css, 'max-height: min(15rem, 34dvh);')
    req('build/audit_kernel_creature_tournament_ascii.py', 'Kernel Creature Tournament audit passed')

    # InteractiveServer topology remains explicit on routed pages except the intentional Error page.
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

    print(f'LocalGPT {VERSION} release audit passed: exact tournament session correlation, higher-resolution ASCII, shared pixel presentation, bounded rejoin/operator return, prior Chat/upload repairs, InteractiveServer topology and {checked} JS hashes are consistent.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
