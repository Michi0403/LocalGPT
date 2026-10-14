#!/usr/bin/env python3
"""Static contract audit for LocalGPT 3.1.8 isolated Chat quick preset selectors."""
from __future__ import annotations

from pathlib import Path
import hashlib
import re
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

    # The detailed Chat Configuration workspace is protected. 3.1.8 may refresh its data,
    # but this feature must not structurally rewrite the modal/ribbon contents.
    config_start = razor.index('<details class="chat-configuration-ribbon"')
    config_end = razor.index('<div id="localgpt-chat-host"', config_start)
    config_hash = hashlib.sha256(razor[config_start:config_end].encode("utf-8")).hexdigest()
    checks += 1
    if config_hash != "cc8adbf3a5e57a225a4754043eb779610baecd1608b0114e9645cb52cb5dcc54":
        raise AssertionError(f"Chat Configuration markup changed: {config_hash}")

    # Protect the entire DevExpress AI Chat subtree. Quick presets are siblings, never children.
    chat_start = razor.index('<DxAIChat Initialized="ChatInitialized"')
    chat_end = razor.index('</DxAIChat>', chat_start) + len('</DxAIChat>')
    dxaichat_hash = hashlib.sha256(razor[chat_start:chat_end].encode("utf-8")).hexdigest()
    checks += 1
    if dxaichat_hash != "ae8a6d9cec66907f073f94c4b83a939e44b209ccaadfa5b0527c2a0386c26b54":
        raise AssertionError(f"DxAIChat subtree changed: {dxaichat_hash}")

    quick_pos = razor.index('data-testid="chat-quick-configuration-bar"')
    checks += 1
    if quick_pos <= chat_end:
        raise AssertionError("quick selector dock is not a sibling after DxAIChat")

    require(
        "src/LocalGPT/Components/Pages/Chat.razor",
        "Quick selectors are intentionally a sibling of DxAIChat",
        '<DxComboBox Data="@CouncilTeams"',
        'ValueChanged="@((OrganicCouncilTeamDefinition team) => OnQuickCouncilTeamChangedAsync(team))"',
        '<DxComboBox Data="@ModelPresets"',
        'ValueChanged="@((CouncilModelPreset preset) => OnQuickModelPresetChangedAsync(preset))"',
        '<DxComboBox Data="@HardwarePerformancePresetItems"',
        'ValueChanged="@((HardwarePerformancePreset preset) => OnQuickHardwarePerformancePresetChangedAsync(preset))"',
    )

    # Everything before the 3.1.8 quick-dock section must be byte-equivalent to the known-good
    # 3.1.5 Chat CSS. This protects the composer, textarea, attachment, send and stop layout.
    marker = "/* v3.1.8: quick Council selectors are a sibling overlay only."
    css_prefix = css[: css.index(marker)].rstrip() + "\n"
    prefix_hash = hashlib.sha256(css_prefix.encode("utf-8")).hexdigest()
    checks += 1
    if prefix_hash != "3bc9693f026e410de1cd03c24544ab5695f58a13d238bc9710498eab6e090ad1":
        raise AssertionError(f"protected pre-quick Chat CSS changed: {prefix_hash}")

    quick_css = css[css.index(marker):]
    for forbidden in (
        ".localgpt-chat-composer {",
        ".localgpt-chat-textarea",
        ".dxbl-chatui-submitarea",
        ".dxbl-chatui-input",
        "min-height:",
        "padding-bottom:",
    ):
        checks += 1
        if forbidden in quick_css:
            raise AssertionError(f"quick selector CSS illegally modifies DevExpress composer layout via {forbidden!r}")

    for required in (
        ".chat-quick-configuration-bar {",
        "position: absolute;",
        "width: max-content;",
        "pointer-events: none;",
        ".chat-quick-configuration-item {",
        "pointer-events: auto;",
    ):
        checks += 1
        if required not in quick_css:
            raise AssertionError(f"isolated quick selector CSS missing {required!r}")

    # Renderer-affinity contract for data refreshed outside normal UI event callbacks.
    require(
        "src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs",
        "var loadedPresets = (await HardwarePerformancePresets",
        "await InvokeAsync(() => selectedId = SelectedHardwarePerformancePreset?.Id)",
        "HardwarePerformancePresetItems = loadedPresets;",
        "var loadedPresets = (await ModelPresetService",
        "await InvokeAsync(() => selectedId = SelectedModelPreset?.Id)",
        "ModelPresets = loadedPresets;",
    )
    require(
        "src/LocalGPT/Components/Pages/Chat.razor.cs",
        "await InvokeAsync(() =>",
        "CouncilTeams = loadedTeams;",
        "AllPromptSuggestions = merged.Values.ToList();",
    )
    require(
        "src/LocalGPT/Components/Pages/Chat.PersistenceAndMemory.razor.cs",
        "await InvokeAsync(() => activeConversationId = ActiveConversationId)",
        "SavedConversations = conversations;",
        "ChatProjects = projects;",
        "SelectedChatProjectDetails = selectedDetails;",
    )
    require(
        "src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs",
        "await InvokeAsync(() => AllPromptSuggestions = Catalog.GetSuggestion())",
        "await InvokeAsync(RefreshPromptSuggestions)",
    )

    forbid(
        "src/LocalGPT/Components/Pages/Chat.PresetsAndCouncilConfiguration.razor.cs",
        "ModelPresets = (await ModelPresetService",
        "HardwarePerformancePresetItems = (await HardwarePerformancePresets",
    )
    forbid(
        "src/LocalGPT/Components/Pages/Chat.razor.cs",
        "CouncilTeams = teams.OrderBy",
    )
    forbid(
        "src/LocalGPT/Components/Pages/Chat.PersistenceAndMemory.razor.cs",
        "SavedConversations = (await ChatMemory.GetConversationsAsync",
        "ChatProjects = (await ProjectService.GetProjectsAsync",
    )

    print(f"LocalGPT 3.1.8 isolated Chat quick-configuration audit passed: {checks} checks.")
except (AssertionError, ValueError) as exc:
    print(f"LocalGPT 3.1.8 isolated Chat quick-configuration audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
