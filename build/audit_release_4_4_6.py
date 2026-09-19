#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.4.6"


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
        "CHANGELOG-v4.4.6-ASCII-INTEGRATION-REPAIR.md": f"# LocalGPT {VERSION}",
        "VALIDATION-v4.4.6-source.md": f"# LocalGPT {VERSION} source validation",
        "docs/index.md": f"**Version {VERSION}**",
        "docs/docfx.json": f'"localgptVersion": "{VERSION}"',
        "docs/pdf/toc.yml": "LocalGPT-4.4.6.pdf",
        "src/LocalGPT/Components/App.razor": "localgpt-game-console.js?v=4.4.6",
    }
    for relative, marker in release_markers.items():
        require(read(relative), marker, relative)

    expected_hashes = {
        "src/LocalGPT/Components/Layout/MainLayout.razor": "bb30368daa8bb8e9b3217ac96ae66a25c5cf40003655e201433fc475269e0f6b",
        "src/LocalGPT/Components/Layout/Drawer.razor": "bde3ed44e84aabcfc8272bafa87e647e46d1097d2038973ddb1bc5a0394ca8a2",
    }
    for relative, expected in expected_hashes.items():
        actual = sha256(ROOT / relative)
        if actual != expected:
            fail(f"{relative} changed from the supplied 4.4.2 menu-restoration source: {actual}")

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
    require(chat, '<DxPopup Visible="@showGameConsole"', 'ASCII DevExpress popup')
    require(chat, 'VisibleChanged="OnGameConsoleVisibilityChangedAsync"', 'ASCII popup state bridge')
    require(chat, 'CloseRequested="CloseGameConsole"', 'ASCII popup close lifetime')
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
    for marker in ('<DxButton ', '<DxComboBox ', '<DxTextBox ', '<DxMemo ', 'BindValueMode="BindValueMode.OnInput"', 'SubmitFormOnClick="true"'):
        require(console, marker, "DevExpress ASCII console controls")
    forbid(console, 'CssClass="ascii-console-button ascii-operator-toggle @(', "Razor mixed-content DevExpress CssClass regression")
    if console.count('CssClass="@("ascii-console-button ascii-operator-toggle" +') != 3:
        fail("the three Chat/Game/Operator mode buttons must bind CssClass as pure Razor expressions")
    for raw in ("<select", "<input", "<textarea"):
        forbid(console, raw, "native ordinary ASCII console control")
    native_buttons = re.findall(r"<button\b[^>]*>", console, flags=re.IGNORECASE)
    if not native_buttons:
        fail("low-level game button DOM contract disappeared")
    for button in native_buttons:
        if "data-game-action=" not in button:
            fail(f"ordinary native button remains in ChatGameConsole: {button[:120]}")

    campaign_models = read("src/LocalGPT/BusinessObjects/CouncilGameModels.cs")
    campaign_service = read("src/LocalGPT/Services/CouncilGameSessionService.Campaign.cs")
    campaign_combat = read("src/LocalGPT/Services/CouncilGameSessionService.Combat.cs")
    campaign_rendering = read("src/LocalGPT/Services/CouncilGameSessionService.Rendering.cs")
    runtime_classes = read("src/LocalGPT/Services/CouncilRuntimeClassService.cs")
    doom_team = read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs")
    dx_game_functions = read("src/LocalGPT/Services/CouncilGameDxAiFunctions.cs")
    for marker_value in (
        "public sealed class CouncilGameLevelProfile",
        "CampaignRuntimeClassKey",
        "CurrentLevelProfile",
        "AutoAdvanceLevels",
    ):
        require(campaign_models, marker_value, "ASCII DOOM campaign models")
    require(runtime_classes, 'BuildDefinition("games.ascii.doom.campaign"', "resettable ASCII DOOM campaign runtime class")
    require(runtime_classes, "private const int CurrentSeedVersion = 5;", "runtime-class seed version")
    require(runtime_classes, "CreateAsciiDoomStarterLevels()", "visible ten-level campaign starter seed")
    for level in range(1, 11):
        require(runtime_classes, f"Level = {level}, Name = \"Corridor {level:02d}\", Difficulty = {level}", f"starter campaign level {level}")
    forbid(campaign_models + campaign_service + campaign_combat, "CouncilGameCampaignDefaults", "hidden engine campaign defaults")
    require(campaign_service, "scopeFactory.CreateScope()", "scoped team/runtime-class campaign resolution")
    require(campaign_service, "ICouncilTeamConfigurationService", "team-assigned campaign resolution")
    require(campaign_service, ".OrderBy(assigned => assigned.IsSystemSeed ? 1 : 0)", "copied campaign precedence")
    require(campaign_service, "must contain at least one level profile", "invalid campaign rejection")
    for marker_value in (
        "var width = level.MapWidth;",
        "var height = level.MapHeight;",
        "level.RoomCount * level.EnemyDensity",
        "level.EnemyHealthMultiplier",
        "level.EnemyDamageMultiplier",
        "session.CurrentLevelIndex++",
    ):
        require(campaign_combat, marker_value, "configured ASCII DOOM campaign engine")
    require(campaign_rendering, "DIFF {levelProfile.Difficulty}/10", "campaign HUD difficulty")
    require(dx_game_functions, '"campaignRuntimeClassKey"', "campaign runtime-class DX start parameter")
    require(dx_game_functions, '"startingLevel"', "campaign starting-level DX parameter")
    require(dx_game_functions, '"autoAdvanceLevels"', "campaign auto-advance DX parameter")
    if doom_team.count("HumanParticipationMode = HumanParticipationMode.Optional") < 3:
        fail("ASCII DOOM Council roles do not all allow optional human participation")
    require(doom_team, 'never interpret an absent Council-role response as "no human game input"', "Council/game human-input separation")
    require(doom_team, "Do not invent or reinterpret difficulty", "model-invented difficulty guard")

    require(console, "ChangeControlModeAsync", "runtime control-mode selector")
    require(console, "snapshot.AutoplayDelayMilliseconds).ConfigureAwait(true);", "renderer-affine control-mode continuation")
    require(console, 'localGptGameConsole.fullscreen", elementId).ConfigureAwait(true)', "renderer-affine fullscreen continuation")
    require(console, 'CloseRequested.InvokeAsync()).ConfigureAwait(true)', "renderer-affine close callback")
    require(chat_css, ".chat-game-popup-body ::deep .ascii-operator-stage", "popup stage grid stretch")
    require(chat_css, "scrollbar-gutter: auto;", "popup scrollbar-gutter cleanup")

    session_interface = read("src/LocalGPT/Interfaces/ICouncilGameSessionService.cs")
    session_service = read("src/LocalGPT/Services/CouncilGameSessionService.cs")
    bootstrap_service = read("src/LocalGPT/Services/CouncilGameWorkflowBootstrapService.cs")
    team_config = read("src/LocalGPT/Services/CouncilTeamConfigurationService.cs")
    council_chat = read("src/LocalGPT/Services/CouncilChatClient.cs")
    registration = read("src/LocalGPT/Program.ServiceRegistration.cs")
    game_js = read("src/LocalGPT/wwwroot/js/localgpt-game-console.js")
    require(session_interface, "GetActiveForCouncilRunAsync", "Council-run game lookup interface")
    require(session_service, "GetActiveForCouncilRunAsync", "Council-run game lookup service")
    require(dx_game_functions, "ResolveSessionSnapshotAsync", "correlated game function resolver")
    require(dx_game_functions, "GetActiveForCouncilRunAsync(activeCouncilRunId", "DXFunction Council-run fallback")
    require(bootstrap_service, "AsciiChatTextService.RequiredSurfaceCapability", "configuration-driven ASCII bootstrap gate")
    require(bootstrap_service, 'StartsWith("games.ascii.doom."', "DOOM runtime-class launcher family")
    require(bootstrap_service, 'StartsWith("games.green-dragon."', "Green Dragon runtime-class launcher family")
    require(bootstrap_service, "CouncilRunId = request.RunId", "prebootstrap Council ownership")
    require(bootstrap_service, "AutoplayEnabled = false", "prebootstrap non-autonomous default")
    require(council_chat, "gameBootstrap.EnsureSessionAsync(request, cancellationToken).ConfigureAwait(false)", "Council execution game prebootstrap boundary")
    require(registration, "AddScoped<CouncilGameWorkflowBootstrapService>()", "Council game bootstrap DI registration")
    require(team_config, "private const int CurrentSeedVersion = 30;", "Council system seed refresh")
    require(doom_team, "Call localgpt.game.session.get first with no sessionId", "DOOM recover-before-start workflow")
    require(doom_team, "Only if no active game exists, call localgpt.game.session.start", "Green Dragon recover-before-start workflow")
    require(console, "GetActiveForCouncilRunAsync(councilRunId).ConfigureAwait(true)", "terminal Council-run attachment")
    require(console, "if (candidate is null || !MatchesCurrentGameContext(candidate))", "terminal unrelated-session rejection")
    for marker_value in (
        "function isInteractiveTarget(target)",
        "[role=\"combobox\"]",
        ".dxbl-combobox",
        "if (isInteractiveTarget(event.target)) return;",
    ):
        require(game_js, marker_value, "DevExpress terminal input routing")
    require(chat, 'Height="min(900px, 90vh)"', "bounded ASCII popup height")

    packaging = read("build/NativeReleasePackaging.ps1")
    for marker in (
        '<title>$escapedDistributionTitle</title>',
        "'--distribution', $distributionPath",
        "'--package-path', ([IO.Path]::GetDirectoryName($componentPackage))",
        "SelectSingleNode('/installer-gui-script/title')",
        "Validated macOS Installer package title '$distributionTitle'.",
    ):
        require(packaging, marker, "macOS Installer identity validation")

    js_relatives = (
        "docs/templates/localgpt/public/main.js",
        "src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.js",
        "src/LocalGPT/wwwroot/help-docs/public/main.js",
    )
    js_texts = [read(relative) for relative in js_relatives]
    if len(set(js_texts)) != 1:
        fail("authored and embedded documentation JavaScript copies are not identical")
    js_text = js_texts[0]
    require(js_text, "function createKawaiiShootingStar(sky)", "shooting-star creation helper")
    require(js_text, "function scheduleKawaiiShootingStars(sky)", "shooting-star scheduler")
    require(js_text, "scheduleKawaiiShootingStars(sky);", "shooting-star sky activation")
    require(js_text, "await document.fonts.ready", "Mermaid font-metric readiness")
    require(js_text, "htmlLabels: false", "Mermaid SVG label mode")
    require(js_text, "wrappingWidth: 180", "Mermaid bounded node-label wrapping")

    css_text = read("src/LocalGPT/wwwroot/help-docs/styles/localgpt-kawaii.css")
    require(css_text, ".localgpt-kawaii-shooting-star", "shooting-star CSS class")
    require(css_text, "@keyframes localgpt-kawaii-shooting-star", "shooting-star keyframes")

    js_cache_key = hashlib.sha256(js_text.encode("utf-8")).hexdigest()[:12]
    css_cache_key = hashlib.sha256(css_text.encode("utf-8")).hexdigest()[:12]
    help_root = ROOT / "src/LocalGPT/wwwroot/help-docs"
    html_files = list(help_root.rglob("*.html"))
    if not html_files:
        fail("generated embedded help HTML is missing")
    stale_js_refs = []
    stale_css_refs = []
    for html in html_files:
        text = html.read_text(encoding="utf-8", errors="ignore")
        if "localgpt-kawaii.js?v=" in text and f"localgpt-kawaii.js?v={js_cache_key}" not in text:
            stale_js_refs.append(str(html.relative_to(ROOT)))
        if "localgpt-kawaii.css?v=" in text and f"localgpt-kawaii.css?v={css_cache_key}" not in text:
            stale_css_refs.append(str(html.relative_to(ROOT)))
    if stale_js_refs:
        fail(f"generated help JavaScript cache key is stale in {stale_js_refs[0]}")
    if stale_css_refs:
        fail(f"generated help CSS cache key is stale in {stale_css_refs[0]}")

    pages_zip = ROOT / ".github/pages/localgpt-kawaii-docs.zip"
    with zipfile.ZipFile(pages_zip) as archive:
        bad = archive.testzip()
        if bad is not None:
            fail(f"tracked Pages snapshot is corrupt at {bad}")
        for name, expected in (
            ("styles/localgpt-kawaii.js", js_text),
            ("public/main.js", js_text),
            ("styles/localgpt-kawaii.css", css_text),
        ):
            if archive.read(name).decode("utf-8") != expected:
                fail(f"tracked Pages snapshot {name} is not synchronized")
        page_html = [name for name in archive.namelist() if name.endswith(".html")]
        if not page_html:
            fail("tracked Pages snapshot contains no HTML")
        for name in page_html:
            text = archive.read(name).decode("utf-8", errors="ignore")
            if "localgpt-kawaii.js?v=" in text and f"localgpt-kawaii.js?v={js_cache_key}" not in text:
                fail(f"tracked Pages snapshot has a stale JavaScript cache key in {name}")
            if "localgpt-kawaii.css?v=" in text and f"localgpt-kawaii.css?v={css_cache_key}" not in text:
                fail(f"tracked Pages snapshot has a stale CSS cache key in {name}")

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
        forbid(read(relative), "4.4.5", f"stale current-version metadata in {relative}")

    print(
        f"LocalGPT {VERSION} release audit passed: Council-run game ownership, configuration-driven ASCII game prebootstrap, DXFunction/session correlation, DevExpress interactive-target routing, popup sizing, configurable ten-level DOOM campaign, documentation assets, InteractiveServer routes and the restored 4.4.2 shell are preserved."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
