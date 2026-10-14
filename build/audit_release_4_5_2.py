#!/usr/bin/env python3
from pathlib import Path
import hashlib
import sys

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.5.2"


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing {rel}")
    return path.read_text(encoding="utf-8")


def req(rel: str, marker: str) -> None:
    if marker not in read(rel):
        raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} missing {marker!r}")


def render_modes() -> list[tuple[str, str]]:
    values: list[tuple[str, str]] = []
    root = ROOT / "src/LocalGPT/Components"
    for path in root.rglob("*.razor"):
        for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
            if "@rendermode" in line:
                values.append((str(path.relative_to(ROOT)), line.strip()))
    return sorted(values)


def main() -> int:
    parts = [int(value) for value in VERSION.split(".")]
    if len(parts) != 3 or parts[1] >= 10 or parts[2] >= 10:
        raise SystemExit("version-slot policy failed")

    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ]:
        req(rel, f"<Version>{VERSION}</Version>")

    req("RELEASE.md", "# LocalGPT 4.5.2")
    req("VALIDATION.md", "# LocalGPT 4.5.2 source validation")
    req("CHANGELOG-v4.5.2-DEVEXPRESS-ASCII-SURFACE-REPAIR.md", "# LocalGPT 4.5.2")
    req("VALIDATION-v4.5.2-source.md", "# LocalGPT 4.5.2 source validation")
    req("src/LocalGPT/Components/App.razor", "localgpt-game-console.js?v=4.5.2")
    req("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=4.5.2")
    req("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.5.2")
    req("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.5.2")
    req("docs/pdf-cover.html", "LocalGPT 4.5.2 Documentation")
    req("docs/index.md", "**Version 4.5.2**")
    req("docs/pdf/toc.yml", "LocalGPT-4.5.2.pdf")
    req("docs/reference/ai-provider-installation.md", "LocalGPT/4.5.2")

    req("src/LocalGPT/Services/CouncilTeamConfigurationService.cs", "private const int CurrentSeedVersion = 33;")
    req("src/LocalGPT/Services/CouncilRuntimeClassService.cs", "private const int CurrentSeedVersion = 7;")

    # Interrupted 4.5.2 checkpoint completion markers.
    req("src/LocalGPT/Components/Pages/Chat.razor", '@key="gameConsoleRenderKey"')
    req("src/LocalGPT/Components/Pages/Chat.razor", 'GameAvailabilityChanged="OnAsciiGameAvailabilityChangedAsync"')
    req("src/LocalGPT/Components/Pages/Chat.razor.cs", '"GAME READY · Open ASCII terminal"')
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "GamePreparationStatus")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor.css", ".chat-game-console.localgpt-game-fullscreen")
    req("src/LocalGPT/wwwroot/js/localgpt-game-console.js", "function fullscreenHost(element)")
    req("src/LocalGPT/wwwroot/js/localgpt-game-console.js", "async exitFullscreen(id)")
    req("src/LocalGPT/wwwroot/js/localgpt-game-console.js", "followTail(id)")
    req("build/audit_devexpress_blazor_controls.py", "DevExpress Blazor control audit passed")
    req("build/Assert-DevExpressBlazorControls.ps1", "only the two circuit-independent App.razor reconnect controls are native")
    req("Directory.Build.targets", 'Name="AssertLocalGptDevExpressBlazorControls"')

    expected_direct = {
        "src/LocalGPT/Components/Layout/NavMenu.razor",
        "src/LocalGPT/Components/Pages/Index.razor",
        "src/LocalGPT/Components/Pages/ModelCouncil.razor",
        "src/LocalGPT/Components/Pages/Install.razor",
        "src/LocalGPT/Components/Pages/Database.razor",
        "src/LocalGPT/Components/Pages/OneWireSecurity.razor",
        "src/LocalGPT/Components/Pages/Help.razor",
        "src/LocalGPT/Components/Pages/TestLab.razor",
        "src/LocalGPT/Components/Pages/Chat.razor",
        "src/LocalGPT/Components/Pages/CouncilTeams.razor",
        "src/LocalGPT/Components/Pages/Projects.razor",
        "src/LocalGPT/Components/Pages/RemoteControl.razor",
        "src/LocalGPT/Components/Pages/DxFunctionCatalog.razor",
        "src/LocalGPT/Components/Pages/ProjectMaintenance.razor",
        "src/LocalGPT/Components/Pages/MinecraftModBuilder.razor",
    }
    expected_no_prerender = {
        "src/LocalGPT/Components/InteractiveStartupMarker.razor",
        "src/LocalGPT/Components/Layout/MenuIsland.razor",
        "src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor",
        "src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor",
        "src/LocalGPT/Components/Layout/ToastWrapper.razor",
    }
    actual = render_modes()
    direct = {path for path, mode in actual if mode == "@rendermode InteractiveServer"}
    no_prerender = {
        path
        for path, mode in actual
        if mode == "@rendermode @(new InteractiveServerRenderMode(prerender: false))"
    }
    unexpected = [entry for entry in actual if entry[0] not in expected_direct | expected_no_prerender]
    if direct != expected_direct or no_prerender != expected_no_prerender or unexpected or len(actual) != 20:
        raise SystemExit(
            f"LocalGPT {VERSION} audit failed: render-mode baseline changed "
            f"(direct={len(direct)}, no-prerender={len(no_prerender)}, total={len(actual)}, unexpected={unexpected[:1]})"
        )

    js = read("src/LocalGPT/wwwroot/js/localgpt-game-console.js").replace("\r\n", "\n").replace("\r", "\n")
    digest = hashlib.sha256(js.encode()).hexdigest()
    if f"{digest}  src/LocalGPT/wwwroot/js/localgpt-game-console.js" not in read("build/javascript-diagnostics-files.sha256"):
        raise SystemExit(f"LocalGPT {VERSION} audit failed: JS diagnostics digest stale")

    print(
        f"LocalGPT {VERSION} release audit passed: single-digit version slots, release identity, "
        "ASCII reopen/fullscreen/follow-tail/game-ready/tournament markers, DevExpress guard, "
        f"{len(direct)} direct + {len(no_prerender)} non-prerender InteractiveServer declarations, "
        "seed versions and JavaScript diagnostics manifest are consistent."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
