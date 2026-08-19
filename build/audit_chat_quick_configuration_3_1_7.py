#!/usr/bin/env python3
"""Static contract audit for the LocalGPT 3.1.7 typed DxComboBox quick-selector callbacks."""
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
        '<DxComboBox Data="@CouncilTeams"',
        'ValueChanged="@((OrganicCouncilTeamDefinition team) => OnQuickCouncilTeamChangedAsync(team))"',
        '<DxComboBox Data="@ModelPresets"',
        'ValueChanged="@((CouncilModelPreset preset) => OnQuickModelPresetChangedAsync(preset))"',
        '<DxComboBox Data="@HardwarePerformancePresetItems"',
        'ValueChanged="@((HardwarePerformancePreset preset) => OnQuickHardwarePerformancePresetChangedAsync(preset))"',
    )
    forbid(
        razor,
        'ValueChanged="OnQuickCouncilTeamChangedAsync"',
        'ValueChanged="OnQuickModelPresetChangedAsync"',
        'ValueChanged="OnQuickHardwarePerformancePresetChangedAsync"',
    )
    require(
        'src/LocalGPT/Components/Pages/Chat.razor.cs',
        'private Task OnQuickCouncilTeamChangedAsync(OrganicCouncilTeamDefinition? team)',
    )
    require(
        'src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs',
        'private Task OnQuickModelPresetChangedAsync(CouncilModelPreset? preset)',
        'private Task OnQuickHardwarePerformancePresetChangedAsync(HardwarePerformancePreset? preset)',
    )
    # 3.1.6 service-refresh behavior remains present.
    require(
        'src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs',
        'RefreshServiceBackedChatConfigurationOnOpen',
        'RefreshServiceBackedChatConfigurationAsync',
    )
    print(f'LocalGPT 3.1.7 typed quick-selector callback audit passed: {checks} checks.')
except AssertionError as exc:
    print(f'LocalGPT 3.1.7 typed quick-selector callback audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
