#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.8.7 Local Chat naming and legacy Council navigation cleanup."""
from pathlib import Path
import json
import re
import sys

root = Path(__file__).resolve().parents[1]


def read(rel):
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8")


def require(rel, *needles):
    text = read(rel)
    missing = [needle for needle in needles if needle not in text]
    if missing:
        raise AssertionError(f"{rel} missing {missing}")


try:
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        require(rel, "<Version>2.9.9</Version>")
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    require(
        "src/LocalGPT/Components/Pages/Index.razor",
        'Home.LocalChat.Welcome',
        'Home.LocalChat.Title',
        'href="@NavigationUrls.GetUrl("/chat", ToggledSidebar)"',
    )
    index = read("src/LocalGPT/Components/Pages/Index.razor")
    if "/model-council" in index:
        raise AssertionError("obsolete AI Council home tile is still visible")
    if "LocalChatGPT" in index or "Local ChatGPT" in index:
        raise AssertionError("home page still exposes the incorrect ChatGPT naming")

    require(
        "src/LocalGPT/Components/Layout/NavMenu.razor",
        '@T("Nav.Chat", "Local Chat")',
        'NavigateUrl="/council-teams"',
    )
    nav = read("src/LocalGPT/Components/Layout/NavMenu.razor")
    if 'NavigateUrl="/model-council"' in nav:
        raise AssertionError("obsolete AI Council main-menu entry is still visible")

    require(
        "src/LocalGPT/Components/Pages/Chat.razor",
        'Home.LocalChat.Title',
        'Home.LocalChat.SetupHelp',
    )
    chat = read("src/LocalGPT/Components/Pages/Chat.razor")
    if "LocalChatGPT" in chat or "Local ChatGPT" in chat:
        raise AssertionError("chat page still exposes the incorrect ChatGPT naming")

    # The old page is intentionally retained as a direct test route.
    require("src/LocalGPT/Components/Pages/ModelCouncil.razor", '@page "/model-council"')

    # Carry the user's authoritative Windows compile repair and remove the nullable warning seen there.
    require("src/LocalGPT/Services/CouncilTextService.cs", "using System.Text.RegularExpressions;")
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.cs",
        "if (request is { SaveToMemory: true } failedRequest)",
        "SaveToMemoryAsync(failedRequest, failedResult, null, CancellationToken.None)",
    )

    cultures = ["de-DE", "en-US", "es-ES", "fr-FR", "ja-JP", "uk-UA"]
    catalogs = {}
    obsolete = {
        "Phrase.Local␠ChatGPT",
        "Phrase.Welcome␠to␠your␠LocalChatGPT",
        "Text.Local␠ChatGPT",
        "Text.Welcome␠to␠your␠LocalChatGPT",
        "Text.Welcome␠to␠your␠LocalChatGPT,␠go␠to␠the␠Setup␠Page␠to␠Setup␠the␠AI␠Chat␠Clients",
    }
    required = {
        "Home.LocalChat.Title",
        "Home.LocalChat.Welcome",
        "Home.LocalChat.SetupHelp",
        "Phrase.Local␠Chat",
        "Text.Local␠Chat",
        "Phrase.Welcome␠to␠your␠Local␠Chat",
        "Text.Welcome␠to␠your␠Local␠Chat",
        "Text.Welcome␠to␠your␠Local␠Chat,␠go␠to␠the␠Setup␠Page␠to␠Setup␠the␠AI␠Chat␠Clients",
        "Nav.Chat",
    }
    for culture in cultures:
        rel = f"src/LocalGPT/Localization/{culture}.json"
        catalog = json.loads(read(rel))
        catalogs[culture] = catalog
        missing = sorted(required - catalog.keys())
        if missing:
            raise AssertionError(f"{culture} missing Local Chat localization keys: {missing}")
        present_obsolete = sorted(obsolete & catalog.keys())
        if present_obsolete:
            raise AssertionError(f"{culture} still contains obsolete LocalChatGPT localization keys: {present_obsolete}")
        for key in required:
            value = str(catalog[key])
            if not value.strip():
                raise AssertionError(f"{culture} has blank Local Chat localization value: {key}")
            if "ChatGPT" in value:
                raise AssertionError(f"{culture} incorrectly renames Local Chat to ChatGPT at {key}: {value}")

    baseline_keys = set(catalogs["en-US"])
    for culture in cultures[1:]:
        if set(catalogs[culture]) != baseline_keys:
            raise AssertionError(f"localization key parity differs for {culture}")

    modes = []
    for path in (root / "src/LocalGPT").rglob("*.razor"):
        for line in path.read_text(encoding="utf-8").splitlines():
            if "@rendermode" in line:
                modes.append((str(path.relative_to(root)), line.strip()))
    if len(modes) != 19:
        raise AssertionError(f"expected 19 LocalGPT rendermode directives, found {len(modes)}")

    print("LocalGPT 2.8.7 Local Chat naming and legacy Council navigation source audit passed.")
except (AssertionError, OSError, json.JSONDecodeError) as exc:
    print(f"LocalGPT 2.8.7 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
