#!/usr/bin/env python3
"""Static contract audit for LocalGPT 3.1.9 normal-flow Chat quick preset row."""
from __future__ import annotations

from pathlib import Path
import hashlib
import sys

root = Path(__file__).resolve().parents[1]
checks = 0


def read(rel: str) -> str:
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8-sig", errors="strict")


def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f"{rel} missing {token!r}")


def forbid(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token in data:
            raise AssertionError(f"{rel} unexpectedly contains {token!r}")


try:
    razor = read("src/LocalGPT/Components/Pages/Chat.razor")
    css = read("src/LocalGPT/Components/Pages/Chat.razor.css")

    # Existing detailed configuration stays structurally untouched.
    config_start = razor.index('<details class="chat-configuration-ribbon"')
    config_end = razor.index('<div id="localgpt-chat-host"', config_start)
    config_hash = hashlib.sha256(razor[config_start:config_end].encode("utf-8")).hexdigest()
    checks += 1
    if config_hash != "cc8adbf3a5e57a225a4754043eb779610baecd1608b0114e9645cb52cb5dcc54":
        raise AssertionError(f"Chat Configuration markup changed: {config_hash}")

    # The DevExpress chat itself remains byte-identical to the known-good source.
    chat_start = razor.index('<DxAIChat Initialized="ChatInitialized"')
    chat_end = razor.index('</DxAIChat>', chat_start) + len('</DxAIChat>')
    dxaichat_hash = hashlib.sha256(razor[chat_start:chat_end].encode("utf-8")).hexdigest()
    checks += 1
    if dxaichat_hash != "ae8a6d9cec66907f073f94c4b83a939e44b209ccaadfa5b0527c2a0386c26b54":
        raise AssertionError(f"DxAIChat subtree changed: {dxaichat_hash}")

    # Quick controls are one normal-flow sibling row after the chat host and before session tools.
    host_start = razor.index('<div id="localgpt-chat-host"')
    host_close = razor.index('</div>', chat_end)
    quick_start = razor.index('<div class="w-100" data-testid="chat-quick-configuration-bar"', host_close)
    session_start = razor.index('<details class="chat-session-tools-ribbon"', quick_start)
    checks += 1
    if not (chat_end < host_close < quick_start < session_start):
        raise AssertionError("quick preset row is not directly outside Chat and before Running session tools")

    quick_block = razor[quick_start:session_start]
    checks += 1
    if quick_block.count('<div') != 1:
        raise AssertionError("quick preset row must contain exactly one explicit div")
    checks += 1
    if quick_block.count('<DxFormLayoutItem') != 3:
        raise AssertionError("quick preset row must contain exactly three DxFormLayoutItem components")

    for caption in ("Team", "Models", "Performance"):
        checks += 1
        if f'<DxFormLayoutItem Caption="{caption}" ColSpanMd="4">' not in quick_block:
            raise AssertionError(f"missing one-third DevExpress form item for {caption}")

    for token in (
        '<DxFormLayout>',
        '<DxComboBox Data="@CouncilTeams"',
        'ValueChanged="@((OrganicCouncilTeamDefinition team) => OnQuickCouncilTeamChangedAsync(team))"',
        '<DxComboBox Data="@ModelPresets"',
        'ValueChanged="@((CouncilModelPreset preset) => OnQuickModelPresetChangedAsync(preset))"',
        '<DxComboBox Data="@HardwarePerformancePresetItems"',
        'ValueChanged="@((HardwarePerformancePreset preset) => OnQuickHardwarePerformancePresetChangedAsync(preset))"',
    ):
        checks += 1
        if token not in quick_block:
            raise AssertionError(f"quick preset row missing {token!r}")

    for forbidden in (
        'chat-quick-configuration-item',
        'position: absolute',
        'overflow-x:',
        'style=',
    ):
        checks += 1
        if forbidden in quick_block:
            raise AssertionError(f"quick preset row contains forbidden layout coupling {forbidden!r}")

    # No selector-specific CSS remains. DevExpress FormLayout owns the row/component geometry.
    forbid(
        "src/LocalGPT/Components/Pages/Chat.razor.css",
        ".chat-quick-configuration-bar",
        ".chat-quick-configuration-item",
    )

    # The only CSS delta versus the known-good pre-feature Chat CSS is adding one normal-flow
    # grid row (and the corresponding optional-game row) for the new sibling surface.
    normalized = css
    replacements = {
        "grid-template-rows: auto auto minmax(0, 1fr) auto auto !important;":
            "grid-template-rows: auto auto minmax(0, 1fr) auto !important;",
        "grid-template-rows: auto auto minmax(18rem, 54dvh) minmax(0, 1fr) auto auto !important;":
            "grid-template-rows: auto auto minmax(18rem, 54dvh) minmax(0, 1fr) auto !important;",
        "grid-template-rows: auto auto minmax(16rem, 44dvh) minmax(0, 1fr) auto auto !important;":
            "grid-template-rows: auto auto minmax(16rem, 44dvh) minmax(0, 1fr) auto !important;",
        "grid-template-rows: auto auto minmax(16rem, 48dvh) minmax(0, 1fr) auto auto !important;":
            "grid-template-rows: auto auto minmax(16rem, 48dvh) minmax(0, 1fr) auto !important;",
        ".chat-game-visible .chat-session-tools-ribbon {\n    grid-row: 6;\n}":
            ".chat-game-visible .chat-session-tools-ribbon {\n    grid-row: 5;\n}",
    }
    for current, baseline in replacements.items():
        checks += 1
        if current not in normalized:
            raise AssertionError(f"missing expected normal-flow grid change {current!r}")
        normalized = normalized.replace(current, baseline, 1)

    normalized = normalized.rstrip() + "\n"
    normalized_hash = hashlib.sha256(normalized.encode("utf-8")).hexdigest()
    checks += 1
    if normalized_hash != "3bc9693f026e410de1cd03c24544ab5695f58a13d238bc9710498eab6e090ad1":
        raise AssertionError(f"Chat CSS changed outside the permitted normal-flow grid rows: {normalized_hash}")

    # Service/state wiring remains the already-tested implementation from 3.1.8.
    require(
        "src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs",
        "private Task OnQuickModelPresetChangedAsync(CouncilModelPreset? preset)",
        "private Task OnQuickHardwarePerformancePresetChangedAsync(HardwarePerformancePreset? preset)",
        "await InvokeAsync(() => selectedId = SelectedModelPreset?.Id)",
        "await InvokeAsync(() => selectedId = SelectedHardwarePerformancePreset?.Id)",
    )
    require(
        "src/LocalGPT/Components/Pages/Chat.razor.cs",
        "private Task OnQuickCouncilTeamChangedAsync(OrganicCouncilTeamDefinition? team)",
        "await InvokeAsync(() =>",
        "CouncilTeams = loadedTeams;",
    )

    print(f"LocalGPT 3.1.9 Chat quick-preset row audit passed: {checks} checks.")
except (AssertionError, ValueError) as exc:
    print(f"LocalGPT 3.1.9 Chat quick-preset row audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
