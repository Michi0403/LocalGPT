#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.6 Chat quick presets and live configuration refresh."""
from __future__ import annotations
from pathlib import Path
import hashlib
import sys

root = Path(__file__).resolve().parents[1]
checks = 0


def read(rel: str) -> str:
    path = root / rel
    if not path.is_file():
        raise AssertionError(f'missing {rel}')
    return path.read_text(encoding='utf-8-sig', errors='strict')


def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f'{rel} missing {token!r}')


def forbid(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token in data:
            raise AssertionError(f'{rel} unexpectedly contains {token!r}')


def tree_digest(path: Path) -> str:
    digest = hashlib.sha256()
    for item in sorted(path.rglob('*')):
        if not item.is_file():
            continue
        rel = item.relative_to(root).as_posix().encode('utf-8')
        digest.update(rel + b'\0' + item.read_bytes() + b'\0')
    return digest.hexdigest()


try:
    for rel in (
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ):
        require(rel, '<Version>3.1.6</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.1.6')
    require('global.json', '"version": "10.0.400"')

    checks += 1
    migration_digest = tree_digest(root / 'src/LocalGPT/Migrations')
    expected_migration_digest = '27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f'
    if migration_digest != expected_migration_digest:
        raise AssertionError(f'migration source changed: {migration_digest} != {expected_migration_digest}')

    checks += 1
    compatibility = root / 'src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
    compatibility_digest = hashlib.sha256(compatibility.read_bytes()).hexdigest()
    expected_compatibility_digest = '50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba'
    if compatibility_digest != expected_compatibility_digest:
        raise AssertionError(f'database migration compatibility source changed: {compatibility_digest} != {expected_compatibility_digest}')

    require('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs',
            'private const int BenchmarkEvidenceSchemaVersion = 1;')

    # Conservative detector contract.
    require('src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs',
            'MaximumBufferedCharacters = 12_288',
            'MinimumObservedCharacters = 1_024',
            'MinimumAnalyzedTokens = 72',
            'MaximumPeriodTokens = 32',
            'MinimumRepeatedCycles = 6',
            'MinimumPeriodicAgreement = 0.97d',
            'RequiredSuspiciousSamples = 4',
            'TimeSpan.FromSeconds(4)',
            'TimeSpan.FromSeconds(2)',
            'TimeSpan.FromSeconds(6)',
            'Stopwatch',
            'provider content was omitted',
            'The repeated output itself remains in provider-stream evidence and is omitted from exception text.')
    forbid('src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs',
           'Process.Kill', 'Kill(', 'Environment.Exit', 'HttpClient.CancelPendingRequests')

    # Benchmark reuses configured Social Team recovery count but preserves exact subject identity.
    require('src/LocalGPT/BusinessObjects/ProviderModelBenchmarkModels.cs',
            'public int RepetitionRecoveryAttempts { get; set; } = 1;')
    require('src/LocalGPT/BusinessObjects/CouncilBenchmarkCalibrationModels.cs',
            'public int RepetitionRecoveryAttempts { get; set; } = 1;')
    require('src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs',
            'definition.MemberFailureRecoveryMode == CouncilMemberFailureRecoveryMode.Disabled',
            'Math.Clamp(definition.MemberFailureRecoveryAttempts, 0, 8)')
    require('src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs',
            'RepetitionRecoveryAttempts = Math.Clamp(request.RepetitionRecoveryAttempts, 0, 8)')
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.cs',
            'var repetitionRecoveryAttempts = Math.Clamp(request.RepetitionRecoveryAttempts, 0, 8);',
            'repetition watchdog recovery',
            'repetitionRecoveryAttempts,')
    require('src/LocalGPT/Services/ProviderModelBenchmarkService.ProfileExecution.cs',
            'var repetitionWatchdog = new ProviderStreamRepetitionWatchdog(logger);',
            'attemptCts.Cancel();',
            'throw repetitionFailure;',
            'retrying the same provider-qualified Benchmark Subject',
            'The failed provider stream remains inspectable and the benchmark will continue instead of blocking this host queue.',
            'taskResult.AttemptCount++')

    # Ordinary Council and its secondary recovery streams are protected; the existing scheduler remains the recovery authority.
    require('src/LocalGPT/Services/MultiModelCouncilService.ParticipantExecution.cs',
            'var repetitionWatchdog = new ProviderStreamRepetitionWatchdog(logger);',
            'streamCts.Cancel();',
            'existing member recovery will now handle this failed attempt')
    require('src/LocalGPT/Services/MultiModelCouncilService.RecoveryAndPersistence.cs',
            'the corrective role retry entered sustained repeated generation and was stopped',
            'the final-answer recovery entered sustained repeated generation and was stopped')
    require('src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs',
            'RecoverConfiguredRoundMemberFailuresAsync',
            'automatic member recovery',
            'LocalGPT did not silently drop or fabricate this member result')
    require('src/LocalGPT/Services/MultiModelCouncilService.LiveInputAndHealth.cs',
            'RetryParticipantWithSafeLimitsAsync',
            'safe Ollama CPU and bounded context/output settings')

    # Caller cancellation remains an expected, separate path.
    require('src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs',
            'was stopped by caller cancellation', 'is not classified as a Council failure')
    require('src/LocalGPT/Services/OllamaThinkingChatClient.Transport.cs',
            'Ollama HTTP request was cancelled by its caller')

    # 3.1.4 documentation completeness remains intact.
    require('build/Assert-XmlDocumentationCoverage.py', 'run_razor', "run_csharp(args.root, 'validate')")
    checks += 1
    components = [p for p in (root / 'src').rglob('*.razor') if p.name != '_Imports.razor' and 'bin' not in p.parts and 'obj' not in p.parts]
    if len(components) != 45:
        raise AssertionError(f'expected 45 maintained Razor components, found {len(components)}')

    # 3.1.6 quick prompt-line selectors use the existing authoritative service-backed paths.
    require('src/LocalGPT/Components/Pages/Chat.razor',
            'data-testid="chat-quick-configuration-bar"',
            'aria-label="Quick Council configuration"',
            '<DxComboBox Data="@CouncilTeams"',
            'ValueChanged="OnQuickCouncilTeamChangedAsync"',
            '<DxComboBox Data="@ModelPresets"',
            'ValueChanged="OnQuickModelPresetChangedAsync"',
            '<DxComboBox Data="@HardwarePerformancePresetItems"',
            'ValueChanged="OnQuickHardwarePerformancePresetChangedAsync"')
    require('src/LocalGPT/Components/Pages/Chat.razor.cs',
            'SelectedQuickCouncilTeam',
            'SelectedQuickModelPreset',
            'SelectedQuickHardwarePerformancePreset',
            'RefreshCouncilTeamItemsAsync',
            'OnQuickCouncilTeamChangedAsync')
    require('src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs',
            'RefreshModelPresetItemsAsync',
            'OnQuickModelPresetChangedAsync',
            'OnQuickHardwarePerformancePresetChangedAsync',
            'OnHardwarePerformancePresetChangedAsync(new ChangeEventArgs',
            'OnModelPresetChangedAsync(new ChangeEventArgs')
    require('src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs',
            'RefreshServiceBackedChatConfigurationOnOpen',
            'RefreshServiceBackedChatConfigurationAsync',
            'RefreshCouncilTeamItemsAsync(cancellationToken)',
            'RefreshModelPresetItemsAsync(cancellationToken)',
            'LoadHardwarePerformancePresetsAsync(cancellationToken)',
            'LoadPersistentPromptSuggestionsAsync(cancellationToken)',
            'LoadChatProjectsAsync(cancellationToken)',
            'RefreshMemoryAsync(cancellationToken)',
            'RefreshProvidersOnChatConfigurationOpen',
            'Interlocked.Exchange(ref chatConfigurationRefreshGate, 1)',
            'Volatile.Write(ref chatConfigurationRefreshGate, 0)')
    require('src/LocalGPT/Components/Pages/Chat.razor.css',
            '.chat-quick-configuration-bar',
            'grid-template-columns: repeat(3',
            '.chat-quick-configuration-item',
            'padding-bottom: 4.4rem !important;')
    if (root / 'src/LocalGPT/wwwroot/js/chat-runtime.js').is_file():
        forbid('src/LocalGPT/wwwroot/js/chat-runtime.js', 'chat-quick-configuration-bar')

    # The 3.1.5 watchdog remains present and its regression audit stays packaged.
    require('build/audit_provider_stream_repetition_policy.py', 'ProviderStreamRepetitionWatchdog')
    require('build/audit_chat_quick_configuration_3_1_6.py', 'Chat quick configuration audit passed')

    require('CHANGELOG-v3.1.6-CHAT-QUICK-PRESETS-LIVE-CONFIG-REFRESH.md',
            'three compact DevExpress selectors',
            'ICouncilTeamConfigurationService',
            'IModelPresetService',
            'IHardwarePerformancePresetService',
            'Opening the Chat configuration ribbon',
            'Provider/Ollama discovery keeps its existing refresh path')
    require('VALIDATION-v3.1.6-source.md', '9,431', '752', 'No `dotnet`', '3.1.6')
    require('RELEASE.md', '# LocalGPT 3.1.6', 'Three compact DevExpress selectors')

    print(f'LocalGPT 3.1.6 Chat quick presets and live configuration refresh source audit passed: {checks} checks.')
except (AssertionError, ValueError) as exc:
    print(f'LocalGPT 3.1.6 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
