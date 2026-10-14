#!/usr/bin/env python3
from pathlib import Path
import hashlib, re, sys

ROOT = Path(__file__).resolve().parents[1]
VERSION = '4.6.6'


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
    req('CHANGELOG-v4.6.6-BUILD-ARCHITECTURE-REPAIR.md', f'# LocalGPT {VERSION}')
    req('VALIDATION-v4.6.6-source.md', f'# LocalGPT {VERSION} source validation')
    req('src/LocalGPT/Components/App.razor', 'localgpt-game-console.js?v=4.6.6')
    req('src/LocalGPT/Components/App.razor', 'localgpt-chat-ui.js?v=4.6.6')
    req('src/LocalGPT/Services/InitialSetupAssistantService.cs', 'LocalGPT/4.6.6')
    req('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/4.6.6')
    req('docs/index.md', '**Version 4.6.6**')
    req('docs/docfx.json', '"localgptVersion": "4.6.6"')
    req('docs/pdf-cover.html', 'LocalGPT 4.6.6 Documentation')
    req('docs/pdf/toc.yml', 'LocalGPT-4.6.6.pdf')

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

    # 4.6.6: compile and architecture repair for the 4.6.5 upload-advisor pass.
    req(advisor, '<BodyContentTemplate Context="reviewPopupContext">')
    req(advisor, '@inject CouncilTextService CouncilText')
    req(advisor, 'CouncilText.FormatDistinctJoinedListOrFallback(values, " · ", fallback)')
    forbid(advisor, 'string.Join(')
    req('src/LocalGPT/Components/Shared/InvariantDoubleEditor.razor', 'ValueChanged="@((double? value) => OnValueChangedAsync(value))"')
    req('src/LocalGPT/Components/Pages/Chat.UploadAdvisor.razor.cs', 'using Microsoft.JSInterop;')
    req('src/LocalGPT/Services/CouncilTextService.cs', 'public string FormatDistinctJoinedListOrFallback(IEnumerable<string>? values, string separator, string fallback)')
    req('src/LocalGPT/Services/CouncilTextService.cs', 'serviceLogger.LogError(exception, "Formatting a distinct display list with fallback failed.");')

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

    print(f'LocalGPT {VERSION} release audit passed: 4.6.5 compile/architecture repairs, DevExpress upload/recommendation UX, invariant SpinEdit, operation activity, quarantine/review boundaries, prior repairs, InteractiveServer topology and {checked} JS hashes are consistent.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
