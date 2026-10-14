#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.1.7 typed DxComboBox callback repair."""
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
        require(rel, '<Version>3.1.7</Version>')
    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs', 'LocalGPT/3.1.7')
    require('global.json', '"version": "10.0.400"')

    checks += 1
    migration_digest = tree_digest(root / 'src/LocalGPT/Migrations')
    if migration_digest != '27c5b6d71b8f9527b64f18ff66ac102ae0558e4ed01317ff02e34f6b77f99c4f':
        raise AssertionError(f'migration source changed: {migration_digest}')

    checks += 1
    compatibility = root / 'src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
    if hashlib.sha256(compatibility.read_bytes()).hexdigest() != '50bb2f62df4b6cfe5846063d5e4f20c2ab930a57cb95efa580ad6617f3a748ba':
        raise AssertionError('database migration compatibility source changed')

    require('src/LocalGPT/Services/ProviderModelBenchmarkService.EvidencePersistence.cs',
            'private const int BenchmarkEvidenceSchemaVersion = 1;')

    # Compile-repair contract: DevExpress ValueChanged receives a typed lambda rather than an untyped method group.
    require('src/LocalGPT/Components/Pages/Chat.razor',
            'ValueChanged="@((OrganicCouncilTeamDefinition team) => OnQuickCouncilTeamChangedAsync(team))"',
            'ValueChanged="@((CouncilModelPreset preset) => OnQuickModelPresetChangedAsync(preset))"',
            'ValueChanged="@((HardwarePerformancePreset preset) => OnQuickHardwarePerformancePresetChangedAsync(preset))"')
    forbid('src/LocalGPT/Components/Pages/Chat.razor',
           'ValueChanged="OnQuickCouncilTeamChangedAsync"',
           'ValueChanged="OnQuickModelPresetChangedAsync"',
           'ValueChanged="OnQuickHardwarePerformancePresetChangedAsync"')

    # Preserve 3.1.6 live configuration refresh and all three quick selectors.
    require('src/LocalGPT/Components/Pages/Chat.razor',
            'data-testid="chat-quick-configuration-bar"',
            '<DxComboBox Data="@CouncilTeams"',
            '<DxComboBox Data="@ModelPresets"',
            '<DxComboBox Data="@HardwarePerformancePresetItems"')
    require('src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs',
            'RefreshServiceBackedChatConfigurationOnOpen',
            'RefreshServiceBackedChatConfigurationAsync',
            'RefreshCouncilTeamItemsAsync(cancellationToken)',
            'RefreshModelPresetItemsAsync(cancellationToken)',
            'LoadHardwarePerformancePresetsAsync(cancellationToken)',
            'LoadPersistentPromptSuggestionsAsync(cancellationToken)',
            'LoadChatProjectsAsync(cancellationToken)',
            'RefreshMemoryAsync(cancellationToken)')

    # Preserve repetition watchdog and recovery behavior from 3.1.5/3.1.3.
    require('src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs',
            'MaximumBufferedCharacters = 12_288',
            'MinimumPeriodicAgreement = 0.97d',
            'RequiredSuspiciousSamples = 4')
    require('src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs',
            'RecoverConfiguredRoundMemberFailuresAsync',
            'automatic member recovery')

    # Documentation completeness remains enforced.
    require('build/Assert-XmlDocumentationCoverage.py', 'run_razor', "run_csharp(args.root, 'validate')")
    require('build/audit_chat_quick_configuration_3_1_7.py', 'typed quick-selector callback audit passed')
    require('CHANGELOG-v3.1.7-DXCOMBOBOX-TYPED-CALLBACK-REPAIR.md',
            'CS1503', 'EventCallback', 'typed lambda')
    require('VALIDATION-v3.1.7-source.md', '9,905', '752', 'No `dotnet`', '3.1.7')
    require('RELEASE.md', '# LocalGPT 3.1.7', 'typed `ValueChanged` lambdas')

    print(f'LocalGPT 3.1.7 typed DxComboBox callback repair source audit passed: {checks} checks.')
except (AssertionError, ValueError) as exc:
    print(f'LocalGPT 3.1.7 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
