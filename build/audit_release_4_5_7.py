#!/usr/bin/env python3
from pathlib import Path
import hashlib
import sys

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.5.7"


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

    req("RELEASE.md", "# LocalGPT 4.5.7")
    req("VALIDATION.md", "# LocalGPT 4.5.7 source validation")
    req("CHANGELOG-v4.5.7-CHAT-APPROVALS-ASCII-COMBAT-COMPILER-ORCHESTRATION.md", "# LocalGPT 4.5.7")
    req("VALIDATION-v4.5.7-source.md", "# LocalGPT 4.5.7 source validation")
    req("src/LocalGPT/Components/App.razor", "localgpt-game-console.js?v=4.5.7")
    req("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=4.5.7")
    req("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.5.7")
    req("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.5.7")
    req("docs/pdf-cover.html", "LocalGPT 4.5.7 Documentation")
    req("docs/index.md", "**Version 4.5.7**")
    req("docs/pdf/toc.yml", "LocalGPT-4.5.7.pdf")
    req("docs/docfx.json", "LocalGPT 4.5.7 · generated documentation")
    req("docs/docfx.json", '"localgptVersion": "4.5.7"')
    req("docs/reference/ai-provider-installation.md", "LocalGPT/4.5.7")

    # The previous DevExpress/Razor compiler repairs and Windows reconnect guard stay intact.
    req("build/Assert-DevExpressBlazorControls.ps1", "-replace '\\\\', '/'")
    req("build/Assert-DevExpressBlazorControls.ps1", "only the two circuit-independent App.razor reconnect controls are native")
    req("src/LocalGPT/Components/Shared/LocalPathExplorer.razor", '<BodyContentTemplate Context="popupBodyContext">')
    req("src/LocalGPT/Components/Pages/Chat.razor", '<MessageContentTemplate Context="messageContext">')
    req("src/LocalGPT/Components/Pages/Chat.razor", "ResolveLiveCouncilMessage(messageContext.Content)")
    forbid("src/LocalGPT/Components/Pages/Chat.razor", "ResolveLiveCouncilMessage(context.Content)")

    # 4.5.7 selector/range-editor repair.
    req("src/LocalGPT/Components/Pages/Chat.razor", 'Data="@ProviderSessionOptions"')
    req("src/LocalGPT/Components/Pages/Chat.razor", 'ValueFieldName="@nameof(LocalGptSelectionOption<string>.Value)"')
    req("src/LocalGPT/Components/Pages/Chat.razor", 'TextFieldName="@nameof(LocalGptSelectionOption<string>.Label)"')
    req("src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs", "private async Task OnProviderSelectionChanged(string selectedName)")
    req("src/LocalGPT/Components/Pages/Chat.razor.cs", "IReadOnlyList<LocalGptSelectionOption<string>> ProviderSessionOptions")
    req("src/LocalGPT/wwwroot/css/site.css", "dxbl-adaptive-dropdown:not(.dxbl-display-none)")
    req("src/LocalGPT/wwwroot/css/site.css", "z-index: 22040 !important")
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", "<DxSpinEdit")
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", 'ValueChanged="@((int value) => OnNumberChangedAsync(value))"')
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", "bounded-number-hint")
    req("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor", "<DxSpinEdit")
    req("src/LocalGPT/Components/Shared/CouncilHardwareRoadEditor.razor", 'ValueChanged="@((int value) => SetLoadOverride(route, value))"')
    for rel in ["src/LocalGPT/Components", "src/LocalGPT/Services"]:
        root = ROOT / rel
        for path in root.rglob("*"):
            if path.is_file() and path.suffix in {".razor", ".cs"}:
                text = path.read_text(encoding="utf-8", errors="ignore")
                if "DxRangeSelector" in text or "RangeSelectorValueChangedEventArgs" in text:
                    raise SystemExit(f"LocalGPT {VERSION} audit failed: obsolete range selector remains in {path.relative_to(ROOT)}")

    # Inline Chat and ASCII collaboration requests.
    req("src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs", "IReadOnlyList<HumanCollaborationRequest> InlineChatRequests")
    req("src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs", "ResolveInlineChatRequestAsync")
    req("src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs", "QueueInlineApprovedExecution")
    req("src/LocalGPT/Components/Pages/Chat.razor", "AI requests for this session")
    req("src/LocalGPT/Components/Pages/Chat.razor", "request.SuggestedResponses")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "gameInteractionRequest.SuggestedResponses")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "gameInteractionRequest = pending.FirstOrDefault")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "COUNCIL WORK CONTROL")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "bounded non-game work console")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "snapshot is not null")

    # InteractiveServer renderer/cancellation resilience.
    req("src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs", "lastCouncilComposerAvailability")
    req("src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs", "catch (TaskCanceledException exception)")
    req("src/LocalGPT/Components/Pages/Chat.Lifecycle.razor.cs", "catch (JSDisconnectedException exception)")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "lastJsSequenceSignature")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "lastJsLayoutSignature")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", '"localGptGameConsole.setSequence"')
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "catch (TaskCanceledException exception)")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "catch (JSDisconnectedException exception)")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "ResetJavaScriptAttachmentState")
    req("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "250")

    # Kernel Tournament persistent actors, combat presentation and member failover.
    for marker in ["CreatureSpecies", "CreatureStyle", "CreatureForm", "CreatureTrait", "CreatureVoice"]:
        req("src/LocalGPT/BusinessObjects/CouncilGameModels.cs", marker)
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", "TrainerSprite")
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", "CreatureSprite")
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", '"CREATURES MOVE"')
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", '"ARENA CLASH"')
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", '"ENGINE IMPACT"')
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", '"RECOVERY BEAT"')
    req("src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs", "usedCreatureNames")
    req("src/LocalGPT/BusinessObjects/MultiModelCouncilModels.cs", "RecoveryTargetModelName")
    req("src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs", "recoveryStep.RecoveryTargetModelName = failedModel")
    req("src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs", "step.RecoveryTargetModelName")
    if read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs").count("CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool") < 5:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: tournament member-pool recovery not applied to maintained compact steps")
    req("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs", 'Role = "ASCII Team Artist"')
    req("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs", 'Key = "team-building-ascii"')

    # Compiler/repository curation and distributed release orchestration presets.
    teams = "src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BuildOrchestrationTemplates.cs"
    req(teams, 'Key = "program-compiler-team"')
    req(teams, 'DisplayName = "Program Compiler & Repository Curator"')
    req(teams, 'Key = "project-release-orchestration"')
    req(teams, 'DisplayName = "Project Maintenance & Cross-Platform Publisher"')
    req(teams, "RetrySameThenEligibleRolePool")
    req(teams, 'Append("human.collaboration.request")')
    req(teams, "SuggestedResponsesText")
    req(teams, "artifacts/<version>/<platform>/<runtime-or-package-kind>/")
    req(teams, "trusted LocalGPT peers")
    req(teams, "strongest eligible")
    req(teams, '"localgpt.public_service.invoke"')
    req("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs", "CreateProgramCompilerTeam()")
    req("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs", "CreateProjectReleaseOrchestrationTeam()")
    req("src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs", "details.Project.CurrentVersion")
    req("src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs", "Versions = details.Versions.Select")
    req("src/LocalGPT/Services/ProjectMaintenanceDxAiFunctions.cs", "Artifacts = details.Artifacts.Select")

    # Seed and runtime topology contracts.
    req("src/LocalGPT/Services/CouncilTeamConfigurationService.cs", "private const int CurrentSeedVersion = 35;")
    req("src/LocalGPT/Services/CouncilRuntimeClassService.cs", "private const int CurrentSeedVersion = 7;")
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
    no_prerender = {path for path, mode in actual if mode == "@rendermode @(new InteractiveServerRenderMode(prerender: false))"}
    unexpected = [entry for entry in actual if entry[0] not in expected_direct | expected_no_prerender]
    if direct != expected_direct or no_prerender != expected_no_prerender or unexpected or len(actual) != 20:
        raise SystemExit(
            f"LocalGPT {VERSION} audit failed: render-mode baseline changed "
            f"(direct={len(direct)}, no-prerender={len(no_prerender)}, total={len(actual)}, unexpected={unexpected[:1]})"
        )

    # Maintained browser code must agree with the diagnostics hash manifest.
    manifest = read("build/javascript-diagnostics-files.sha256")
    js_root = ROOT / "src/LocalGPT/wwwroot/js"
    checked = 0
    for line in manifest.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        digest, rel = line.split(maxsplit=1)
        path = ROOT / rel
        if not path.is_file():
            raise SystemExit(f"LocalGPT {VERSION} audit failed: JS diagnostics source missing: {rel}")
        normalized = path.read_text(encoding="utf-8").replace("\r\n", "\n").replace("\r", "\n")
        actual_digest = hashlib.sha256(normalized.encode()).hexdigest()
        if actual_digest != digest:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: JS diagnostics digest stale for {rel}")
        checked += 1
    if checked < 20:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: unexpectedly small JavaScript diagnostics manifest ({checked})")

    print(
        f"LocalGPT {VERSION} release audit passed: release identity, selector/range repair, inline review projection, "
        f"renderer cancellation resilience, ASCII combat/failover, compiler/publisher presets, "
        f"{len(direct)} direct + {len(no_prerender)} non-prerender InteractiveServer declarations and {checked} JS hashes are consistent."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
