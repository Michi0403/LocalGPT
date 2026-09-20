#!/usr/bin/env python3
from pathlib import Path
import hashlib
import sys

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.6.0"

def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing {rel}")
    return path.read_text(encoding="utf-8", errors="strict")

def req(rel: str, marker: str) -> None:
    if marker not in read(rel):
        raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} missing {marker!r}")

def forbid(rel: str, marker: str) -> None:
    if marker in read(rel):
        raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} still contains forbidden {marker!r}")

def render_modes():
    values=[]
    for path in (ROOT / "src/LocalGPT/Components").rglob("*.razor"):
        for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
            if "@rendermode" in line:
                values.append((str(path.relative_to(ROOT)), line.strip()))
    return sorted(values)

def main() -> int:
    parts=[int(v) for v in VERSION.split(".")]
    if len(parts)!=3 or parts[1] >= 10 or parts[2] >= 10:
        raise SystemExit("version-slot policy failed")
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ]:
        req(rel, f"<Version>{VERSION}</Version>")
    req("RELEASE.md", f"# LocalGPT {VERSION}")
    req("VALIDATION.md", f"# LocalGPT {VERSION} source validation")
    req("CHANGELOG-v4.6.0-UI-EDITOR-COMPILE-REPAIR.md", f"# LocalGPT {VERSION}")
    req("VALIDATION-v4.6.0-source.md", f"# LocalGPT {VERSION} source validation")
    for marker in ["localgpt-game-console.js?v=4.6.0", "localgpt-chat-ui.js?v=4.6.0"]:
        req("src/LocalGPT/Components/App.razor", marker)
    req("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.6.0")
    req("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.6.0")
    req("docs/index.md", "**Version 4.6.0**")
    req("docs/docfx.json", '"localgptVersion": "4.6.0"')

    # DevExpress-first provider controls and the two portal modes seen in the supplied runtime DOM/video.
    chat="src/LocalGPT/Components/Pages/Chat.razor"
    req(chat, '<DxFormLayout CssClass="chat-provider-row">')
    req(chat, '<DxFormLayoutItem Caption="AI provider" ColSpanMd="5">')
    req(chat, '<DxFormLayoutItem ColSpanMd="3">')
    if read(chat).count('<DxFormLayoutItem ColSpanMd="2">') < 2:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: provider form did not retain the two compact medium columns")
    forbid(chat, '<DxFormLayoutItem Caption="AI provider" ColSpanMd="12">')
    req(chat, 'Data="@ProviderSessionOptions"')
    req(chat, 'ValueChanged="@((string selectedName) => OnProviderSelectionChanged(selectedName))"')
    req(chat, '<DxCheckBox @bind-Checked="ReuseContextWhenSwitching"')
    req("src/LocalGPT/Components/Shared/BoundedNumberEditor.razor", "<DxSpinEdit")
    css="src/LocalGPT/wwwroot/css/site.css"
    req(css, "dxbl-adaptive-dropdown:not(.dxbl-display-none)")
    req(css, 'dxbl-popup-cell:has(dxbl-dropdown-dialog[role="listbox"])')
    req(css, "z-index: 22040 !important")
    for rel in ["src/LocalGPT/Components", "src/LocalGPT/Services"]:
        for path in (ROOT / rel).rglob("*"):
            if path.is_file() and path.suffix in {".razor", ".cs"}:
                text=path.read_text(encoding="utf-8", errors="ignore")
                if "DxRangeSelector" in text or "RangeSelectorValueChangedEventArgs" in text:
                    raise SystemExit(f"LocalGPT {VERSION} audit failed: obsolete range selector in {path.relative_to(ROOT)}")

    # Freshness workflow: report is automatic-safe, exact external refresh is separately human gated.
    req("src/LocalGPT/Services/KnowledgeFreshnessDxAiFunctions.cs", '"localgpt.knowledge.freshness.report"')
    req("src/LocalGPT/Services/KnowledgeFreshnessDxAiFunctions.cs", '"localgpt.knowledge.source.refresh"')
    req("src/LocalGPT/Services/KnowledgeFreshnessDxAiFunctions.cs", "RequiresHumanConfirmation: true")
    req("src/LocalGPT/Services/KnowledgeFreshnessReviewService.cs", "Keep current knowledge\\nReview exact source first\\nRefresh exact source\\nReject as outdated")
    req("src/LocalGPT/Services/KnowledgeFreshnessReviewService.cs", "IsClearlyLocalSource")
    req("src/LocalGPT/Services/HumanCollaborationService.cs", 'request.OperationKey.StartsWith("knowledge.freshness."')
    req("src/LocalGPT/Services/CouncilKnowledgeService.cs", ".Append(\" [knowledgeId \")")
    seed="src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs"
    req(seed, "localgpt.knowledge.freshness.report")
    req(seed, "exact external source refresh remains human-approved")

    # Tournament roster/switch/rest/trainer action/timeline and stable joint-rig contracts.
    models="src/LocalGPT/BusinessObjects/CouncilGameModels.cs"
    for marker in ["CouncilAsciiActorRig", "CouncilKernelTournamentCreatureState", "CouncilKernelTournamentTimelineEntry", "CreaturesPerTrainer", "MaximumCreatureSwitchesPerFight", "RestRecoveryPerExchange", "TrainerFocusBonus", "TrainerBraceReduction"]:
        req(models, marker)
    tournament="src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs"
    for marker in ["BuildTrainerRig", "BuildCreatureRig", "RenderRig", "SwitchCreature", "RestBenchedCreatures", "TRAINER_ACTION", '"SWITCH"', '"RECOVERY / BENCH"', "TournamentTimeline"]:
        req(tournament, marker)
    rules="src/LocalGPT/Services/CouncilGameSessionService.KernelTournamentRules.cs"
    for marker in ["creaturesPerTrainer", "maximumCreatureSwitchesPerFight", "restRecoveryPerExchange", "trainerFocusBonus", "trainerBraceReduction"]:
        req(rules, marker)
    req(seed, "TRAINER_ACTION: FOCUS|BRACE|REST|SWITCH|NONE")
    req(seed, "CouncilMemberFailureRecoveryMode.RetrySameThenEligibleRolePool")
    req("src/LocalGPT/Services/CouncilRuntimeClassService.cs", "private const int CurrentSeedVersion = 8;")
    req("src/LocalGPT/Services/CouncilTeamConfigurationService.cs", "private const int CurrentSeedVersion = 36;")

    # Bounded browser rejoin retries while server work remains server-owned.
    live="src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs"
    req(live, "var retryDelays = new[] { 250, 700 };")
    req(live, "CouncilLiveSessions.GetSummary(selectedRunId)?.IsRunning != true")
    req(live, "after bounded retries")


    # 4.6.0 compiler/UI corrective guards from the supplied Windows build/video.
    req(live, "await InvokeAsync(() =>")
    req(live, "if (stillRunning)")
    forbid(live, "await InvokeAsync(() => stillRunning")
    req(chat, 'BoundedNumberEditor Label="Hardware load"')
    req(chat, 'ValueChanged="OnCouncilResourceLoadChanged"')
    forbid(chat, 'ValueChanged="@((int value) => OnCouncilResourceLoadChanged(new ChangeEventArgs')
    req("src/LocalGPT/Components/Pages/Chat.CouncilRunControlsAndBenchmarks.razor.cs", "private Task OnCouncilResourceLoadChanged(int value)")
    benchmark = "src/LocalGPT/Components/Shared/ProviderModelBenchmarkCouncilPanel.razor"
    forbid(benchmark, '<DxSpinEdit @bind-Value="maxProfiles" CssClass="form-control"')
    forbid(benchmark, '<DxSpinEdit @bind-Value="maxCouncilReviewers" CssClass="form-control"')
    forbid("src/LocalGPT/Components/Shared/ProviderModelPanel.razor", '<DxSpinEdit @bind-Value="maxProfiles" CssClass="form-control"')
    req(chat, 'class="council-direct-starter-content"')
    req("src/LocalGPT/Components/Pages/Chat.razor.css", ".council-direct-starter-content")
    req(css, "grid-template-columns: minmax(240px, 1fr) minmax(240px, 1fr) auto auto;")

    # Existing Compiler/Publisher teams and role recovery are retained.
    teams="src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BuildOrchestrationTemplates.cs"
    for marker in ['Key = "program-compiler-team"', 'Key = "project-release-orchestration"', "RetrySameThenEligibleRolePool"]:
        req(teams, marker)

    expected_direct={
        "src/LocalGPT/Components/Layout/NavMenu.razor","src/LocalGPT/Components/Pages/Index.razor","src/LocalGPT/Components/Pages/ModelCouncil.razor","src/LocalGPT/Components/Pages/Install.razor","src/LocalGPT/Components/Pages/Database.razor","src/LocalGPT/Components/Pages/OneWireSecurity.razor","src/LocalGPT/Components/Pages/Help.razor","src/LocalGPT/Components/Pages/TestLab.razor","src/LocalGPT/Components/Pages/Chat.razor","src/LocalGPT/Components/Pages/CouncilTeams.razor","src/LocalGPT/Components/Pages/Projects.razor","src/LocalGPT/Components/Pages/RemoteControl.razor","src/LocalGPT/Components/Pages/DxFunctionCatalog.razor","src/LocalGPT/Components/Pages/ProjectMaintenance.razor","src/LocalGPT/Components/Pages/MinecraftModBuilder.razor",
    }
    expected_no={
        "src/LocalGPT/Components/InteractiveStartupMarker.razor","src/LocalGPT/Components/Layout/MenuIsland.razor","src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor","src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor","src/LocalGPT/Components/Layout/ToastWrapper.razor",
    }
    actual=render_modes(); direct={p for p,m in actual if m=="@rendermode InteractiveServer"}; no={p for p,m in actual if m=="@rendermode @(new InteractiveServerRenderMode(prerender: false))"}
    unexpected=[e for e in actual if e[0] not in expected_direct | expected_no]
    if direct != expected_direct or no != expected_no or unexpected or len(actual)!=20:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: render mode topology changed direct={len(direct)} no={len(no)} total={len(actual)} unexpected={unexpected[:2]}")

    # Browser diagnostics manifest remains byte-consistent.
    checked=0
    for line in read("build/javascript-diagnostics-files.sha256").splitlines():
        line=line.strip()
        if not line or line.startswith("#"): continue
        digest, rel=line.split(maxsplit=1)
        path=ROOT/rel
        normalized=path.read_text(encoding="utf-8").replace("\r\n","\n").replace("\r","\n")
        if hashlib.sha256(normalized.encode()).hexdigest()!=digest:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: stale JS hash {rel}")
        checked += 1
    if checked < 20: raise SystemExit("unexpectedly small JS diagnostics manifest")
    print(f"LocalGPT {VERSION} release audit passed: completed 4.6.0 compile/UI repair plus preserved freshness/rejoin/tournament/dropdown work, render topology and {checked} JS hashes are consistent.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
