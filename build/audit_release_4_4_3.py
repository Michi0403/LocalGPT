#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.4.3"


def fail(message: str) -> None:
    raise SystemExit(f"LocalGPT {VERSION} audit failed: {message}")


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing file: {relative}")
    return path.read_text(encoding="utf-8")


def require(text: str, marker: str, label: str) -> None:
    if marker not in text:
        fail(f"{label}: missing {marker!r}")


def forbid(text: str, marker: str, label: str) -> None:
    if marker in text:
        fail(f"{label}: forbidden marker remains: {marker!r}")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    # Version policy: no two-digit minor or patch segment. 4.4.3 is the maintained next line.
    parts = tuple(int(part) for part in VERSION.split("."))
    if len(parts) != 3 or parts[1] >= 10 or parts[2] >= 10:
        fail(f"version violates single-digit minor/patch policy: {VERSION}")

    for relative in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        require(read(relative), f"<Version>{VERSION}</Version>", f"{relative} version")

    release_markers = {
        "RELEASE.md": f"# LocalGPT {VERSION}",
        "VALIDATION.md": f"# LocalGPT {VERSION} source validation",
        "CHANGELOG-v4.4.3-ASCII-SURFACE-PKG-DOCS-REPAIR.md": f"# LocalGPT {VERSION}",
        "VALIDATION-v4.4.3-source.md": f"# LocalGPT {VERSION} source validation",
        "docs/index.md": f"**Version {VERSION}**",
        "docs/docfx.json": f'"localgptVersion": "{VERSION}"',
        "docs/pdf/toc.yml": "LocalGPT-4.4.3.pdf",
        "src/LocalGPT/Components/App.razor": "localgpt-game-console.js?v=4.4.3",
    }
    for relative, marker in release_markers.items():
        require(read(relative), marker, relative)

    # Preserve the known-good 4.4.2 shell/menu exactly.
    expected_hashes = {
        "src/LocalGPT/Components/Layout/MainLayout.razor": "bb30368daa8bb8e9b3217ac96ae66a25c5cf40003655e201433fc475269e0f6b",
        "src/LocalGPT/Components/Layout/Drawer.razor": "bde3ed44e84aabcfc8272bafa87e647e46d1097d2038973ddb1bc5a0394ca8a2",
    }
    for relative, expected in expected_hashes.items():
        actual = sha256(ROOT / relative)
        if actual != expected:
            fail(f"{relative} changed from the supplied 4.4.2 menu-restoration source: {actual}")

    # Keep the maintained InteractiveServer routing model.
    missing_render_modes: list[str] = []
    pages = ROOT / "src/LocalGPT/Components/Pages"
    for page in pages.rglob("*.razor"):
        text = page.read_text(encoding="utf-8", errors="ignore")
        if "@page " in text and page.name != "Error.razor" and "@rendermode InteractiveServer" not in text:
            missing_render_modes.append(str(page.relative_to(ROOT)))
    if missing_render_modes:
        fail(f"InteractiveServer render mode missing from {missing_render_modes[0]}")

    chat = read("src/LocalGPT/Components/Pages/Chat.razor")
    chat_code = read("src/LocalGPT/Components/Pages/Chat.razor.cs")
    lifecycle = read("src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs")
    provider = read("src/LocalGPT/Components/Pages/Chat.ProviderRuntime.razor.cs")
    chat_css = read("src/LocalGPT/Components/Pages/Chat.razor.css")
    ascii_text = read("src/LocalGPT/Services/AsciiChatTextService.cs")
    hotseat = read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.AsciiHotSeatTemplate.cs")

    require(chat, "@rendermode InteractiveServer", "Chat render mode")
    require(chat, "<DxPopup Visible=\"@showGameConsole\"", "ASCII DevExpress popup")
    require(chat, "VisibleChanged=\"OnGameConsoleVisibilityChangedAsync\"", "ASCII popup state bridge")
    require(chat, "CloseRequested=\"CloseGameConsole\"", "ASCII popup close lifetime")
    forbid(chat, "chat-game-ribbon", "legacy ASCII chat grid row")
    forbid(chat, "chat-game-visible", "legacy ASCII chat grid visibility class")
    require(chat_css, ".chat-game-popup-body", "ASCII popup responsive body")
    require(chat_css, "#localgpt-chat-host", "DXAIChat minimum-height protection")
    forbid(chat_css, ".main-container.chat-game-visible", "legacy chat grid coupling")

    require(ascii_text, 'RequiredSurfaceCapability = "localgpt.ascii.surface.required"', "persisted ASCII surface capability")
    require(ascii_text, "RequiresAsciiSurface", "ASCII surface team classification")
    require(hotseat, "AsciiChatTextService.RequiredSurfaceCapability", "hot-seat required-surface capability")
    if hotseat.count('"localgpt.ascii.surface.get"') < 3:
        fail("hot-seat blueprint does not expose/recheck the ASCII surface in all required phases")
    require(hotseat, "If the shared ASCII surface reports CLOSED, do not mutate the display", "Display Director hidden-surface guard")
    require(chat_code, "EnsureRequiredAsciiSurfaceState()", "required ASCII surface state publication")
    require(chat_code, "EnsureRequiredAsciiSurfaceForSelectedCouncilTeamAsync()", "required ASCII surface renderer refresh")
    require(lifecycle, "await EnsureRequiredAsciiSurfaceForSelectedCouncilTeamAsync().ConfigureAwait(true)", "direct Council starter surface opening")
    require(provider, "EnsureRequiredAsciiSurfaceState();", "manual Council request surface opening")

    console = read("src/LocalGPT/Components/Shared/ChatGameConsole.razor")
    for marker in ("<DxButton ", "<DxComboBox ", "<DxTextBox ", "<DxMemo ", "BindValueMode=\"BindValueMode.OnInput\"", "SubmitFormOnClick=\"true\""):
        require(console, marker, "DevExpress ASCII console controls")
    for raw in ("<select", "<input", "<textarea"):
        forbid(console, raw, "native ordinary ASCII console control")
    native_buttons = re.findall(r"<button\b[^>]*>", console, flags=re.IGNORECASE)
    if not native_buttons:
        fail("low-level game button DOM contract disappeared")
    for button in native_buttons:
        if "data-game-action=" not in button:
            fail(f"ordinary native button remains in ChatGameConsole: {button[:120]}")

    packaging = read("build/NativeReleasePackaging.ps1")
    for marker in (
        '<title>$escapedDistributionTitle</title>',
        "'--distribution', $distributionPath",
        "'--package-path', ([IO.Path]::GetDirectoryName($componentPackage))",
        "SelectSingleNode('/installer-gui-script/title')",
        "Validated macOS Installer package title '$distributionTitle'.",
    ):
        require(packaging, marker, "macOS Installer identity validation")

    # Mermaid fix must be identical in authored + embedded help assets and all generated HTML cache refs.
    mermaid_relatives = (
        "docs/templates/localgpt/public/main.js",
        "src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.js",
        "src/LocalGPT/wwwroot/help-docs/public/main.js",
    )
    mermaid_texts = [read(relative) for relative in mermaid_relatives]
    if len(set(mermaid_texts)) != 1:
        fail("authored and embedded Mermaid JavaScript copies are not identical")
    mermaid_js = mermaid_texts[0]
    require(mermaid_js, "await document.fonts.ready", "Mermaid font-metric readiness")
    require(mermaid_js, "htmlLabels: false", "Mermaid SVG label mode")
    require(mermaid_js, "wrappingWidth: 180", "Mermaid bounded node-label wrapping")
    js_hash = hashlib.sha256(mermaid_js.encode("utf-8")).hexdigest()
    cache_key = js_hash[:12]
    help_root = ROOT / "src/LocalGPT/wwwroot/help-docs"
    html_files = list(help_root.rglob("*.html"))
    if not html_files:
        fail("generated embedded help HTML is missing")
    stale_refs = []
    new_ref_count = 0
    for html in html_files:
        text = html.read_text(encoding="utf-8", errors="ignore")
        if "localgpt-kawaii.js?v=" in text:
            if f"localgpt-kawaii.js?v={cache_key}" not in text:
                stale_refs.append(str(html.relative_to(ROOT)))
            else:
                new_ref_count += 1
    if stale_refs:
        fail(f"generated help Mermaid cache key is stale in {stale_refs[0]}")
    if new_ref_count == 0:
        fail("generated help contains no synchronized Mermaid cache-key references")

    pages_zip = ROOT / ".github/pages/localgpt-kawaii-docs.zip"
    with zipfile.ZipFile(pages_zip) as archive:
        bad = archive.testzip()
        if bad is not None:
            fail(f"tracked Pages snapshot is corrupt at {bad}")
        for js_name in ("styles/localgpt-kawaii.js", "public/main.js"):
            if archive.read(js_name).decode("utf-8") != mermaid_js:
                fail(f"tracked Pages snapshot {js_name} is not synchronized")
        page_html = [name for name in archive.namelist() if name.endswith(".html")]
        if not page_html:
            fail("tracked Pages snapshot contains no HTML")
        if not any(f"localgpt-kawaii.js?v={cache_key}" in archive.read(name).decode("utf-8", errors="ignore") for name in page_html):
            fail("tracked Pages snapshot does not contain the new Mermaid cache key")
        for name in page_html:
            text = archive.read(name).decode("utf-8", errors="ignore")
            if "localgpt-kawaii.js?v=" in text and f"localgpt-kawaii.js?v={cache_key}" not in text:
                fail(f"tracked Pages snapshot has a stale Mermaid cache key in {name}")

    # Current release-bearing files must not still identify themselves as 4.4.2.
    current_metadata = (
        "docs/index.md",
        "docs/docfx.json",
        "docs/pdf-cover.html",
        "docs/pdf/toc.yml",
        "docs/reference/ai-provider-installation.md",
        "src/LocalGPT/Components/App.razor",
        "src/LocalGPT/Services/InitialSetupAssistantService.cs",
        "src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs",
    )
    for relative in current_metadata:
        forbid(read(relative), "4.4.2", f"stale current-version metadata in {relative}")

    print(
        f"LocalGPT {VERSION} release audit passed: required ASCII surface/popup lifetime, DevExpress console controls, "
        "macOS Installer title, Mermaid documentation layout/cache, InteractiveServer routes and the restored 4.4.2 shell are preserved."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
