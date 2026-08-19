#!/usr/bin/env python3
"""Static contract audit for the LocalGPT 3.1.6 Chat quick selectors and live configuration refresh."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
checks = 0


def text(rel: str) -> str:
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8-sig")


def require(rel: str, *tokens: str) -> None:
    global checks
    data = text(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f"{rel} missing {token!r}")


def forbid(rel: str, *tokens: str) -> None:
    global checks
    data = text(rel)
    for token in tokens:
        checks += 1
        if token in data:
            raise AssertionError(f"{rel} unexpectedly contains {token!r}")


try:
    razor = 'src/LocalGPT/Components/Pages/Chat.razor'
    require(
        razor,
        'data-testid="chat-quick-configuration-bar"',
        'aria-label="Quick Council configuration"',
        '<DxComboBox Data="@CouncilTeams"',
        'Value="@SelectedQuickCouncilTeam"',
        'ValueChanged="OnQuickCouncilTeamChangedAsync"',
        '<DxComboBox Data="@ModelPresets"',
        'Value="@SelectedQuickModelPreset"',
        'ValueChanged="OnQuickModelPresetChangedAsync"',
        '<DxComboBox Data="@HardwarePerformancePresetItems"',
        'Value="@SelectedQuickHardwarePerformancePreset"',
        'ValueChanged="OnQuickHardwarePerformancePresetChangedAsync"',
    )
    checks += 1
    if text(razor).count('<DxComboBox Data=') < 3:
        raise AssertionError('Chat quick configuration must expose all three DevExpress selectors')

    require(
        'src/LocalGPT/Components/Pages/Chat.razor.cs',
        'SelectedQuickCouncilTeam',
        'SelectedQuickModelPreset',
        'SelectedQuickHardwarePerformancePreset',
        'RefreshCouncilTeamItemsAsync',
        'CouncilTeamConfigurations',
        'OnQuickCouncilTeamChangedAsync',
    )
    require(
        'src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs',
        'RefreshModelPresetItemsAsync',
        'ModelPresetService',
        'OnQuickModelPresetChangedAsync',
        'OnQuickHardwarePerformancePresetChangedAsync',
        'OnHardwarePerformancePresetChangedAsync(new ChangeEventArgs',
        'OnModelPresetChangedAsync(new ChangeEventArgs',
    )
    require(
        'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs',
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
        'Volatile.Write(ref chatConfigurationRefreshGate, 0)',
    )
    require(
        'src/LocalGPT/Components/Pages/Chat.razor.css',
        '.chat-quick-configuration-bar',
        'grid-template-columns: repeat(3',
        '.chat-quick-configuration-item',
        '.chat-session-active .chat-quick-configuration-bar',
        'padding-bottom: 4.4rem !important;',
    )
    # The selectors stay in Blazor-owned markup. No DOM transplant is used because moving
    # component-owned nodes into DevExpress internals can cause renderer churn/flicker.
    forbid(
        'src/LocalGPT/wwwroot/js/chat-runtime.js',
        'chat-quick-configuration-bar',
    ) if (root / 'src/LocalGPT/wwwroot/js/chat-runtime.js').is_file() else None

    print(f'LocalGPT 3.1.6 Chat quick configuration audit passed: {checks} checks.')
except AssertionError as exc:
    print(f'LocalGPT 3.1.6 Chat quick configuration audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
