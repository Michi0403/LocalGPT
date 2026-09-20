#!/usr/bin/env python3
from pathlib import Path
import hashlib
import sys

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.5.6"


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing {rel}")
    return path.read_text(encoding="utf-8")


def req(rel: str, marker: str) -> None:
    if marker not in read(rel):
        raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} missing {marker!r}")


def forbid(rel: str, marker: str) -> None:
    if marker in read(rel):
        raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} still contains forbidden {marker!r}")


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

    req("RELEASE.md", "# LocalGPT 4.5.6")
    req("VALIDATION.md", "# LocalGPT 4.5.6 source validation")
    req("CHANGELOG-v4.5.6-UI-READABILITY-FULLSCREEN-TOURNAMENT-RESILIENCE.md", "# LocalGPT 4.5.6")
    req("VALIDATION-v4.5.6-source.md", "# LocalGPT 4.5.6 source validation")
    req("src/LocalGPT/Components/App.razor", "localgpt-game-console.js?v=4.5.6")
    req("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=4.5.6")
    req("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.5.6")
    req("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.5.6")
    req("docs/pdf-cover.html", "LocalGPT 4.5.6 Documentation")
    req("docs/index.md", "**Version 4.5.6**")
    req("docs/pdf/toc.yml", "LocalGPT-4.5.6.pdf")
    req("docs/reference/ai-provider-installation.md", "LocalGPT/4.5.6")

    # Compile-surface repairs from the supplied Windows 4.5.3 build diagnostics.
    req("src/LocalGPT/Components/Shared/LocalPathExplorer.razor", '<BodyContentTemplate Context="popupBodyContext">')
    req("src/LocalGPT/Components/Pages/Chat.razor", '<MessageContentTemplate Context="messageContext">')
    req("src/LocalGPT/Components/Pages/Chat.razor", "ResolveLiveCouncilMessage(messageContext.Content)")
    forbid("src/LocalGPT/Components/Pages/Chat.razor", "ResolveLiveCouncilMessage(context.Content)")

    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", 'ValueChanged="@((int value) => OnNumberChangedAsync(value))"')
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", 'title="@($"Adjust {Label} with a slider")"')
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", 'aria-label="@($"Adjust {Label} with a slider")"')
    forbid("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", 'title="Adjust @Label with a slider"')
    forbid("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", 'aria-label="Adjust @Label with a slider"')

    dx_catalog = read("src/LocalGPT/Components/Pages/DxFunctionCatalog.razor")
    if dx_catalog.count('@bind-Checked:after="() => MarkGridEntryDirty(entry)"') != 5:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: DxFunctionCatalog dirty-tracking checkbox count changed")
    if '/> MarkGridEntryDirty(entry)"' in dx_catalog or 'MarkGridEntryDirty(entry)" @onclick' in dx_catalog:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: malformed DxFunctionCatalog conversion fragment remains")

    req("src/LocalGPT/Components/Pages/TestLab.razor", 'CheckedChanged="@((bool selected) => ToggleLearnBaseExtension(extension, selected))"')
    req("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor", 'CheckedChanged="@((bool value) => SetEnabled(route, value))"')
    req("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor", 'ValueChanged="@((OneWireHardwareKind value) => SetHardwareKind(route, value))"')
    req("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor", 'ValueChanged="@((int? value) => SetNullableInt(route, nameof(route.OllamaNumGpu), value))"')
    req("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor", 'ValueChanged="@((RangeSelectorValueChangedEventArgs args) => SetLoadOverride(route, args))"')
    req("src/LocalGPT/Components/Shared/ProviderModelBenchmarkCouncilPanel.razor", 'CheckedChanged="@((bool selected) => ToggleReviewer(reviewer.SelectionKey, selected))"')
    req("src/LocalGPT/Components/Shared/ProviderModelBenchmarkEvidenceHistory.razor", 'ValueChanged="@((string value) => OnStoredRunChangedAsync(value))"')
    req("src/LocalGPT/Components/Shared/ProviderModelBenchmarkEvidenceHistory.razor", 'ValueChanged="@((string value) => OnTargetChanged(value))"')
    req("src/LocalGPT/Components/Shared/ProviderModelPanel.razor", 'CheckedChanged="@((bool value) => OnSelectionChangedAsync(value))"')
    req("src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor", 'CheckedChanged="@((bool value) => ToggleModel(model.SelectionKey, value))"')
    req("src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor", 'CheckedChanged="@((bool value) => TogglePreferredModel(model.SelectionKey, value))"')
    req("src/LocalGPT/Components/Pages/ModelCouncil.razor", 'CheckedChanged="@((bool value) => ToggleArtifactGeneration(value))"')
    req("src/LocalGPT/Components/Pages/ModelCouncil.razor", 'ValueChanged="@((string value) => SelectProjectAsync(value))"')
    req("src/LocalGPT/Components/Pages/ModelCouncil.razor", 'ValueChanged="@((string value) => SelectProjectTopic(value))"')

    # XML documentation warnings reported by the successful Windows 4.5.4 compilation.
    req("src/LocalGPT/Components/Pages/ModelCouncil.razor.cs", '<param name="selectedValue">Selected project identifier')
    req("src/LocalGPT/Components/Pages/ModelCouncil.razor.cs", '<param name="selectedValue">Selected project-topic identifier')
    req("src/LocalGPT/Components/Pages/ModelCouncil.razor.cs", '<param name="enabled">Whether implementation-artifact generation')
    forbid("src/LocalGPT/Components/Pages/ModelCouncil.razor.cs", '<param name="args">Args value supplied to the model council operation')
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", '<param name="value">New bounded numeric value')
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", '<param name="args">Args value supplied to the bounded number editor operation')
    req("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor", '<param name="kind">Hardware kind selected for the route.</param>')
    if read("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor").count('<param name="value">') < 5:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: CouncilHardwareRoadEditor value parameter documentation incomplete")
    if read("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor").count('<param name="args">') != 1:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: CouncilHardwareRoadEditor should retain exactly one range-selector args parameter tag")
    req("src/LocalGPT/Services/MultiModelCouncilService.cs", '<param name="gameSessions">Council game-session service')

    # 4.5.6 UI/readability and tournament resilience.
    req("src/LocalGPT/Components/Shared/ConfigurationWorkbenchNav.razor.css", "grid-template-rows: auto auto;")
    forbid("src/LocalGPT/Components/Pages/CouncilTeams.razor", "form-check council-runtime-class-option")
    req("src/LocalGPT/Components/Pages/CouncilTeams.razor", "council-runtime-class-checkbox")
    req("src/LocalGPT/Components/Pages/CouncilTeams.razor", "council-runtime-field-list")
    forbid("src/LocalGPT/Components/Pages/CouncilTeams.razor", "table-responsive")
    req("src/LocalGPT/wwwroot/css/site.css", ".council-team-fields{display:grid;grid-template-columns:minmax(0,1fr)")
    req("src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor.css", "max-height: none; overflow: visible;")
    req("src/LocalGPT/wwwroot/css/site.css", "dxbl-popup-root.localgpt-game-fullscreen-host:fullscreen")
    req("src/LocalGPT/wwwroot/js/localgpt-game-console.js", "setFullscreenPopupPresentation")
    req("src/LocalGPT/wwwroot/js/localgpt-chat-ui.js", ".localgpt-live-participant-board")
    forbid("src/LocalGPT/wwwroot/js/localgpt-chat-ui.js", "host.querySelectorAll('button').length <= 16")
    req("src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs", "bool forceEnabled = false")
    req("src/LocalGPT/Services/ProviderStreamRepetitionWatchdog.cs", "enabled = forceEnabled || catalog.ProviderStreamRepetitionWatchdogEnabled;")
    req("src/LocalGPT/Services/MultiModelCouncilService.ParticipantExecution.cs", "ex is not ProviderStreamRepetitionException")
    req("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs", 'Role = "ASCII Team Artist"')
    req("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs", 'Key = "team-building-ascii"')
    req("src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs", "ReadAsciiFrames")
    req("src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs", "LocalGPT will not resurrect or mutate the ended deterministic session")
    req("src/LocalGPT/BusinessObjects/CouncilGameModels.cs", "PresentationFrames")
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", "ASCII Team Artist reveal complete")

    # Previous repairs and critical topology must remain intact.
    req("build/Assert-DevExpressBlazorControls.ps1", "-replace '\\\\', '/'")
    req("build/Assert-DevExpressBlazorControls.ps1", "only the two circuit-independent App.razor reconnect controls are native")
    req("src/LocalGPT/Services/CouncilTeamConfigurationService.cs", "private const int CurrentSeedVersion = 34;")
    req("src/LocalGPT/Services/CouncilRuntimeClassService.cs", "private const int CurrentSeedVersion = 7;")
    req("src/LocalGPT/Components/Pages/Chat.razor", '@key="gameConsoleRenderKey"')
    req("src/LocalGPT/Components/Pages/Chat.razor", 'GameAvailabilityChanged="OnAsciiGameAvailabilityChangedAsync"')
    req("src/LocalGPT/Components/Pages/Chat.razor.cs", '"GAME READY · Open ASCII terminal"')
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "GamePreparationStatus")
    req("src/LocalGPT/wwwroot/js/localgpt-game-console.js", "function fullscreenHost(element)")
    req("src/LocalGPT/wwwroot/js/localgpt-game-console.js", "followTail(id)")

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
        f"LocalGPT {VERSION} release audit passed: release identity, UI/fullscreen/tournament resilience contracts, prior compiler repairs, "
        f"{len(direct)} direct + {len(no_prerender)} non-prerender InteractiveServer declarations, "
        "seed versions, tournament presentation contracts and JavaScript diagnostics manifest are consistent."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
