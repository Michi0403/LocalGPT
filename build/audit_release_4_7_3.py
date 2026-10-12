#!/usr/bin/env python3
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.7.3"


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing {rel}")
    return path.read_text(encoding="utf-8", errors="strict")


def require(rel: str, *markers: str) -> None:
    text = read(rel)
    for marker in markers:
        if marker not in text:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} missing {marker!r}")


def forbid(rel: str, *markers: str) -> None:
    text = read(rel)
    for marker in markers:
        if marker in text:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} contains forbidden {marker!r}")


def block(text: str, start_marker: str, end_marker: str) -> str:
    start = text.find(start_marker)
    if start < 0:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing block start {start_marker!r}")
    end = text.find(end_marker, start + len(start_marker))
    if end < 0:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing block end {end_marker!r}")
    return text[start:end]


def main() -> int:
    parts = [int(part) for part in VERSION.split(".")]
    if len(parts) != 3 or parts[1] >= 10 or parts[2] >= 10:
        raise SystemExit("LocalGPT version-slot policy failed")

    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        ET.parse(ROOT / rel)
        require(rel, f"<Version>{VERSION}</Version>")

    require("RELEASE.md", f"# LocalGPT {VERSION}", "PublisherStudio is unchanged")
    require("VALIDATION.md", f"# LocalGPT {VERSION} source validation")
    require("CHANGELOG-v4.7.3-ASCII-POPUP-INTERACTION-REPAIR.md", f"# LocalGPT {VERSION}")
    require("VALIDATION-v4.7.3-source.md", f"# LocalGPT {VERSION} source validation")
    require("src/LocalGPT/Components/App.razor", "localgpt-game-console.js?v=4.7.3", "localgpt-chat-ui.js?v=4.7.3")
    require("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.7.3")
    require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.7.3")
    require("docs/index.md", "**Version 4.7.3**", "LocalGPT-4.7.3.pdf")
    require("docs/docfx.json", '"localgptVersion": "4.7.3"')
    require("docs/pdf/toc.yml", "LocalGPT-4.7.3.pdf")
    require("docs/pdf-cover.html", "LocalGPT 4.7.3 Documentation", "Version 4.7.3")
    json.loads(read("docs/docfx.json"))

    # Council false-missing-model repair.
    identity = "src/LocalGPT/BusinessObjects/ProviderModelModels.cs"
    require(
        identity,
        "AreEquivalentSelectionKeys",
        "Legacy bare model names are accepted only as candidate matches",
        "IsOllamaOpenAiCompatibilityFacade",
        "ModelNamesEquivalent(leftModel, rightModel)",
    )
    selection = "src/LocalGPT/Services/MultiModelCouncilService.WorkflowSelection.cs"
    require(
        selection,
        "ResolveConfiguredRoleParticipantKeys",
        "identity.AreEquivalentSelectionKeys(configuredModelKey, participant)",
        "no equivalent provider/model identity is active in this run",
    )
    health = "src/LocalGPT/Services/MultiModelCouncilService.LiveInputAndHealth.cs"
    require(
        health,
        "ApplyApprovedOneRunModelExclusionsAsync(",
        "OrganicCouncilTeamDefinition team",
        "GetConfiguredTeamModelBindings(team)",
        "approved one-run health exclusion consumed",
        "instead of invalidating its role assignment",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs",
        "ApplyApprovedOneRunModelExclusionsAsync(selectedParticipants, organicTeam, cancellationToken)",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs",
        "identity.AreEquivalentSelectionKeys(definition.AssignedModelName, model)",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.RoleSynthesis.cs",
        "identity.AreEquivalentSelectionKeys(definition.RoleResultSynthesisModelName, model)",
    )

    # Tournament speed and bounded context.
    require("src/LocalGPT/BusinessObjects/CouncilGameModels.cs", "TournamentCurrentFighterIds", "TrainerGreeting")
    require("src/LocalGPT/Services/CouncilGameSessionService.Snapshots.cs", "TournamentCurrentFighterIds")
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs",
        "LimitKernelTournamentRoundParticipantsAsync",
        "game.TournamentCurrentFighterIds.Count != 2",
        "current legal-match member(s)",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs",
        "LimitKernelTournamentRoundParticipantsAsync(",
    )
    prompting = read("src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs")
    if "latest bounded battle context plus the opening identities" not in prompting or ".TakeLast(8)" not in prompting:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: tournament battle context is not bounded")

    seed = read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs")
    trainer = block(seed, 'Key = "trainer-round-command"', 'Key = "fight-round"')
    creature = block(seed, 'Key = "fight-round"', 'Key = "arena-round-engine"')
    for name, content in (("trainer", trainer), ("creature", creature)):
        if 'ExecutionMode = "AllMembersParallel"' not in content:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {name} battle round is not benchmark-bounded parallel")
    if "HUD: <one new compact printable-ASCII arena HUD motif" not in trainer:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: trainer HUD motif output missing")
    if "EFFECT: <one new compact printable-ASCII motion/effect motif" not in creature:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: creature effect motif output missing")

    # Arena presentation and reusable ASCII/Pixel state stream.
    require(
        "src/LocalGPT/Services/CouncilGameWorkflowBootstrapService.cs",
        "FrameWidth = highResolutionTournament ? 168 : 80",
        "FrameHeight = highResolutionTournament ? 44 : 25",
    )
    arena = "src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs"
    require(
        arena,
        "Math.Min(180, Math.Max(112, session.FrameWidth))",
        "Math.Min(52, Math.Max(34, session.FrameHeight))",
        "ROUND {session.TournamentRound} // TRAINER GREETING",
        "ROUND {session.TournamentRound} // MATCH GREETING",
        "CREATURE GREETING // READY STANCE",
        "CREATURE SALUTE",
        "ATTACK ANIMATION // LEFT BURST",
        "ATTACK ANIMATION // RIGHT BURST",
        "FINISHER // CHARGE",
        "FINISHER // VICTORY SIGNAL",
        'ReadTaggedValue(leftTrainer, "HUD")',
        'ReadTaggedValue(leftCreature, "EFFECT")',
        "Compact(creature.Name, 18)",
    )
    require(
        "src/LocalGPT/Components/Shared/ChatGameConsole.razor",
        'new("Pixel", "Pixel screen")',
        "data-game-pixel-screen",
    )
    require(
        "src/LocalGPT/wwwroot/js/localgpt-game-console.js",
        "renderPixelFrame",
        "state.currentFrameText",
    )

    # Responsive game modal and dropdown portal.
    require(
        "src/LocalGPT/Components/Pages/Chat.razor",
        "@rendermode InteractiveServer",
        'Width="min(1920px, 98vw)"',
        'Height="96dvh"',
        'MaxHeight="98dvh"',
    )
    chat_css = read("src/LocalGPT/Components/Pages/Chat.razor.css")
    if ".chat-game-popup-body" not in chat_css or "height: 100%;" not in chat_css or "max-height: none;" not in chat_css:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: responsive game popup sizing missing")
    require(
        "src/LocalGPT/Components/Shared/ChatGameConsole.razor.css",
        ".chat-game-console-actions ::deep .ascii-console-button",
        "min-width: 7.5rem",
    )
    require(
        "src/LocalGPT/wwwroot/css/site.css",
        "dxbl-popup-root > dxbl-popup-cell:has(> dxbl-dropdown)",
        "pointer-events: none !important",
        "dxbl-popup-root > dxbl-popup-cell:has(> dxbl-dropdown:not(.dxbl-popup-hidden))",
        "z-index: 22040 !important",
    )
    require(
        "src/LocalGPT/wwwroot/js/localgpt-game-console.js",
        "resolveScaleSurface",
        "setFittedFontSize",
        "scrollback ? 168 : Number.POSITIVE_INFINITY",
        "state.resizeObserver.observe(element);",
    )
    forbid(
        "src/LocalGPT/wwwroot/js/localgpt-game-console.js",
        "state.resizeObserver.observe(element.parentElement)",
        "state.followTailRootObserver = new MutationObserver",
    )
    require(
        "src/LocalGPT/Services/AsciiChatTextService.cs",
        "participantProjectionCache",
        "LiveParticipantTraceCharacters = 8192",
        "BoundParticipantSource",
        "canonical Chat/Council history",
    )

    # Interactive Server directives are a protected architectural surface.
    render_files = []
    for path in (ROOT / "src/LocalGPT/Components").rglob("*.razor"):
        if re.search(r"(?m)^@rendermode\s+InteractiveServer\s*$", path.read_text(encoding="utf-8")):
            render_files.append(path)
    if len(render_files) < 15:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: InteractiveServer coverage dropped to {len(render_files)} component(s)")

    # Keep the previous release's major safety/feature anchors present.
    require("src/LocalGPT/Services/WorkspaceVisionOcrService.cs", "ResolveWorkspacePath", "deepseek-ocr", "var baseAddress = new Uri(normalizedHost, UriKind.Absolute)")
    require("src/LocalGPT/wwwroot/js/localgpt-context-menu.js", "localgpt.controllerMode", "controllerContextAction")
    require("src/LocalGPT/Services/ProjectRepositoryLearningService.cs", "NeedsUserReview", "ParentRevisionId")
    require("src/LocalGPT/Services/UploadFileProcessingCapabilityService.cs", "x-publisher-format-families", "publisher.media.capabilities")

    print(f"LocalGPT {VERSION} release audit passed: Council binding/startup and tournament features remain wired while the ASCII popup dropdown shield, scroll/scale feedback paths, and large-Council terminal projection regressions are repaired without dropping protected prior surfaces.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
